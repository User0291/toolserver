namespace MainWeb
{
    public class VerifyRespond
    {
        public VerifyRespond(bool isValid, DateTime expiryTime)
        {
            this.isValid = isValid;
            this.expiryTime = expiryTime;
        }

        public bool isValid { get; set; }
        public DateTime expiryTime { get; set; }
    }
}
