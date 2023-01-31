using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using landrope.mod.shared;
using auth.mod;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using landrope.mod3;
using DynForm.shared;
using landrope.mod2;
using landrope.mod3.shared;
using landrope.common;
using MongoDB.Driver;
using landrope.mod;
using DocumentFormat.OpenXml.Drawing;
using System.Reflection;

namespace landrope.api2
{
	public static class SecurityExtensions
	{
		public static User ToUser(this user usr) =>
			new User { key = usr.key, FullName = usr.FullName, identifier = usr.identifier, invalid = usr.invalid };

		public static Role ToRole(this role rol) =>
			new Role { key = rol.key, invalid = rol.invalid, description = rol.description };

		public static landrope.mod.shared.Action ToAction(this action act) =>
			new landrope.mod.shared.Action { key = act.key, invalid = act.invalid, identifier = act.identifier, description = act.description };
	}

	public static class ControllerContextExtensions
	{
		public static IWebHostEnvironment GetEnvironment(this ControllerContext ctx)
			=> ctx.HttpContext.RequestServices.GetService<IWebHostEnvironment>();

		public static string GetContentRootPath(this ControllerContext ctx)
			=> ctx.HttpContext.RequestServices.GetService<IWebHostEnvironment>().ContentRootPath;
	}

	public static class ContextExtensios
	{
		public static Dictionary<string, string> StrCats = new Dictionary<string, string>();

		static ContextExtensios()
		{
			var cats = Enumerable.Range(1, Enum.GetValues(typeof(AssignmentCat)).Cast<int>().Max()).Cast<AssignmentCat>().ToArray();
			StrCats = cats.ToDictionary(c => c.Discriminator(), c => $"{c:g}");
		}

