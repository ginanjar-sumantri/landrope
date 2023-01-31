namespace landrope.scheduler.Helpers
{
    public static class UriExtension
    {
        public static Uri SetPort(this Uri uri, int port)
        {
            var builder = new UriBuilder(uri);
            builder.Port = port;
            return builder.Uri;
        }
    }
}
