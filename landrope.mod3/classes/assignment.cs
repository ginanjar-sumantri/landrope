using auth.mod;
using DynForm.shared;
using GenWorkflow;
using graph.mod;
using landrope.common;
using landrope.mod.shared;
using landrope.mod2;
using landrope.mod3.shared;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GraphHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;
using Newtonsoft.Json;
using landrope.mod3.classes;
using System.Security.AccessControl;
using landrope.engines;
using landrope.documents;
using flow.common;
using GraphConsumer;
using landrope.consumers;

namespace landrope.mod3
{
    using InnerDocCoreList = List<ParticleDocCore>;

    [Entity("assignment", "assignments")]
    public class Assignment : namedentity3, IAssignment
    {
        public string instkey { get; set; }

        [BsonIgnore]
        public GraphMainInstance instance => ghost?.Get(instkey).GetAwaiter().GetResult();
        public void CreateGraphInstance(user user)
        {
            if (ghost == null || instkey != null)
                return;
            if (this.type == null)
                return;

            instkey = ghost.Create(user, this.type.Value).GetAwaiter().GetResult()?.key;
        }

        IGraphHostConsumer ghost => ContextService.services.GetService<IGraphHostConsumer>();

        //		//[BsonIgnore]
        //[Newtonsoft.Json.JsonIgnore]
        //[System.Text.Json.Serialization.JsonIgnore]
        //IAssignmentHost host { get; set; }


        public Assignment Inject(GraphHostConsumer graph)
        {
            //this.host = host;
            //this.graph = graph;
            return this;
        }

        public Assignment()
        {

        }

        public Assignment(user user)
        {
            key = MakeKey;
            created = DateTime.Now;
            keyCreator = user.key;
        }

        public Assignment(ToDoType type, user user, GraphHostConsumer graph = null)
            : this(user)
        {
            //this.graph = graph;
            this.type = type;
            CreateGraphInstance(user);
        }

        public Assignment(DocProcessStep step, user user, GraphHostConsumer graph = null)
            : this(user)
        {
            //this.graph = graph;
            this.step = step;
            this.type = step.StepType();
            /*				switch
						{
							DocProcessStep.PBT_Perorangan,DocProcessStep.PBT_PT,DocProcessStep.SK_BPN,DocProcessStep.Cetak_Buku,
							DocProcessStep.Penurunan_Hak,DocProcessStep.Peningkatan_Hak,DocProcessStep.Balik_Nama => ToDoType.Proc_BPN,
			*//*				DocProcessStep.Akta_Notaris or DocProcessStep.AJB or DocProcessStep.SPH => ToDoType.Proc_Non_BPN,
							DocProcessStep.Riwayat_Tanah or DocProcessStep.GPS_Dan_Ukur => ToDoType.Proc_Pengukuran,*//*
							_ => ToDoType.Proc_Non_BPN
						};*/
            CreateGraphInstance(user);
        }

        // subject to change
        public string keyPIC => instance?.FindData("ACTOR", "delegated_")?.ToString();


