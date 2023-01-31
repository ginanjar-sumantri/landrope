using mongospace;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using landrope.consumers;
using landrope.mod2;
using landrope.common;

namespace landrope.mod3
{
    public static class Helpers
    {
        public static T GetService<T>() where T : class =>
            ContextService.services.GetService<T>();

        public static TImp GetService<T, TImp>() where T : class where TImp : class =>
            ContextService.services.GetService<T>() as TImp;

        public static IAssignerHostConsumer GetAssigner() => GetService<IAssignerHostConsumer>();
        public static IBundlerHostConsumer GetBundler() => GetService<IBundlerHostConsumer>();
        public static IGraphHostConsumer GetGraph() => GetService<IGraphHostConsumer>();

    }

    public static class PersilHelper
    {
        public static double GetLuasBayar(this Persil persil)
        {
            var context = ContextService.services.GetService<LandropePlusContext>();

            var bundle = context.mainBundles.FirstOrDefault(b => b.key == persil.key);

            if (bundle == null)
                return 0;

            //var Doc = bundle.Current().FirstOrDefault(b => b.keyDocType == "JDOK032");

            //if (Doc == null)
            //	return null;

            var doclist = bundle.doclist.FirstOrDefault(b => b.keyDocType == "JDOK032");
            if (doclist == null)
                return 0;

            var entry = doclist.entries.LastOrDefault();
            if (entry == null)
                return 0;

            //var part = Doc.docs.ToArray().FirstOrDefault().Value;
            var part = entry.Item.FirstOrDefault().Value;
            if (part == null)
                return 0;

            var typename = MetadataKey.Luas.ToString("g");
            var dyn = part.props.TryGetValue(typename, out Dynamic val) ? val : null;
            var castvalue = dyn?.Value;

            double result = 0;
            if (castvalue != null)
                result = Convert.ToDouble(castvalue);

            return result;
        }

        public static (string PBT, string NIB) GetNomorPBT(this Persil persil)
        {
            var context = ContextService.services.GetService<LandropePlusContext>();

            var bundle = context.mainBundles.FirstOrDefault(b => b.key == persil.key);

            if (bundle == null)
                return ("", "");

            //var Doc = bundle.Current().FirstOrDefault(b => b.keyDocType == "JDOK032");
            //if (Doc == null)
            //	return null;

            var doclist = bundle.doclist.FirstOrDefault(b => b.keyDocType == "JDOK032");
            if (doclist == null)
                return ("", "");

            var entry = doclist.entries.LastOrDefault();
            if (entry == null)
                return ("", "");

            //var part = Doc.docs.ToArray().FirstOrDefault().Value;

            var part = entry.Item.FirstOrDefault().Value;
            if (part == null)
                return ("", "");

            var typename = MetadataKey.Nomor_PBT.ToString("g");
            var dyn = part.props.TryGetValue(typename, out Dynamic val) ? val : null;
            //return dyn?.Value as string;
            var castvalue = dyn?.Value;

            string result = string.Empty;
            if (castvalue != null)
                result = Convert.ToString(castvalue);

            var typename2 = MetadataKey.Nomor_NIB.ToString("g");
            var dyn2 = part.props.TryGetValue(typename2, out Dynamic val2) ? val2 : null;
            //return dyn?.Value as string;
            var castvalue2 = dyn2?.Value;

            string result2 = string.Empty;
            if (castvalue2 != null)
                result2 = Convert.ToString(castvalue2);

            return (result, result2);
        }
    }

}
