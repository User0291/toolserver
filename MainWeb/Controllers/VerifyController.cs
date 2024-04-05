using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MainWeb
{

    [ApiController]
    [Route("api")]
    public class VerifyController : ControllerBase
    {
        private readonly ILicenseService _licenseService;

        public VerifyController(ILicenseService licenseService)
        {
            _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        }

        [HttpGet("login")]
        public async Task<IActionResult> ValidateLicense([FromQuery] string licenseKey, [FromQuery] string deviceId)
        {
            try
            {
                VerifyRespond VerifyRespond = await _licenseService.ValidateLicenseAsync(licenseKey, deviceId);

                if (VerifyRespond.isValid)
                {
                    return Ok(new { IsValid = true, Message = "許可證有效期至:"+ VerifyRespond.expiryTime});
                }
                else
                {
                    return BadRequest(new { IsValid = false, Message = "拒絕登入" });
                }
            }
            catch (Exception ex)
            {
                // 记录异常并返回错误响应
                // ILogger 或者其他日志库可以用于记录异常信息
                return StatusCode(500, new { IsValid = false, Message = "未知錯誤" });
            }
        }
    }
    public class ValidationRequest
    {
        /// <summary>
        /// 密钥
        /// </summary>
        public string LicenseKey { get; set; }
        /// <summary>
        /// 设备编号
        /// </summary>
        public string DeviceId { get; set; }
    }
}
