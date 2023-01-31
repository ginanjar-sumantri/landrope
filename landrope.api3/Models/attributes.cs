using auth.mod;
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
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace landrope.api3.Models
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
            //accontext.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            //accontext.HttpContext.Response.Headers.Add("Access-Control-Allow-Method", "POST,GET");
            //accontext.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            //accontext.HttpContext.Response.Headers.Add("Access-Control-Max-Age", "86400");

            string token = null;
            //if (UseSession)
            //	token = accontext.HttpContext.Session.GetString("token");
            if (String.IsNullOrWhiteSpace(token))
                token = accontext.HttpContext.Request.FindToken();
            if (String.IsNullOrWhiteSpace(token))
            {
                accontext.Result = new UnauthorizedObjectResult("Silahkan login dahulu");
                return;
            }

            var context = accontext.HttpContext.RequestServices.GetService(typeof(LandropeContext)) as LandropeContext;
            var user = context.users.FirstOrDefault(u => u.LastLog.pass.token == token || u.mLastLog.pass.token == token);

            //var user = context.db.GetCollection<User>("auths").Find($"{{token:'{token}'}}").FirstOrDefault();
            if (user == null)
            {
                accontext.Result = new UnauthorizedObjectResult("Informasi login tidak valid");
                return;
            }

            var mobile = user.mLastLog?.pass.token == token;

            var expired = mobile ? user.mLastLog.pass.expired == null || user.mLastLog.pass.expired.Value <= DateTime.Now :
                user.LastLog?.pass.expired == null || user.LastLog?.pass.expired.Value <= DateTime.Now;

            if (expired)
            {
                accontext.Result = new StatusCodeResult((int)HttpStatusCode.NotExtended);
                return;
            }

            if (unrestricted)
                return;
            var privs = user.privileges;
            if (privs == null || !privs.Select(r => r.identifier).Intersect(idroles).Any())
            {
                accontext.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
                return;
            }
            context.users.Update(user);
            user.ExtendToken(token);
            context.SaveChanges();
        }
    }
}
