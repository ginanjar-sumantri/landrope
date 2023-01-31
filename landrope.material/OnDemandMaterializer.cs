using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using landrope.mod;
using landrope.mod3;
using System.Runtime.InteropServices;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using landrope.mod2;
using System.Reflection;
using Tracer;
using Microsoft.AspNetCore.Http;

namespace landrope.material
{
	public class MaterializerAttribute : ActionFilterAttribute//, IAlwaysRunResultFilter
	{
		protected string src;
		protected string dst;
		protected string pre;
		protected string DstKey = "key";
		public string PreKey = "key";
		public bool Auto = true;
		public bool Async = false;
		string PreValue = null;

		public void SetPreValue(string value)
		{
			var str = $"{PreKey}={value}";
			var qryS = HttpAccessor.Helper.HttpContext.Request.QueryString.Value;
			
			if (string.IsNullOrWhiteSpace(qryS))
				qryS = "?" + str;
			else
				qryS += "&" + str;

			HttpAccessor.Helper.HttpContext.Request.QueryString = new QueryString(qryS);
			//PreValue = value;
			//Auto = true;
		}

		public static object KEY = new { name = "Key for Lock" };

		public virtual void ManualExecute(ExtLandropeContext lacontext, string stval, bool Async = false)
		{
			if (Auto || stval == null)
				return;
			if (Async)
				Task.Run(() => DoExecute());
			else
				DoExecute();

			void DoExecute()
			{
#if _USE_MATCH_
				Execute(lacontext, stval);
#else
				lock (KEY)
				{
					var stages = MakePreStages(stval);
					MyTracer.TraceInfo2($"stages:{string.Join(',', stages)}");
					Execute(lacontext, stages);
				}
#endif
			}
		}

		protected virtual string[] MakePreStages(string stvalue)
		{
			return new string[0];
		}

		protected virtual void Execute(ExtLandropeContext context, params string[] prestages)
		{
			if (pre != null && prestages.Any())
				context.db.GetCollection<BsonDocument>(pre).Aggregate<BsonDocument>(
					PipelineDefinition<BsonDocument, BsonDocument>.Create(prestages));
			try
			{
				context.Materialize(src, dst, true, DstKey);
			}
			catch(Exception ex)
			{
				MyTracer.TraceError2(ex);
			}
		}

		protected virtual void Execute(ExtLandropeContext context, string keyvalue)
		{
			try
			{
				if (src.Contains(",") && dst.Contains(","))
				{
					var srcs = src.Split(",");
					var dsts = dst.Split(",");
					if (srcs.Length != dsts.Length)
						throw new InvalidOperationException("Destination collections must be same count with source views");
					var tasks = new List<Task>();
					for(int i=0;i<srcs.Length;i++)
						tasks.Add(context.MaterializeAsync(srcs[i], dsts[i], keyvalue, true, DstKey));
					Task.WaitAll(tasks.ToArray());
				}
				else
					context.Materialize(src, dst, keyvalue, true, DstKey);
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
			}
		}

		//public void ForceValue(HttpContext hcontext, string value)
		//{
		//	hcontext.Request.Form.Append(new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>(PreKey, value));
		//}

		public override void OnActionExecuted(ActionExecutedContext context)
		{
			//if (!Auto)
			//	return;
			var lacontext = context.HttpContext.RequestServices.GetService<ExtLandropeContext>();
			var stval = PreValue ?? TryGetPreValue();
			if (stval != null && context.Result is OkResult)
			{
				if (Async)
					Task.Run(() => DoExecute());
				else
					DoExecute();
			}

			void DoExecute()
			{
#if _USE_MATCH_
				Execute(lacontext, stval);
#else
				lock (KEY)
				{
					var stages = MakePreStages(stval);
					Execute(lacontext, stages);
				}
#endif
			}
			string TryGetPreValue()
			{
				try
				{
					return context.HttpContext.Request.GetValue(PreKey);
				}
				catch(Exception)
				{
					return null;
				}
		}

		}
	}

	public class PersilMaterializerAttribute : MaterializerAttribute
	{
		public PersilMaterializerAttribute()
		{
#if _USE_MATCH_
			src = "persil_core_m";
#else
			src = "persil_core_m_selected";
#endif
			dst = "material_persil_core";
			DstKey = "key";
			pre = "persils_v2";
		}

		protected override string[] MakePreStages(string stvalue)
		{
			var sts = stvalue == null ? new string[0] : new string[]
			{
				$"{{$match:{{key:'{stvalue}'}}}}",
				"{$project:{key:1}}",
				"{$out:'persil_selected'}"
			};
			return sts;
		}
	}

	public class MapMaterializerAttribute : MaterializerAttribute
	{
		public MapMaterializerAttribute()
		{
#if _USE_MATCH_
			src = "persil_map";
#else
			src = "persil_map_selected";
#endif
			dst = "material_persil_map";
			DstKey = "key";
			pre = "persils_v2";
		}

		protected override string[] MakePreStages(string stvalue)
		{
			var sts = stvalue == null ? new string[0] : new string[]
			{
				$"{{$match:{{key:'{stvalue}'}}}}",
				"{$project:{key:1}}",
				"{$out:'persil_selected'}"
			};
			return sts;
		}
	}

