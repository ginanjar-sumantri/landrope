using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tracer;

namespace landrope.api2.Controllers
{
	internal static class DbSwitcher
	{
		public static void SwitchDB(this MongoEntities context, bool cutoff, int appno = 1)
		{
			MyTracer.TraceInfo2($"cutoff:{cutoff}, appno:{appno}");

			var dbname = (cutoff, appno) switch
			{
				(true, 1) => "landrope_cutoff",
				(true, 2) => "landrope_new_cutoff",
				(false, 2) => "landrope_new",
				_ => "landrope"
			};
			if (dbname != context.db.DatabaseNamespace.DatabaseName)
				context.ChangeDB(dbname);
		}

	}
}
