using auth.mod;
using System;
using System.Linq;

namespace landrope.api3.Models
{
	public static class AuthExtensions
	{
		public static user FindUser(this authEntities context, string token)
		{
			var usr = context.users.All().FirstOrDefault(u => (u.pass != null && u.pass.token == token) ||
																					(u.mpass != null && u.mpass.token == token));
			if (usr == null)
				throw new UnauthorizedAccessException("401");

			var mobile = usr.mLastLog?.pass.token == token;

			if ((mobile && usr.mpass?.expired < DateTime.Now) || (!mobile && usr.pass?.expired < DateTime.Now))
				throw new UnauthorizedAccessException("510");

			usr.ExtendToken(token);
			return usr;
		}

		public static (user user, bool rep) FindUserReg(this authEntities context, string token)
		{
			var user = context.FindUser(token);
			var privs = user.getPrivileges(null).Select(p => p.identifier).ToArray();
			var rep = privs.Intersect(new[] { "VIEW_MAP" }).Any();
			return (user, rep);
		}
	}
}
