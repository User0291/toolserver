
namespace MainWeb.Service
{
    public class LicenseService : ILicenseService
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseService _databaseService;

        public LicenseService(IConfiguration configuration, DatabaseService databaseService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        public async Task<bool> ValidateLicenseAsync(string licenseKey, string deviceId)
        {
            User user = await _databaseService.GetUserByLicenseKeyAsync(licenseKey);
            //先判断卡密是否存在
            if (user != null)
            {
                //再判断卡密是否被使用
                if (!user.IsUsed)
                {// 更新数据库中卡密状态为已使用，并将设备编号更新为当前设备编号
                    user.IsUsed = true;
                    user.DeviceId = deviceId;
                    await _databaseService.UpdateUserAsync(user);
                    return true;
                }
                //然后判断卡密是否过期
                else if (user.ExpiryDate > DateTime.UtcNow)
                {
                    //未过期则进行设备号判断 是否一致
                    if (user.DeviceId == deviceId)
                    {
                        // 设备号匹配，允许登录
                        return true;
                    }
                    //不一致
                    else
                    {
                        // 设备号不匹配
                        if (user.ExpiryDate > DateTime.UtcNow.AddHours(3))
                        {
                            // 更新卡密的到期时间，减少3小时
                            user.ExpiryDate = user.ExpiryDate.AddHours(-3);
                            await _databaseService.UpdateUserAsync(user);
                            return true;
                        }
                        else
                            // 卡密剩余时间不足3小时，不允许登录
                            return false;
                    }
                }
                else
                    return false;
            }
            else
                // 卡密不存在
                return false;
        }
    }
}
