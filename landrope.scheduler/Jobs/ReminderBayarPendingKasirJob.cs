﻿using landrope.scheduler.Helpers;
using landrope.scheduler.Interfaces.Common;
using landrope.scheduler.Enums;
using MailSender;
using Newtonsoft.Json;
using Quartz;
using landrope.scheduler.Interfaces;

namespace landrope.scheduler.Jobs
{
    public class ReminderBayarPendingKasirJob : IJob
    {
        readonly IMailService _mailService;
        readonly string _mailType;

        public ReminderBayarPendingKasirJob(IMailService mailService)
        {
            _mailService = mailService;
            _mailType = nameof(ReminderBayarPendingKasirJob);
        }

        public async Task Execute(IJobExecutionContext context)
            => await _mailService.SendMail(_mailType);
    }
}