        public DocProcessStep? step { get; set; }
        public AssignmentCat? category { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string keyPenampung { get; set; }
        public string keyNotaris { get; set; }
        public string keyPic { get; set; }

        public ToDoType? type { get; set; }

        public ToDoState currstate => instance?.lastState?.state ?? ToDoState.unknown_;
        public DateTime? created { get; set; }
        public string keyCreator { get; set; }

        public DateTime? issued => instance?.states.LastOrDefault(s => s.state == ToDoState.issued_)?.time;
        public DateTime? accepted => instance?.states.LastOrDefault(s => s.state == ToDoState.accepted_)?.time;
        public DateTime? closed => (instance?.closed ?? false) ? instance?.states.LastOrDefault()?.time : null;


        public uint? duration { get; protected set; }

        public void CalcDuration()
        {
            duration = null;
            var count = details.Length;
            if (count < 1)
                return;

            var today = DateTime.Today;

            var keyProject = MyContext().GetVillage(keyDesa).project?.key;
            var slas = SLA.list.Where(s => s._step == step.Value && (today >= s.since) && (today <= (s.until ?? DateTime.MaxValue)) &&
                                        (s.keyProject == null || s.keyProject == keyProject));
            if (slas.Any(s => s.keyProject == keyProject))
                slas = slas.Where(s => s.keyProject == keyProject);
            slas = slas.Where(s => s.keyDesa == null || s.keyDesa == keyDesa);
            if (slas.Any(s => s.keyDesa == keyDesa))
                slas = slas.Where(s => s.keyDesa == keyDesa);

            var sla = slas.OrderByDescending(s => s.since).FirstOrDefault();
            if (sla == null)
                throw new InvalidOperationException("Tabel SLA belum dikonfigurasi dengan benar");
            var range = sla.ranges.FirstOrDefault(r => r.IsIn(count));
            if (range == null || (range.from == null && range.upto == null))
                throw new InvalidOperationException("Tabel SLA belum dikonfigurasi dengan benar");
            duration = range.Duration;
        }

        public void CalcDuration2()
        {
            duration = null;
            var count = details.Length;
            if (count < 1)
                return;

            var today = DateTime.Today;

            var keyProject = MyContext().GetVillage(keyDesa).project?.key;
            var list = MyContext().GetCollections(new SLA(), "timings", "{}", "{_id:0}").ToList().AsParallel();
            var slas = list.Where(s => s._step == step.Value && (today >= s.since) && (today <= (s.until ?? DateTime.MaxValue)) &&
                                        (s.keyProject == null || s.keyProject == keyProject));
            if (slas.Any(s => s.keyProject == keyProject))
                slas = slas.Where(s => s.keyProject == keyProject);
            slas = slas.Where(s => s.keyDesa == null || s.keyDesa == keyDesa);
            if (slas.Any(s => s.keyDesa == keyDesa))
                slas = slas.Where(s => s.keyDesa == keyDesa);

            var sla = slas.OrderByDescending(s => s.since).FirstOrDefault();
            if (sla == null)
                throw new InvalidOperationException("Tabel SLA belum dikonfigurasi dengan benar");
            var range = sla.ranges.FirstOrDefault(r => r.IsIn(count));
            if (range == null || (range.from == null && range.upto == null))
                throw new InvalidOperationException("Tabel SLA belum dikonfigurasi dengan benar");
            duration = range.Duration;
        }

        [BsonIgnore]
        public DateTime? duedate => accepted?.Date.AddDays(duration ?? 0);

        public Expense[] expenses { get; set; } = new Expense[0];
        public AssignmentDtl[] details { get; set; } = new AssignmentDtl[0];

        [BsonIgnore]
        public Dictionary<string, option[]> extras { get; set; }

        public bool BeforeIssuing()
        {
            if (type == null || step == null || details.Length == 0)
                return false;
            CalcDuration();
            if (duration == null)
                return false;

            var keys = details.Select(d => d.key).ToArray();
            ghost.RegisterDocs(instkey, keys, false).Wait();

            return true;
        }

        public void AddDetail(AssignmentDtl asgdtl)
        {
            var lst = details.ToList();
            var dtl = lst.Find(d => d.keyPersil == asgdtl.keyPersil);
            if (dtl != null)
                return;

            lst.Add(asgdtl);
            details = lst.ToArray();
        }

        public void DelDetail(string keyPersil)
        {
            var lst = details.ToList();
            var idx = lst.FindIndex(l => l.keyPersil == keyPersil);
            if (idx != -1)
                lst.RemoveAt(idx);
            details = lst.ToArray();
            if (type != null && step != null)
                CalcDuration();
        }

        public void DelDetail(AssignmentDtl dtl)
        {
            var lst = details.ToList();
            lst.Remove(dtl);
            details = lst.ToArray();
            if (type != null && step != null)
                CalcDuration();
        }

        public void AddExpense(Expense exp)
        {
            var lst = expenses.ToList();
            lst.Add(exp);
            expenses = lst.ToArray();
        }
        public void DelExpense(Expense exp)
        {
            var lst = expenses.ToList();
            lst.Remove(exp);
            expenses = lst.ToArray();
        }

        public class Comparer : IEqualityComparer<Assignment>
        {
            public bool Equals(Assignment x, Assignment y) => x.key == y.key;

            public int GetHashCode(Assignment obj) => obj.key.GetHashCode();
        }
        public static Comparer comparer => new Comparer();

        public AssignmentCore ToCore()
        {
            var core = new AssignmentCore();
            (core.key, core.identifier, core.step, core.category, core.keyProject, core.keyDesa,
                core.keyPTSK, core.keyPenampung,
                core.created, core.creator, core.invalid, core.extras, core.keyPic) =
                     (key, identifier, $"{step:g}", $"{category:g}", MyContext().GetVillage(keyDesa).project?.key, keyDesa,
                        keyPTSK, keyPenampung, created,
                        MyContext().users.FirstOrDefault(u => u.key == keyCreator)?.FullName,
                        invalid, extras, keyPic);
            return core;
        }

        public AssignmentViewM ToViewM(LandropePlusContext context)
        {
            var view = new AssignmentViewM();
            (var project, var desa) = context.GetVillage(keyDesa);
            (view.key, view.identifier, view.step, view.category, view.issued, view.accepted,
                view.closed, view.project, view.desa, view.company, view.duration, view.duedate, view.overdue) =
                (key, identifier, step, category.Value, issued, accepted, closed,
                context.companies.FirstOrDefault(c => c.key == (keyPTSK ?? keyPenampung))?.identifier, project.identity, desa.identity,
                duration, duedate,
                duedate == null ? (int?)null : DateTime.Today <= duedate.Value ? (int?)null : (DateTime.Today - duedate.Value).Days
                );
            return view;
        }

        public AssignmentView ToView(LandropePlusContext context)
        {
            var view = new AssignmentView();
            (var project, var desa) = context.GetVillage(keyDesa);
            (view.key, view.identifier, view.step, view.category, view.type, view.instkey,
                view.issued, view.accepted, view.closed, view.project, view.desa, view.PTSK, view.penampung, view.duration) =
                (key, identifier, step.ToString().Replace("_", " ") ?? DocProcessStep.Belum_Bebas.ToString().Replace("_", " "), category ?? AssignmentCat.Unknown, type ?? ToDoType.Proc_Non_BPN,
                instkey, issued, accepted, closed,
                context.companies.FirstOrDefault(c => c.key == keyPTSK)?.identifier,
                context.companies.FirstOrDefault(c => c.key == keyPenampung)?.identifier,
                project?.identity, desa?.identity, duration);
            return view;
        }

        public AssignmentViewExt ToViewExt((ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)[] villages,
                                                                                ExtLandropeContext.entitas[] companies, string routekey, ToDoState state,
                                                                                string status, DateTime? time, ToDoVerb verb,
                                                                                ToDoControl[] cmds, bool IsCreator,
                                                                            (DateTime? iss, DateTime? acc, DateTime? clo) tmf)
        {
            var view = new AssignmentViewExt();
            (var project, var desa) = villages.FirstOrDefault(v => v.desa.key == keyDesa);
            (
                view.key,
                view.identifier,
                view.step,
                view.category,
                view.type,
                view.instkey,
                view.issued,
                view.accepted,
                view.closed,
                view.project,
                view.desa,
                view.PTSK,
                view.penampung,
                view.duration,
                view.routekey,
                view.state,
                view.status,
                view.statustm,
                view.verb,
                view.todo,
                view.cmds,
                view.isCreator,
                view.issued,
                view.accepted,
                view.closed
            ) =
            (
                key,
                identifier,
                step.ToString().Replace("_", " ") ?? DocProcessStep.Belum_Bebas.ToString().Replace("_", " "),
                category ?? AssignmentCat.Unknown,
                type ?? ToDoType.Proc_Non_BPN,
                instkey,
                issued,
                accepted,
                closed,
                project?.identity,
                desa?.identity,
                companies.FirstOrDefault(c => c.key == keyPTSK)?.identity,
                companies.FirstOrDefault(c => c.key == keyPenampung)?.identity,
                duration,
                routekey,
                state,
                status,
                time,
                verb,
                verb.Title(),
                cmds,
                IsCreator,
                tmf.iss,
                tmf.acc,
                tmf.clo
            );
            return view;
        }

        public AssignmentViewExt ToViewExt2((ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)[] villages,
                                                                               ExtLandropeContext.entitas[] companies,
                                                                               List<Notaris> notarists,
                                                                               List<Internal> internals,
                                                                               string routekey, LatestState mstate,
                                                                               GraphState sstate, ToDoVerb verb,
                                                                               ToDoControl[] cmds, bool IsCreator,
                                                                           (DateTime? iss, DateTime? acc, DateTime? clo) tmf)
        {
            var view = new AssignmentViewExt();
            var partial = mstate != null ? mstate.partial : false;
            var xstate = mstate != null ? (mstate.state, mstate.time) : (sstate?.state, sstate?.time);
            var isNotaris = notarists.Any(n => n.key == keyPic);
            var isAnyInternal = internals.Any(i => i.key == keyPic);
            string pic = "";
            if (isNotaris)
                pic = $"Notaris - " + notarists.FirstOrDefault(n => n.key == keyPic)?.identifier;
            else if (isAnyInternal)
            {
                var inter = isAnyInternal ? internals.FirstOrDefault(x => x.key == keyPic) : new Internal();
                pic = $"Internal - {inter.salutation} {inter.identifier}";
            }

            (var project, var desa) = villages.FirstOrDefault(v => v.desa.key == keyDesa);
            (
                view.key,
                view.identifier,
                view.step,
                view.category,
                view.type,
                view.instkey,
                view.issued,
                view.accepted,
                view.closed,
                view.project,
                view.desa,
                view.PTSK,
                view.penampung,
                view.duration,
                view.routekey,
                view.state,
                view.status,
                view.statustm,
                view.verb,
                view.todo,
                view.cmds,
                view.isCreator,
                view.issued,
                view.accepted,
                view.closed,
                view.PIC,
                view.keyPic
            ) =
            (
                key,
                identifier,
                step.ToString().Replace("_", " ") ?? DocProcessStep.Belum_Bebas.ToString().Replace("_", " "),
                category ?? AssignmentCat.Unknown,
                type ?? ToDoType.Proc_Non_BPN,
                instkey,
                issued,
                accepted,
                closed,
                project?.identity,
                desa?.identity,
                companies.FirstOrDefault(c => c.key == keyPTSK)?.identity,
                companies.FirstOrDefault(c => c.key == keyPenampung)?.identity,
                duration,
                routekey,
                xstate.state ?? ToDoState.unknown_,
                $"{xstate.state?.AsStatus()} {(partial == true ? "(partial)" : "")}",
                xstate.time,
                verb,
                verb.Title(),
                cmds,
                IsCreator,
                tmf.iss,
                tmf.acc,
                tmf.clo,
                pic,
                keyPic
            );
            return view;
        }

        public void FromCore(AssignmentCore core)
        {
            (identifier, step, category, keyProject, keyDesa,
                keyPTSK, keyPenampung, invalid, keyPic) =
                     (core.identifier,
                     Enum.TryParse<DocProcessStep>(core.step, out DocProcessStep stp) ? stp : default,
                     Enum.TryParse<AssignmentCat>(core.category, out AssignmentCat cat) ? cat : default,
                     core.keyProject, core.keyDesa, core.keyPTSK, core.keyPenampung, core.invalid, core.keyPic);
        }

        public void FromImport(DocProcessStep stp, AssignmentCat cat, string keyProject, string keyDesa, string keyPTSK, string keyPenampung, string keyPIC)
        {
            (step,
                category,
                this.keyProject,
                this.keyDesa,
                this.keyPTSK,
                this.keyPenampung,
                invalid,
                keyPic) =
                (stp,
                    cat,
                    keyProject,
                    keyDesa,
                    keyPTSK,
                    keyPenampung,
                    false,
                    keyPic);
        }

        public static Dictionary<string, (int order, DocProcessStep step)[]> StepOrders = new Dictionary<string, (int order, DocProcessStep step)[]>
        {
            { "persilHGB",new[]{
                                                        (1, DocProcessStep.Riwayat_Tanah),
                                                        (2, DocProcessStep.Akta_Notaris),
                                                        (3,DocProcessStep.AJB),
                                                        (4, DocProcessStep.Balik_Nama) } },
            { "persilSHM",new[]{
                                                        (1, DocProcessStep.Riwayat_Tanah),
                                                        (2, DocProcessStep.Akta_Notaris),
                                                        (3, DocProcessStep.Penurunan_Hak),
                                                        (4, DocProcessStep.AJB),
                                                        (5, DocProcessStep.Balik_Nama) } },
            { "persilSHP",new[]{
                                                        (1, DocProcessStep.Riwayat_Tanah),
                                                        (2, DocProcessStep.Akta_Notaris),
                                                        (3, DocProcessStep.Peningkatan_Hak),
                                                        (4, DocProcessStep.AJB),
                                                        (5, DocProcessStep.Balik_Nama) } },
            { "persilGirik",new[]{
                                                        (1, DocProcessStep.Riwayat_Tanah),
                                                        (2, DocProcessStep.Akta_Notaris),
                                                        (3, DocProcessStep.PBT_Perorangan),
                                                        (4, DocProcessStep.SPH),
                                                        (5, DocProcessStep.PBT_PT),
                                                        (6, DocProcessStep.SK_BPN),
                                                        (7, DocProcessStep.Cetak_Buku),   } },
            { "PersilHibah",new[]{
                                                        (1, DocProcessStep.Riwayat_Tanah),
                                                        (2, DocProcessStep.SHM_Hibah),
                                                        (3, DocProcessStep.Akta_Notaris),
                                                        (4, DocProcessStep.Penurunan_Hak),
                                                        (5, DocProcessStep.AJB),
                                                        (6, DocProcessStep.Balik_Nama) } }
        };

        public static DocProcessStep GetLatest(string disc, params DocProcessStep[] steps)
        {
            (int order, DocProcessStep step)[] sto = null;
            if (!StepOrders.TryGetValue(disc, out sto))
                return DocProcessStep.Belum_Bebas;
            return sto.Join(steps, o => o.step, s => s, (o, s) => o).OrderBy(s => s.order).LastOrDefault().step;
        }

        public static DocProcessStep? GetNext(string disc, DocProcessStep? step)
        {
            if (step == null || step.Value == DocProcessStep.Belum_Bebas)
                return step;

            (int order, DocProcessStep step)[] sto = null;
            if (!StepOrders.TryGetValue(disc, out sto))
                return null;
            var stp = sto.FirstOrDefault(s => s.step == step);
            if (stp.order == 0)
                return null;
            var nx = sto.Where(s => s.order > stp.order).OrderBy(s => s.order).FirstOrDefault();
            var ret = nx.order == 0 ? (DocProcessStep?)null : nx.step;
            return ret;
        }

        public static DocProcessStep? GetPrev(string disc, DocProcessStep? step)
        {
            if (step == null || step.Value == DocProcessStep.Belum_Bebas)
                return step;
            if (step == DocProcessStep.Riwayat_Tanah)
                return DocProcessStep.Baru_Bebas;

            (int order, DocProcessStep step)[] sto = null;
            if (!StepOrders.TryGetValue(disc, out sto))
                return null;
            var stp = sto.FirstOrDefault(s => s.step == step);
            if (stp.order == 0)
                return null;
            var nx = sto.Where(s => s.order < stp.order).OrderBy(s => s.order).LastOrDefault();
#if (DEBUG_)
			if (nx.order == 0)
				return DocProcessStep.Baru_Bebas;
			return nx.step;
#else
            return nx.order == 0 ? DocProcessStep.Baru_Bebas : nx.step;
#endif
        }

        public static DocProcessStep[] CollectNexts(string disc, DocProcessStep step)
        {
            (int order, DocProcessStep step)[] sto = null;
            if (!StepOrders.TryGetValue(disc, out sto))
                return new DocProcessStep[0];

            if (step == DocProcessStep.Baru_Bebas)
                return sto.Select(s => s.step).ToArray();

            var stp = sto.FirstOrDefault(s => s.step == step);
            if (stp.order == 0)
                return new DocProcessStep[0];

            return sto.Where(s => s.order > stp.order).OrderBy(s => s.order).Select(s => s.step).ToArray();
        }

        static DocProcessStep[] SKNeeds = new DocProcessStep[] {
            DocProcessStep.PBT_PT,
            DocProcessStep.SK_BPN,
            DocProcessStep.Cetak_Buku,
            DocProcessStep.Balik_Nama
        };


        public static Assignment Create(LandropePlusContext contextplus, AssignmentCore assg, user user, bool strict = true)
        {
            var sstep = assg.step;
            DocProcessStep? step = (sstep != null) ?
                            Enum.TryParse<DocProcessStep>(sstep, out DocProcessStep stp) ? (DocProcessStep?)stp : null : null;

            var ent = step == null ? new Assignment(user) : new Assignment(step.Value, user);

            ent.FromCore(assg);
            ent.key = entity.MakeKey;
            assg.key = ent.key;
            //ent.created = DateTime.Now;
            //ent.keyCreator = user.key;
            var type = step?.StepType();
            var docnokey = $"{nameof(Assignment)}-{type?.ToString("g").Substring(5)}";
            var dno = contextplus.docnoes.FirstOrDefault(d => d.key == docnokey);
            if (dno == null)
                throw new Exception("Tidak bisa menambahkan penugasan, ada kesalahan di penomoran dokumen (no match ID)");
            var step3 = step?.Code() ?? "XXX";

            if (string.IsNullOrEmpty(assg.keyPic))
                throw new Exception("Tidak bisa menambahkan penugasan, PIC belum dipilih !");

            var codept = string.Empty;

            var ptsk = contextplus.ptsk.FirstOrDefault(p => p.key == assg.keyPTSK);
            codept = ((Func<string, string>)(st => st == null ? null : st.Length <= 3 ? st : st.Substring(0, 3))).Invoke(ptsk?.identifier)?
                                           .ToUpper() ?? "XXX";
            if (strict && SKNeeds.Contains(step ?? DocProcessStep.Belum_Bebas) && ptsk == null)
                throw new Exception("Gagal menambahkan penugasan, untuk proses yang dimaksud harus sudah ada PT pemegang SK");
            if (ptsk == null)
            {
                var penampung = contextplus.companies.FirstOrDefault(p => p.key == assg.keyPenampung);
                codept = ((Func<string, string>)(st => st == null ? null : st.Length <= 3 ? st : st.Substring(0, 3))).Invoke(penampung?.identifier)?
                                            .ToUpper() ?? "XXX";
            }

            ent.identifier = dno.Generate(DateTime.Today, true, step3, codept);
            return ent;
        }
    }

