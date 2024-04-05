using Microsoft.AspNetCore.SignalR;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Tls;

namespace MainWeb
{
    public class MyHub : Hub
    {
        private readonly Dictionary<string, string> _userConnectionMap = new Dictionary<string, string>();
        private readonly DatabaseService _databaseService;
        public MyHub(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        // 当客户端连接时调用此方法
        public override async Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;

            // 保存连接 ID 和用户标识符之间的映射关系
            string userId = GetUserIdFromContext(Context);
            _userConnectionMap[userId] = connectionId;

            await base.OnConnectedAsync();
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
            User user = await _databaseService.GetUserByLicenseKeyAsync(license);
            //先判断卡密是否存在
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "许可证不存在");
                return;
            }
            //判断卡密是否过期
            if (DateTime.UtcNow > user.ExpiryDate)
            {
                await Clients.Caller.SendAsync("Error", "许可证已过期");
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
                await Clients.Caller.SendAsync("LoginSuccess", "登录成功");
            }
            //被使用過则进行设备号判断 是否一致
            else if (user.DeviceId != deviceId)
            {
                // 设备号不匹配，進行判斷剩餘時間是否允許解綁
                if (user.ExpiryDate.AddHours(-3) < DateTime.UtcNow)
                {
                    // 卡密剩余时间不足3小时，不允许登录
                    await Clients.Caller.SendAsync("Error", "许可证时间不足");
                }
                else
                {
                    // 更新卡密的到期时间，减少3小时
                    //user.ExpiryDate = user.ExpiryDate;
                    user.DeviceId = deviceId;
                    user.ExpiryDate=  user.ExpiryDate.AddHours(-3);
                    await _databaseService.UpdateUserAsync(user);
                    //允許登錄
                    // 登录成功逻辑
                    await Clients.Caller.SendAsync("LoginSuccess", "登录成功");
                }
            }
            else
            {
                // 登录成功逻辑
                await Clients.Caller.SendAsync("LoginSuccess", "登录成功");
            }
        }
    }
}
