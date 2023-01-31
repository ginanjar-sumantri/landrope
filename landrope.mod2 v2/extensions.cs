using auth.mod;
using landrope.common;
//using landrope.mcommon;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.mod2
{
	public static class AuthExtensions
	{
		public static user FindUser(this ExtLandropeContext context, string token)
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

		public static (user user, bool regular, bool pra, bool map) FindUserReg(this ExtLandropeContext context, string token)
		{
			var user = context.FindUser(token);
			var privs = user.getPrivileges(null).Select(p => p.identifier).ToArray();
			var regular = privs.Intersect(new[] { "PASCA_VIEW", "PASCA_FULL", "PASCA_REVIEW" }).Any();
			var mapper = privs.Intersect(new[] { "MAP_VIEW", "MAP_FULL", "MAP_REVIEW" }).Any();
			var prabebas = privs.Intersect(new[] { "PRA_VIEW", "PRA_FULL", "PRA_REVIEW" }).Any();
			return (user, regular, prabebas, mapper);
		}
	}

	public static class PersilExtensions
	{
		public static IEnumerable<(int order, string name, string descr)> GetStepsX(this Persil persil)
		{
			return persil.GetType().GetProperties().Select(p => p.GetCustomAttributes(typeof(StepAttribute), true)
															.FirstOrDefault())
												.Where(a => a != null).Cast<StepAttribute>().OrderBy(a => a.order)
												.Select(a => (a.order, a.name, a.descr));
		}

		public static IEnumerable<(int order, string name, string descr)> GetStepsX(this ExtLandropeContext context, string key,
								auth.mod.user user, bool rejectonly = false)
		{
			var persil = context.persils.FirstOrDefault(p => p.key == key);
			if (persil == null)
				return new (int order, string name, string descr)[0];
			if (user == null)
				return persil.GetStepsX();

			var privs = user.getPrivileges(null).Select(a => a.identifier);
			var steps = persil.GetType().GetProperties().Select(p => (p, attr: p.GetCustomAttributes(typeof(StepAttribute), true)
															.FirstOrDefault() as StepAttribute))
												.Where(x => x.attr != null &&
														(!rejectonly || (((ValidatableShell)x.p.GetValue(persil))?.isRejected() ?? false))).OrderBy(x => x.attr.order)
												.Select(x => new { x.attr.order, x.attr.name, x.attr.descr, privs = x.attr.privs.Split(',') })
												.Where(s => s.privs.Intersect(privs).Any());
			return steps.Select(s => (s.order, s.name, s.descr)).ToList();
		}

		public static IEnumerable<int> GetStepsP(this ExtLandropeContext context, string discriminator)
		{
			var data= context.GetCollections(new { steps = new int[0] }, "persil_steps_asgn", $"{{disc:'{discriminator}'}}","{steps:1, _id:0}").ToList();
			return data.SelectMany(d => d.steps).ToArray();
		}

		public static IEnumerable<(string disc,int[] steps)> GetAllSteps(this ExtLandropeContext context)
		{
			var data = context.GetCollections(new { disc = "", steps = new int[0] }, "persil_steps_asgn", "{}").ToList();
			var supplements = Enum.GetValues(typeof(AssignmentCat)).Cast<AssignmentCat>().Where(c=>c!=AssignmentCat.Unknown)
				.Select(c => new
			{
				disc = c.Discriminator(),
				steps = new[] { (int)DocProcessStep.Riwayat_Tanah }
			});
			data.AddRange(supplements);
			return data.Select(d=>(d.disc,d.steps)).ToArray();
		}
	}
}
