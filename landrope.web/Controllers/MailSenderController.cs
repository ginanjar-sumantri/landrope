using Microsoft.AspNetCore.Mvc;
using MailSender;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Cors;

namespace landrope.web.Controllers
{
    [ApiController]
    [Route("api/mailsender")]
    [EnableCors(nameof(landrope))]
    public class MailSenderController : Controller
    {
        [HttpPost("sendemail")]
        public async Task<IActionResult> SendEmail([FromBody] EmailStructure email)
        {
            try
            {
                var mailServices = new MailSenderClass();
                await mailServices.SendEmailAyncSMTP(email);

                return Ok();
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }
    }
}
