using landrope.scheduler.Enums;
using landrope.scheduler.Helpers;
using landrope.scheduler.Interfaces;
using landrope.scheduler.Interfaces.Common;
using MailSender;
using Newtonsoft.Json;

namespace landrope.scheduler.Services
{
    public class MailService : IMailService
    {
        readonly IHttpClientService _client;
        MailSetting _mailSetting =  new();

        public MailService(IHttpClientService client)
        {
            _client = client;
        }

        public async Task SendMail(string jobName)
        {
            SetMailSetting(jobName);

            Uri uri1 = AppSetting.Server1.SetPort(_mailSetting.ApiService.GetPort());
            var res1 = await _client.PostAsync($"{uri1}{_mailSetting.Endpoint}");
            await Task.Delay(2000);

            HttpResponseMessage res2 = new();
            if (_mailSetting.Multiple)
            {
                Uri uri2 = AppSetting.Server2.SetPort(_mailSetting.ApiService.GetPort());
                res2 = await _client.PostAsync($"{uri2}{_mailSetting.Endpoint}");
                await Task.Delay(2000);
            }

            if (!res1.IsSuccessStatusCode || (_mailSetting.Multiple && !res2.IsSuccessStatusCode))
                throw new Exception(await res1.Content.ReadAsStringAsync());
        }

        void SetMailSetting(string jobName)
        {
            _mailSetting = AppSetting.MailSettings.FirstOrDefault(x => x.Name == jobName) ?? new();
            
            if (_mailSetting == null)
                throw new ArgumentNullException($"{jobName} : MailSetting not found.");
        }
    }
}
