using landrope.scheduler.Helpers;
using landrope.scheduler.Interfaces.Common;
using landrope.scheduler.Enums;
using MailSender;
using Newtonsoft.Json;
using Quartz;
using landrope.scheduler.Interfaces;

namespace landrope.scheduler.Jobs
{
    public class ReminderBayarPendingAccountingJob : IJob
    {
        readonly IMailService _mailService;
        readonly string _mailType;

        public ReminderBayarPendingAccountingJob(IMailService mailService)
        {
            _mailService = mailService;
            _mailType = nameof(ReminderBayarPendingAccountingJob);
        }

        public async Task Execute(IJobExecutionContext context)
            => await _mailService.SendMail(_mailType);
    }
}
