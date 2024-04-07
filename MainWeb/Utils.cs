namespace MainWeb
{
    public static class Utils
    {
        public static string FormatDateTime(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
