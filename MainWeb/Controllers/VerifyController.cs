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
                bool isValid = await _licenseService.ValidateLicenseAsync(licenseKey, deviceId);

                if (isValid)
                {
                    return Ok(new { IsValid = true, Message = "License is valid." });
                }
                else
                {
                    return BadRequest(new { IsValid = false, Message = "License is invalid." });
                }
            }
            catch (Exception ex)
            {
                // 记录异常并返回错误响应
                // ILogger 或者其他日志库可以用于记录异常信息
                return StatusCode(500, new { IsValid = false, Message = "An error occurred while validating license." });
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
