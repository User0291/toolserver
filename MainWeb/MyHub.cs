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
        private readonly string directoryPath = "/www/toolserverscr/";
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

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLogToFile("/www/toolserverlog.txt", logMessage);
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
                    await Clients.Caller.SendAsync("LoginSuccess", "登錄成功 到期時間:" + user.ExpiryDate);
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
                        await Clients.Caller.SendAsync("LoginSuccess", "登錄成功 到期時間:" + user.ExpiryDate);
                    }
                }
                else
                {
                    // 登录成功逻辑
                    await Clients.Caller.SendAsync("LoginSuccess", "登錄成功 到期時間:" + user.ExpiryDate);
                }
            }
            catch (Exception ex)
            {
                string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLogToFile("/www/toolserverlog.txt", logMessage);
            }

        }

        public async Task GetNotice()
        {
            try
            {

                await Clients.All.SendAsync("ReceiveNotice", Notice);
            }
            catch (Exception ex)
            {
                string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLogToFile("/www/toolserverlog.txt", logMessage);
                await Clients.Caller.SendAsync("Error", "未知錯誤！" + ex.Message);
            }
        }
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
                    var tasks = fileNames.Select(async fileName =>
                    {
                        await _semaphore.WaitAsync(); // 等待信号量，限制并发请求数
                        try
                        {
                            // 根据订阅和设备ID生成密钥
                            //byte[] key = GenerateKeyFromSubscriptionAndDeviceID(license, deviceId);
                            byte[] fileContent = await GetFileContent(Path.Combine(directoryPath,fileName));
                            if (fileContent != null)
                            {
                                // 使用对称加密算法加密文件
                                //byte[] encryptedFile = EncryptFile(fileContent, key);
                                string base64Content = Convert.ToBase64String(fileContent);
                                await Clients.Caller.SendAsync("ReceiveFiles", fileName, base64Content);
                            }
                            else
                            {
                                await Clients.Caller.SendAsync("Error", "未知錯誤！");
                            }
                        }
                        catch (Exception ex)
                        {
                            string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                            WriteLogToFile("/www/toolserverlog.txt", logMessage);
                            await Clients.Caller.SendAsync("Error", "未知錯誤！" + ex.Message);
                        }
                        finally
                        {
                            _semaphore.Release(); // 释放信号量
                        }
                    }).ToList(); // 转换成列表以触发立即执行

                    await Task.WhenAll(tasks); // 等待所有文件请求完成
                }
            }
            catch (Exception ex)
            {

                string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLogToFile("/www/toolserverlog.txt", logMessage);
                await Clients.Caller.SendAsync("Error", "未知錯誤！" + ex.Message);
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
                return await Task.Run(() => File.ReadAllBytes(fileName));
            }
            catch (Exception ex)
            {
                string logMessage = $"An error occurred: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                WriteLogToFile("/www/toolserverlog.txt", logMessage);
                await Clients.Caller.SendAsync("Error", "未知錯誤！");
                return null;
            }
        }
        private void WriteLogToFile(string filePath, string logMessage)
        {
            try
            {
                // 将日志消息写入文件
                File.AppendAllText(filePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // 如果写入文件时发生异常，则记录异常并输出到控制台
                Console.WriteLine("An error occurred while writing log to file: " + ex.Message);
            }
        }

    }
}
