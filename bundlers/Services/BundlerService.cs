using auth.mod;
using flow.common;
//using GenWorkflow;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
//using GraphSerialize;
//using graph.mid;
using bundler.bridge;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using mongospace;
using protobsonser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracer;
using landrope.mod3;
using bundle.host;

namespace bundlers
{
	public class BundlerService : Bundler.BundlerBase
	{
		readonly ILogger<BundlerService> _logger;
		IServiceProvider services;
		BundleHost BundleHost => services.GetService<BundleHost>();
		LandropePlusContext context => services.GetService<landrope.mod3.LandropePlusContext>();
		authEntities authcontext => services.GetService<authEntities>();

		public BundlerService(IServiceProvider services, ILogger<BundlerService> logger)
		{
			MyTracer.TraceInfo2("Enter...");

			_logger = logger;
			this.services = services;
			MyTracer.TraceInfo2($"services is null: {this.services == null}");
			if (services != null)
			{
				var sb = new StringBuilder();
				sb.Append($"Constructing WorklowService... Inspecting Dependency Injections:");
				sb.Append($"Inspecting...authEntities: {services.GetService<authEntities>() != null}");
				sb.Append($"Inspecting...authEntities: {services.GetService<BundleHost>() != null}");
				sb.Append($"Inspecting...authEntities: {services.GetService<landrope.mod3.LandropePlusContext>() != null}");
				MyTracer.TraceInfo2(sb.ToString());
			}
			MyTracer.TraceInfo2("Exit...");
		}

