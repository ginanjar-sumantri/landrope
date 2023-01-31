using auth.mod;
using landrope.mod2;
using landrope.mod4.classes;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using mongospace;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace landrope.mod4
{
    public class LandropePayContext : ExtLandropeContext
    {
        static LandropePayContext()
        {
            Registrar.Register(MethodBase.GetCurrentMethod());
        }

        public LandropePayContext()
            : base()
        {
            InitColSets<LandropePayContext>();
            MongoEntities.PutDefault<LandropePayContext>(this);
        }

        public LandropePayContext(IConfigurationRoot configuration)
            : base(configuration)
        {
            InitColSets<LandropePayContext>();
            MongoEntities.PutDefault<LandropePayContext>(this);
        }

        public LandropePayContext(string url, string database)
            : base(url, database)
        {
            InitColSets<LandropePayContext>();
            MongoEntities.PutDefault<LandropePayContext>(this);
        }

        public static LandropePayContext current;
        public CollSet<Bayar> bayars;
        public CollSet<Sertifikasi> sertifikasis;
        public CollSet<Pajak> pajaks;
        public CollSet<sertifikasidate> sertifikasidates;
        public CollSet<pajakdate> pajakdates;
        public CollSet<FUMarketing> fuMarketing;
        public CollSet<BidangFollowUp> bidangFollowUp;
        public CollSet<FollowUpDocument> fuDocs;
        public CollSet<DocSettings> docSettings;
        public CollSet<StateRequest> stateRequests;
        public CollSet<MapRequest> mapRequest;
        public CollSet<LogWorklist> logWorklist;
        public CollSet<PersilRequest> persilApproval;
        public CollSet<EnProsesRequest> enprosesRequest;
        public CollSet<ProjectRequest> projectRequests;
        public CollSet<LandSplitRequest> landsplitRequests;

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
