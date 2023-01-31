using landrope.common;
using landrope.mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace landrope.mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IConfiguration configuration;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult sla(string token, string kp, string kd, string sk)
        {
           
            var client = new HttpClient();
            var cli = client.GetStringAsync($"https://cs.agungsedayu.com:7879/api/reporting/assign/sla/json?token={token}&kp={kp}&kd={kd}&sk={sk}").GetAwaiter().GetResult();
            var result = JsonConvert.DeserializeObject<List<AssignmentSLAProject>>(cli);

            return View(result);
        }

        
    }
}
