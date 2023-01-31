using landrope.mod;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using mongospace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using auth.mod;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace landrope.api2.Models
{

	public class ExpiredUser : user
	{
		public ExpiredUser(user usr)
		{
			this.identifier = usr.identifier;
			this.key = usr.key;
			this.FullName = usr.FullName;
		}
	}
	public class NeedTokenAttribute : ActionFilterAttribute
	{
		bool UseSession = false;
		string[] idroles = new string[0];
		bool unrestricted = false;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'NeedTokenAttribute.NeedTokenAttribute(string, bool)'
		public NeedTokenAttribute(string roleids, bool UseSession = false)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'NeedTokenAttribute.NeedTokenAttribute(string, bool)'
		{
			if (roleids == null)
				return;
			this.UseSession = UseSession;
			unrestricted = roleids == "%";
			idroles = unrestricted ? new string[0] : roleids.Split(',', ';', '|');
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'NeedTokenAttribute.OnActionExecuting(ActionExecutingContext)'
		public override void OnActionExecuting(ActionExecutingContext accontext)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'NeedTokenAttribute.OnActionExecuting(ActionExecutingContext)'
		{
			try
			{
				var token = accontext.HttpContext.Request.FindToken();

				var context = accontext.HttpContext.RequestServices.GetService(typeof(LandropeContext)) as LandropeContext;

				(var ok, var result, var user) = DoAuthenticate(token, context, idroles);
				if (!ok)
				{
					accontext.Result = result;
					return;
				}

			}
			catch (Exception ex)
			{
				accontext.Result = new UnprocessableEntityObjectResult($"Error 500 - {ex.Message}");
				return;
			}
		}

		public static (bool OK, IActionResult result, user user) DoAuthenticate(string token, LandropeContext context, string[] idroles, bool unrestricted = false)
		{
			if (String.IsNullOrWhiteSpace(token))
			{
				return (false, new UnauthorizedObjectResult("Silahkan login dahulu"), null);
				//return;
			}

			var user = context.users.FirstOrDefault(u => u.LastLog.pass.token == token || u.mLastLog.pass.token == token);

			//var user = context.db.GetCollection<User>("auths").Find($"{{token:'{token}'}}").FirstOrDefault();
			if (user == null)
			{
				return (false, new UnauthorizedObjectResult("Informasi login tidak valid"), null);
				//return;
			}

			//var mobile = user.mLastLog?.pass.token == token;

			//var expired = mobile ? user.mLastLog.pass.expired == null || user.mLastLog.pass.expired.Value <= DateTime.Now :
			//	user.LastLog?.pass.expired == null || user.LastLog?.pass.expired.Value <= DateTime.Now;

			//if (expired)
			//{
			//	return (false, new StatusCodeResult((int)HttpStatusCode.NotExtended), null);
			//	//return;
			//}

			//if (idroles.Count() > 0)
			//{
			//	if (unrestricted)
			//		return (true, null, user);
			//	var privs = user.privileges;
			//	if (privs == null || !privs.Select(r => r.identifier).Intersect(idroles).Any())
			//	{
			//		return (false, new StatusCodeResult((int)HttpStatusCode.Forbidden), null);
			//		//return;
			//	}
			//}
			context.users.Update(user);
			user.ExtendToken(token);
			context.SaveChanges();
			return (true, null, user);
		}
	}
}
