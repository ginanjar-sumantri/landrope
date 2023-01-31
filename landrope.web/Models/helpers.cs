using landrope.mod;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using mongospace;
using auth;
using MongoDB.Driver;
using System.IO;
using System.Text;
using auth.mod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using landrope.mod.shared;

namespace landrope.web
{

	public static class AutenticationHelper
	{
		public static void PutLoginInfo(this ISession session, user user, IEnumerable<role> roles)
		{
			var obj = new { user, roles };
			var ses = JsonConvert.SerializeObject(obj);
			session.SetString("user", ses);
		}


		public static (user user, role[] roles) GetLoginInfo(this ISession session)
		{
			var obj = new { user = new user(), roles = new role[0] };
			var ses = session.GetString("user");
			if (ses == null)
				return (null, null);
			obj = JsonConvert.DeserializeAnonymousType(ses, obj);
			return (obj.user, obj.roles);
		}

		public static bool UserAvailable(this ISession session) => session.IsAvailable && session.Keys.Contains("user");

		public static user GetUser(this LandropeContext context, string token)
		{
			return context.users.FirstOrDefault(u => u.LastLog.pass.token == token);
		}


		public static user GetUser(this HttpContext htc)
		{
			var token = htc.Request.FindToken();

			var context = htc.RequestServices.GetService(typeof(LandropeContext)) as LandropeContext;
			return context.users.FirstOrDefault(u => u.LastLog.pass.token == token);
		}


		public static string FindToken(this HttpRequest req)
		{
			var token = "";
			if (req.HasFormContentType && req.Form.Any(f => f.Key == "token"))
				token = req.Form["token"];
			else if (req.Query.Any(f => f.Key == "token"))
				token = req.Query["token"];
			return token;
		}

		public static string ReadBody(this HttpRequest req)
		{
			using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
			{
				var task = reader.ReadToEndAsync();
				task.Wait();
				return task.Result;
			}
		}
	}


	public class NotModifiedResult : ObjectResult
	{

		public NotModifiedResult(object value)

			: base(value)
		{
			StatusCode = StatusCodes.Status304NotModified;
		}
	}


	public class InternalErrorResult : ObjectResult

	{

		public InternalErrorResult(object value)

			: base(value)
		{
			StatusCode = StatusCodes.Status500InternalServerError;
		}
	}
}
