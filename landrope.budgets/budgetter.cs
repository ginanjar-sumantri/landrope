using landrope.common;
using landrope.mod3;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace landrope.budgets
{
	public class Budgetter
	{
		LandropePlusContext contextplus;
		IServiceProvider services;
		IServiceScope scope;

		List<SPS> allSPS;

		public Budgetter(IServiceProvider services)
		{
			this.services = services;
			this.scope = services.CreateScope();
			contextplus = scope.ServiceProvider.GetService<LandropePlusContext>();
		}

		List<(AssignmentCat cat, int order, DocProcessStep step, double price)> allcosts = null;
		public void PrepareBudget()
		{
			allSPS = contextplus.AllSPS();
			FlatCost.LoadFlatCosts(contextplus);
			var stporders = Assignment.StepOrders.Select(d => (cat: d.Key.Category(), d.Value))
					.SelectMany(x => x.Value.Select(v => (x.cat, v.order, v.step))).ToArray();
			allcosts = stporders.GroupJoin(FlatCost.Lists, s => (s.cat, s.step), c => (c.categ, c.step),
					(s, sc) => (s, cost: sc.FirstOrDefault()))?
					.Select(x => (x.s.cat, x.s.order, x.s.step, vari: x.cost?.variable ?? 0)).ToList();
		}

		public static Dictionary<AssignmentCat, DocProcessStep[]> budgetSteps =
			new Dictionary<AssignmentCat, DocProcessStep[]>
		{
			{ AssignmentCat.HGB,new[]{DocProcessStep.Balik_Nama} },
			{ AssignmentCat.SHM,new[]{DocProcessStep.Penurunan_Hak,DocProcessStep.Balik_Nama } },
			{ AssignmentCat.SHP,new[]{DocProcessStep.Peningkatan_Hak,DocProcessStep.Balik_Nama } },
			{ AssignmentCat.Girik,new[]{DocProcessStep.PBT_Perorangan,DocProcessStep.PBT_PT,
														DocProcessStep.SK_BPN,DocProcessStep.Cetak_Buku,   } },
			{ AssignmentCat.Hibah,new[]{DocProcessStep.Penurunan_Hak,DocProcessStep.Balik_Nama } }
		};

		public void CalcBudget(List<ReportWithBudget> list)
		{
			var inBPN = new[] { DocProcessStep.PBT_Perorangan,DocProcessStep.PBT_PT,DocProcessStep.SK_BPN,DocProcessStep.Cetak_Buku,
												DocProcessStep.Penurunan_Hak,DocProcessStep.Peningkatan_Hak,DocProcessStep.Balik_Nama}
								.Select(s => (int)s).ToArray();

			foreach (var l in list)//.ForEach(l =>
			{
				var adaSPS = allSPS.FirstOrDefault(s => s.keyPersil == l.key && ((int)s.step == l.next_step) && s.date != null) != null;
				l.MasukBPN = adaSPS;// && inBPN.Contains(l.next_step);
				bool inclBPHTB = false;
				bool inclPPH = false;
				var cat = (AssignmentCat)l.category;
				var incost = allcosts.Where(c => c.cat == cat).Select(c => new { c.order, c.step, c.price }).ToArray();
				var istart = incost.FirstOrDefault(c => (int)c.step == l.next_step).order;
				if (istart == 0)
					istart = 1;
				var ncosts = incost.Where(c => c.order >= istart).ToArray();
				var includes = budgetSteps[cat];

				switch ((AssignmentCat)l.category)
				{
					case AssignmentCat.Girik:
						inclBPHTB = l.status == (int)DocProcessStep.SK_BPN;
						break;
					case AssignmentCat.Hibah or AssignmentCat.SHM or AssignmentCat.SHP or AssignmentCat.HGB or AssignmentCat.Hibah:
						inclBPHTB = l.next_step == (int)DocProcessStep.AJB;
						inclPPH = l.next_step == (int)DocProcessStep.Akta_Notaris;
						break;
				}
				var bsteps = budgetSteps.GetValueOrDefault(cat).Select((s, i) => (step: s, ord: i)).ToArray();
				//.seled => d.Value.Select((v, i) => (cat: (int)d.Key, ord: i, step: (int)v))).ToArray();
				var budgets = ncosts.Join(includes, c => c.step, s => s, (c, s) => c).ToArray();
				var gaccbudgets = budgets.Select(b => new BudgetDtl
				{
					step = b.step,
					price = b.price,
					amount = b.price * l.luas
				}).Join(bsteps, b => b.step, s => s.step, (b, s) => (b, s.ord)).ToArray();

				var accbudgets = gaccbudgets.Join(gaccbudgets, g1 => 1, g2 => 1, (g1, g2) => (g1.ord, g1.b, ord2: g2.ord, b2: g2.b))
					.GroupBy(x => (x.ord, x.b))
					.Select(g => (g.Key.ord, own: g.Key.b, accs: g.Where(d => d.ord2 <= g.Key.ord).Select(d => d.b2).ToArray()))
					.ToList();
				accbudgets.ForEach(x => x.own.accumulative = x.accs.Sum(a => a.amount));
				l.budgets = accbudgets.Select(a => a.own).ToArray();
			};
			return;
			/*			var focused = list.Select(l => (l, steps: Assignment.CollectNexts(((AssignmentCat)l.category).Discriminator(), (DocProcessStep)l.next_step)))
							.SelectMany(f=>f.steps.Select(x=>(f.l,step:x)))
							.ToArray();
						var foundSPS = focused.GroupJoin(allSPS,
											f => (f.l.key, f.step), s => (s.keyPersil, s.step), (f, ss) => (f.l, f.step, have: ss.FirstOrDefault() != null))
									.ToArray(); 
						focused.Select(f => (f.l, f.steps));
			*/
		}

	}
}
