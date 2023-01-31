using DynForm.shared;
using flow.common;
using GenWorkflow;
using landrope.common;
using landrope.mod.shared;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace landrope.mod3.shared
{
	using InnerDocCoreList = List<ParticleDocCore>;

	public class AssignmentCore : CoreBase
	{
		public string key { get; set; }
		public string identifier { get; set; }
		public bool? invalid { get; set; }
		public string step { get; set; }
		public string category { get; set; }
		public string keyProject { get; set; }
		public string keyDesa { get; set; }
		public string keyPTSK { get; set; }
		public string keyPenampung { get; set; }
		public string keyNotaris { get; set; }
		public DateTime? created { get; set; }
		public string creator { get; set; }
        public string keyPic { get; set; }
        public Dictionary<string, option[]> extras { get; set; }

		public string Parameters() => $"{category}/{step}/{keyProject}/{keyDesa}/{keyPTSK}/{keyPenampung}";
	}

	public class AssignmentDtlCore : CoreBase
	{
		public string key { get; set; }
		public string keyPersil { get; set; }
		//public string IdBidang { get; set; }
		//public string keyBundle { get; set; } // TaskBundle
		//public PersilCore persil { get; set; }
		//public CostDtl costs { get; set; }
		//public InnerDocCoreList[] results { get; set; } = new InnerDocCoreList[0];
		public Dictionary<string, option[]> extras { get; set; }

		//public AssignmentDtlCore SetId(string idBidang)
		//{
		//	this.IdBidang = idBidang;
		//	return this;
		//}
	}

	public class CostDtl
	{
		public double? nilaiSPS { get; set; }
		public double? taktis { get; set; }
		public double? notaris { get; set; }
		public double? tanah { get; set; }
		public double? pajak { get; set; }
		public double? lainnya { get; set; }

		public double jumlah => (nilaiSPS ?? 0) + (taktis ?? 0) + (notaris ?? 0) + (tanah ?? 0) + (pajak ?? 0) + (lainnya ?? 0);
	}

	public class AssignmentDtlView
	{
		public string key { get; set; }
		public string keyPersil { get; set; }
		public string IdBidang { get; set; }
		public string noPeta { get; set; }
		public double? luasDibayar { get; set; }
		public double? luasSurat { get; set; }
		public string alasHak { get; set; }
		public string namaSurat { get; set; }
		public string pemilik { get; set; }
		public string group { get; set; }
		public int? tahap { get; set; }
		public double? satuan { get; set; }

		public double jumlah { get; set; }
		public DateTime? submitted { get; set; }
		public DateTime? resaccepted { get; set; }
		public DateTime? archived { get; set; }
		public DateTime? validated { get; set; }
	}

	public class AssignmentDtlViewExt :  AssignmentDtlView
	{
		public string routekey { get; set; }
		public ToDoState state { get; set; }
		public string status { get; set; }
		public DateTime? statustm { get; set; }

		public ToDoVerb verb { get; set; }
		public string todo { get; set; }
		public ToDoControl[] cmds { get; set; } = new ToDoControl[0];

		public AssignmentDtlViewExt SetRoute(string key) { this.routekey = key; return this; }
		public AssignmentDtlViewExt SetState(ToDoState state) { this.state = state; return this; }
		public AssignmentDtlViewExt SetStatus(string status,DateTime? time) { (this.status,this.statustm) = (status,time); return this; }
		public AssignmentDtlViewExt SetVerb(ToDoVerb verb) { this.verb = verb; return this; }
		public AssignmentDtlViewExt SetTodo(string todo) { this.todo = todo; return this; }
		public AssignmentDtlViewExt SetCmds(ToDoControl[] cmds) { this.cmds = cmds; return this; }
		public AssignmentDtlViewExt SetAttributes(string routekey, ToDoState state, DateTime? time,
																	ToDoVerb verb, string todo, ToDoControl[] cmds)
		{
			(this.routekey, this.state, this.status, this.statustm, 
				this.verb, this.todo, this.cmds) = 
			(routekey, state,state.AsStatus(), time, verb, todo, cmds);
			return this;
		}

		public static AssignmentDtlViewExt Upgrade(AssignmentDtlView old)
			=> System.Text.Json.JsonSerializer.Deserialize<AssignmentDtlViewExt>(
					System.Text.Json.JsonSerializer.Serialize(old)
				);
	}

	public static class AssignmentHelper
	{
		public static string GetName(this DocProcessStep step)
			=> step.ToString("g").Replace("_", " ");

		public static (JenisProses, JenisAlasHak) Convert(this AssignmentCat cat)
			=> cat switch
			{
				AssignmentCat.Girik => (JenisProses.standar, JenisAlasHak.girik),
				AssignmentCat.HGB => (JenisProses.standar, JenisAlasHak.hgb),
				AssignmentCat.SHM => (JenisProses.standar, JenisAlasHak.shm),
				AssignmentCat.SHP => (JenisProses.standar, JenisAlasHak.shp),
				AssignmentCat.Hibah => (JenisProses.hibah, JenisAlasHak.girik),
				_ => (JenisProses.batal, JenisAlasHak.unknown)
			};

		public static AssignmentCat Convert(this JenisProses proc, JenisAlasHak alh)
			=> (proc, alh) switch
			{
				(JenisProses.standar, JenisAlasHak.girik) => AssignmentCat.Girik,
				(JenisProses.standar, JenisAlasHak.hgb) => AssignmentCat.HGB,
				(JenisProses.standar, JenisAlasHak.shm) => AssignmentCat.SHM,
				(JenisProses.standar, JenisAlasHak.shp) => AssignmentCat.SHP,
				(JenisProses.hibah, JenisAlasHak.girik) => AssignmentCat.Hibah,
				_ => AssignmentCat.Unknown
			};
	}

	public class AssignmentView
	{
		public string key { get; set; }
		public string instkey { get; set; }
		public bool? invalid { get; set; }
		public string identifier { get; set; }
		public ToDoType type { get; set; }
		public string step { get; set; }
		public AssignmentCat category { get; set; }
		public DateTime? created { get; set; }
		public string creator { get; set; }
		public DateTime? issued { get; set; }
		public string issuer { get; set; }
		public DateTime? delegated { get; set; }
		public DateTime? accepted { get; set; }

		public uint? duration { get; set; }
		public DateTime? closed { get; set; }
		public string PIC { get; set; }
		public string keyPic { get; set; }
		public string notaris { get; set; }
		public string project { get; set; }
		public string desa { get; set; }
		public string PTSK { get; set; }
		public string penampung { get; set; }

		public DateTime Today { get; set; } = DateTime.Today;

		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public DateTime? duedate => accepted == null ? (DateTime?)null : accepted.Value.AddDays(duration ?? 0);
	}

	public class AssignmentViewExt : AssignmentView
	{
		public string routekey { get; set; }
		public ToDoState state { get; set; }
		public string status { get; set; }
		public DateTime? statustm { get; set; }

		public ToDoVerb verb { get; set; }
		public string todo { get; set; }
		public ToDoControl[] cmds { get; set; } = new ToDoControl[0];
		public bool isCreator { get; set; }

		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		[BsonIgnore]
		public int? overdue => (duedate==null || state == ToDoState.complished_ || Today<=(duedate??DateTime.MaxValue)) ? 
									(int?)null : ((int)(Today - duedate.Value).TotalDays - 1) / 7 + 1;
		public AssignmentViewExt SetAttributes(string routekey, ToDoState state, string status, DateTime? time, ToDoVerb verb,
																	ToDoControl[] cmds, bool IsCreator,
																	(DateTime? iss, DateTime? acc, DateTime? clo) tmf)
		{
			(this.routekey, this.state,this.status, this.statustm, this.verb, this.todo, 
				this.cmds, this.isCreator, this.issued, this.accepted, this.closed) 
				= (routekey, state,status, time, verb, verb.Title(), cmds,IsCreator,tmf.iss, tmf.acc, tmf.clo);
			return this;
		}

		public AssignmentViewExt SetRoute(string key) { this.routekey = key; return this; }
		public AssignmentViewExt SetState(ToDoState state) { this.state = state; return this; }
		public AssignmentViewExt SetStatus(string status,DateTime? time) { (this.status, this.statustm) = (status, time); return this; }
		public AssignmentViewExt SetVerb(ToDoVerb verb) { this.verb = verb; return this; }
		public AssignmentViewExt SetTodo(string todo) { this.todo = todo; return this; }
		public AssignmentViewExt SetCmds(ToDoControl[] cmds) { this.cmds = cmds; return this; }
		public AssignmentViewExt SetCreator(bool IsCreator) { this.isCreator = IsCreator; return this; }
		public AssignmentViewExt SetMilestones(DateTime? iss, DateTime? del,DateTime? acc, DateTime? clo) 
				{ (this.issued, this.delegated, this.accepted, this.closed) = (iss, del, acc, clo); ; return this; }

		public static AssignmentViewExt Upgrade(AssignmentView old)
			=> System.Text.Json.JsonSerializer.Deserialize<AssignmentViewExt>(
					System.Text.Json.JsonSerializer.Serialize(old)
				);
	}
}
