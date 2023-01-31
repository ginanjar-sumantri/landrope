using MailSender;

namespace landrope.scheduler.Interfaces
{
    public interface IMailService
    {
        Task SendMail(string jobName);
    }
}