    [BsonIgnoreExtraElements]
    public class AssignmentDtl : IAssignmentDtl
    {
        [BsonExtraElements]
        public Dictionary<string, object> extaelems { get; set; }
        public string key { get; set; }
        public string keyPersil { get; set; }
        public string keyBundle { get; set; } // TaskBundle
        public Persil persil(LandropePlusContext context) => context.persils.FirstOrDefault(p => p.key == keyPersil);
        public Persil persilHibah(LandropePlusContext context) => context.GetCollections(new Persil(), "persils_v2_hibah", $"<key : '{keyPersil}'>".MongoJs(), "{_id:0}").FirstOrDefault();
        public TaskBundle bundle(LandropePlusContext context) => context.taskBundles.FirstOrDefault(b => b.key == keyBundle);
        //public InnerDocList[] results { get; set; } = null;

        public AssignmentPreResult preresult { get; set; } = null;
        public AssignmentResult result { get; set; } = null;


        //public IDocControl[] DocControls { get; set; } = new IDocControl[0];
        public CostDtl costs { get; set; } = new CostDtl();

        [BsonIgnore]
        public Dictionary<string, option[]> extras { get; set; }

        public AssignmentDtlCore ToCore(LandropePlusContext context)
        {
            var core = new AssignmentDtlCore();
            //(core.key, core.keyPersil, core.IdBidang, core.persil, core.costs, core.extras) =
            //	(key, keyPersil, "", GetPersilCore(), costs, extras);
            (core.key, core.keyPersil) =
                (key, keyPersil);
            return core;

            //PersilCore GetPersilCore()
            //{
            //	PersilCore pcore = new PersilCore();
            //	var persil = this.persil(context);
            //	var current = persil?.basic?.current;
            //	(pcore.key, pcore.luasSurat, pcore.namaSurat, pcore.nomorSurat, pcore.proses, pcore.jenis, pcore.pemilik, pcore.group) =
            //		(persil?.key, current?.luasSurat, current?.surat?.nama, current?.surat?.nomor,
            //		current?.proses, current?.jenis, current?.pemilik, current?.group);
            //	return pcore;
            //}
        }

