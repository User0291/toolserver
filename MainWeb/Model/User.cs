namespace MainWeb
{
    public class User
    {
        public int Id { get; set; }
        public string Key16 { get; set; }
        public string DeviceId { get; set; }
        public bool IsUsed { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
