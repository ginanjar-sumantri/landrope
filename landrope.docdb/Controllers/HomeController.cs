using landrope.docdb.Models;
using landrope.docdb.Repo;
using landrope.mod3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace landrope.docdb.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IServiceProvider services;
		private readonly LandropePlusContext context;
		private readonly AssignmentRepo assign = new AssignmentRepo();
		//private readonly DatabaseFileProvider fileProvider;

        public HomeController(ILogger<HomeController> logger, IServiceProvider services)
		{
			_logger = logger;
			context = services.GetService<mod3.LandropePlusContext>();
		}

        [HttpGet]
        public IActionResult Index(string layout="/Index.cshtml")
        {
            return View(layout);
        }

		[HttpGet("{layout}/{key}")]
		public IActionResult SuratTugas(string layout, string key)
		{
            try
            {
				if (string.IsNullOrEmpty(layout) || string.IsNullOrEmpty(key))
					return View();
				string layoutView = $"/{layout}.cshtml";
                var obj = (object)assign.GetDataSuratTugas(context, key, layoutView);
				var jsonObj = JsonConvert.SerializeObject(obj);
                return View(layoutView, obj);
			}
            catch(Exception ex)
            {
				return View();
            }
		}

		public IActionResult Test()
        {
			return View();
        }

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