        public void FromCore(AssignmentDtlCore core)
        {
            (key, keyPersil) =
            (core.key, core.keyPersil);
            //(key, keyPersil, costs, results) =
            //(core.key, core.keyPersil, core.costs, core.results.Select(r => (InnerDocList)r).ToArray());
        }

        public void FromCore(string key, string keyPersil)
        {
            (this.key, this.keyPersil) =
            (key, keyPersil);
            //(key, keyPersil, costs, results) =
            //(core.key, core.keyPersil, core.costs, core.results.Select(r => (InnerDocList)r).ToArray());
        }

        public AssignmentDtlView ToView(LandropePlusContext context)
        {
            //var uacceptor = context.users.FirstOrDefault(u => u.key == keyAcceptor);
            var persil = this.persil(context) ?? this.persilHibah(context);// GetPersilCore();
            var view = new AssignmentDtlView();
            (view.key, view.keyPersil, view.IdBidang, view.noPeta, view.alasHak, view.group, view.luasDibayar,
                view.luasSurat, view.pemilik, view.jumlah, view.namaSurat, view.tahap, view.satuan) =
                (key, keyPersil, persil.IdBidang, persil.basic?.current?.noPeta, persil?.basic?.current?.surat?.nomor,
                persil?.basic?.current?.group, persil?.basic?.current?.luasDibayar,
                persil?.basic?.current?.luasSurat, persil?.basic?.current?.pemilik,
                costs?.jumlah ?? 0, persil?.basic?.current?.surat?.nama, persil?.basic?.current?.tahap, persil?.basic?.current?.satuan);
            return view;

            //PersilCore GetPersilCore()
            //{
            //	PersilCore pcore = new PersilCore();
            //	var persil = this.persil(context);
            //	var current = persil?.basic?.current;
            //	(pcore.key, pcore.luasSurat, pcore.luasDibayar, pcore.namaSurat, pcore.nomorSurat, pcore.proses, pcore.jenis, 
            //		pcore.pemilik, pcore.group,pcore.tahap,pcore.satuan) =
            //	(persil?.key, current?.luasSurat, current?.luasDibayar,current?.surat?.nama, current?.surat?.nomor,
            //		current?.proses, current?.jenis, current?.pemilik, current?.group,current?.tahap,current?.satuan);
            //	return pcore;
            //}
        }
    }

