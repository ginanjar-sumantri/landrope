using landrope.common;
using landrope.hosts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using landrope.mod3;
using landrope.mod2;
using landrope.api2.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace landrope.api2.Models
{
	public class candidateModel
	{
		public List<PersilNextReady> Items = new List<PersilNextReady>();
		public void Get()
		{
			var httpContext = HttpAccessor.Helper.HttpContext;
			var contextplus = httpContext.RequestServices.GetService<LandropePlusContext>();
			var contextex = httpContext.RequestServices.GetService<ExtLandropeContext>();
			try
			{
				var host = HostServicesHelper.GetBundleHost(httpContext.RequestServices) as BundleHost;
#if _LOOSE_
				var pnxs = host.NextReadies(true);
#else
				var pnxs = host.NextReadies().Where(x=>x.keyProject!=null&&x.keyDesa!=null);
#endif
				var projects = contextplus.GetVillages();
				var locations = projects.Select(p => (keyProject: p.project.key, keyDesa: p.desa.key, project: p.project.identity, desa: p.desa.identity));
				var companies = contextex.companies.All();
				/*				var ptsks = companies.Where(c => c.status == StatusPT.pembeli).ToArray();
								var penampungs = companies.Where(c => c.status == StatusPT.penampung).ToArray();
				*/
/*				var user = contextplus.FindUser(token);
				var privs = user.privileges.Select(p => p.identifier).ToArray();
*/
				var result = new List<PersilNx>();
				//if (privs.Contains("NOT_CREATOR"))
				result.AddRange(pnxs.Where(p => AssignController.CatSteps["NOT_CREATOR"].Contains(p._step)));
				//if (privs.Contains("BPN_CREATOR"))
					result.AddRange(pnxs.Where(p => AssignController.CatSteps["BPN_CREATOR"].Contains(p._step)));

				Items = result.Select(p => new PersilNextReady
				{
					keyProject = p.keyProject,
					keyDesa = p.keyDesa,
					keyPTSK = p.keyPTSK,
					keyPenampung = p.keyPenampung,
					PTSK = companies.FirstOrDefault(c => c.key == p.keyPTSK)?.identifier,
					penampung = companies.FirstOrDefault(c => c.key == p.keyPenampung)?.identifier,
					cat = p.cat,
					disc = p.cat.ToString("g"),
					_step = p._step,
					step = p._step.ToString("g"),
					count = p.count
				}.SetLocation(locations.FirstOrDefault(l => l.keyDesa == p.keyDesa)))
				.OrderBy(n=>n.project).ThenBy(n=>n.desa).ThenBy(n=>n.PTSK).ToList();
			}
			catch (Exception ex)
			{
			}

		}
	}

	public class candidateDtlModel
	{
		public List<PersilView> Items = new List<PersilView>();
		public void Get(string keyDesa, string keyComp, string disc, DocProcessStep step)
		{
			var httpContext = HttpAccessor.Helper.HttpContext;
			var contextplus = httpContext.RequestServices.GetService<LandropePlusContext>();
			var contextex = httpContext.RequestServices.GetService<ExtLandropeContext>();
			try
			{
				var host = HostServicesHelper.GetBundleHost(httpContext.RequestServices) as BundleHost;
				Items = host.NextReadiesDtl(keyDesa,keyComp,disc,step)
										.Select(p=>p.ToView(contextex))
										.OrderBy(n => n.IdBidang).ToList();
			}
			catch (Exception ex)
			{
			}

		}
	}
}
