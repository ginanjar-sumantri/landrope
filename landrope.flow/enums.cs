using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.flow
{
	public enum ToDoType
	{
		Proc_Pengukuran = 1,
		Proc_Non_BPN = 2,
		Proc_BPN = 3
	}

	[Flags]
	public enum ToDoState
	{
		Unknown = 0,
		Created = 1,
		Issued = 2,
		Delegated = 3,
		Bundling = 4,
		Bundled = 5,
		Delegated_Bundled = 6,
		Analysing = 7,
		Canceled = 8,
		AnalisysReady = 9,
		Analised = 10,
		FormFilling = 11,
		BundleTaken = 12,
		Accepted = 13,
		SentToAdmin = 14,
		AdminCompleting = 15,
		ResultArchiving = 16,
		ResultArchived = 17,
		ResultValidated = 18,
		Complete = 19
	}

	public enum ToDoVerb
	{
		Unknown = 0,
		Create = 1,
		Issue = 2,
		Delegate = 3,
		Bundle = 4,
		BundleComplete = 5,
		Analyse = 6,
		AnalyseResult = 7,
		AnalyseEntry = 8,
		FormFill = 9,
		GiveBundle = 10,
		Accept = 11,
		SendToAdmin = 12,
		AdminCompletion = 13,
		SendToArchive = 14,
		ArchiveReceive = 15,
		ArchiveValidate = 16
	}

	public enum ToDoControl
	{
		OK = 1,
		Yes = 3,
		Continue = 4,
		Accept=5,
		Approve=6,
		Resume=7,

		Cancel = 101,
		No = 102,
		Abort = 103,
		Reject=104,
		Discard = 105,
		Pending=106,
		Postpone=107,
		Hold=108
	}
}
