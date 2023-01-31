using landrope.mod2;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace idmaker
{
	class Program
	{
		static void Main(string[] args)
		{
			var connst = "mongodb://sa:M0ng0DB4dmin@10.10.1.80:27017/admin";
			var context = new ExtLandropeContext(connst, "landrope");
			var allpersils = context.persils.Query(p => p.IdBidang==null && p.basic != null && 
															p.basic.current != null && p.basic.current.keyProject!=null && p.basic.current.keyDesa != null);
			allpersils.ForEach(p => p.MakeID());
			context.persils.Update(allpersils);
			context.SaveChanges();
			//var IDs = context.GetCollections(new { key = "" }, "persils_ID", "{}", "{_id:0,key:1}").ToList()
			//				.Select(i=>i.key).ToList();
			//var incs = allpersils.Select(p => p.key).Except(IDs);
			//var persils = allpersils.Join(incs, p => p.key, i => i, (p, i) => p).ToList();

			//var grp = persils.GroupBy(p => (p: p.basic.current.keyProject, d: p.basic.current.keyDesa)).ToList();
			//var lst = new List<ID>();
			//grp.ForEach(pd =>
			//{
			//	pd.ToList().ForEach(p =>
			//	{
			//		var Id = Persil.MakeID(context, pd.Key.p, pd.Key.d);
			//		Console.ForegroundColor = Id == null? ConsoleColor.Red : ConsoleColor.White;
			//		Console.WriteLine($"key:{p.key}, project:{pd.Key.p}, desa={pd.Key.d}, Id={Id}");
			//		lst.Add(new ID { key = p.key, IdBidang = Id });
			//	});
			//	//Console.Write("Press any key to continue...");
			//	//Console.ReadKey();
			//});
			//context.db.GetCollection<ID>("persils_ID").InsertMany(lst);
		}
	}

	public class ID
	{
		public string key { get; set; }
		public string IdBidang { get; set; }
	}
}
