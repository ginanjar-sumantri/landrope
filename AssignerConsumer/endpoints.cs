//using Microsoft.Extensions.DependencyInjection;
using auth.mod;
//using landrope.engines;
using flow.common;
using GenWorkflow;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using assigner.bridge;
using Grpc.Core;
using Grpc.Net.Client;
using HttpAccessor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using mongospace;
using protobsonser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using landrope.mod3;
using landrope.consumers;
using landrope.documents;
using landrope.mod3.shared;
using APIGrid;

//using landrope.bundhost;

namespace AssignerConsumer
{
	public class AssignerHostConsumer : IAssignerHostConsumer, IDisposable
	{
		string addr = "https://localhost:17882";
		//authEntities acontext;
		GrpcChannel channel = null;

		GrpcChannel MakeChannel()
		{
			var httpHandler = new HttpClientHandler();
			httpHandler.ServerCertificateCustomValidationCallback =
					HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
			return GrpcChannel.ForAddress(addr, new GrpcChannelOptions { HttpHandler = httpHandler, MaxReceiveMessageSize = null });
		}

		public AssignerHostConsumer()
		{
			AppContext.SetSwitch(
					"System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

			var appsets = Config.AppSettings;
			if (appsets == null)
				throw new InvalidOperationException("Unable to retrieve assigner host connection informations");

			if (!appsets.TryGet("assigner_grpc:addr", out addr))
				throw new InvalidOperationException("Unable to retrieve assigner host connection informations");
			channel = MakeChannel();
		}

		public AssignerHostConsumer(string address)
		{
			this.addr = address;
			channel = MakeChannel();
		}

		async public Task<List<IAssignment>> OpenedAssignments(string[] keys)
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				var req = new ListValue();
				if (keys == null)
					keys = new[] { "*" };

				req.Values.AddRange(keys.Select(s => new Value { StringValue = s }));

				//req.Values.AddRange(keys.Select(s => new StringValue { Value = s }).Cast<Value>());
				var resp = await an.OpenedAssignmentsAsync(req);
				return resp.BsonDeserializeBV<List<Assignment>>().Cast<IAssignment>().ToList();
			}
			catch (Exception ex)
			{
				return await Task.FromException<List<IAssignment>>(ex);
			}
		}

		async public Task<(string key, landrope.common.DocProcessStep step)[]> AssignedPersils()
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				var resp = await an.AssignedPersilsAsync(new Empty());
				var ret = resp.Items.Select(r => (r.Key, (landrope.common.DocProcessStep)(int)r.Step)).ToArray();
				return ret;
			}
			catch (Exception ex)
			{
				return await Task.FromException<(string key, landrope.common.DocProcessStep step)[]>(ex);
			}
		}

		async public Task Delete(string key)
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				await an.DeleteAsync(new StringValue { Value=key});
			}
			catch (Exception ex)
			{
				await Task.FromException(ex);
			}
		}

		async public Task Add(IAssignment assg)
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				await an.AddAsync(((Assignment)assg).BsonSerializeBV());
			}
			catch (Exception ex)
			{
				await Task.FromException(ex);
			}
		}

		async public Task<List<IAssignment>> AssignmentList(string key)
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				var resp = await an.AssignmentListAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<List<Assignment>>().Cast<IAssignment>().ToList();
			}
			catch (Exception ex)
			{
				return await Task.FromException<List<IAssignment>>(ex);
			}
		}

		async public Task<IAssignment> GetAssignment(string key)
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				var resp = await an.GetAssignmentAsync(new StringValue { Value = key });
				return resp.BsonDeserializeBV<Assignment>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<IAssignment>(ex);
			}
		}

		async public Task<IAssignment> GetAssignmentOfDtl(string dtlkey)
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				var resp = await an.GetAssignmentofDtlAsync(new StringValue { Value = dtlkey });
				return resp.BsonDeserializeBV<Assignment>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<IAssignment>(ex);
			}
		}


		async public Task<bool> Update(string akey)
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				var resp = await an.UpdateAsync(new StringValue { Value = akey });
				return resp.Value;
			}
			catch (Exception ex)
			{
				return await Task.FromException<bool>(ex);
			}
		}

		async public Task<bool> Update(IAssignment assg, bool save = true)
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				var req = new UpdateRequest { Assg = ((Assignment)assg).BsonSerializeBS(), Save = save };
				var resp = await an.UpdateExAsync(req);
				return resp.Value;
			}
			catch (Exception ex)
			{
				return await Task.FromException<bool>(ex);
			}
		}

		async public Task<Dictionary<string,object>> ListAssignmentViews(string userkey, AgGridSettings gs)
		{
			try
			{
				var an = new Assigner.AssignerClient(channel);
				var req = new GAVRequest { Userkey=userkey,Gs=gs.BsonSerializeBS() };
				var resp = await an.ListAssignmentViewsAsync(req);
				return resp.Value.BsonDeserializeBS<Dictionary<string, object>>();
			}
			catch (Exception ex)
			{
				return await Task.FromException<Dictionary<string, object>>(ex);
			}
		}

		public void Dispose()
		{
			if (channel != null)
				channel.ShutdownAsync().ContinueWith(t => channel.Dispose());
		}

		async public void AssignReload()
        {
            try
            {
				var an = new Assigner.AssignerClient(channel);
				await an.AssignReloadAsync(new Empty());
            }
            catch (Exception)
            {

                throw;
            }
        }
	}
}

