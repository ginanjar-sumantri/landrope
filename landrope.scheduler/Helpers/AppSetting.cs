using landrope.scheduler.Enums;

namespace landrope.scheduler.Helpers
{
    public static class AppSetting
    {
        public static Uri Server1 { get; set; }
        public static Uri Server2 { get; set; }
        public static IEnumerable<MailSetting> MailSettings { get; set; }
    }
        
    public class MailSetting
    {
        public string Name { get; set; }
        public ApiService ApiService { get; set; }
        public string Endpoint { get; set; }
        public bool Multiple { get; set; }
    }
}
