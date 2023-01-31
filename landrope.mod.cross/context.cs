using auth.mod;
using landrope.mod2;
using landrope.mod3;
using Microsoft.Extensions.Configuration;
using mongospace;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace landrope.mod.cross
{
    public class LandropeCrossContext : authEntities
    {
        static LandropeCrossContext()
        {
            Registrar.Register(MethodBase.GetCurrentMethod());
        }

        public LandropeCrossContext() : base()
        {
            InitColSets<LandropeCrossContext>();
            MongoEntities.PutDefault<LandropeCrossContext>(this);
        }

        public LandropeCrossContext(IConfigurationRoot configuration) : base(configuration)
        {
            InitColSets<LandropeCrossContext>();
            MongoEntities.PutDefault<LandropeCrossContext>(this);
        }

        public LandropeCrossContext(string url, string database) : base(url, database)
        {
            InitColSets<LandropeCrossContext>();
            MongoEntities.PutDefault<LandropeCrossContext>(this);
        }

        public override string ConfigRoot => "cross";

        public CollSet<MainProject> mainprojects;

        public CollSet<MainBundle> mainBundles;

        public CollSet<LogBundle> logBundles;
    }
}