		public static Dictionary<string, option[]> GetXtras(this LandropePlusContext context,
																								IEnumerable<DynElement> layout, string note,
																								params KeyValuePair<string, Func<option[]>>[] funcs)
		{

			var dict = new Dictionary<string, option[]>();

			var compsample = new
			{
				keyDesa = "",
				Companies = new[] { new { key = "", name = "" } },
			};

			//var projects = context.GetProjects();
			var locations = context.GetVillages();
			var xcompanies = context.persils.Query(p => p.invalid != true && (p.en_state == null || p.en_state == 0) && p.basic != null && p.basic.current != null)
								.Select(p => (p.basic.current.keyProject,p.basic.current.keyDesa, p.basic.current.keyPTSK, p.basic.current.keyPenampung))
								.Where(x => x.keyPTSK != null || x.keyPenampung != null).ToArray();
			var keyPTSKs = xcompanies.Select(x => (x.keyProject,x.keyDesa,keyComp:x.keyPTSK))
				.Where(k => !string.IsNullOrEmpty(k.keyComp)).Distinct();
			var keyPenampungs = xcompanies.Select(x => (x.keyProject,x.keyDesa, keyComp: x.keyPenampung))
				.Where(k => !string.IsNullOrEmpty(k.keyComp)).Distinct();
			//var ptsks = context.companies.All().Join(keyPTSKs, c => c.key, k => k.keyComp, (c, k) => (k.keyProject,k.keyDesa, c.key, c.identifier));
			var ptsks = context.ptsk.All().Join(keyPTSKs, c => c.key, k => k.keyComp, (c, k) => (k.keyProject, k.keyDesa, c.key, c.identifier));
			var penampungs = context.companies.All().Join(keyPenampungs, c => c.key, k => k.keyComp, (c, k) => (k.keyProject,k.keyDesa, c.key, c.identifier));

			var others = funcs.Select(f => f.Key).ToArray();
			layout.Where(e => !string.IsNullOrWhiteSpace(e.options)).Select(e => e.options).Distinct().ToList().ForEach(
				o =>
				{
					var opt = o.Replace("extras.", "");

					var opts = (others.Contains(opt))? funcs.First(f => f.Key == opt).Value.Invoke() :
						opt switch
						{
							"optProjects" => locations.Select(x => new option { key = x.project.key, identity = x.project.identity }).ToArray(),
							"optDesas" => locations.Select(v => new option { keyparent = v.project.key, key = v.desa.key, identity = v.desa.identity })
																			.ToArray(),
							"lstCategories" => Enum.GetValues(typeof(AssignmentCat)).Cast<AssignmentCat>().Where(c => c != AssignmentCat.Unknown)
																	.Select(v => new option { key = $"{v:g}", identity = $"{v:g}" }).ToArray(),
							//"lstSteps" => GetSteps(),
							"lstSifats" => Enum.GetNames(typeof(SifatBerkas)).Select(v => new option { key = v, identity = v }).ToArray(),
							"optPTSKs" => ptsks.Select(c => new option { keyparent = c.keyDesa, key = c.key, identity = c.identifier }).ToArray(),
							"optPenampungs" => penampungs.Select(c => new option { keyparent = c.keyDesa, key = c.key, identity = c.identifier }).ToArray(),
							"optNotarists" => GetNotaris(),
							"optUsers" => context.users.Query(u => u.invalid != true)
															.ToList().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
															.Distinct().ToArray(),
							//"optCoICs" => GetCoICs(),
							"optPICs" => GetPICs(),
							_ => new option[0]
						};

					dict.Add(opt, opts.ToArray());
				});
			if (!string.IsNullOrWhiteSpace(note))
				dict.Add("NOTE", new option[] { new option { key = "NOTE", identity = note } });
			if (dict.Count == 0)
				dict.Add("DUMMY", null);
			return dict;

			option[] GetNotaris() => context.notarists.Query(n => n.invalid != true)
						.ToArray().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
						.Distinct().ToArray();
			//option[] GetCoICs()
			//{
			//	var conusers = context.conusers.Query(u => u.invalid != true && u.level == ControlLevel.Koordinator).ToArray();
			//	var users = conusers.Join(context.users.All(), c => c.key, u => u.key,
			//						(c, u) => (c.key, t1: c.team & AssignmentTeam.Pra_Bebas, t2: c.team & AssignmentTeam.Paska_Bebas, u.FullName));
			//	var users1 = users.Where(u => u.t1 == AssignmentTeam.Pra_Bebas);
			//	var users2 = users.Where(u => u.t2 == AssignmentTeam.Paska_Bebas);
			//	var steps = StepDocType.List.GroupBy(s => s.step).Select(g => (step: g.Key, g.First().team));
			//	var steps1 = steps.GroupJoin(users1, s => s.team, u => u.t1,
			//							(s, su) => su.Select(u => new option { keyparent = $"{s.step:g}", key = u.key, identity = u.FullName }));
			//	var steps2 = steps.GroupJoin(users2, s => s.team, u => u.t2,
			//							(s, su) => su.Select(u => new option { keyparent = $"{s.step:g}", key = u.key, identity = u.FullName }));
			//	return steps1.Union(steps2).SelectMany(s => s).ToArray();
			//}
			option[] GetPICs() => context.internals.Query(i => i.invalid != true)
												   .OrderBy(i => i.identifier)
												   .Select(x => new option { keyparent = null, key = x.key, identity = $"Internal - {x.salutation} {x.identifier}" })
												   .Distinct()
												   .Union(context.notarists.Query(n => n.invalid != true)
																			.OrderBy(n => n.identifier)
																			.Select(x => new option { keyparent = null, key = x.key, identity = $"Notaris - {x.identifier}" })
																			.Distinct()
												).ToArray();
		}

		public static string FindPersilKey(this LandropePlusContext context, string IdBidang)
			=> context.GetCollections(new { key = "", IdBidang = "" }, "persils_ID", $"{{IdBidang:'{IdBidang}'}}", "{_id:0}")
				.FirstOrDefault()?.key;
	}
}
