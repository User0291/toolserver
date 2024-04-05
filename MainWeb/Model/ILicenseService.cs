namespace MainWeb
{
    public interface ILicenseService
    {
        Task<VerifyRespond> ValidateLicenseAsync(string licenseKey, string deviceId);
    }
}