    [BsonIgnoreExtraElements]
    public class AssignmentPreResult
    {
        public PreResultDoc info { get; set; }

        [BsonExtraElements]
        public Dictionary<string, object> extraelems { get; set; }
    }

    public class AssignmentResult
    {
        public ResultDoc[] infoes { get; set; }
        public static explicit operator AssignmentResult(AssignmentPreResult pre)
            => new AssignmentResult { infoes = new ResultDoc[] { pre.info } };

        public static DynElement[] MakeLayout(ResultDoc doc, bool editable = false)
        {
            var docname = DocType.List.FirstOrDefault(d => d.invalid != true && d.key == doc.keyDT)?.identifier;
            var dyns = doc.props.Select(k => new DynElement
            {
                visible = "true",
                editable = $"{editable}".ToLower(),
                group = $"{docname}|Properties",
                label = k.mkey.ToString("g").Replace("_", " "),
                value = $"#props.${k.mkey:g}",
                nullable = true,
                cascade = "",
                dependency = "",
                correction = false,
                inittext = "",
                options = "",
                swlabels = new string[0],
                type = k.val.type switch
                {
                    Dynamic.ValueType.Int => ElementType.Numeric,
                    Dynamic.ValueType.Number => ElementType.Number,
                    Dynamic.ValueType.Bool => ElementType.Check,
                    Dynamic.ValueType.Date => ElementType.Date,
                    _ => ElementType.Text,
                },
            });

            var exi = new (Existence key, string type, string label)[]
            {
                (Existence.Soft_Copy,"Check","Di-scan"),
                (Existence.Asli,"Check","Asli"),
                (Existence.Copy,"Numeric","Copy"),
                (Existence.Salinan,"Numeric","Salinan"),
                (Existence.Legalisir,"Numeric","Legalisir"),
                (Existence.Avoid,"Check","Tidak Diperlukan/Memo"),
            };

            var existencies = exi.Select(x => new DynElement
            {
                visible = "true",
                editable = $"{editable}".ToLower(),
                group = $"{docname}|Keberadaan",
                label = x.label,
                value = $"#{x.key:g}",
                nullable = false,
                cascade = "",
                dependency = "",
                correction = false,
                inittext = "",
                options = "",
                swlabels = new string[0],
                xtype = x.type
            });

            var res = dyns.Union(existencies).ToArray();
            return res;
        }
    }