        public override Task<Empty> LoadData(Empty request, ServerCallContext context)
        {
            BundleHost.LoadData();
            context.Status = Status.DefaultSuccess;
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> AddProcessRequirement(AddPRRequest request, ServerCallContext context)
		{
			try
			{
				BundleHost.AddProcessRequirement(request.Token, request.Key, (landrope.common.DocProcessStep)(int)request.Step,
										request.KeyDocType,
										new KeyValuePair<string, landrope.common.Dynamic>(request.Prop.Key,
													new landrope.common.Dynamic(
														(landrope.common.Dynamic.ValueType)(int)request.Prop.Value.Type, request.Prop.Value.Val)),
										(landrope.common.Existence)(int)request.Ex);
				return Task.FromResult(new Empty());
			}
			catch(Exception ex)
			{
				return Task.FromException<Empty>(ex);
			}
		}

		public override Task<BytesValue> Available(StringValue request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.Available(request.Value);
				return Task.FromResult<BytesValue>(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BytesValue> ChildrenList(StringValue request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.ChildrenList(request.Value).Cast<TaskBundle>().ToList();
				return Task.FromResult<BytesValue>(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BytesValue> Current(StringValue request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.Current(request.Value);
				return Task.FromResult<BytesValue>(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<MTBReturn> DoMakeTaskBundle(DMTBRequest request, ServerCallContext context)
		{
			try
			{
				var Asgnkey = request.AsgnKey;
				var asgn = this.context.assignments.FirstOrDefault(a => a.key == Asgnkey);
				var asgdtl = asgn.details.FirstOrDefault(d=>d.key==request.DtlKey);
				MainBundle bundle = null;// this.context.mainBundles.FirstOrDefault(b=>b.key==request.BundleKey);
				var user = this.context.users.FirstOrDefault(u => u.key == request.UserKey);
				var resp = BundleHost.DoMakeTaskBundle(asgn,asgdtl,bundle,user,request.Save,request.Token);
				var ret = new MTBReturn { Bundle = resp.bundle.BsonSerializeBS(), Reason = resp.reason };
				return Task.FromResult(ret);
			}
			catch (Exception ex)
			{
				return Task.FromException<MTBReturn>(ex);
			}
		}

		public override Task<BytesValue> ListProps(StringValue request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.ListProps(request.Value).ToArray();
				return Task.FromResult<BytesValue>(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BoolValue> MainDelete(ChangeRequest request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.MainDelete(request.Key,request.DbSave);
				return Task.FromResult<BoolValue>(new BoolValue{ Value = resp});
			}
			catch (Exception ex)
			{
				return Task.FromException<BoolValue>(ex);
			}
		}

		public override Task<BoolValue> PreDelete(ChangeRequest request, ServerCallContext context)
        {
            try
            {
				var resp = BundleHost.PreDelete(request.Key, request.DbSave);
				return Task.FromResult<BoolValue>(new BoolValue { Value = resp });
			}
            catch (Exception ex)
            {
				return Task.FromException<BoolValue>(ex);
			}
        }

		public override Task<BytesValue> MainGet(StringValue request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.MainGet(request.Value);
				return Task.FromResult<BytesValue>(((MainBundle)resp).BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<Empty> BundleReload(StringValue request, ServerCallContext context)
		{
			try
			{
				BundleHost.BundleReload(request.Value);
				return Task.FromResult<Empty>(new Empty());
			}
			catch (Exception ex)
			{
				return Task.FromException<Empty>(ex);
			}
		}

		public override Task<TupleBS> ReloadData(Empty request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.ReloadData();
				var ret = new TupleBS { OK = resp.OK, Error = resp.error ?? String.Empty };
				return Task.FromResult<TupleBS>(ret);
			}
			catch (Exception ex)
			{
				return Task.FromException<TupleBS>(ex);
			}
		}
		public override Task<Empty> PreBundleReload(StringValue request, ServerCallContext context)
		{
			try
			{
				BundleHost.PreBundleReload(request.Value);
				return Task.FromResult<Empty>(new Empty());
			}
			catch (Exception ex)
			{
				return Task.FromException<Empty>(ex);
			}
		}

		public override Task<BytesValue> PreBundleGet(StringValue request, ServerCallContext context)
        {
            try
            {
				var resp = BundleHost.PreBundleGet(request.Value);
				return Task.FromResult<BytesValue>(((PreBundle)resp).BsonSerializeBV());
            }
            catch(Exception ex)
            {
				return Task.FromException<BytesValue>(ex);
            }
        }

		public override Task<BoolValue> MainUpdate(UpdateRequest request, ServerCallContext context)
		{ 
			try
			{
				var resp = BundleHost.MainUpdate(request.Bundle.BsonDeserializeBS<MainBundle>(), request.DbSave);
				return Task.FromResult<BoolValue>(new BoolValue { Value = resp });
			}
			catch (Exception ex)
			{
				return Task.FromException<BoolValue>(ex);
			}
		}

		public override Task<BoolValue> PreUpdate(UpdateRequest request, ServerCallContext context)
        {
			try
			{
				var resp = BundleHost.PreUpdate(request.Bundle.BsonDeserializeBS<PreBundle>(), request.DbSave);
				return Task.FromResult<BoolValue>(new BoolValue { Value = resp });
			}
			catch (Exception ex)
			{
				return Task.FromException<BoolValue>(ex);
			}
		}


		public override Task<MTBReturn> MakeTaskBundle(MTBRequest request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.MakeTaskBundle(request.Token, request.Asgdtlkey, request.Save);
				var ret = new MTBReturn { Bundle=((TaskBundle)resp.bundle).BsonSerializeBS(),Reason=resp.reason};
				return Task.FromResult<MTBReturn>(ret);
			}
			catch (Exception ex)
			{
				return Task.FromException<MTBReturn>(ex);
			}
		}

		public override Task<BytesValue> NextReadiesDiscrete(NRDRequest request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.NextReadiesDiscrete(request.Loose,request.InclActive).ToArray();
				return Task.FromResult<BytesValue>(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<Empty> Realize(RealizeRequest request, ServerCallContext context)
		{
			try
			{
				BundleHost.Realize(request.Token, request.AsgnKey);
				return Task.FromResult<Empty>(new Empty());
			}
			catch (Exception ex)
			{
				return Task.FromException<Empty>(ex);
			}
		}

		public override Task<BytesValue> Reserved(StringValue request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.Reserved(request.Value);
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BytesValue> SavedPersilNext(Empty request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.SavedPersilNext();
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BytesValue> SavedPersilPosition(BoolValue request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.SavedPersilPosition(request.Value);
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BytesValue> SavedPositionWithNext(RequestKeys request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.SavedPositionWithNext(request.Keys.ToArray());
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<TupleBS> SaveLastPositionDiscete(Empty request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.SaveLastPositionDiscete();
				var ret = new TupleBS { OK = resp.OK, Error = resp.error };
				return Task.FromResult(ret);
			}
			catch (Exception ex)
			{
				return Task.FromException<TupleBS>(ex);
			}
		}

		public override Task<TupleBS> SaveNextStepDiscrete(Empty request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.SaveNextStepDiscrete();
				var ret = new TupleBS { OK = resp.OK, Error = resp.error?? "" };
				return Task.FromResult(ret);
			}
			catch (Exception ex)
			{
				return Task.FromException<TupleBS>(ex);
			}
		}

		public override Task<BytesValue> TaskGet(StringValue request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.TaskGet(request.Value);
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BytesValue> TaskList(StringValue request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.TaskList(request.Value);
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BoolValue> TaskUpdate(UpdateRequest request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.TaskUpdate(request.Bundle.BsonDeserializeBS<TaskBundle>(), request.DbSave);
				return Task.FromResult<BoolValue>(new BoolValue { Value = resp });
			}
			catch (Exception ex)
			{
				return Task.FromException<BoolValue>(ex);
			}
		}

		public override Task<BoolValue> TaskUpdateEx(ChangeRequest request, ServerCallContext context)
		{
			try
			{
				var resp = BundleHost.TaskUpdateEx(request.Key, request.DbSave);
				return Task.FromResult<BoolValue>(new BoolValue { Value = resp });
			}
			catch (Exception ex)
			{
				return Task.FromException<BoolValue>(ex);
			}
		}
	}
}
