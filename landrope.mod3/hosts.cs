using GenWorkflow;
using landrope.common;
using landrope.mcommon;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod3
{
	public interface IBundleHost
	{
		IAssignmentHost GetAssignmentHost();
		IGraphHostSvc GetGraphHost();

		(TaskBundle bundle, string reason) MakeTaskBundle(string token, string asgdtlkey);

		MainBundle MainGet(string key);
		DocEx[] Available(string key);
		DocEx[] Current(string key);
		DocEx[] Reserved(string key);

		bool Reserve(string token, string key, string asgnkey);
		void Realize(string token, string asgnkey);

		TaskBundle TaskGet(string key);
		List<TaskBundle> ChildrenList(string key);
		void AddProcessRequirement(string token, string key, DocProcessStep step, string keyDocType, KeyValuePair<string, Dynamic> prop, Existence ex);

		IEnumerable<PersilNextReady> NextReadies();
	}

	public interface IAssignmentHost
	{
		IBundleHost GetBundleHost();
		IGraphHostSvc GetGraphHost();

		Assignment GetAssignment(string key);
		List<Assignment> AssignmentList(string key);
		//List<Assignment> Assignments { get; set; } = new List<Assignment>();
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
