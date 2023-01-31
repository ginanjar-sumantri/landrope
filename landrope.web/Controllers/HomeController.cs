using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using landrope.mod;
using MongoDB.Driver;
using Newtonsoft.Json;
using landrope.web.Models;
using Microsoft.AspNetCore.Cors;

namespace landrope.web.Controllers
{
	public class HomeController : Controller
	{
		LandropeContext context = Contextual.GetContext();

		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		[EnableCors(nameof(landrope))]
		public IActionResult Root()
		{
			var root = ControllerContext.HttpContext.Request.PathBase;
			return new ContentResult { Content = $"const $myroot='{root}';", ContentType = "text/javascript", StatusCode = 200 };
		}

		[EnableCors(nameof(landrope))]
		public IActionResult Index()
		{
			return View();
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("/home/login")]
		public IActionResult Login(string username, string password)
		{
			try
			{
				var token = AuthController.doLogin(context, ControllerContext.HttpContext, username, password);
				return new OkObjectResult(token);
			}
			catch (UnauthorizedAccessException ex)
			{
				return new UnauthorizedObjectResult(ex.Message);
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("14ndr0p3")]
		public IActionResult CekExists([FromQuery]string dtm)
		{
			return Ok();
		}

	}
}
