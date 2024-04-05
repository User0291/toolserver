
/// <summary>
/// 版本異常類
/// </summary>
public class VersionException : Exception
{
    public VersionException(string message) : base(message) { }
}
/// <summary>
/// 許可證異常類
/// </summary>
public class LicenseException : Exception
{
    public LicenseException(string message) : base(message) { }
}
/// <summary>
/// 許可證無效異常
/// </summary>
public class InvalidLicenseException : LicenseException
{
    public InvalidLicenseException(string message) : base(message) { }
}
/// <summary>
/// 許可證過期異常
/// </summary>
public class ExpiredLicenseException : LicenseException
{
    public ExpiredLicenseException(string message) : base(message) { }
}
/// <summary>
/// 許可證時間不足異常
/// </summary>
public class InsufficientTimeException : LicenseException
{
    public InsufficientTimeException(string message) : base(message) { }
}


