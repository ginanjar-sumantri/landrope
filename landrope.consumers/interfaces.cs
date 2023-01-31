using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using auth.mod;
using flow.common;
using GenWorkflow;
using landrope.common;
using landrope.documents;
using landrope.engines;
using APIGrid;

namespace landrope.consumers
{
	public interface IGraphHostConsumer
	{
		Task Compacting();
		Task<GraphMainInstance> Create(user user, ToDoType type, string discriminator = null, int version = -1);
		Task<GraphMainInstance> Create(string userkey, ToDoType type, string discriminator = null, int version = -1);
		Task<GraphTree[]> List(user user, int pay = 0);
		Task<GraphMainInstance> Get(string key);
		Task<GraphTree> Get(user user, string instkey, string dockey);
		Task<(string key, GraphSubTree[] trees)[]> GetFromDetails(user user, string instkey, string[] dockeys);
		Task<(bool OK, string reason)> Take(user user, string ikey, string rkey, DateTime? date=null, bool save = true);
		Task Del(string key);
		Task<(bool OK, string reason)> Summary(user user, string ikey, string rkey, string command, SummData data, bool save = true);
		Task RegisterDoc(string instakey, string dockey, bool save);
		Task RegisterDocs(string instakey, string[] dockeys, bool save);
		Task<GraphTemplate> GetTemplate(string name, int version = -1);
		Task<GraphSubInstance> GetSub(string instakey, string dockey);
		Task<(string instkey, string dockey, GraphSubInstance sub)[]> GetSubs(IEnumerable<(string instakey, string dockey)> keys);
		Task<GraphTree> FindRoute(string instakey, string subinstakey, string[] privs);
	}

	public interface IBundlerHostConsumer
	{
		Task<IMainBundle> MainGet(string key);
		Task<ITaskBundle> TaskGet(string key);
		Task<List<ITaskBundle>> ChildrenList(string key);
		Task<ITaskBundle[]> TaskList(string akey);
		Task<DocEx[]> Available(string key);
		Task<DocEx[]> Reserved(string key);
		Task<DocEx[]> Current(string key);
		Task<(ITaskBundle bundle, string reason)> MakeTaskBundle(string token, string asgdtlkey, bool save = true);
		Task<(ITaskBundle bundle, string reason)> DoMakeTaskBundle(string token, IAssignment asgn, IAssignmentDtl asgdtl, IMainBundle bundle, user user, bool save = true);
		Task Realize(string token, string asgnkey);
		Task AddProcessRequirement(string token, string key, landrope.common.DocProcessStep step, string keyDocType, KeyValuePair<string, Dynamic> prop, landrope.common.Existence ex);
		Task<IEnumerable<PersilNxDiscrete>> NextReadiesDiscrete(bool loose = false, bool inclActive = false);
		Task<IEnumerable<DocProp>> ListProps(string key);
		Task<bool> MainUpdate(IMainBundle bundle, bool dbSave = true);
		Task<bool> TaskUpdate(ITaskBundle bundle, bool dbSave = true);
		Task<bool> TaskUpdateEx(string key, bool dbSave = true);
		Task<bool> MainDelete(string key, bool dbSave = true);
		Task<List<PersilPositionView>> SavedPersilPosition(bool inclActive = false);
		Task<List<PersilNxDiscrete>> SavedPersilNext();
		Task<List<PersilPositionWithNext>> SavedPositionWithNext(string[] prokeys = null);
		Task<(bool OK, string error)> SaveLastPositionDiscete();
		Task<(bool OK, string error)> SaveNextStepDiscrete();
	}
	public interface IAssignerHostConsumer
	{
		Task<List<IAssignment>> OpenedAssignments(string[] keys);
		Task<(string key, landrope.common.DocProcessStep step)[]> AssignedPersils();
		Task Delete(string key);
		Task Add(IAssignment assg);
		Task<List<IAssignment>> AssignmentList(string key);
		Task<IAssignment> GetAssignment(string key);
		Task<IAssignment> GetAssignmentOfDtl(string dtlkey);
		Task<bool> Update(string akey);
		Task<bool> Update(IAssignment assg, bool save = true);
		Task<Dictionary<string, object>> ListAssignmentViews(string userkey, AgGridSettings gs);
	}
}
