using System;

namespace assigner.bridge
{
	public class Class1
	{
		public List<IAssignment> OpenedAssignments()

		public IEnumerable<(string key, DocProcessStep step)> AssignedPersils()

		public AssignmentHost(IConfiguration config, IServiceProvider services)

		public void Delete(string key)

		public void Add(Assignment assg)

		public List<IAssignment> AssignmentList(string key)

		public IAssignment GetAssignment(string key)

		public bool Update(string akey)

		public bool Update(Assignment assg, bool save = true)

		public void PrepareBudget()
	}
}
