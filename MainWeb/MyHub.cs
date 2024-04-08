using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Tls;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MainWeb
{
    public class MyHub : Hub
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(10); // 限制并发请求数为 10
        private readonly Dictionary<string, string> _userConnectionMap = new Dictionary<string, string>();
        private readonly DatabaseService _databaseService;
        //private readonly string directoryPath = "/www/toolserverscr/";
        private readonly List<string> fileNames = ["content-metadata", "content-document"];
        private readonly string Notice = "暫時沒有新通知";
        private readonly ILogger<MyHub> _logger;
        public MyHub(ILogger<MyHub> logger, DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        // 当客户端连接时调用此方法
        public override async Task OnConnectedAsync()
        {
            try
            {
                string connectionId = Context.ConnectionId;
                // 保存连接 ID 和用户标识符之间的映射关系
                string userId = GetUserIdFromContext(Context);
                _userConnectionMap[userId] = connectionId;
                // 获取客户端的 IP 地址
                var clientIpAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
                // 将 IP 地址写入文件
                // 将连接 ID 和 IP 地址写入日志文件
                string logMessage = $"Client connected. Connection ID: {connectionId}, IP Address: {clientIpAddress ?? "Unknown"}";
                WriteLog(logMessage);
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLog(logMessage);
            }

        }
        
        private string GetUserIdFromContext(HubCallerContext context)
        {
            // 从 Context 中获取用户标识符，您可以根据您的身份验证方案来实现此方法
            // 这里仅作为示例，您可能需要替换为实际的用户标识符获取逻辑
            return context.User?.Identity?.Name ?? "unknown";
        }
        // 当客户端断开连接时调用此方法
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // 获取断开连接的连接 ID
            string connectionId = Context.ConnectionId;

            // 在连接映射中查找并删除对应的条目
            string userId = _userConnectionMap.FirstOrDefault(x => x.Value == connectionId).Key;
            if (userId != null)
            {
                _userConnectionMap.Remove(userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        //校驗客戶端版本號
        public async Task CheckClinetVersion(string version)
        {
            string result = await _databaseService.GetVersion();
            //對比版本號實現
            if (!string.Equals(result, version))
            {
                await Clients.Caller.SendAsync("Error", "版本錯誤");
                return;
            }
            /*// 版本检查成功
            await Clients.Caller.SendAsync("ReceiveVersionCheckResult", "當前程序為最新版本");*/

        }

        // 客户端登录方法
        public async Task Login(string license, string deviceId)
        {
            try
            {
                User user = await _databaseService.GetUserByLicenseKeyAsync(license);
                //先判断卡密是否存在
                if (user == null)
                {
                    await Clients.Caller.SendAsync("Error", "許可證不存在");
                    return;
                }
                //判断卡密是否过期
                if (DateTime.UtcNow > user.ExpiryDate)
                {
                    await Clients.Caller.SendAsync("Error", "許可證已經過有效期限");
                    return;
                }
                //判断卡密是否被使用
                if (!user.IsUsed)
                {   // 更新数据库中卡密状态为已使用，并将设备编号更新为客戶端傳入的设备编号
                    user.IsUsed = true;
                    user.DeviceId = deviceId;
                    await _databaseService.UpdateUserAsync(user);
                    //允許登錄
                    // 登录成功逻辑
                    await Clients.Caller.SendAsync("LoginSuccess", "登錄成功 到期時間:" + Utils.FormatDateTime(user.ExpiryDate));
                }
                //被使用過则进行设备号判断 是否一致
                else if (user.DeviceId != deviceId)
                {
                    // 设备号不匹配，進行判斷剩餘時間是否允許解綁
                    if (user.ExpiryDate.AddHours(-3) < DateTime.UtcNow)
                    {
                        // 卡密剩余时间不足3小时，不允许登录
                        await Clients.Caller.SendAsync("Error", "許可證時間不足解綁");
                    }
                    else
                    {
                        // 更新卡密的到期时间，减少3小时
                        //user.ExpiryDate = user.ExpiryDate;
                        user.DeviceId = deviceId;
                        user.ExpiryDate = user.ExpiryDate.AddHours(-3);
                        await _databaseService.UpdateUserAsync(user);
                        //允許登錄
                        // 登录成功逻辑
                        await Clients.Caller.SendAsync("LoginSuccess", "登錄成功 到期時間:" + Utils.FormatDateTime(user.ExpiryDate));
                    }
                }
                else
                {
                    // 登录成功逻辑
                    await Clients.Caller.SendAsync("LoginSuccess", "登錄成功 到期時間:" + Utils.FormatDateTime(user.ExpiryDate));
                }
            }
            catch (Exception ex)
            {
                string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLog(logMessage);
            }

        }
        /// <summary>
        /// 公告实现
        /// </summary>
        /// <returns></returns>
        public async Task GetNotice()
        {
            try
            {

                await Clients.All.SendAsync("ReceiveNotice", Notice);
            }
            catch (Exception ex)
            {
                string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLog(logMessage);
                await Clients.Caller.SendAsync("Error", "未知錯誤！");
            }
        }
        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="license">订阅</param>
        /// <param name="deviceId">设备编号</param>
        /// <returns></returns>
        public async Task RequestFiles(string license, string deviceId)
        {
            User user = await _databaseService.GetUserByLicenseKeyAsync(license);
            try
            {
                if (user == null)
                {
                    // 越權請求
                    await Clients.Caller.SendAsync("Unauthorized", "未知錯誤！");
                }
                else if (user.DeviceId != deviceId && DateTime.Now > user.ExpiryDate)
                {
                    // 越權請求
                    await Clients.Caller.SendAsync("Unauthorized", "未知錯誤！");
                }
                else
                {
                    foreach (string fileName in fileNames)
                    {
                        await SendFileAsync(fileName, license, deviceId);
                    }
                }
            }
            catch (Exception ex)
            {

                string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLog(logMessage);
                await Clients.Caller.SendAsync("Error", "未知錯誤！");
            }
        }

        private async Task SendFileAsync(string fileName, string license, string deviceId)
        {
            await _semaphore.WaitAsync();
            try
            {
                byte[] key = GenerateKeyFromSubscriptionAndDeviceID(license, deviceId);
                byte[] fileContent = await GetFileContent(fileName);
                if (fileContent != null)
                {
                    await SendFileChunksAsync(fileName, fileContent, key);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "未知錯誤！");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SendFileChunksAsync(string fileName, byte[] fileContent, byte[] key)
        {
            const int chunkSize = 1024 * 1024; // 1MB
            int totalChunks = (int)Math.Ceiling((double)fileContent.Length / chunkSize);

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * chunkSize;
                int length = Math.Min(chunkSize, fileContent.Length - offset);
                byte[] chunk = new byte[length];
                Array.Copy(fileContent, offset, chunk, 0, length);

                // 使用对称加密算法加密文件块
                byte[] encryptedChunk = EncryptFile(chunk, key);
                string base64Content = Convert.ToBase64String(encryptedChunk);
                WriteLog("发送文件的：" + fileName+"   块数:"+ i);
                // 发送文件块给客户端
                await Clients.Caller.SendAsync("ReceiveFileChunk", fileName, i, totalChunks, base64Content);
            }
        }

        /// <summary>
        /// 得到文件内容
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private async Task<byte[]> GetFileContent(string fileName)
        {
            try
            {
                // 使用异步方式打开文件并读取内容
                using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
                {
                    byte[] buffer = new byte[stream.Length];
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                    WriteLog("得到的文件的文件："+fileName);
                    return buffer;
                }
            }
            catch (Exception ex)
            {
                string logMessage = $"An error occurred while reading file: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLog(logMessage);
                await Clients.Caller.SendAsync("Error", "未知錯誤！");
                return null;
            }
        }
        private void WriteLog(string logMessage)
        {
            string filePath = "toolserverlog.txt";
            try
            {
                File.AppendAllText(filePath, $"{DateTime.Now} - {logMessage}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while writing to file: {ex.Message}");
            }
        }
        private byte[] GenerateKeyFromSubscriptionAndDeviceID(string license, string deviceId)
        {
            string combinedString = license + deviceId;

            // 使用 SHA256 计算哈希值
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));
                return hashBytes;
            }
        }
        private byte[] EncryptFile(byte[] fileBytes, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(fileBytes, 0, fileBytes.Length);
                    }

                    return ms.ToArray();
                }
            }
        }

    }
}