	public class MainBundleMaterializerAttribute : MaterializerAttribute
	{
		public MainBundleMaterializerAttribute()
		{
#if _USE_MATCH_
			src = "main_bundle_core_m";
#else
			src = "main_bundle_core_m_selected";
#endif
			dst = "material_main_bundle_core";
			DstKey = "key";
			pre = "persils_v2";
		}

		protected override string[] MakePreStages(string stvalue)
		{
			var sts = stvalue == null ? new string[0] : new string[]
			{
				$"{{$match:{{key:'{stvalue}'}}}}",
				"{$project:{key:1}}",
				"{$out:'persil_selected'}"
			};
			return sts;
		}
	}

	// src and dst have comma separators
	public class MultiMaterializerAttribute : MaterializerAttribute
	{
		protected override void Execute(ExtLandropeContext context, params string[] prestages)
		{
			var sources = src.Split(",");
			var dests = dst.Split(",");
			if (sources.Length != dests.Length)
				return;
			var couples = sources.Select((s, i) => (s, i)).Join(dests.Select((d, i) => (d, i)), s => s.i, d => d.i, (s, d) => (s.s, d.d));

#if _USE_MATCH_
#else
			if (pre != null && prestages.Any())
				context.db.GetCollection<BsonDocument>(pre).Aggregate<BsonDocument>(
					PipelineDefinition<BsonDocument, BsonDocument>.Create(prestages));
			var tasks = couples.Select(x => context.MaterializeAsync(x.s, x.d, true, DstKey)).ToArray();
			if (!Async)
				Task.WaitAll(tasks);
#endif
		}

		protected override void Execute(ExtLandropeContext context, string keyvalue)
		{
			var sources = src.Split(",");
			var dests = dst.Split(",");
			if (sources.Length != dests.Length)
				return;
			var couples = sources.Select((s, i) => (s, i)).Join(dests.Select((d, i) => (d, i)), s => s.i, d => d.i, (s, d) => (s.s, d.d));

#if _USE_MATCH_
			var tasks = couples.Select(x => context.MaterializeAsync(x.s, x.d, keyvalue, true, DstKey)).ToArray();
			if (!Async)
				Task.WaitAll(tasks);
#else
#endif
		}

	}

	public class PersilCSMaterializerAttribute : MultiMaterializerAttribute
	{
		public PersilCSMaterializerAttribute()
		{
#if _USE_MATCH_
			//src = "persil_core_m,persilMaps_m,persil_position,main_bundle_core_m";
			src = "persil_core_m,persilMaps_m,main_bundle_core_m";
#else
			src = "persil_core_m_selected,persil_map_selected,persil_position_selected,main_bundle_core_m_selected";
#endif
			//dst = "material_persil_core,material_persil_map,material_persil_position,material_main_bundle_core";
			dst = "material_persil_core,material_persil_map,material_main_bundle_core";
			DstKey = "key";
			pre = "persils_v2";
		}

		protected override string[] MakePreStages(string stvalue)
		{
			var sts = stvalue == null ? new string[0] : new string[]
			{
				$"{{$match:{{key:'{stvalue}'}}}}",
				"{$project:{key:1}}",
				"{$out:'persil_selected'}"
			};
			return sts;
		}
	}

	public class PersilCS2MaterializerAttribute : PersilCSMaterializerAttribute
	{
		public PersilCS2MaterializerAttribute()
		{
#if _USE_MATCH_
			//src = "persil_core_m,persilMaps_m,persil_position";
			src = "persil_core_m,persilMaps_m";
#else
			src = "persil_core_m_selected,persil_map_selected,persil_position_selected";
#endif
			//dst = "material_persil_core,material_persil_map,material_persil_position";
			dst = "material_persil_core,material_persil_map";
			DstKey = "key";
			pre = "persils_v2";
		}
	}

	public class AssigmmentMaterializerAttribute : MaterializerAttribute
	{
		public AssigmmentMaterializerAttribute()
		{
#if _USE_MATCH_
			src = "assignment_core_m";
#else
			src = "assignment_core_m_selected";
#endif
			dst = "material_assignment_core";
			DstKey = "key";
			pre = "assignments";
		}

		protected override string[] MakePreStages(string stvalue)
		{
			var sts = stvalue==null? new string[0] : new string[]
			{
				$"{{$match:{{key:'{stvalue}'}}}}",
				"{$project:{key:1}}",
				"{$out:'assign_selected'}"
			};
			return sts;
		}
	}

	public class AssignmentSuppMaterializerAttribute : MultiMaterializerAttribute
	{
		public AssignmentSuppMaterializerAttribute()
		{
#if _USE_MATCH_
			src = "persil_position,main_bundle_core_m";
#else
			src = "persil_position_selected,main_bundle_core_m_selected";
#endif
			dst = "material_persil_position,material_main_bundle_core";
			DstKey = "key";
			pre = "persils_v2";
		}

		protected override string[] MakePreStages(string stvalue)
		{
			var sts = stvalue == null ? new string[0] : new string[]
			{
				$"{{$match:{{key:'{stvalue}'}}}}",
				"{$project:{key:1}}",
				"{$out:'persil_selected'}"
			};
			return sts;
		}
	}

	public static class MethodBaseHelpers
	{
		public static void SetKeyValue<T>(this MethodBase meth, string value) where T: MaterializerAttribute 
		{
			var attrs = meth.GetCustomAttributes<T>();
			if (attrs == null)
				return;
			
			foreach(var attr in attrs)
				attr.SetPreValue(value);
		}
	}
}
