using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auth.mod;
using landrope.mod;
using landrope.mod2;
using landrope.mod.shared;
using landrope.web.Models;
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

namespace landrope.web.Controllers
{
	[ApiController]
	[Route("/auth")]
	public class AuthController : ControllerBase
	{
		LandropeContext context = Contextual.GetContext();
		ExtLandropeContext contextex = Contextual.GetContextExt();

		private readonly ILogger<AuthController> _logger;
		public AuthController(ILogger<AuthController> logger)
		{
			_logger = logger;
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
		public IActionResult Login([FromBody]LoginData data)
		{
			try
			{
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

		[EnableCors(nameof(landrope))]
		[HttpGet("pong")]
		public IActionResult Pong(string token)
		{
			try
			{
				var user = contextex.FindUser(token);
				return Ok();
			}
			catch (UnauthorizedAccessException ex)
			{
				return new ContentResult { StatusCode = int.Parse(ex.Message) };
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("user/list")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult GetUserList(string token, [FromQuery]AgGridSettings gs)
		{
			try
			{
				var users = context.users.Query(u => u.invalid != true);
				return Ok(users.Select(u => u.ToUser()).ToList().GridFeed(gs));
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("role/list")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult GetRoleList(string token, [FromQuery]AgGridSettings gs)
		{
			try
			{
				var roles = context.roles.Query(r => r.invalid != true);
				return Ok(roles.Select(r => r.ToRole()).ToList().GridFeed(gs));
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("action/list")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult GetActionsList(string token, [FromQuery]AgGridSettings gs)
		{
			try
			{
				var actions = context.actions.Query(u => u.invalid != true);
				return Ok(actions.Select(a => a.ToAction()).ToList().GridFeed(gs));
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("user/role-list")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult GetUserRoleList(string token, [FromQuery]string userkey)
		{
			try
			{
				var user = context.users.FirstOrDefault(u => u.key == userkey);
				if (user == null)
					return NoContent();
				var roles = user.userinroles;
				return new JsonResult(roles.Select(ur => new UserInRole_u { key = ur.key, invalid = ur.invalid, role = ur.role.ToRole() }));
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("role/user-list")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult GetRoleUserList(string token, [FromQuery]string rolekey)
		{
			try
			{
				var role = context.roles.FirstOrDefault(u => u.key == rolekey);
				var users = context.users.Query(u => u.userinrolekeys.Any(ur => ur.rolekey == rolekey));
				var usersext = users.Select(u => (u, r: u.userinrolekeys.FirstOrDefault(ur => ur.rolekey == rolekey)))
												.Where(x => x.r != null)
												.Select(x => new UserInRole_r { key = x.r.key, user = x.u.ToUser(), invalid = x.r.invalid });

				return new JsonResult(usersext);
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("user/add-role")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult AddUserRole(string token, [FromBody]Couple keys) // key1 as userkey, key2 as rolekey
		{
			try
			{
				var user = context.users.FirstOrDefault(u => u.key == keys.key1);
				if (user == null)
					return new NotModifiedResult("invalid user key given");

				var role = context.roles.FirstOrDefault(u => u.key == keys.key2);
				if (role == null)
					return new NotModifiedResult("invalid role key given");

				user.AddUserInRoleKey(keys.key2, null);
				context.users.Update(user);
				context.SaveChanges();
				return Ok();
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("user/del-role")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult DelUserRole(string token, [FromBody]Couple keys) // key1 as userkey, key2 as userinrolekey
		{
			try
			{
				var user = context.users.FirstOrDefault(u => u.key == keys.key1);
				if (user == null)
					return new NotModifiedResult("invalid user key given");

				if (!user.userinrolekeys.Any(ur=>ur.key==keys.key2))
					return new NotModifiedResult("invalid user-role key given");

				user.DelUserInRoleKey2(keys.key2);
				context.users.Update(user);
				context.SaveChanges();
				return Ok();
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("role/action-list")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult GetRoleActionList(string token, [FromQuery]string rolekey)
		{
			try
			{
				var role = context.roles.FirstOrDefault(u => u.key == rolekey);
				if (role == null)
					return NoContent();
				var actions = role.roleactions;
				return new JsonResult(actions.Select(ra => new RoleAction_r { key = ra.key, invalid = ra.invalid, action = ra.action.ToAction() }));
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("action/role-list")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult GetActionRoleList(string token, [FromQuery]string actionkey)
		{
			try
			{
				var action = context.roles.FirstOrDefault(u => u.key == actionkey);
				var roles = context.roles.Query(u => u.roleactionkeys.Any(ra => ra.actionkey == actionkey));
				var rolesext = roles.Select(r => (r, a: r.roleactionkeys.FirstOrDefault(ur => ur.actionkey == actionkey)))
												.Where(x => x.a != null)
												.Select(x => new RoleAction_a { key = x.r.key, role = x.r.ToRole(), invalid = x.r.invalid });

				return new JsonResult(rolesext);
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("role/add-action")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult AddRoleAction(string token, [FromBody]Couple keys) // key1 as rolekey, key2 as actionkey
		{
			try
			{
				var role = context.roles.FirstOrDefault(u => u.key == keys.key1);
				if (role == null)
					return new NotModifiedResult("invalid role key given");

				var action = context.actions.FirstOrDefault(u => u.key == keys.key2);
				if (action == null)
					return new NotModifiedResult("invalid action key given");

				role.AddRoleActionKey(keys.key2);
				context.roles.Update(role);
				context.SaveChanges();
				return Ok();
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("role/del-action")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult DelRoleAction(string token, [FromBody]Couple keys) // key1 as rolekey, key2 as roleactionkey
		{
			try
			{
				var role = context.roles.FirstOrDefault(u => u.key == keys.key1);
				if (role == null)
					return new NotModifiedResult("invalid role key given");

				if (!role.roleactionkeys.Any(ra => ra.key == keys.key2))
					return new NotModifiedResult("invalid role-action key given");

				role.DelRoleActionKey2(keys.key2);
				context.roles.Update(role);
				context.SaveChanges();
				return Ok();
			}
			catch (Exception ex)
			{
				return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status500InternalServerError };
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("action/save/{oper}")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult SaveAction(string token, [FromBody] Action action, string oper)
		{
			return Save<Action, action>(action, context.actions, oper);
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("role/save/{oper}")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult SaveRole(string token, [FromBody] Role role, string oper)
		{
			return Save<Role, role>(role, context.roles, oper);
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("user/save/{oper}")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult SaveUser(string token, [FromBody]User user, string oper)
		{
			return Save<User, user>(user, context.users, oper);
		}

		private IActionResult Save<TIn, TSave>(TIn obj, ICollSet<TSave> collset, string oper) where TIn : Auth where TSave : entity
		{
			try
			{
				var inprops = typeof(TIn).GetProperties(BindingFlags.Public | BindingFlags.Instance)
											.Where(p => new[] { "key", "invalid" }.Contains(p.Name)).ToArray();
				var saveprops = typeof(TSave).GetProperties(BindingFlags.Public | BindingFlags.Instance)
											.Where(p => new[] { "key", "invalid" }.Contains(p.Name)).ToArray();
				var propnames = inprops.Select(p => p.Name).Intersect(saveprops.Select(p => p.Name)).ToArray();
				inprops = inprops.Where(p => propnames.Contains(p.Name)).ToArray();
				saveprops = saveprops.Where(p => propnames.Contains(p.Name)).ToArray();
				var couples = inprops.Join(saveprops, i => i.Name, s => s.Name, (i, s) => (inp: i, savep: s)).ToList();

				switch (oper.ToLower())
				{
					case "add": return Create();
					case "del": return Delete();
					case "edit": return Edit();
				}
				return new NotModifiedResult("Unknown save operator");

				IActionResult Create()
				{
					var newobj = Activator.CreateInstance<TSave>();
					newobj.key = obj.key;
					newobj.invalid = obj.invalid;
					couples.ForEach(c => c.savep.SetValue(newobj, c.inp.GetValue(obj)));
					collset.Insert(newobj);
					context.SaveChanges();
					return Ok(newobj.key);
				}

				IActionResult Delete()
				{
					var oldobj = collset.FirstOrDefault(a => a.key == obj.key);
					if (oldobj == null)
						return new NotModifiedResult("invalid key given");
					collset.Remove(oldobj);
					context.SaveChanges();
					return Ok();
				}

				IActionResult Edit()
				{
					var oldobj = collset.FirstOrDefault(a => a.key == obj.key);
					if (oldobj == null)
						return new NotModifiedResult("invalid key given");
					oldobj.invalid = obj.invalid;
					couples.ForEach(c => c.savep.SetValue(oldobj, c.inp.GetValue(obj)));
					collset.Update(oldobj);
					context.SaveChanges();
					return Ok();
				}

			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}


		public class SetPwdData
		{
			public string oldpwd { get; set; }
			public string newpwd { get; set; }
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("user/password/set")]
		[NeedToken("%")]
		public IActionResult SetPassword(string token, [FromBody]SetPwdData pwd)
		{
			try
			{
				var user = ControllerContext.HttpContext.GetUser();

				if (pwd == null || pwd.newpwd == null)
					return new NotModifiedResult("Data yang diberikan tidak valid");
				if (pwd.newpwd == pwd.oldpwd)
					return new NotModifiedResult("Password baru harus berbeda dengan password sebelumnya");
				if (!user.TestPassword(pwd.oldpwd))
					return new ObjectResult("Password sebelumnya tidak sesuai") { StatusCode = StatusCodes.Status403Forbidden };
				user.SetPassword(pwd.newpwd);
				context.users.Update(user);
				context.SaveChanges();
				return new OkResult();
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}

		}

		[EnableCors(nameof(landrope))]
		[HttpPost("user/password/reset")]
		[NeedToken("*SECURITY_FULL")]
		public IActionResult ResetPassword(string token, [FromBody]string key)
		{
			try
			{
				var user = context.users.FirstOrDefault(u => u.key == key && u.invalid != true);
				if (user == null)
					return new NotModifiedResult("User tidak ditemukan");
				user.SetPassword("123456");
				context.users.Update(user);
				context.SaveChanges();
				return new OkResult();
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("logout")]
		public IActionResult Logout(string token)
		{
			var user = contextex.FindUser(token);
			if (user == null)
				return Unauthorized();
			user.LoggedOut(token);
			context.users.Update(user);
			context.SaveChanges();
			return Ok();
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("user/fullname")]
		public IActionResult GetUserName(string token)
		{
			var user = contextex.FindUser(token);
			if (user == null)
				return Unauthorized();
			var privs = user.getPrivileges(null).Select(a => a.identifier).ToArray();
			var newmap = privs.Contains("MAP_FULL");
			var newpersil = privs.Contains("PASCA_FULL");

			return Ok(new { name = user.FullName, allownew = newmap, allownew2 = newpersil });
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("privs")]
		public IActionResult GetPrivs(string token)
		{
			try
			{
        var user = contextex.FindUser(token);
				if (user==null)
          return new UnauthorizedResult();
				return Ok(new{
						name=user.FullName, 
						privs= user.privileges.Select(a=>a.identifier).Where(s=>!s.StartsWith("*")).ToArray()
				});
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				return new InternalErrorResult(ex.Message);
			}
		}
	}
}