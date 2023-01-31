using auth.mod;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using HttpAccessor;
using Microsoft.Extensions.Configuration;
using mongospace;
using protobsonser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using bundler.bridge;
using landrope.common;
using landrope.mod2;
using landrope.engines;
using landrope.documents;
using landrope.mod3;
using landrope.consumers;

namespace BundlerConsumer
{
	public class BundlerHostConsumer : IBundlerHostConsumer,IDisposable
	{
		string addr = "https://localhost:17881";
		//authEntities acontext;
		GrpcChannel channel = null;

		GrpcChannel MakeChannel()
		{
			var httpHandler = new HttpClientHandler();
			httpHandler.ServerCertificateCustomValidationCallback =
					HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
			return GrpcChannel.ForAddress(addr, new GrpcChannelOptions { HttpHandler = httpHandler, MaxReceiveMessageSize = null });
		}

		public BundlerHostConsumer()
		{
			AppContext.SetSwitch(
					"System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

			var appsets = Config.AppSettings;
			if (appsets == null)
				throw new InvalidOperationException("Unable to retrieve Bundler host connection informations");

			if (!appsets.TryGet("Bundler_grpc:addr", out addr))
				throw new InvalidOperationException("Unable to retrieve Bundler host connection informations");
			channel = MakeChannel();
			//LoadData();
		}

		public BundlerHostConsumer(string address)
		{
			this.addr = address;
			channel = MakeChannel();
		}

        async public Task LoadData()
        {
            try
            {
                var bn = new Bundler.BundlerClient(channel);
                await bn.LoadDataAsync(new Empty());
            }
            catch (Exception ex)
            {
                await Task.FromException(ex);
            }
        }

        async public Task<IMainBundle> MainGet(string key)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.MainGetAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<MainBundle>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<MainBundle>(ex);
			}
		}