    public class ViewAssignmentJoined
    {
        public string projectKey { get; set; }
        public string projectName { get; set; }
        public string desaKey { get; set; }
        public string desaName { get; set; }
        public string picKey { get; set; }
        public string picName { get; set; }
        public string ptskKey { get; set; }
        public string ptskName { get; set; }
        public ViewAssignment assignment { get; set; }
        public GraphMainInstance graph { get; set; }
        public Persil[] persils { get; set; }
        public MainBundle[] bundles { get; set; }
    }

    public class ViewAssignment : namedentity3
    {
        public string instkey { get; set; }
        public DocProcessStep? step { get; set; }
        public AssignmentCat? category { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string keyPenampung { get; set; }
        public string keyNotaris { get; set; }
        public string keyPic { get; set; }

        public ToDoType? type { get; set; }

        public DateTime? created { get; set; }
        public string keyCreator { get; set; }

        public DateTime? issued { get; set; }
        public DateTime? accepted { get; set; }
        public DateTime? closed { get; set; }


        public uint? duration { get; set; }
        public DateTime? duedate { get; set; }

        public Expense[] expenses { get; set; } = new Expense[0];
        public AssignmentDtl[] details { get; set; } = new AssignmentDtl[0];

        public Dictionary<string, option[]> extras { get; set; }
    }
}


