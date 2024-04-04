namespace MainWeb
{
    public interface ILicenseService
    {
        Task<bool> ValidateLicenseAsync(string licenseKey, string deviceId);
    }
}
