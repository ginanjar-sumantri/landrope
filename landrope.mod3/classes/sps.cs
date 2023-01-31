using landrope.common;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.mod3
{

	public class SPS
	{
		public string keyPersil { get; set; }
		public DocProcessStep step { get; set; }
		public DateTime date { get; set; }
		public string nomor { get; set; }
		public double? amount { get; set; }
		public DateTime? paid { get; set; }
	}

	public static class SPSHelper
	{
		public static List<SPS> AllSPS(this LandropePlusContext ctx)
			=> ctx.GetCollections(new SPS(), "sps", "{}", "{_id:0}").ToList();

		public static List<SPS> FindSPS(this LandropePlusContext ctx, params string[] keys)
		{
			if (keys == null || !keys.Any())
				return ctx.AllSPS();
			var stkeys = string.Join(",", keys.Select(k => $"'{k}'"));
			return ctx.GetCollections(new SPS(), "sps", $"{{keyPersil:{{$in:[{stkeys}]}}}}", "{_id:0}").ToList();
		}

		public static void AddSPS(this LandropePlusContext ctx, SPS sps)
		{
			ctx.db.GetCollection<BsonDocument>("sps").InsertOne(sps.ToBsonDocument());
		}
	}
}