		async public Task<ITaskBundle> TaskGet(string key)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.TaskGetAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<TaskBundle>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<TaskBundle>(ex);
			}
		}

		async public Task<IPreBundle> PreBundleGet(string key)
        {
            try
            {
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.PreBundleGetAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<PreBundle>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<PreBundle>(ex);
			}
		}

		async public Task<List<ITaskBundle>> ChildrenList(string key)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.ChildrenListAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<List<TaskBundle>>().Cast<ITaskBundle>().ToList();
			}
			catch (Exception ex)
			{
				return await Task.FromException<List<ITaskBundle>>(ex);
			}
		}

		async public Task<ITaskBundle[]> TaskList(string akey)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.TaskListAsync(new StringValue { Value = akey });
				return resp.BsonDeserializeBV<TaskBundle[]>().Cast<ITaskBundle>().ToArray();
			}
			catch (Exception ex)
			{
				return await Task.FromException<ITaskBundle[]>(ex);
			}
		}

		async public Task<DocEx[]> Available(string key)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.AvailableAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<DocEx[]>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<DocEx[]>(ex);
			}
		}

		async public Task<DocEx[]> Reserved(string key)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.ReservedAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<DocEx[]>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<DocEx[]>(ex);
			}
		}

		async public Task<DocEx[]> Current(string key)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.CurrentAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<DocEx[]>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<DocEx[]>(ex);
			}
		}

		async public Task<(ITaskBundle bundle, string reason)> MakeTaskBundle(string token, string asgdtlkey, bool save = true)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new MTBRequest { Token = token, Asgdtlkey = asgdtlkey, Save = save };
				var resp = await bn.MakeTaskBundleAsync(req);
				return (resp.Bundle.BsonDeserializeBS<TaskBundle>(), resp.Reason);
			}
			catch (Exception ex)
			{
				return await Task.FromException<(TaskBundle bundle, string reason)>(ex);
			}
		}

		async public Task<(ITaskBundle bundle, string reason)> DoMakeTaskBundle(string token, IAssignment asgn, IAssignmentDtl asgdtl, IMainBundle bundle, user user, bool save = true)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new DMTBRequest { Token = token, AsgnKey = ((Assignment)asgn).key, DtlKey = ((AssignmentDtl)asgdtl).key, 
					/*BundleKey = ((MainBundle)bundle).key, */UserKey = user.key, Save = save };
				var resp = await bn.DoMakeTaskBundleAsync(req);
				return (resp.Bundle.BsonDeserializeBS<TaskBundle>(), resp.Reason);
			}
			catch (Exception ex)
			{
				return await Task.FromException<(TaskBundle bundle, string reason)>(ex);
			}
		}

		async public Task Realize(string token, string asgnkey)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				await bn.RealizeAsync(new RealizeRequest { Token = token, AsgnKey = asgnkey });
			}
			catch (Exception ex)
			{
				await Task.FromException(ex);
			}
		}

		async public Task AddProcessRequirement(string token, string key, landrope.common.DocProcessStep step, string keyDocType, KeyValuePair<string, Dynamic> prop, landrope.common.Existence ex)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new AddPRRequest
				{
					Token = token,
					Key = key,
					Step = (bundler.bridge.DocProcessStep)(int)step,
					KeyDocType = keyDocType,
					Ex = (bundler.bridge.Existence)(int)ex,
					Prop = new AddPRRequest.Types.KVP_SD
					{
						Key = prop.Key,
						Value = new Dynamic_ { Type = (Dynamic_.Types.ValueType)(int)(ValueType)prop.Value.type, Val = prop.Value.val }
					}
				};
				await bn.AddProcessRequirementAsync(req);
			}
			catch (Exception exx)
			{
				await Task.FromException(exx);
			}
		}

		async public Task<IEnumerable<PersilNxDiscrete>> NextReadiesDiscrete(bool loose = false, bool inclActive = false)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new NRDRequest { Loose = loose, InclActive = inclActive };
				var resp = await bn.NextReadiesDiscreteAsync(req);
				return resp.BsonDeserializeBV<PersilNxDiscrete[]>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<PersilNxDiscrete[]>(ex);
			}
		}

		async public Task<IEnumerable<DocProp>> ListProps(string key)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.ListPropsAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<DocProp[]>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<DocProp[]>(ex);
			}
		}

		async public Task<bool> MainUpdate(IMainBundle bundle, bool dbSave = true)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new UpdateRequest { Bundle = ((MainBundle)bundle).BsonSerializeBS(), DbSave = dbSave };
				var resp = await bn.MainUpdateAsync(req);
				return resp.Value;
			}
			catch (Exception ex)
			{
				return await Task.FromException<bool>(ex);
			}
		}

		async public void BundleReload(string key)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new StringValue { Value = key };
				await bn.BundleReloadAsync(req);
			}
			catch (Exception ex)
			{
				await Task.FromException(ex); 
			}
		}

		async public Task<(bool OK, string error)> ReloadData()
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.ReloadDataAsync(new Empty());
				return (resp.OK, resp.Error);
			}
			catch (Exception ex)
			{
				return await Task.FromException<(bool, string)>(ex);
			}
		}

		async public void PreBundleReload(string key)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new StringValue { Value = key };
				await bn.PreBundleReloadAsync(req);
			}
			catch (Exception ex)
			{
				await Task.FromException(ex);
			}
		}

		async public Task<bool> TaskUpdate(ITaskBundle bundle, bool dbSave = true)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new UpdateRequest { Bundle = ((TaskBundle)bundle).BsonSerializeBS(), DbSave = dbSave };
				var resp = await bn.TaskUpdateAsync(req);
				return resp.Value;
			}
			catch (Exception ex)
			{
				return await Task.FromException<bool>(ex);
			}
		}

		async public Task<bool> TaskUpdateEx(string key, bool dbSave = true)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new ChangeRequest { Key = key, DbSave = dbSave };
				var resp = await bn.TaskUpdateExAsync(req);
				return resp.Value;
			}
			catch (Exception ex)
			{
				return await Task.FromException<bool>(ex);
			}
		}

		async public Task<bool> PreUpdate(IPreBundle preBundle, bool dbSave = true)
        {
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new UpdateRequest { Bundle = ((PreBundle)preBundle).BsonSerializeBS(), DbSave = dbSave };
				var resp = await bn.PreUpdateAsync(req);
				return resp.Value;
			}
			catch (Exception ex)
			{
				return await Task.FromException<bool>(ex);
			}
		}

		async public Task<bool> MainDelete(string key, bool dbSave = true)
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new ChangeRequest { Key = key, DbSave = dbSave };
				var resp = await bn.MainDeleteAsync(req);
				return resp.Value;
			}
			catch (Exception ex)
			{
				return await Task.FromException<bool>(ex);
			}
		}

		async public Task<bool> PreDelete(string key, bool dbSave = true)
        {
            try
            {
				var bn = new Bundler.BundlerClient(channel);
				var req = new ChangeRequest { Key = key, DbSave = dbSave };
				var resp = await bn.PreDeleteAsync(req);
				return resp.Value;
            }
			catch(Exception ex)
            {
				return await Task.FromException<bool>(ex);
            }
        }

		async public Task<List<PersilPositionView>> SavedPersilPosition(bool inclActive = false) 
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.SavedPersilPositionAsync(new BoolValue { Value=inclActive});
				return resp.BsonDeserializeBV<List<PersilPositionView>>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<List<PersilPositionView>>(ex);
			}
		}

		async public Task<List<PersilNxDiscrete>> SavedPersilNext() 
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.SavedPersilNextAsync(new Empty());
				return resp.BsonDeserializeBV<List<PersilNxDiscrete>>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<List<PersilNxDiscrete>>(ex);
			}
		}

		async public Task<List<PersilPositionWithNext>> SavedPositionWithNext(string[] prokeys = null) 
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var req = new RequestKeys();
				req.Keys.AddRange(prokeys);
				var resp = await bn.SavedPositionWithNextAsync(req);
				return resp.BsonDeserializeBV<List<PersilPositionWithNext>>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<List<PersilPositionWithNext>>(ex);
			}
		}

		async public Task<(bool OK, string error)> SaveLastPositionDiscete() 
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.SaveLastPositionDisceteAsync(new Empty());
				return (resp.OK,resp.Error);
			}
			catch (Exception ex)
			{
				return await Task.FromException<(bool,string)>(ex);
			}
		}

		async public Task<(bool OK, string error)> SaveNextStepDiscrete() 
		{
			try
			{
				var bn = new Bundler.BundlerClient(channel);
				var resp = await bn.SaveNextStepDiscreteAsync(new Empty());
				return (resp.OK, resp.Error);
			}
			catch (Exception ex)
			{
				return await Task.FromException<(bool, string)>(ex);
			}
		}

		public void Dispose()
		{
			if (channel != null)
				channel.ShutdownAsync().ContinueWith(t => channel.Dispose());
		}
	}
}

