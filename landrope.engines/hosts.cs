using GenWorkflow;
using landrope.common;
using landrope.documents;
// landrope.mod2;
//using landrope.mod3;
using System;
using System.Collections.Generic;

namespace landrope.engines
{
	public interface IBundleHost
	{
		//AssignerHostConsumer GetAssignmentHost();
		//GraphConsumer.GraphHostConsumer GetGraphHost();

		(ITaskBundle bundle, string reason) MakeTaskBundle(string token, string asgdtlkey, bool save=true);

		IMainBundle MainGet(string key);
		bool MainUpdate(IMainBundle bundle, bool dbSave=true);
		bool MainDelete(string key, bool dbSave = true);

		ITaskBundle TaskGet(string key);
		bool TaskUpdate(ITaskBundle bundle, bool dbSave = true);
		bool TaskUpdateEx(string key, bool dbSave = true);

		DocEx[] Available(string key);
		DocEx[] Current(string key);
		DocEx[] Reserved(string key);

/*		bool Reserve(string token, string key, string asgnkey);*/
		void Realize(string token, string asgnkey);

		List<ITaskBundle> ChildrenList(string key);
		void AddProcessRequirement(string token, string key, DocProcessStep step, string keyDocType, KeyValuePair<string, Dynamic> prop, Existence ex);

		//IEnumerable<PersilStat> LastPosition();
		//IEnumerable<PersilNx> NextReadies(bool loose=false);
		//IEnumerable<Persil> NextReadies(string akey, bool loose = false, params string[] exceptkeys);

		IEnumerable<DocProp> ListProps(string key);
		//PersilPositionView[] LastPositionDiscrete(Persil[] persils = null, IAssignment[] assigns = null);
	}

	public interface IAssignmentHost
	{
		//IBundleHost GetBundleHost();
		//GraphConsumer.GraphHostConsumer GetGraphHost();

		List<IAssignment> OpenedAssignments(string[] keys);
		IAssignment GetAssignment(string key);
		List<IAssignment> AssignmentList(string key);
		public bool Update(IAssignment assg, bool dbSave = true);
	}

	public interface IBayarHost
    {
		List<IBayar> OpenedBayar();
		IBayar GetBayar(string key);
	}

	public interface IProsesHost
	{

	}

	public interface IPersilRequestHost
	{
		List<IPersilApproval> OpenReqNewPersil();
		IPersilApproval GetPersilAppByKey(string key);
	}

	public interface IStateRequestHost
	{
		List<IStateRequest> OpenedStateRequest();
		List<IStateRequest> OpenedStateRequest(string[] instkeys);
		IStateRequest GetStateRequest(string key);
	}

	public interface ITrxBatchHost
	{
		ITrxBatch Get(string key);
		List<ITrxBatch> ListByPersil(string key, bool activeOnly=false);
		List<ITrxBatch> ListByDocType(string key, bool activeOnly = false);
		List<ITrxBatch> ListByTrxType(TrxType type, bool activeOnly = false);
		List<ITrxBatch> ListActive();
	}

	public class DocEx
	{
		public string key { get; set; }
		public string keyDocType { get; set; }
		public string chainkey { get; set; }
		public int[] exists { get; set; }
		public DocEx(string key, string keyDocType, string chainkey, (Existence ex, int cnt)[] exists)
		{
			(this.key, this.keyDocType, this.chainkey) = (key, keyDocType, chainkey);
			this.exists = new int[6];
			foreach (var x in exists)
				this.exists[(int)x.ex] = x.cnt;
		}
		public DocEx(string key, string keyDocType, string chainkey, int[] exists)
		{
			(this.key, this.keyDocType, this.chainkey) = (key, keyDocType, chainkey);
			this.exists = exists;
		}
	}
}
