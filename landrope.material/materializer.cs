using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using landrope.mod;
using landrope.mod2;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Tracer;

namespace landrope.material
{
	public static class MaterializerHelper
	{
		async static Task AggregateAsync(this IMongoDatabase db, string coll, string[] stages)
		{
			await db.GetCollection<BsonDocument>(coll).AggregateAsync(
					PipelineDefinition<BsonDocument, BsonDocument>.Create(stages)
				);
		}

		static void Aggregate(this IMongoDatabase db, string coll, string[] stages)
		{
			db.GetCollection<BsonDocument>(coll).Aggregate(
					PipelineDefinition<BsonDocument, BsonDocument>.Create(stages)
				);
		}

		static string[] MakeStages(string dest, bool merge = true, string key = "key") =>
			merge ? new[] {
							"{$project:{_id:0}}",
							$"{{$merge:{{into:'{dest}',on:'{key}',whenMatched:'replace',whenNotMatched:'insert'}}}}"
				} :
			new[] {
							"{$project:{_id:0}}",
							$"{{$out:'{dest}'}}"
				};

		static string[] MakeStages(string dest, string keyvalue, bool merge = true, string key = "key")
		{
			try
			{
				var multi = keyvalue.Contains(",");
				if (multi)
				{
					keyvalue = $"[{string.Join(',', keyvalue.Split(",").Select(s => $"'{s}'"))}]";
				}

				var st = multi ? $"{key}:<$in:{keyvalue}>".ToJsonFilter() : $"{key}: '{keyvalue}'";

				return merge ? new[] {
							$"<$match:<{st}>>".ToJsonFilter(),
							"{$project:{_id:0}}",
							$"<$merge:<into:'{dest}',on:'{key}',whenMatched:'replace',whenNotMatched:'insert'>>".ToJsonFilter()
				} :
				new[] {
							$"<$match:<{st}>>".ToJsonFilter(),
							"{$project:{_id:0}}",
							$"{{$out:'{dest}'}}"
					};
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				throw;
			}
		}

		async public static Task ImmaterializeAsync(this LandropeContext context, string coll, string value, string key = "key")
		{
			await context.db.GetCollection<BsonDocument>(coll).FindOneAndDeleteAsync($"{{'{key}':'{value}'}}");
		}

		public static void Immaterialize(this LandropeContext context, string coll, string value, string key = "key")
		{
			context.db.GetCollection<BsonDocument>(coll).FindOneAndDelete($"{{'{key}':'{value}'}}");
		}

		async public static Task ImmaterializeAsync(this ExtLandropeContext context, string coll, string value, string key = "key")
		{
			await context.db.GetCollection<BsonDocument>(coll).FindOneAndDeleteAsync($"{{'{key}':'{value}'}}");
		}

		public static void Immaterialize(this ExtLandropeContext context, string coll, string value, string key = "key")
		{
			context.db.GetCollection<BsonDocument>(coll).FindOneAndDelete($"{{'{key}':'{value}'}}");
		}

		async public static Task MaterializeAsync(this LandropeContext context, string from, string to, bool merge = true, string key ="key")
		{
			var stages = MakeStages(to,merge,key);
			await context.db.AggregateAsync(from,stages);
		}

		public static void Materialize(this LandropeContext context, string from, string to, bool merge = true, string key ="key")
		{
			var stages = MakeStages(to, merge, key);
			context.db.Aggregate(from, stages);
		}

		async public static Task MaterializeAsync(this ExtLandropeContext context, string from, string to, bool merge = true, string key ="key")
		{
			var stages = MakeStages(to, merge, key);
			await context.db.AggregateAsync(from, stages);
		}

		public static void Materialize(this ExtLandropeContext context, string from, string to, bool merge = true, string key ="key")
		{
			var stages = MakeStages(to, merge, key);
			context.db.Aggregate(from, stages);
		}

		public static void Materialize(this ExtLandropeContext context, string from, string to, string keyvalue, bool merge = true, string key = "key")
		{
			try
			{
				var stages = MakeStages(to, keyvalue, merge, key);
				MyTracer.TraceInfo2($"Materialize stages:{stages}");
				context.db.Aggregate(from, stages);
				MyTracer.TraceInfo2($"Materialize Done");
			}
			catch(Exception ex)
			{
				MyTracer.TraceError2(ex);
				throw;
			}
		}

		async public static Task MaterializeAsync(this ExtLandropeContext context, string from, string to, string keyvalue, bool merge = true, string key = "key")
		{
			var stages = MakeStages(to, keyvalue, merge, key);
			MyTracer.TraceInfo2($"Materialize stages:{stages}");
			await context.db.AggregateAsync(from, stages);
			MyTracer.TraceInfo2($"Materialize Done");
		}

		public static string GetValue(this HttpRequest req, string keyname)
		{
			if (req.Query !=null && req.Query.Any(q => q.Key == keyname))
				return req.Query[keyname];
			if (req.Form!=null && req.Form.Any(q => q.Key == keyname))
				return req.Form[keyname];
			return null;
		}
	}

	internal static class Helper
	{
		public static string ToJsonFilter(this string src)
			=> src.Replace("<", "{").Replace(">", "}");
	}
}
