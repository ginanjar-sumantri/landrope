using System.Reflection;
using Microsoft.Extensions.Configuration;
using HitApiScheduler.Classes;
using HitApiScheduler;
using System.Reflection;

var configuration = new ConfigurationBuilder()
                    .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();

var config = configuration.Build();

// read config
string[] urls = config.GetSection("Urls").GetChildren().Select(s => s.Value).ToArray();
MailSettings emailSetting = config.GetSection("MailSetting").Get<MailSettings>();
string[] receivers = config.GetSection("Receivers").GetChildren().Select(s => s.Value).ToArray();

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

// execute
#region execute
if (urls.Count() == 0)
{
    string write = $"[{String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now)}] [Error] : URL's not set. \n";
    await File.AppendAllTextAsync(file, write);
}
else
{
    List<string> messages = new();
    foreach (string url in urls)
    {
        string write = "";
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            write = $"[{String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now)}] [Success] : {url} ";
        }
        catch (HttpRequestException e)
        {
            write = $"[{String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now)}] [Error] : {e.Message} => {url} ";
            messages.Add(write);
        }
        catch (Exception e)
        {
            write = $"[{String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now)}] [Error] : {e.Message} => {url} ";
            messages.Add(write);
        }

        await File.AppendAllTextAsync(file, write + Environment.NewLine);
    }

    if(messages.Count() > 0)
    {
        string message = string.Join("<br/>", messages);
        await File.AppendAllTextAsync(file, message + Environment.NewLine);
        await SendEmailAsync(message, ResultTriggerEnum.ERROR);
    }
}

#endregion

async Task SendEmailAsync(string message, ResultTriggerEnum resultTrigger = ResultTriggerEnum.SUCCESS)
{
    // string subject = $"Landrope call API [{resultTrigger}]";
    string subject = $"Landrope call API [{resultTrigger}] [{String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now)}]";
    if(receivers.Length > 0)
    {
        using var context = new SqlContextMod();

        try
        {
            EmailList email = new()
            {
                Pk = Guid.NewGuid(),
                Subject = subject,
                Body = message,
                Receivers = String.Join(";", receivers),
                ProccessCount = 0,
                IsSuccess = false,
                CreatedAt = DateTime.Now,
                CreatedBy = "Landrope API Scheduler"
            };
            context.EmailList.Add(email);
            await context.SaveChangesAsync();
        }
        catch(Exception e)
        {
            await File.AppendAllTextAsync(file, e.Message + Environment.NewLine);
        }
    }    
}