using System;
using System.Collections.Generic;
using System.Reflection;
using auth.mod;
using landrope.mod2;
using landrope.mod3.classes;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using mongospace;

namespace landrope.mod3
{
	public class LandropePlusContext : ExtLandropeContext
	{
		static LandropePlusContext()
		{
			Registrar.Register(MethodBase.GetCurrentMethod());
		}

		public LandropePlusContext()
			: base()
		{
			InitColSets<LandropePlusContext>();
			MongoEntities.PutDefault<LandropePlusContext>(this);
		}

		public LandropePlusContext(IConfigurationRoot configuration)
			: base(configuration)
		{
			InitColSets<LandropePlusContext>();
			MongoEntities.PutDefault<LandropePlusContext>(this);
		}

		public LandropePlusContext(string url, string database)
			: base(url, database)
		{
			InitColSets<LandropePlusContext>();
			MongoEntities.PutDefault<LandropePlusContext>(this);
		}

		public static LandropePlusContext current;

		public CollSet<Bundle> bundles;
		public CollSet<MainBundle> mainBundles;
		public CollSet<TaskBundle> taskBundles;
		public CollSet<PreBundle> preBundles;
		public CollSet<Assignment> assignments;
		public CollSet<Doc> docs;
		public CollSet<docno> docnoes;
		public CollSet<StepDocType> stepdocs;
		public CollSet<ControlUser> conusers;
		public CollSet<Budget> budgets;
		public CollSet<trxBatch> batches;
		public CollSet<LogBundle> logBundle;
		public CollSet<PraPembebasan> praDeals;
		public CollSet<DealBundle> DealBundle;
		public CollSet<LogDeal> logDeal;
		public CollSet<FormPraBebas> formPraBebas;
		public CollSet<Internal> internals;
		public CollSet<LogPreBundle> logPreBundle;
		public CollSet<MeasurementRequest> measurement;

		/*		public List<T> GetMainBundles<T>() where T : Bundle
				{
					var attr = typeof(T).GetCustomAttribute<EntityAttribute>();
					if (attr == null)
						return new List<T>();
					return this.GetCollections(Activator.CreateInstance<T>(), "bundles", $"<_t:'{attr.Discriminator}'>".MongoJs(),"{_t:0}").ToList();
				}

				public bool Update<T>(T item) where T:Bundle
				{
					var attr = typeof(T).GetCustomAttribute<EntityAttribute>();
					if (attr == null)
						return false;
					this.db.GetCollection<T>("bundle").FindOneAndUpdate("{key:}")
				}*/
	}
}
