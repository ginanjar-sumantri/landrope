using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auth.mod;
using landrope.mod;
using landrope.mod2;
using landrope.mod.shared;
//using landrope.web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
//using Remotion.Linq.Parsing.Structure.IntermediateModel;
using Tracer;
using APIGrid;
using Action = landrope.mod.shared.Action;
using mongospace;
using System.Reflection;
// using Microsoft.EntityFrameworkCore.Metadata.Internal;
// using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.DependencyInjection;

namespace landrope.api2.Controllers
{
	[ApiController]
	[Route("/auth")]
	public class AuthController : ControllerBase
	{
		IServiceProvider services;
		LandropeContext context;
		ExtLandropeContext contextex;

		private readonly ILogger<AuthController> _logger;
		public AuthController(IServiceProvider services,ILogger<AuthController> logger)
		{
			_logger = logger;
			context = services.GetService<LandropeContext>();
			contextex = services.GetService<ExtLandropeContext>();
		}

		internal static string doLogin(LandropeContext context, HttpContext htc, string username, string password, bool mobile = false)
		{
			//MyTracer.TraceInfo2($"username:{username}, password:{password}");
			var user = context.users.FirstOrDefault(u => u.identifier == username);
			//MyTracer.TraceInfo2($"user.identifier:{user?.identifier}");
			if (user == null || !user.TestPassword(password))
				throw new UnauthorizedAccessException("Username atau Password salah");
			var IP = htc.Connection.RemoteIpAddress.ToString();
			string token = user.LoggedIn(IP, mobile);
			context.users.Update(user);
			context.SaveChanges();
			return token;
		}

		public class LoginData
		{
			public string username { get; set; }
			public string password { get; set; }
			public bool mobile { get; set; } = false;
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("login")]
		public IActionResult Login([FromQuery] int appno, bool cutoff, [FromBody]LoginData data)
		{
			try
			{
				context.SwitchDB(cutoff, appno);
				var token = doLogin(context, ControllerContext.HttpContext, data?.username, data?.password, data?.mobile??false);
				return token == null ? new NotModifiedResult("Gagal login. Error tidak diketahui") : (IActionResult)new JsonResult(token);
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

		public static (bool, IActionResult, user user) CheckToken(authEntities context, string token, string actions)
		{
			try
			{
				var arrroles = actions.Split(",");
				return DoAuthenticate(token, context, arrroles);
			}
			catch (Exception ex)
			{
				return (false,new UnprocessableEntityObjectResult($"Error 500 - {ex.Message}"),null);
			}
		}

		public static (bool OK, IActionResult result, user user) DoAuthenticate(string token, authEntities context, string[] idroles, bool unrestricted = false)
		{
			if (String.IsNullOrWhiteSpace(token))
				return (false, new UnauthorizedObjectResult("Silahkan login dahulu"), null);

			var user = context.users.FirstOrDefault(u => u.LastLog.pass.token == token || u.mLastLog.pass.token == token);

			if (user == null)
				return (false, new UnauthorizedObjectResult("Informasi login tidak valid"), null);

			context.users.Update(user);
			user.ExtendToken(token);
			context.SaveChanges();
			return (true, null, user);
		}

	}
}