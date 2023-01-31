using System.Reflection;
using Microsoft.Extensions.Configuration;
using MailSender.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net.Mime;
using MimeKit;
using MimeKit.Text;
using MailKit.Security;
using MailKit.Net.Smtp;
using Org.BouncyCastle.Utilities.Encoders;
using MimeKit.Encodings;

namespace MailSender
{
    public class MailSenderClass
    {
        MailSettings smtpSetting = new MailSettings();
        public MailSenderClass()
        {
            var configuration = new ConfigurationBuilder()
                   .AddJsonFile($"mailsettings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables();
            var config = configuration.Build();
            // read config
            MailSettings emailSetting = config.GetSection("MailSetting").Get<MailSettings>();
            string[] receivers = config.GetSection("Receivers").GetChildren().Select(s => s.Value).ToArray();

            smtpSetting = config.GetSection("MailSettingSMTP").Get<MailSettings>();
        }

        public async Task SendEmailAsync(EmailList email)
        {
            // init
            DateTime now = DateTime.Now;
            HttpClient client = new();
            string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            // check logs directory
            string dirPath = Path.Combine(basePath, "logs");
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            // cteate log file
            string file = Path.Combine(basePath, "logs", "log.txt");
            if (!File.Exists(file))
                File.Create(file).Close();

            if (email.Receivers.Length > 0)
            {
                using var context = new SqlContextMod();
                try
                {
                    context.EmailList.Add(email);
                    await context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    await File.AppendAllTextAsync(file, e.Message + Environment.NewLine);
                }
            }
        }

        public async Task SendEmailFileAsync(EmailListFile email)
        {
            // init
            DateTime now = DateTime.Now;
            HttpClient client = new();
            string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            // check logs directory
            string dirPath = Path.Combine(basePath, "logs");
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            // cteate log file
            string file = Path.Combine(basePath, "logs", "log.txt");
            if (!File.Exists(file))
                File.Create(file).Close();

            if (email.Receivers.Length > 0)
            {
                using var context = new SqlContextMod();
                try
                {
                    context.EmailListFile.Add(email);
                    await context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    await File.AppendAllTextAsync(file, e.Message + Environment.NewLine);
                }
            }
        }

        public async Task SendEmailFileNotFound(string receivers, string path)
        {
            using var context = new SqlContextMod();
            try
            {
                string sql = string.Format(@" EXEC msdb.dbo.sp_send_dbmail              
				                    @profile_name = 'Landrope',              
				                    @recipients = '{0}',              
				                    @body_format = 'HTML',              
				                    @body = 'Gagal mengirimkan email, File attachment tidak ditemukan. Pastikan folder {1} sudah ada pada perangkat anda.',              
				                    @subject = 'Error proses on sending (SEND_MAIL_OM) pada server 10.10.1.80' ; ", receivers, path);

                context.Database.ExecuteSqlRaw(sql);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task SendEmailAyncSMTP(EmailStructure emailStructure)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse("notif.landrope@agungsedayu.com"));
                foreach (var address in emailStructure.Receiver)
                {
                    if (!string.IsNullOrWhiteSpace(address))
                        email.To.Add(MailboxAddress.Parse(address));
                }
                if (emailStructure.CC != null)
                {
                    if (emailStructure.CC.Length > 0)
                    {
                        foreach (var address in emailStructure.CC)
                        {
                            if (!string.IsNullOrWhiteSpace(address))
                                email.Cc.Add(MailboxAddress.Parse(address));
                        }
                    } 
                }
                if (emailStructure.BCC != null)
                {
                    if (emailStructure.BCC.Length > 0)
                    {
                        foreach (var address in emailStructure.BCC)
                        {
                            if (!string.IsNullOrWhiteSpace(address))
                                email.Bcc.Add(MailboxAddress.Parse(address));
                        }
                    }
                }
                email.Subject = emailStructure.Subject;
                var body = new TextPart(TextFormat.Html) { Text = emailStructure.Body };

                var streams = new List<Stream>();
                var multipart = new Multipart("mixed");

                multipart.Add(body);

                if (emailStructure.Attachment != null)
                {
                    for (int i = 0; i < emailStructure.Attachment.Count; i++)
                    {
                        //var stream = File.OpenRead(emailStructure.FilePaths[i]);
                        var bytes = Convert.FromBase64String(emailStructure.Attachment[i].FileBase64);
                        Stream stream = new MemoryStream(bytes);

                        var attachment = new MimePart(emailStructure.ContentType)
                        {
                            Content = new MimeContent(stream),
                            ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Attachment),
                            ContentTransferEncoding = ContentEncoding.Base64,
                            //FileName = Path.GetFileName(emailStructure.FilePaths[i]),
                            FileName = Path.GetFileName(emailStructure.Attachment[i].FileName)
                        };

                        multipart.Add(attachment);
                        streams.Add(stream);
                    }
                }
                email.Body = multipart;

                // send email
                using var smtp = new SmtpClient();
                smtp.Connect(smtpSetting.Host, smtpSetting.Port, SecureSocketOptions.None);
                smtp.Authenticate(smtpSetting.Username, smtpSetting.Password);
                smtp.Send(email);
                smtp.Disconnect(true);

                foreach (var stream in streams)
                    stream.Dispose();
            }
            catch (Exception err)
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse("notif.landrope@agungsedayu.com"));
                email.To.Add(MailboxAddress.Parse(smtpSetting.ErrorPIC));
                email.Subject = "Error proses on sending email";
                email.Body = new TextPart(TextFormat.Html) { Text = "Gagal mengirimkan email. Pesan error: " + err.Message };

                // send email
                using var smtp = new SmtpClient();
                smtp.Connect(smtpSetting.Host, smtpSetting.Port, SecureSocketOptions.None);
                smtp.Authenticate(smtpSetting.Username, smtpSetting.Password);
                smtp.Send(email);
                smtp.Disconnect(true);

                throw err;
            }
        }
    }

    public class EmailStructure
    {
        public string[] Receiver { get; set; }
        public string[]? CC { get; set; }
        public string[]? BCC { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string ContentType { get; set; }
        public List<EmailAttachment>? Attachment { get; set; }
    }

    public class EmailAttachment 
    { 
        public string FileBase64 { get; set; }
        public string FileName { get; set; }
    }

    public static class FileType
    {
        public const string Excel = "application/vnd.ms-excel";
        public const string Zip = "application/zip";

    }
}