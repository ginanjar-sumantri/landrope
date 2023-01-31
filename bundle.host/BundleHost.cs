#define _HIBAH_ONLY
#define _LOOSE_DOCS_
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using auth.mod;
using GenWorkflow;
//using GraphHost;
using landrope.documents;
using landrope.common;
using landrope.engines;
using landrope.mod2;
using landrope.mod3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using MongoDB.Driver;
//using TriangleNet.Topology;
//using MongoDB.Bson;
using System.Globalization;
using GraphConsumer;
using AssignerConsumer;
using landrope.consumers;
using MongoDB.Driver;
using MongoDB.Bson;
using Tracer;

namespace bundle.host
{

    public class BundleHost : IBundleHost
    {
        IServiceProvider services;
        IServiceScope scope;
        IServiceProvider scopeds;

        IConfiguration configuration;
        LandropePlusContext contextplus;
        authEntities authcontext;
        //IGraphHostSvc GraphHost;
        AssignerHostConsumer assign;
        GraphHostConsumer graph;

        ObservableCollection<MainBundle> MainBundles = new ObservableCollection<MainBundle>();
        ObservableCollection<TaskBundle> TaskBundles = new ObservableCollection<TaskBundle>();
        ObservableCollection<PreBundle> PreBundles = new ObservableCollection<PreBundle>();
        //List<MainBundleMem> MainBundleMems = new List<MainBundleMem>();
        List<DocProp> MainProps = new List<DocProp>();
        List<DocExistence> MainExistences = new List<DocExistence>();

        public AssignerHostConsumer GetAssignmentHost() => assign;
        public GraphHostConsumer GetGraphHost() => graph;

        public BundleHost(IServiceProvider services, IConfiguration configuration)
        {
            this.services = services;
            scope = services.CreateScope();
            scopeds = scope.ServiceProvider;

            this.configuration = configuration;
            graph = services.GetService<GraphHostConsumer>();
            assign = services.GetService<IAssignerHostConsumer>() as AssignerHostConsumer;

            authcontext = scopeds.GetService<authEntities>();
            contextplus = scopeds.GetService<LandropePlusContext>();

            LoadData();
        }

        private void TaskBundle_Realized(object sender, EventArgs e)
        {
            var task = sender as TaskBundle;
            var tvals = task.doclist.SelectMany(d => d.docs.Select(dd => (akey: task.keyAssignment, task.key, d.keyDocType, chainkey: dd.Key, exis: dd.Value)));
            var TM = tvals.Join(MainExistences, t => (t.key, t.keyDocType, t.chainkey),
                                        m => (m.key, m.keyDocType, m.chainkey), (t, m) => (t, m)).ToList();
            TM.ForEach(x =>
            {
                x.m.Realize(x.t.akey, x.t.exis);
            });

            var usrkey = task.assignment().keyCreator;

            var mbx = TM.Select(x => (x.t.key, x.t.keyDocType, x.t.chainkey, x.m))
                .Join(MainProps, tm => (tm.key, tm.keyDocType, tm.chainkey), p => (p.key, p.keyDocType, p.chainkey),
                        (t, p) => (t.key, t.keyDocType, t.chainkey, t.m, p.props))
                .GroupBy(x => (x.key, x.keyDocType, x.chainkey))
                .Select(g => (mbkey: g.Key.key,
                                doclist: g.GroupBy(d => (d.keyDocType, d.chainkey))
                                    .Select(g1 => (DTkey: g1.Key.keyDocType, chain: g1.GroupBy(d1 => d1.chainkey)
                                        .Select(g2 => (chainkey: g2.Key, g2.FirstOrDefault().props, parts: g2.FirstOrDefault().m.current)).ToList())).ToList()
                                        )).ToList();
            /*									)).ToList();*/
            mbx.ForEach(m =>
            {
                var mb = (MainBundle)MainGet(m.mbkey);
                var doclist = mb.doclist.Join(m.doclist, d1 => d1.keyDocType, d2 => d2.DTkey, (d1, d2) => (d1, d2.chain)).ToList();
                doclist.ForEach(doc =>
                {
                    var entry = new ValidateEntry<ParticleDocChain>();
                    (entry.created, entry.approved, entry.keyCreator, entry.keyReviewer, entry.kind, entry.reviewed, entry.sourceFile) =
                    (DateTime.Now, true, usrkey, usrkey, ChangeKind.Delete, DateTime.Now, "*realized");
                    entry.Item = new ParticleDocChain(doc.chain.Select(c => new KeyValuePair<string, ParticleDoc>(c.chainkey,
                            new ParticleDoc { props = c.props, exists = c.parts.ConvertBack2() })));
                    doc.d1.Add(entry);
                });
                contextplus.mainBundles.Update(mb);
            });
            contextplus.SaveChanges();
        }

        public void LoadData()
        {
            var persils = CleanPersil;
            // preload bundles
            lock (MainBundles)
            {
                MainBundles = new ObservableCollection<MainBundle>(contextplus.mainBundles.Query(b => b.invalid != true)
                                            .Join(persils, b => b.key, p => p.key, (b, p) => b.Inject<MainBundle>(this)));
                MyTracer.TraceInfo2($"The MainBundles has been collected with {MainBundles.Count} items");
            }
            lock (TaskBundles)
            {
                TaskBundles = new ObservableCollection<TaskBundle>(contextplus.taskBundles.Query(b => b.invalid != true && b.Realized != true)
                                            .Join(MainBundles, b => b.keyParent, p => p.key, (b, p) => b.Inject<TaskBundle>(this)));
                MyTracer.TraceInfo2($"The TaskBundles has been collected with {TaskBundles.Count} items");
            }

            MainBundles.CollectionChanged += MainBundles_CollectionChanged;
            TaskBundles.CollectionChanged += TaskBundles_CollectionChanged;
            MainBundles.ToList().ForEach(b =>
            {
                b.doclist.CollectionChanged += (s, e) => Doclist_CollectionChanged(b, s, e);
                b.doclist.ToList().ForEach(d => d.SpecialReqs.CollectionChanged += (s, e) => Spcreqs_CollectionChanged(b, d, s, e));
            });
            TaskBundles.ToList().ForEach(t =>
            {
                t.OnRealized += TaskBundle_Realized;
            });

            LoadPropsAndExistences();
        }

        void LoadPropsAndExistences()
        {
            MainBundles.CheckLock();
            var doclists = MainBundles.SelectMany(mb => mb.doclist.Select(d => (mb, d,
                            item: d.entries.LastOrDefault()?.Item?.LastOrDefault())));
            MainProps = doclists.Select(d => new DocProp(d.mb.key, d.d.keyDocType, d.item?.Key, d.item?.Value.props ?? new Dictionary<string, Dynamic>())).ToList();
            MainExistences = MainBundles.SelectMany(mb => mb.doclist.SelectMany(d => DocExistence.Parse(mb.key, d))).ToList();
            LoadReservations();
        }

        private void LoadReservations()
        {
            TaskBundles.CheckLock();
            AddReservations(TaskBundles.Where(t => t.Realized != true).ToArray());
        }

        List<TaskBundle> RealizedTaskBundles() => contextplus.taskBundles.Query(b => b.invalid != true && b.Realized != true);

        void AddReservations(params TaskBundle[] tbuns)
        {
            var tvals = tbuns
                                .SelectMany(t => t.doclist.SelectMany(d => d.docs.Select(dd => (akey: t.keyAssignment, t.key, d.keyDocType, chainkey: dd.Key, exis: dd.Value))));
            var TM = tvals.Join(MainExistences, t => (t.key, t.keyDocType, t.chainkey),
                                        m => (m.key, m.keyDocType, m.chainkey), (t, m) => (t, m)).ToList();
            TM.ForEach(x => x.m.AddReserve(x.t.akey, x.t.exis));
        }

        void DelReservations(params TaskBundle[] tbuns)
        {
            var tvals = tbuns
                                .SelectMany(t => t.doclist.SelectMany(d => d.docs.Select(dd => (akey: t.keyAssignment, t.key, d.keyDocType, chainkey: dd.Key, exis: dd.Value))));
            var TM = tvals.Join(MainExistences, t => (t.key, t.keyDocType, t.chainkey),
                                        m => (m.key, m.keyDocType, m.chainkey), (t, m) => (t, m)).ToList();
            TM.ForEach(x => x.m.DelReserve(x.t.akey, x.t.exis));
        }

        private void TaskBundles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TaskBundle[] tbuns = null;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    tbuns = e.NewItems.Cast<TaskBundle>().ToArray();
                    AddReservations(tbuns);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    tbuns = e.OldItems.Cast<TaskBundle>().ToArray();
                    DelReservations(tbuns);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    MainExistences.ForEach(i => i.ClearReserve());
                    LoadReservations();
                    break;
            }
        }

        /*		private void Reservations_CollectionChanged(MainBundle mb, BundledDoc d, object sender, NotifyCollectionChangedEventArgs e)
				{
					if (mb == null || d==null || sender==null)
						return;

					var dest = mem.docs.FirstOrDefault(dm => dm.source.keyDocType == d.keyDocType);
					if (dest != null)
						dest.Reload();
				}*/
        private void Doclist_CollectionChanged(MainBundle mb, object sender, NotifyCollectionChangedEventArgs e)
        {
            if (mb == null)
                return;

            lock (MainProps)
            {
                lock (MainExistences)
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            var docs = e.NewItems.Cast<BundledDoc>();
                            docs.ToList().ForEach(d => d.SpecialReqs.CollectionChanged += (s, a) => Spcreqs_CollectionChanged(mb, d, s, a));
                            /*							var DTkeys = docs.Select(d => d.keyDocType).ToArray();
														MainProps.RemoveAll(p => p.key == mb.key && DTkeys.Contains(p.keyDocType));
														MainExistences.RemoveAll(p => p.key == mb.key && DTkeys.Contains(p.keyDocType));
							*/
                            MainProps.RemoveAll(p => p.key == mb.key);
                            MainExistences.RemoveAll(p => p.key == mb.key);
                            var lst = mb.doclist.SelectMany(d => DocFact.Parse(mb.key, d));
                            var lst2 = lst.Select(l => l.Split());
                            var props = lst2.Select(l => l.prop);
                            var exists = lst2.Select(l => l.exis);
                            MainProps.AddRange(props);
                            MainExistences.AddRange(exists);
                            AddReservations(TaskBundles.Where(t => t.Realized != true && t.keyParent == mb.key).ToArray());
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            var DTkeys2 = e.OldItems.Cast<BundledDoc>().Select(d => d.keyDocType).ToArray();
                            MainProps.RemoveAll(p => p.key == mb.key && DTkeys2.Contains(p.keyDocType));
                            MainExistences.RemoveAll(p => p.key == mb.key && DTkeys2.Contains(p.keyDocType));
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            MainProps.RemoveAll(p => p.key == mb.key);
                            MainExistences.RemoveAll(p => p.key == mb.key);
                            break;
                    }
                }
            }
        }

        private void Spcreqs_CollectionChanged(MainBundle tb, BundledDoc d, object sender, NotifyCollectionChangedEventArgs e)
        {
            var tasks = TaskBundles.Where(t => t.doclist.Any(d => d.keyDocType == d.keyDocType));
            tasks.ToList().ForEach(t => t.obsolete = true);
        }

        private void MainBundles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            lock (MainProps)
            {
                lock (MainExistences)
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            var bundles = e.NewItems.Cast<MainBundle>();
                            var doclists = bundles.SelectMany(b => b.doclist.Select(d => (b.key, d)));
                            var facts = doclists.SelectMany(x => DocFact.Parse(x.key, x.d));
                            var splits = facts.Select(f => f.Split());
                            var props = splits.Select(s => s.prop);
                            var exis = splits.Select(s => s.exis);
                            MainProps.AddRange(props);
                            MainExistences.AddRange(exis);

                            var nkeys = e.NewItems.Cast<MainBundle>().Select(b => b.key).ToArray();
                            var tbuns = TaskBundles.Where(t => t.Realized != true && t.invalid != true && nkeys.Contains(t.keyParent)).ToArray();
                            AddReservations(tbuns);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            var keys = e.OldItems.Cast<MainBundle>().Select(b => b.key).ToArray();
                            MainProps.RemoveAll(p => keys.Contains(p.key));
                            MainExistences.RemoveAll(p => keys.Contains(p.key));

                            var tasks = TaskBundles.Where(t => keys.Contains(t.keyParent)).ToArray().Clone() as TaskBundle[];
                            foreach (var task in tasks)
                                TaskBundles.Remove(task);
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            MainProps.Clear();
                            MainExistences.Clear();
                            LoadPropsAndExistences();
                            break;
                    }
                }
            }

            /*			void LoadBundles(IEnumerable<MainBundle> bundles)
						{
							var doclists = bundles.SelectMany(b => b.doclist.Select(d => (b.key, d));
							var facts = doclists.SelectMany(x => DocFact.Parse(x.key, x.d));
							var splits = facts.Select(f => f.Split());
							var props = splits.Select(s => s.prop);
							var exis = splits.Select(s => s.exis);
							MainProps.AddRange(props);
							MainExistences.AddRange(exis);

						}*/
        }

        public IMainBundle MainGet(string key)
        {
            lock (MainBundles)
            {
                var bundle = MainBundles.FirstOrDefault(b => b.key == key);
                if (bundle == null && contextplus.persils.FirstOrDefault(p => p.key == key) != null)
                {
                    MyTracer.TraceWarning2($"The MainBundles buffer has no item with key '{key}'");
                    bundle = new MainBundle(contextplus, key);
                    MyTracer.TraceWarning2($"Try to get it from DB...");
                    var exbundle = contextplus.mainBundles.FirstOrDefault(b => b.key == key);
                    if (exbundle != null)
                    {
                        MyTracer.TraceWarning2($"Bundle '{key}' found in DB");
                        MainBundles.Add(exbundle.Inject<MainBundle>(this));
                        bundle = exbundle;
                    }
                    else
                    {
                        MyTracer.TraceWarning2($"Bundle '{key}' not found in DB, so try to make a new one");
                        MainBundles.Add(bundle.Inject<MainBundle>(this));
                        contextplus.mainBundles.Insert(bundle);
                        contextplus.SaveChanges();
                    }
                }
                return bundle;
            }
        }

        public void BundleReload(string key)
        {
            string[] keys = key.Split(",");
            var bundles = contextplus.mainBundles.Query(x => keys.Contains(x.key)).ToList();

            if (bundles.Count() != 0 || bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    var pos = MainBundles.Select((b, i) => (b.key, i)).FirstOrDefault(x => x.key == ((MainBundle)bundle).key);
                    var bund = MainBundles.FirstOrDefault(x => x.key == ((MainBundle)bundle).key);

                    if (bund != null)
                        MainBundles[pos.i] = (MainBundle)bundle;
                    else
                    {
                        MainBundles.Add((MainBundle)bundle);
                    }
                }
            }

        }

        public (bool OK, string error) ReloadData()
        {
            MainBundles.Clear();
            TaskBundles.Clear();
            MainProps.Clear();
            MainExistences.Clear();

            var persils = CleanPersil;
            lock (MainBundles)
            {
                MainBundles = new ObservableCollection<MainBundle>(contextplus.mainBundles.Query(b => b.invalid != true)
                                            .Join(persils, b => b.key, p => p.key, (b, p) => b.Inject<MainBundle>(this)));
                MyTracer.TraceInfo2($"The MainBundles has been collected with {MainBundles.Count} items");
            }

            lock (TaskBundles)
            {
                TaskBundles = new ObservableCollection<TaskBundle>(contextplus.taskBundles.Query(b => b.invalid != true && b.Realized != true)
                                            .Join(MainBundles, b => b.keyParent, p => p.key, (b, p) => b.Inject<TaskBundle>(this)));
                MyTracer.TraceInfo2($"The TaskBundles has been collected with {TaskBundles.Count} items");
            }

            LoadPropsAndExistences();

            return (true, String.Empty);
        }

        public void PreBundleReload(string key)
        {
            string[] keys = key.Split(",");
            var prebundles = contextplus.preBundles.Query(x => keys.Contains(x.key)).ToList();

            foreach (var prebundle in prebundles)
            {
                var pos = PreBundles.Select((b, i) => (b.key, i)).FirstOrDefault(x => x.key == ((PreBundle)prebundle).key);
                var pre = PreBundles.FirstOrDefault(x => x.key == ((PreBundle)prebundle).key);
                if (pre != null)
                    PreBundles[pos.i] = (PreBundle)prebundle;
                else
                {
                    PreBundles.Add((PreBundle)prebundle);

                }
            }

        }

        public ITaskBundle TaskGet(string key)
        {
            lock (TaskBundles)
            {
                return TaskBundles.FirstOrDefault(b => b.key == key);
            }
        }

        public IPreBundle PreBundleGet(string key)
        {
            lock (PreBundles)
            {
                var preBundle = PreBundles.FirstOrDefault(b => b.key == key);
                if (preBundle == null)
                {
                    var exbundle = contextplus.preBundles.FirstOrDefault(b => b.key == key);
                    if (exbundle != null)
                    {
                        MyTracer.TraceWarning2($"Bundle '{key}' found in DB");
                        PreBundles.Add(exbundle.Inject<PreBundle>(this));
                        preBundle = exbundle;
                    }
                }
                return preBundle;
            }
        }

        public List<ITaskBundle> ChildrenList(string key)
        {
            TaskBundles.CheckLock();
            return TaskBundles.Where(t => t.keyParent == key).ToList<ITaskBundle>();
        }
        public TaskBundle[] TaskList(string akey)
        {
            TaskBundles.CheckLock();
            return TaskBundles.Where(t => t.keyAssignment == akey).ToArray();
        }

        public DocEx[] Available(string key)
            => MainExistences.Where(x => x.key == key).Select(x => new DocEx(x.key, x.keyDocType, x.chainkey, x.available))
                .ToArray();

        public DocEx[] Reserved(string key)
            => MainExistences.Where(x => x.key == key).Select(x => new DocEx(x.key, x.keyDocType, x.chainkey, x.Reserved))
                .ToArray();

        public DocEx[] Current(string key)
            => MainExistences.Where(x => x.key == key).Select(x => new DocEx(x.key, x.keyDocType, x.chainkey, x.current))
                .ToArray();


        public (ITaskBundle bundle, string reason) MakeTaskBundle(string token, string asgdtlkey, bool save = true)
        {
            TaskBundles.CheckLock();
            var user = contextplus.FindUser(token);
            var asgn = assign.GetAssignmentOfDtl(asgdtlkey).GetAwaiter().GetResult() as Assignment;
            if (asgn == null)
                return (null, "Penugasan tidak ada");
            var asgdtl = asgn.details.First(d => d.key == asgdtlkey);
            var persil = asgdtl.persil(contextplus);
            if (persil == null)
                return (null, "Bidang yang dipilih tidak ada");
            var bundle = (MainBundle)MainGet(asgdtl.keyPersil);
            if (bundle == null)
                return (null, "Bundle utama tidak ada");
            return DoMakeTaskBundle(asgn, asgdtl, bundle, user, save);
        }

        public (ITaskBundle bundle, string reason) DoMakeTaskBundle(Assignment asgn, AssignmentDtl asgdtl, MainBundle bundle, user user, bool save = true, string token = null)
        {
            if (token != null)
            {
                var pass = user.pass;
                if (pass == null || pass.token != token)
                {
                    Pass mpass = user.mpass;
                    if (mpass == null || mpass.token != token)
                        throw new Exception("The given token is not match with the given user");
                }
            }

            var persil = asgdtl.persil(contextplus);
            if (bundle == null)
                bundle = (MainBundle)MainGet(asgdtl.keyPersil);
            if (bundle == null)
                return (null, "Bundle utama tidak ada");
            lock (bundle) //prevents another creation of taskbundle for the same persil/main bundle
            {
                var step = asgn.step.Value;

                var tbun = TaskBundles.FirstOrDefault(b => b.keyParent == asgn.key);//bundle.children().FirstOrDefault(b => b.keyParent == asgn.key);
                if (tbun != null)
                {
                    if (tbun.obsolete != true)
                    {
                        var reason = tbun.IsComplete() ? null : "Ada kebutuhan dokumen yang belum terpenuhi";
                        return (tbun, reason);
                    }
                    if (tbun.Realized == true)
                        DelReservations(tbun);
                    TaskBundles.Remove(tbun);
                    contextplus.taskBundles.Remove(tbun);
                    contextplus.SaveChanges();
                }
                tbun = new TaskBundle(bundle, asgn);
                var stepreq = StepDocType.GetItem(step, persil.Discriminator);
                if (stepreq == null)
                    return (null, "Kebutuhan dokumen untuk proses dan kategori ini tidak didefinisikan");
                var reqlist = stepreq.send.Where(r => FilterDT(persil.Discriminator, r.keyDocType) && r.req).GroupBy(r => r.key).Select(g => (keyDocType: g.Key, exis: g.Select(d => d.ex).ToArray().Convert()));
                //tbun.missing = reqlist;

                var spcreqs = bundle.doclist.SelectMany(d => d.SpecialReqs.Select(r => (d.keyDocType, r)))
                                                .Where(r => r.r.step == step).ToList();

                var bdocs = bundle.doclist.Join(MainExistences, d => (bundle.key, d.keyDocType), x => (x.key, x.keyDocType),
                                    (b, x) => (x.keyDocType, x.chainkey, available: x.availableCopy));

                //var cprops = MainProps.Where(p=>p.props.Values)
                var props = MainProps.Join(bdocs, p => (p.key, p.keyDocType), x => (bundle.key, x.keyDocType), (p, x) => p);
                var spcr = spcreqs.Where(s => s.r.step == asgn.step).Select((r, i) => (r, i)).ToArray();
                var sreq = spcr.Join(props, x => x.r.keyDocType, p => p.keyDocType, (x, p) => (x.i, x.r.keyDocType, x.r.r, p))
                                            .Where(x => x.p.props.Any(p => p.Key == x.r.prop.Key && p.Value == x.r.prop.Value));
                sreq.Select(s => s.i).OrderByDescending(i => i).ToList().ForEach(i => spcreqs.RemoveAt(i));

                var reqsupp = sreq.GroupBy(x => x.keyDocType)
                    .Select(g => (keyDocType: g.Key, exis: g.Select(d => d.r.exis).ToArray().Convert()));
                reqlist = reqlist.Union(reqsupp).ToArray();

                var bdocs1 = bdocs.Join(reqlist, d => d.keyDocType, r => r.keyDocType,
                                        (d, r) => (d.keyDocType, d.chainkey, reqs: r.exis, subs: d.available.Sub(r.exis))).ToList();
                var bdocsavail = bdocs1.Where(d => d.subs.All(s => s >= 0))
                                                .Select(b => (b.keyDocType, b.chainkey, reqs: joinex(b.reqs, b.subs)));
                var bdocsmiss = bdocs1.Where(d => d.subs.Any(s => s < 0));

                tbun.doclist = bdocsavail.GroupBy(d => d.keyDocType).Select(g =>
                                new DocSeries
                                {
                                    keyDocType = g.Key,
                                    docs = new FixExistencesDocChain(g.Select(d => new KeyValuePair<string, int[]>(d.chainkey, d.reqs)))
                                }).ToArray();
                tbun.missing = bdocsmiss.SelectMany(d => d.reqs.Where(r => r > 0).Select((r, i) => (d.keyDocType, d.chainkey, ex: (Existence)i)))
                                    .Where(d => d.ex != Existence.Avoid)
                                    .Select(d =>
                                     new DocRequirement { keyDocType = d.keyDocType, key = d.chainkey, ex = d.ex }).ToArray();
                tbun.spcreqs = spcreqs.Select(s => new DocProcessReq(s.keyDocType, s.r)).ToArray();

                var reas = "";
                if (tbun.missing.Any())
                    reas = "Ada kebutuhan dokumen yang belum tersedia; ";
                if (tbun.spcreqs.Any())
                    reas += "Ada kebutuhan dokumen khusus yang belum tersedia";

                TaskBundles.Add(tbun);
                asgdtl.keyBundle = tbun.key;

                assign.Update(asgn, false).Wait();
                contextplus.mainBundles.Update(bundle);
                contextplus.taskBundles.Insert(tbun);
                if (save)
                    contextplus.SaveChanges();

                return (tbun, reas);
            }

            int[] joinex(int[] reqs, int[] subs)
            {
                var ret = reqs.Clone() as int[];
                ret[5] = subs[5];
                return ret;
            }

        }

        public void Realize(string token, string asgnkey)
        {
            var user = contextplus.FindUser(token);
            var assign = (Assignment)this.assign.GetAssignment(asgnkey).GetAwaiter().GetResult();
            if (assign == null)
                throw new InvalidOperationException("Penugasan tidak ada");

            var tbuns = TaskBundles.Where(t => t.keyAssignment == asgnkey);
            if (!tbuns.Any())
                throw new InvalidOperationException("Penugasan dimaksud tidak memiliki bundle penugasan sama sekali");

            var kbns = assign.details.Select(d => d.keyBundle).ToArray();
            var cnt = kbns.Intersect(tbuns.Select(t => t.key)).Count();
            if (cnt != kbns.Length)
                throw new InvalidOperationException("Belum semua bidang dalam Penugasan ini memiliki bundle penugasan");
            tbuns = tbuns.Join(kbns, t => t.key, tb => tb, (t, tb) => t);

            if (tbuns.Any(t => !t.IsComplete()))
                throw new InvalidOperationException("Ada bundle penugasan yang isinya belum lengkap");

            tbuns.ToList().ForEach(t => t.Realized = true);
            contextplus.taskBundles.Update(tbuns);
            contextplus.SaveChanges();
        }

        public void ContextCommit()
        {
            contextplus.SaveChanges();
        }

        public void ContextRollback()
        {
            contextplus.DiscardChanges();
        }
        /*		public bool Reserve(string token, string key, string asgnkey)
				{
					var user = contextplus.FindUser(token);
					var bundle = MainGet(key);
					if (bundle == null)
						return false;
					var assignment = contextplus.assignments.FirstOrDefault(a => a.key == asgnkey);
					if (assignment == null)
						return false;
					var step = assignment.step;
					if (step == null)
						return false;

					var persil = bundle.persil();
					var required = StepDocType.GetItem(step.Value, persil.Discriminator)?
													.send.Select(s => (keyDT: s.key, exis: (s.ex, s.req)))
													.GroupBy(s => s.keyDT).Select(g => (keyDT: g.Key, exs: g.Select(d => d.exis).ToArray()))
													.ToArray();
					var OKs = bundle.doclist.Join(required, d => d.keyDocType, r => r.keyDT, (d, r) => d.Reserve(asgnkey, r.exs));
					if (OKs.Any(o => !o))
					{
						bundle.AbortReserve(asgnkey);
						return false;
					}
					contextplus.mainBundles.Update(bundle);
					contextplus.SaveChanges();
					return true;
				}*/

        public void AddProcessRequirement(string token, string key, DocProcessStep step, string keyDocType, KeyValuePair<string, Dynamic> prop, Existence ex)
        {
            var user = contextplus.FindUser(token);
            var bundle = (MainBundle)MainGet(key);
            if (bundle == null)
                throw new Exception("Bundle tidak ada");
            var docl = bundle.doclist.First(d => d.keyDocType == keyDocType);
            if (docl == null)
                throw new Exception("Jenis dokumen tidak ada dalam bundle");
            if (docl.SpecialReqs.Any(p => p.exis == ex && p.step == step && p.prop.Key == prop.Key))
                throw new Exception("Request untuk proses, property dan eksistensi dimaksud sudah ada");

            var req = new ProcessReq { exis = ex, step = step, prop = prop };
            docl.SpecialReqs.Add(req);

            contextplus.mainBundles.Update(bundle);
            var tbun = TaskBundles.FirstOrDefault(b => b.keyParent == key);
            if (tbun != null)
            {
                var lst = tbun.spcreqs.ToList();
                lst.Add(new DocProcessReq(keyDocType, req));
                tbun.spcreqs = lst.ToArray();
                contextplus.taskBundles.Update(tbun);
            }
            contextplus.SaveChanges();
        }

        //public IEnumerable<PersilStat> LastPosition()
        //{
        //	var items = LastPositionDiscrete();
        //	var luasPbts = items.GroupBy(x => x.noPBT).Select(g => (g.Key, luas: g.Sum(d => d.luasPBT ?? 0))).ToArray();
        //	items = items.Join(luasPbts, u => u.noPBT, l => l.Key, (u, l) => u.SetLuasPropsPBT(l.luas)).ToArray();

        //	var luasHgbs = items.GroupBy(x => x.noHGB).Select(g => (g.Key, luas: g.Sum(d => d.luasHGB ?? 0))).ToArray();
        //	items = items.Join(luasHgbs, u => u.noHGB, l => l.Key, (u, l) => u.SetLuasPropsHGB(l.luas)).ToArray();

        //	var normals = items.Where(i => i.noHGB == null && i.noPBT == null).ToArray();
        //	var unifieds = items.Where(i => i.noPBT != null && i.noHGB == null).ToArray();
        //	var endeds = items.Where(i => i.noHGB != null).ToArray();


        //	var gnormals = normals.GroupBy(x => (x.keyProject, x.keyDesa, x.keyPTSK, x.keyPenampung, x.category,
        //							x.step))
        //						.Select(g => new PersilStat(g.Key.keyProject, g.Key.keyDesa, g.Key.keyPTSK, g.Key.keyPenampung, g.Key.category,
        //						g.Key.step, null, g.Count())
        //						{
        //							luasDibayar = g.Sum(d => d.luasDibayar),
        //							luasSurat = g.Sum(d => d.luasSurat),
        //						}).ToArray();

        //	var gunifieds = unifieds.GroupBy(x => (x.keyProject, x.keyDesa, x.keyPTSK, x.keyPenampung, x.category,
        //							x.step))
        //						.Select(g => new PersilStat(g.Key.keyProject, g.Key.keyDesa, g.Key.keyPTSK, g.Key.keyPenampung, g.Key.category,
        //						g.Key.step, null, g.Count())
        //						{
        //							luasDibayar = g.Sum(d => d.luasDibayar),
        //							luasSurat = g.Sum(d => d.luasSurat),
        //							luasPBT = g.Sum(d => d.luasPropsPBT)
        //						}).ToArray();

        //	var gendeds = endeds.GroupBy(x => (x.keyProject, x.keyDesa, x.keyPTSK, x.keyPenampung, x.category,
        //							x.step))
        //						.Select(g => new PersilStat(g.Key.keyProject, g.Key.keyDesa, g.Key.keyPTSK, g.Key.keyPenampung, g.Key.category,
        //						g.Key.step, null, g.Count())
        //						{
        //							luasDibayar = g.Sum(d => d.luasDibayar),
        //							luasSurat = g.Sum(d => d.luasSurat),
        //							luasPBT = g.Sum(d => d.luasPropsPBT),
        //							luasHGB = g.Sum(d => d.luasPropsHGB)
        //						}).ToArray();

        //	return gnormals.Union(gunifieds).Union(gendeds);
        //}

        /*IEnumerable<(string key, string disc, DocProcessStep next)> NextSteps(Persil[] persils = null, params Assignment[] assigns)
		{
			if (persils == null)
				persils = contextplus.persils.Query(p => (p.en_state == null || p.en_state == StatusBidang.bebas) && p.invalid != true &&
											p.basic != null && p.basic.current != null)
#if _HIBAH_ONLY_
					.Where(p => p is PersilHibah)
#endif
					.ToArray();
			if (persils.Length == 0)
				return new List<(string, string, DocProcessStep)>();

			if (assigns == null || assigns.Length == 0)
				assigns = AssignHost.OpenedAssignments().Cast<Assignment>().ToArray();

			var akeys = assigns.SelectMany(d => d.details.Select(dd => dd.keyPersil)).Distinct().ToArray();

			// all availability of props should be treat as existence of all document kind
			var availables0 = MainProps.Where(p => p.props.Any(pp => pp.Value != null && pp.Value.val != null))
				.Select(a => new DocExistence(a.key, a.keyDocType, a.chainkey, new[] { 0, 1, 1, 1, 1, 0 })).ToArray();
			//			var availables1 = MainExistences.Where(p => p.availableCopy.Any(pp => pp.Value != null && pp.Value.val != null))
			//				.Select(a => new DocExistence(a.key, a.keyDocType, a.chainkey, new[] { 0, 1, 1, 1, 1, 0 })).ToArray();
			//
			var availables = MainExistences.Where(m => m.available.IsExists()).ToArray();

			var usedkeys = akeys.Select(d => d).Distinct();
			var freepersilkeys = persils.Select(p => p.key).Except(usedkeys).ToArray();
			var freepersils = persils.Join(freepersilkeys, p => p.key, x => x, (p, x) => p).ToArray();

			var persilavs = freepersils.Join(availables, p => p.key, a => a.key, (p, a) => (p.key, p.Discriminator, a));
			var keyavs = persilavs.Select(p => p.key).ToArray();
			var persilavs0 = freepersils.Where(p => !keyavs.Contains(p.key)).Join(availables0, p => p.key, a => a.key, (p, a) => (p.key, p.Discriminator, a));
			var persilavs1 = persilavs.Union(persilavs0).ToArray();

			var receives = StepDocType.List.Where(s => s.invalid != true && s.receive.Any(r => r.req))
									.Join(Assignment.StepOrders.SelectMany(so => so.Value.Select(v => (so.Key, v.step))), s => (s.disc, s._step), so => (so.Key, so.step),
										(s, so) => s)
									.SelectMany(s => s.receive.Select(r => (s.disc, step: s._step, r.keyDocType, chain: (string)null, r.ex, r.req)))
									.ToArray();
			var in_requireds = receives.GroupBy(r => (r.disc, r.step, r.keyDocType))
									.Select(g => (g.Key.disc, g.Key.step, g.Key.keyDocType,
													exis: g.Select(d => new Existency(d.ex, d.req ? 1 : 0)).ToArray().Convert()));
			var docexists = persilavs1.Join(in_requireds, p => (p.Discriminator, p.a.keyDocType), i => (i.disc, i.keyDocType), (p, i) => (p, i))
									.Select(x => (x.p.key, x.i.disc, x.i.step, x.i.keyDocType, chain: x.p.a.chainkey, exis: x.p.a.available.Sub(x.i.exis).IsExists()))
									.GroupBy(x => (x.key, x.disc, x.step))
									.Select(g => (g.Key.key, g.Key.disc, g.Key.step,
												combo: g.GroupBy(d => d.keyDocType).Select(g1 => (keyDocType: g1.Key,
																 chains: g1.Select(d1 => (d1.chain, d1.exis)).ToArray())).ToArray()));
			//.Where(x=>x.combo.);
			var allgot = docexists.Where(x => x.combo.All(c => c.chains.All(p => p.exis)))
								.GroupBy(x => (x.key, x.disc))
								.Select(g => (g.Key.key, g.Key.disc,
										step: Assignment.GetNext(g.Key.disc, Assignment.GetLatest(g.Key.disc, g.Select(d => d.step).ToArray())))).ToArray();
			var completekeys = allgot.Where(g => g.step == null).Select(p => p.key).ToArray();
			var allgotkeys = allgot.Select(g => g.key);
			var notgot = freepersils.Where(p => !allgotkeys.Contains(p.key))
									.Select(p => (p.key, disc: p.Discriminator,
													step: (DocProcessStep?)DocProcessStep.Akta_Notaris)).ToArray();
			allgot = allgot.Where(p => !completekeys.Contains(p.key)).ToArray();
			return allgot.Union(notgot).Select(g => (g.key, g.disc, g.step.Value));
		}*/

        IEnumerable<(string key, string disc, DocProcessStep next)> NextSteps(Persil[] persils = null, params Assignment[] assigns)
        {
            var items = LastPositionDiscrete(persils, assigns);
            return items.Select(p => (p.key, disc: p.category.Discriminator(),
                                                    next: Assignment.GetNext(p.category.Discriminator(), p.step) ?? DocProcessStep.Belum_Bebas))
                            .Where(p => p.next != DocProcessStep.Belum_Bebas).ToArray();
        }

        IEnumerable<(PersilPositionView ppv, DocProcessStep next)> NextSteps2(bool inclActive)
        {
            var items = SavedPersilPosition(inclActive);
            return items.Select(p => (ppv: p,
                                                    next: Assignment.GetNext(p.category.Discriminator(), p.step) ?? DocProcessStep.Belum_Bebas))
                            .Where(p => p.next != DocProcessStep.Belum_Bebas).ToArray();
        }

        /*public */
        IEnumerable<PersilNx> NextReadies(bool loose = false)
        {
            var persils = CleanPersil;
            if (persils.Length == 0)
                return new PersilNx[0];

            var assigns = assign.OpenedAssignments(null).GetAwaiter().GetResult()?.Cast<Assignment>().ToArray() ?? new Assignment[0];

            var nexts = NextSteps(persils, assigns);
            IEnumerable<(string key, string disc, DocProcessStep step)> phase1;
            if (loose)
                phase1 = nexts;
            else
            {
                var soes = Assignment.StepOrders.SelectMany(so => so.Value.Select(v => (so.Key, v.step)));
                var sends = StepDocType.List.Where(s => s.invalid != true && s.send.Any())
                                        .Join(soes, s => (s.disc, s._step), so => (so.Key, so.step),
                                                                    (s, so) => s)
                                        .SelectMany(s => s.send.Where(ss => FilterDT(s.disc, ss.keyDocType) && ss.req)
                                                .Select(r => (s.disc, s._step, r.keyDocType, chain: (string)null, r.ex, r.req))
                                            );

                phase1 = CollectPhase1(sends, nexts);
            }
            var phase2 = phase1.Join(persils, x => x.key, p => p.key, (x, p) => (x, basic: p.basic.current))
                                    .Select(x => (x.x, x.basic.keyProject, x.basic.keyDesa, keyComp: x.basic.keyPTSK ?? x.basic.keyPenampung))
                                .GroupBy(x => (x.x.disc, x.keyProject, x.keyDesa, x.keyComp, x.x.step))
                                .Select(g => (g.Key.disc, g.Key.keyProject, g.Key.keyDesa, g.Key.keyComp, g.Key.step, count: g.Count()))
                                .Select(x => new PersilNx(x.keyProject, x.keyDesa, DecideCompany(x.keyComp),
                                (x.disc ?? "").Category(), x.step, x.count)).ToArray();
            return phase2;
        }

        public IEnumerable<PersilNxDiscrete> NextReadiesDiscrete(bool loose = false, bool inclActive = false)
        {
            var nexts = NextSteps2(inclActive);
            IEnumerable<(PersilPositionView ppv, DocProcessStep next)> phase1;
            if (loose)
                phase1 = nexts;
            else
            {
                var sends = StepDocType.List.Where(s => s.invalid != true && s.send.Any())
                                    .Join(Assignment.StepOrders.SelectMany(so => so.Value.Select(v => (so.Key, v.step))),
                                        s => (s.disc, s._step), so => (so.Key, so.step),
                                        (s, so) => s)
                                    .SelectMany(s => s.send.Where(ss => FilterDT(s.disc, ss.keyDocType) && ss.req)
                                    .Select(r => (s.disc, step: s._step, r.keyDocType, chain: (string)null, r.ex, r.req)));
                phase1 = CollectPhase1_1(sends, nexts);
            }
            return phase1.Select(p => new PersilNxDiscrete(p.ppv, p.next)).ToArray();
        }

        (string, string) DecideCompany(string key)
        {
            var comp = contextplus.companies.FirstOrDefault(c => c.key == key);
            string stnull = null;
            if (comp == null)
                return (stnull, stnull);
            return comp.status == StatusPT.pembeli ? (key, stnull) : comp.status == StatusPT.penampung ? (stnull, key) : (stnull, stnull);
        }


        IEnumerable<(string key, string disc, DocProcessStep step)> CollectPhase1(
                    IEnumerable<(string disc, DocProcessStep step, string keyDocType, string chain, Existence ex, bool req)> sends,
                    IEnumerable<(string key, string disc, DocProcessStep next)> nexts)
        {
            var out_requireds = sends.GroupBy(r => (r.disc, r.step, r.keyDocType))
                        .Select(g => (g.Key.disc, g.Key.step, g.Key.keyDocType,
                                        exis: g.Select(d => new Existency(d.ex, d.req ? 1 : 0)).ToArray().Convert()));

            var nextstepsrn = nexts.GroupJoin(out_requireds, n => (n.disc, n.next), r => (r.disc, r.step),
                                    (n, sr) => (n, sr))
                    .SelectMany(x => x.sr.Select(rr => (x.n, r: rr)));

            var phase0 = nextstepsrn.Where(x => x.r.disc == null).Select(x => x.n);

            var nextsteps = nextstepsrn.Where(x => x.r.disc != null)
                            .Select(x => (x.n.key, x.n.disc, step: x.n.next, x.r.keyDocType, x.r.exis));

            var availables = MainExistences.Where(m => !m.available.IsZero()).ToArray();
            var persilavs1 = nextsteps.Join(availables, p => (p.key, p.keyDocType), a => (a.key, a.keyDocType),
                                (p, a) => (p.key, p.disc, p.step, p.keyDocType, p.exis, a.chainkey, available: a.availableCopy))
                    .Select(x => (x.key, x.disc, x.step, x.chainkey, x.available, x.exis, fit: x.available.Sub(x.exis).IsExists())).ToArray();
            var persilavs = persilavs1.GroupBy(x => (x.key, x.disc, x.step))
                .Select(g => (g.Key.key, g.Key.disc, g.Key.step, facts: g.Select(d => (d.chainkey, d.available, d.fit)).ToArray())).ToArray();

            var phase1 = persilavs.Where(x => x.facts.All(f => f.fit)).ToArray();

            var bdocs = MainBundles.Join(persilavs, b => b.key, p => p.key, (b, p) => (b.key, b.doclist))
                                    .SelectMany(b => b.doclist.Select(d => (b.key, d)));

            // all requireds doc props
            var allspcreqs = bdocs.SelectMany(x => x.d.SpecialReqs.Select(d => (x.key, x.d, SpecialReqs: d))).ToList();
            var spcreqs = allspcreqs.Select(r => (r.key, r.d.keyDocType, r.SpecialReqs))
                                    .Select(x => (x.key, x.keyDocType, x.SpecialReqs.step, prop: x.SpecialReqs.prop.Key, val: x.SpecialReqs.prop.Value,
                                    x.SpecialReqs.exis));

            var props = MainProps.Join(bdocs, p => (p.key, p.keyDocType), x => (x.key, x.d.keyDocType), (p, x) => p);
            var reqsupp = spcreqs.Join(props, x => (x.key, x.keyDocType), p => (p.key, p.keyDocType),
                            (x, p) => (x.key, x.keyDocType, x.step, x.prop, x.val, x.exis, p.props))
                                        .Where(x => x.props.Any(p => p.Key == x.prop && p.Value == x.val))
                                        .Join(availables, p => (p.key, p.keyDocType), a => (a.key, a.keyDocType),
                                                    (p, a) => (p, fit: a.availableCopy[(int)p.exis] >= 0))
                                        .Where(x => x.fit)
                                        .Select(x => (x.p.key, x.p.keyDocType, x.p.step));
            var spcs = allspcreqs.Select((a, i) => (a, i)).Join(reqsupp, a => (a.a.key, a.a.d.keyDocType, a.a.SpecialReqs.step),
                                r => (r.key, r.keyDocType, r.step), (a, r) => a.i).OrderByDescending(i => i).ToList();
            spcs.ForEach(i => allspcreqs.RemoveAt(i)); // remaining allspcreqs are the still required doc props

            // exclusion persils keys
            var missingreqs = allspcreqs.Select(s => s.key).Distinct();

            phase1 = phase1.Where(p => !missingreqs.Contains(p.key)).ToArray();
            return phase1.Select(p => (p.key, p.disc, p.step)).Union(phase0.Select(p => (p.key, p.disc, step: p.next)));
        }

        DocProcessStep[] looses =
#if _LOOSE_DOCS_
            new[] { DocProcessStep.Akta_Notaris, DocProcessStep.Baru_Bebas, DocProcessStep.SHM_Hibah };
#else
			new DocProcessStep[0];
#endif

        IEnumerable<(PersilPositionView ppv, DocProcessStep next)> CollectPhase1_1(
                    IEnumerable<(string disc, DocProcessStep step, string keyDocType, string chain, Existence ex, bool req)> sends,
                    IEnumerable<(PersilPositionView ppv, DocProcessStep next)> nexts)
        {
            var out_requireds = sends.GroupBy(r => (r.disc, r.step, r.keyDocType))
                        .Select(g => (g.Key.disc, g.Key.step, g.Key.keyDocType,
                                        exis: g.Select(d => new Existency(d.ex, d.req ? 1 : 0)).ToArray().Convert()));

            var nexts2 = nexts.Select(x => (x.ppv, disc: x.ppv.category.Discriminator(), x.ppv.step, x.next)).ToArray();
            var nextstepsrn = nexts2.GroupJoin(out_requireds, n => (n.disc, n.next), r => (r.disc, r.step),
                                    (n, sr) => (n, sr))
                    .SelectMany(x => x.sr.Select(rr => (x.n, r: rr)));

            var phase0 = nextstepsrn.Where(x => x.r.disc == null || !looses.Contains(x.r.step)).Select(x => x.n);

            var nextsteps = nextstepsrn.Where(x => x.r.disc != null && looses.Contains(x.r.step))
                            .Select(x => (x.n.ppv, x.n.disc, step: x.n.next, x.r.keyDocType, x.r.exis));

            var availables = MainExistences.Where(m => !m.available.IsZero()).ToArray();
            var persilavs1 = nextsteps.Join(availables, p => (p.ppv.key, p.keyDocType), a => (a.key, a.keyDocType),
                                (p, a) => (p.ppv, p.disc, next: p.step, p.keyDocType, p.exis, a.chainkey, available: a.availableCopy))
                    .Select(x => (x.ppv, x.disc, x.next, x.chainkey, x.available, x.exis, fit: x.available.Sub(x.exis).IsExists())).ToArray();
            var persilavs = persilavs1.GroupBy(x => (x.ppv.key, x.disc, x.next))
                .Select(g => (g.First().ppv, g.Key.disc, g.Key.next, facts: g.Select(d => (d.chainkey, d.available, d.fit)).ToArray())).ToArray();

            var phase1 = persilavs.Where(x => x.facts.All(f => f.fit)).ToArray();

            var bdocs = MainBundles.Join(persilavs, b => b.key, p => p.ppv.key, (b, p) => (b.key, b.doclist))
                                    .SelectMany(b => b.doclist.Select(d => (b.key, d)));

            // all requireds doc props
            var allspcreqs = bdocs.SelectMany(x => x.d.SpecialReqs.Select(d => (x.key, x.d, SpecialReqs: d))).ToList();
            var spcreqs = allspcreqs.Select(r => (r.key, r.d.keyDocType, r.SpecialReqs))
                                    .Select(x => (x.key, x.keyDocType, x.SpecialReqs.step, prop: x.SpecialReqs.prop.Key, val: x.SpecialReqs.prop.Value,
                                    x.SpecialReqs.exis));

            var props = MainProps.Join(bdocs, p => (p.key, p.keyDocType), x => (x.key, x.d.keyDocType), (p, x) => p);
            var reqsupp = spcreqs.Join(props, x => (x.key, x.keyDocType), p => (p.key, p.keyDocType),
                            (x, p) => (x.key, x.keyDocType, x.step, x.prop, x.val, x.exis, p.props))
                                        .Where(x => x.props.Any(p => p.Key == x.prop && p.Value == x.val))
                                        .Join(availables, p => (p.key, p.keyDocType), a => (a.key, a.keyDocType),
                                                    (p, a) => (p, fit: a.availableCopy[(int)p.exis] >= 0))
                                        .Where(x => x.fit)
                                        .Select(x => (x.p.key, x.p.keyDocType, x.p.step));
            var spcs = allspcreqs.Select((a, i) => (a, i)).Join(reqsupp, a => (a.a.key, a.a.d.keyDocType, a.a.SpecialReqs.step),
                                r => (r.key, r.keyDocType, r.step), (a, r) => a.i).OrderByDescending(i => i).ToList();
            spcs.ForEach(i => allspcreqs.RemoveAt(i)); // remaining allspcreqs are the still required doc props

            // exclusion persils keys
            var missingreqs = allspcreqs.Select(s => s.key).Distinct();

            phase1 = phase1.Where(p => !missingreqs.Contains(p.ppv.key)).ToArray();
            return phase1.Select(p => (p.ppv, p.next)).Union(phase0.Select(p => (p.ppv, p.next)));
        }

        /*public */
        IEnumerable<Persil> NextReadies(string akey, bool loose = false, params string[] exceptkeys)
        {
            if (!(assign.GetAssignment(akey).GetAwaiter().GetResult() is Assignment asgn))
                return new Persil[0];

            var persils = CleanPersil;
            if (persils.Length == 0)
                return new Persil[0];

            var exckeys = asgn.details.Select(d => d.keyPersil).Union(exceptkeys);
            persils = persils.Where(p => p.basic.current.keyDesa == asgn.keyDesa && (
                        p.basic.current.keyPTSK == (asgn.keyPTSK ?? asgn.key) ||
                        p.basic.current.keyPenampung == (asgn.keyPenampung ?? asgn.key)) &&
                        !exckeys.Contains(p.key)).ToArray();

            var nexts = NextSteps(persils, asgn).Where(x => x.next == asgn.step.Value);
            IEnumerable<(string key, string disc, DocProcessStep step)> phase1;
            if (loose)
                phase1 = nexts;
            else
            {
                var sends = StepDocType.List.Where(s => s.invalid != true && s._step == asgn.step.Value && s.send.Any())
                                    .Join(Assignment.StepOrders.SelectMany(so => so.Value.Select(v => (so.Key, v.step))),
                                        s => (s.disc, s._step), so => (so.Key, so.step),
                                        (s, so) => s)
                                    .SelectMany(s => s.send.Where(ss => FilterDT(s.disc, ss.keyDocType) && ss.req).Select(r => (s.disc, step: s._step, r.keyDocType, chain: (string)null, r.ex, r.req)));
                phase1 = CollectPhase1(sends, nexts);
            }
            var phase2 = phase1.Join(persils, x => x.key, p => p.key, (x, p) => p).ToArray();
            return phase2;
        }

        /*public */
        IEnumerable<Persil> NextReadiesDtl(string keyDesa, string keyComp, string disc, DocProcessStep step)
        {
            var persils = CleanPersil;
            if (persils.Length == 0)
                return new Persil[0];

            var assigns = assign.OpenedAssignments(null).GetAwaiter().GetResult()?.Cast<Assignment>().ToArray();

            var nexts = NextSteps(persils, assigns).Where(n => n.next == step).ToArray();
            IEnumerable<(string key, string disc, DocProcessStep step)> phase1;
            var sends = StepDocType.List.Where(s => s.invalid != true && s._step == step && s.send.Any())
                                    .Join(Assignment.StepOrders.SelectMany(so => so.Value.Select(v => (so.Key, v.step))), s => (s.disc, s._step), so => (so.Key, so.step),
                                        (s, so) => s)
                                    .SelectMany(s => s.send.Where(ss => FilterDT(s.disc, ss.keyDocType) && ss.req).Select(r => (s.disc, step: s._step, r.keyDocType, chain: (string)null, r.ex, r.req)));

            phase1 = CollectPhase1(sends, nexts);

            var phase2 = phase1.Join(persils, x => x.key, p => p.key, (x, p) => p).ToArray();
            return phase2;
        }

        public IEnumerable<DocProp> ListProps(string key) =>
            MainProps.Where(p => p.key == key).ToArray();

        public bool MainUpdate(IMainBundle bundle, bool dbSave = true)
        {
            if (bundle == null)
                return false;
            var pos = MainBundles.Select((b, i) => (b.key, i)).FirstOrDefault(x => x.key == ((MainBundle)bundle).key);
            if (pos.key == null)
                return false;
            MainBundles[pos.i] = (MainBundle)bundle;

            contextplus.mainBundles.Update((MainBundle)bundle);
            if (dbSave)
            {
                contextplus.SaveChanges();

                var stages = new[]{
                    $"<$match:<key:'{((MainBundle)bundle).key}'>>".ToJsonFilter(),
                    "{$merge:{ into: 'material_main_bundle_core',on: 'key',whenMatched: 'replace',whenNotMatched: 'insert'}}"
                };
                contextplus.GetDocuments<BsonDocument>(null, "main_bundle_core_m", stages);
            }
            return true;
        }

        public bool TaskUpdateEx(string key, bool dbSave = true)
        {
            var tbundle = TaskBundles.Where(b => b.keyParent == key).ToList();
            if (tbundle == null)
                return false;
            if (tbundle.Count > 0)
            {
                contextplus.taskBundles.Update(tbundle);
                if (dbSave)
                    contextplus.SaveChanges();
            }
            return true;
        }

        public bool TaskUpdate(ITaskBundle bundle, bool dbSave = true)
        {
            if (bundle == null)
                return false;
            var pos = TaskBundles.Select((b, i) => (b.key, i)).FirstOrDefault(x => x.key == ((TaskBundle)bundle).key);
            if (pos.key == null)
                return false;
            TaskBundles[pos.i] = (TaskBundle)bundle;

            contextplus.taskBundles.Update((TaskBundle)bundle);
            if (dbSave)
                contextplus.SaveChanges();
            return true;
        }

        public bool PreUpdate(IPreBundle preBundle, bool dbSave = true)
        {
            if (preBundle == null)
                return false;
            var pos = PreBundles.Select((b, i) => (b.key, i)).FirstOrDefault(x => x.key == ((PreBundle)preBundle).key);
            if (pos.key == null)
                return false;
            PreBundles[pos.i] = (PreBundle)preBundle;

            contextplus.preBundles.Update((PreBundle)preBundle);
            if (dbSave)
            {
                contextplus.SaveChanges();

                //var stages = new[]{
                //	$"<$match:<key:'{((PreBundle)preBundle).key}'>>".ToJsonFilter(),
                //	"{$merge:{ into: 'material_main_bundle_core',on: 'key',whenMatched: 'replace',whenNotMatched: 'insert'}}"
                //};
                //contextplus.GetDocuments<BsonDocument>(null, "main_bundle_core_m", stages);
            }
            return true;
        }


        public bool MainDelete(string key, bool dbSave = true)
        {
            var bundle = MainBundles.FirstOrDefault(b => b.key == key);
            if (bundle == null)
                return false;
            contextplus.mainBundles.Remove(bundle);
            if (dbSave)
            {
                contextplus.SaveChanges();
                contextplus.db.GetCollection<BsonDocument>("material_main_bundle_core").DeleteOne($"<key:'{key}'>".ToJsonFilter());
            }
            return true;
        }

        public bool PreDelete(string key, bool dbSave = true)
        {
            var preBundle = PreBundles.FirstOrDefault(p => p.key == key);
            if (preBundle == null)
                return false;
            contextplus.preBundles.Remove(preBundle);
            if (dbSave)
            {
                contextplus.SaveChanges();
            }
            return true;
        }

        static JenisProses[] inclproses = new[] { JenisProses.standar, JenisProses.lokal };

        public Persil[] CleanPersil => contextplus.GetCollections(new Persil(), "APP_LIST_PersilCombine", "{}").ToList()
                //contextplus.persils.Query(p => (p.en_state == null || p.en_state == StatusBidang.bebas) && p.invalid != true && p.basic != null && p.basic.current != null && inclproses.Contains(p.basic.current.en_proses))
#if _HIBAH_ONLY_
					.Where(p => p is PersilHibah)
#endif
                .ToArray();

        /*		public PersilNxDiscrete[] OnProcessPersils
				{
					get
					{
						var included = ((AssignmentHost)AssignHost).OpenedAssignments().Cast<Assignment>()
														.SelectMany(a => a.details.Select(d => (d.keyPersil, step:a.step??DocProcessStep.Baru_Bebas)))
														.Where(i=>i.step != DocProcessStep.Baru_Bebas).ToArray();

						var keyincl = included.Select(i => i.keyPersil).ToArray();
						var clnPersil = contextplus.persils.Query(p => (p.en_state == null || p.en_state == StatusBidang.bebas) && p.invalid != true &&
															p.basic != null && p.basic.current != null && inclproses.Contains(p.basic.current.en_proses)
															&& keyincl.Contains(p.key))
#if _HIBAH_ONLY_
							.Where(p => p is PersilHibah)
#endif
						.ToArray();

						return clnPersil.Join(included,p=>p.key,i=>i.keyPersil,(p,i)=> new PersilNxDiscrete
						{
							key = p.key,
							cat = p.Discriminator.Category(),
							IdBidang = p.IdBidang,
							_step=i.step,
							keyProject = p.basic.current.keyProject,
							keyDesa = p.basic.current.keyDesa,
							keyPTSK = p.basic.current.keyPTSK,
							keyPenampung = p.basic.current.keyPenampung,
							LuasDibayar = p.basic.current.luasDibayar,
							LuasSurat = p.basic.current.luasSurat

						})
									.ToArray();
					}
				}

		*/
        /*public */
        PersilPositionView[] LastPositionDiscrete(Persil[] persils = null, IAssignment[] iassigns = null)
        {
            if (persils == null || persils.Length == 0)
                persils = CleanPersil;
            if (persils.Length == 0)
                return new PersilPositionView[0];

            var assigns = new Assignment[0];
            var usedkeys = new string[0];
            var akeys = new (DocProcessStep step, string keyPersil/*, string keyBundle*/)[0];

            /*			assigns = ((iassigns == null || iassigns.Length == 0)) ? AssignHost.OpenedAssignments().Cast<Assignment>().ToArray() :
												iassigns.Select(ia => ia as Assignment).ToArray();*/
            MyTracer.TraceInfo2("100");
            var pactive = assign.AssignedPersils().GetAwaiter().GetResult()?.ToArray() ?? new (string, DocProcessStep)[0];
            MyTracer.TraceInfo2("200");
            akeys = pactive.Select(d => (d.step, d.key/*, dd.keyBundle*/)).ToArray();
            MyTracer.TraceInfo2("300");
            usedkeys = akeys.Select(d => d.keyPersil).Distinct().ToArray();
            MyTracer.TraceInfo2("400");
            var freepersils = persils.Join(persils.Select(p => p.key).Except(usedkeys), p => p.key, x => x, (p, x) => p).ToArray();
            MyTracer.TraceInfo2("500");
            var availables0 = MainProps.Where(p => p.props.Any(pp => pp.Value != null && pp.Value.val != null))
                .Select(a => new DocExistence(a.key, a.keyDocType, "-", new[] { 0, 1, 1, 1, 1, 0 }));
            MyTracer.TraceInfo2("600");
            var availables = MainExistences.Where(m => m.available.IsExists());
            MyTracer.TraceInfo2("700");
            var persilwips = persils.Join(akeys, p => p.key, a => a.keyPersil, (p, a) => (p.key, p.Discriminator, a.step));
            MyTracer.TraceInfo2("800");
            var wip = persilwips.Select(p => (p.key, disc: p.Discriminator, ongoing: true,
                                                            step: Assignment.GetPrev(p.Discriminator, p.step) ?? DocProcessStep.Belum_Bebas,
                                                            nextstep: (DocProcessStep?)p.step));
            MyTracer.TraceInfo2("900");
            var persilavs = freepersils.Join(availables, p => p.key, a => a.key, (p, a) => (p.key, p.Discriminator, a)).ToArray();
            MyTracer.TraceInfo2("1000");
            var keyavs = persilavs.Select(p => p.key);
            MyTracer.TraceInfo2("1100");
            var persilavs0 = freepersils.Where(p => !keyavs.Contains(p.key))
                            .Join(availables0, p => p.key, a => a.key, (p, a) => (p.key, p.Discriminator, a)).ToArray();
            persilavs = persilavs.Union(persilavs0).ToArray();

            MyTracer.TraceInfo2("1200");
            var receives = StepDocType.List.Where(s => s.invalid != true && s.receive.Any(x => x.req))
                                    .Join(Assignment.StepOrders.SelectMany(so => so.Value.Select(v => (so.Key, v.step))), s => (s.disc, s._step), so => (so.Key, so.step),
                                        (s, so) => s)
                                    .SelectMany(s => s.receive.Select(r => (s.disc, s._step, r.keyDocType, chain: (string)null, r.ex, r.req))).ToArray();
            MyTracer.TraceInfo2("1300");
            var in_requireds = receives.GroupBy(r => (r.disc, r._step, r.keyDocType))
                                    .Select(g => (g.Key.disc, g.Key._step, g.Key.keyDocType,
                                                    exis: g.Select(d => new Existency(d.ex, d.req ? 1 : 0)).ToArray().Convert())).ToArray();
            MyTracer.TraceInfo2("1400");
            var docexists = persilavs.Join(in_requireds, p => (p.Discriminator, p.a.keyDocType), i => (i.disc, i.keyDocType), (p, i) => (p, i))
                                    .Select(x => (x.p.key, x.i.disc, x.i._step, x.i.keyDocType, chain: x.p.a.chainkey, exis: x.p.a.available.Sub(x.i.exis).IsExists()))
                                    .GroupBy(x => (x.key, x.disc, x._step))
                                    .Select(g => (g.Key.key, g.Key.disc, step: g.Key._step,
                                                combo: g.GroupBy(d => d.keyDocType).Select(g1 => (keyDocType: g1.Key,
                                                                 chains: g1.Select(d1 => (d1.chain, d1.exis)).ToArray())).ToArray()));
            //.Where(x=>x.combo.);
            var NIBPT = DocType.List.FirstOrDefault(d => d.key == "JDOK050");
            var HGBPT = DocType.List.FirstOrDefault(d => d.key == "JDOK054");
            MyTracer.TraceInfo2("1500");
            var allgot = docexists.Where(x => x.combo.All(c => c.chains.All(p => p.exis)))
                                .GroupBy(x => (x.key, x.disc))
                                .Select(g => (g.Key.key, g.Key.disc, step: Assignment.GetLatest(g.Key.disc, g.Select(d => d.step).ToArray())))
                                .Select(x => (x.key, x.disc, ongoing: false, x.step, nextstep: (DocProcessStep?)null))
                                .Union(wip)
                                .Join(persils, a => a.key, p => p.key, (a, p) => (a, p.IdBidang, p: p.basic.current))
                                .Select(g => (g.a.key, g.IdBidang, disc: g.a.disc.Category(), g.a.ongoing,
                                g.a.step, g.a.nextstep, g.p.keyProject, g.p.keyDesa, g.p.keyPTSK, g.p.keyPenampung,
                                                    luasDibayar: g.p.luasDibayar ?? g.p.luasSurat ?? 0,
                                                    luasSurat: g.p.luasSurat ?? g.p.luasDibayar ?? 0,
                                                    luasPBT: (double?)null, luasHGB: (double?)null, luasProp: (double?)null,
                                                    noPBT: (string)null, noHGB: (string)null)).ToArray();
            MyTracer.TraceInfo2("1600");
            var allgotkeys = allgot.Select(g => g.key).ToArray();
            MyTracer.TraceInfo2("1700");
            var notgot = freepersils.Where(p => !allgotkeys.Contains(p.key))
                                    .Select(p => (p.key, disc: p.Discriminator.Category(),
                                                    step: DocProcessStep.Riwayat_Tanah, nextstep: Assignment.GetNext(p.Discriminator, DocProcessStep.Riwayat_Tanah),
                                                    p.IdBidang,
                                                    p.basic.current.keyProject, p.basic.current.keyDesa, p.basic.current.keyPTSK, p.basic.current.keyPenampung,
                                                    luasDibayar: p.basic.current.luasDibayar ?? p.basic.current.luasSurat ?? 0,
                                                    luasSurat: p.basic.current.luasSurat ?? p.basic.current.luasDibayar ?? 0,
                                                    luasPBT: (double?)null, luasHGB: (double?)null, luasProp: (double?)null,
                                                    noPBT: (string)null, noHGB: (string)null))
                                                    .Select(g => new PersilPositionView(g.key, g.disc, g.keyProject, g.keyDesa,
                                                                            g.keyPTSK, g.keyPenampung, g.IdBidang, g.luasSurat, g.luasDibayar, g.step, null, null)
                                                    { ongoing = false }
                                                    ).ToArray();

            var endsteps = new[] { DocProcessStep.Balik_Nama, DocProcessStep.Cetak_Buku };
            MyTracer.TraceInfo2("1800");
            var endeds = allgot.Where(g => endsteps.Contains(g.step)).Select(g => (g.key, g.IdBidang, g.keyProject, g.keyDesa, g.keyPTSK, g.keyPenampung, g.disc, g.step,
                                                                    g.ongoing, g.nextstep, g.luasDibayar, g.luasSurat, g.luasPBT, g.luasHGB, g.luasProp, g.noPBT, g.noHGB)).ToArray();
            var unitysteps = new[] { DocProcessStep.PBT_PT, DocProcessStep.SK_BPN };
            MyTracer.TraceInfo2("1900");
            var unifieds = allgot.Where(g => unitysteps.Contains(g.step)).Select(g => (g.key, g.IdBidang, g.keyProject, g.keyDesa, g.keyPTSK, g.keyPenampung, g.disc, g.step,
                                                                    g.ongoing, g.nextstep, g.luasDibayar, g.luasSurat, g.luasPBT, g.luasHGB, g.luasProp, g.noPBT, g.noHGB)).ToArray();
            MyTracer.TraceInfo2("2000");
            var normals = allgot.Where(g => !endeds.Select(e => e.key).Contains(g.key) &&
                                                                            !unifieds.Select(e => e.key).Contains(g.key))
                                        .Select(g => (g.key, g.IdBidang, g.keyProject, g.keyDesa, g.keyPTSK, g.keyPenampung, g.disc, g.step, g.nextstep,
                                                                            g.luasDibayar, g.luasSurat, g.luasPBT, g.luasHGB, g.luasProp, g.noPBT, g.noHGB, g.ongoing))
                                .Select(g => new PersilPositionView(g.key, g.disc, g.keyProject, g.keyDesa, g.keyPTSK, g.keyPenampung, g.IdBidang,
                                                    g.luasSurat, g.luasDibayar, g.step, g.noPBT, g.noHGB)
                                { ongoing = g.ongoing }
                                ).ToList();
            normals.AddRange(notgot);

            var PBTpropNomor = MetadataKey.Nomor_PBT.ToString("g");
            var PBTpropLuas = MetadataKey.Luas.ToString("g");
            var HGBpropNomor = MetadataKey.Nomor.ToString("g");
            var HGBpropLuas = MetadataKey.Luas.ToString("g");

            MyTracer.TraceInfo2("2100");
            var allPBTs = MainProps.Where(p => p.keyDocType == NIBPT.key).Select(p => (
                                p.key, noPBT: p.props.TryGetValue(PBTpropNomor, out Dynamic no) ? no.val : null,
                                luas: p.props.TryGetValue(PBTpropLuas, out Dynamic ls) ? double.TryParse(ls.val, out double dv) ? dv : (double?)null : (double?)null
                                )).Distinct().ToArray();
            MyTracer.TraceInfo2("2200");
            var PBTs = allPBTs.GroupBy(p => p.noPBT).Select(g => (noPBT: g.Key, luas: g.Max(d => d.luas ?? 0))).ToArray();

            MyTracer.TraceInfo2("2300");
            var allHGBs = MainProps.Where(p => p.keyDocType == HGBPT.key).Select(p => (
                                p.key, nomor: p.props.TryGetValue(HGBpropNomor, out Dynamic no) ? no.val : null,
                                luas: p.props.TryGetValue(HGBpropLuas, out Dynamic ls) ? double.TryParse(ls.val, out double dv) ? dv : (double?)null : (double?)null
                                )).Distinct().ToArray();

            MyTracer.TraceInfo2("2400");
            var HGBs = allHGBs.GroupBy(p => p.nomor).Select(g => (nomor: g.Key, luas: g.Max(d => d.luas ?? 0))).ToArray();

            MyTracer.TraceInfo2("2500");
            var unifieds2 = unifieds.Join(allPBTs, d => d.key, p => p.key, (d, p) => (d, p.noPBT))
                                .GroupBy(x => x.noPBT).Select(g => (noPBT: g.Key, luas: g.Sum(d => d.d.luasDibayar), ds: g.Select(d => d)))
                                .SelectMany(x => x.ds.Select(y => (y.d, x.noPBT, x.luas))).ToArray();
            MyTracer.TraceInfo2("2600");
            unifieds = unifieds2.Join(PBTs, x => x.noPBT, p => p.noPBT, (x, p) => (x.d.key, x.d.IdBidang, x.d.keyProject, x.d.keyDesa, x.d.keyPTSK, x.d.keyPenampung,
                                                 x.d.disc, x.d.step, x.d.ongoing, x.d.nextstep, x.d.luasDibayar, x.d.luasSurat, luasPBT: (double?)p.luas, x.d.luasHGB,
                                                 luasProp: (double?)x.d.luasDibayar * p.luas / x.luas, p.noPBT, x.d.noHGB)).ToArray();
            MyTracer.TraceInfo2("2700");
            var endeds2 = endeds.Join(allHGBs, d => d.key, p => p.key, (d, p) => (d, p.nomor))
                                .GroupBy(x => x.nomor).Select(g => (nomor: g.Key, luas: g.Sum(d => d.d.luasDibayar), ds: g.Select(d => d)))
                                .SelectMany(x => x.ds.Select(y => (y.d, x.nomor, x.luas))).ToArray();
            MyTracer.TraceInfo2("2800");
            endeds = endeds2.Join(HGBs, x => x.nomor, p => p.nomor,
                            (x, p) => (x.d.key, x.d.IdBidang, x.d.keyProject, x.d.keyDesa, x.d.keyPTSK, x.d.keyPenampung,
                                                 x.d.disc, x.d.step, x.d.ongoing, x.d.nextstep, x.d.luasDibayar, x.d.luasSurat, x.d.luasPBT, luasHGB: (double?)p.luas,
                                                 luasProp: (double?)x.d.luasDibayar * p.luas / x.luas, x.d.noPBT, noHGB: p.nomor)).ToArray();
            MyTracer.TraceInfo2("2900");
            var psUnifieds = unifieds
                                //.GroupBy(x => (x.keyProject, x.keyDesa, x.keyPTSK, x.keyPenampung, x.disc, x.step, x.nextstep))
                                .Select(g => new PersilPositionView(g.key, g.disc, g.keyProject, g.keyDesa, g.keyPTSK, g.keyPenampung,
                                g.IdBidang, g.luasSurat, g.luasDibayar, g.step, g.noPBT, g.noHGB, g.luasPBT)
                                { ongoing = g.ongoing }).ToArray();
            MyTracer.TraceInfo2("3000");
            var psEndeds = endeds
                                //.GroupBy(x => (x.keyProject, x.keyDesa, x.keyPTSK, x.keyPenampung, x.disc, x.step, x.nextstep))
                                .Select(g => new PersilPositionView(g.key, g.disc, g.keyProject, g.keyDesa, g.keyPTSK, g.keyPenampung,
                                g.IdBidang, g.luasSurat, g.luasDibayar, g.step, g.noPBT, g.noHGB, null, g.luasHGB)
                                { ongoing = g.ongoing }
                                ).ToArray();
            MyTracer.TraceInfo2("3100");
            return normals.Union(psUnifieds).Union(psEndeds).ToArray();
        }

        const string PersilPositionCollname = "persilLastPos";
        const string PersilNextCollname = "persilNextStep";

        public List<PersilPositionView> SavedPersilPosition(bool inclActive = false) =>
            contextplus.GetCollections(new PersilPositionView(), PersilPositionCollname, inclActive ? "{}" : "{ongoing:false}", "{_id:0}")
            .ToList();

        public List<PersilNxDiscrete> SavedPersilNext()
        {
            lock (this)
            {
                return contextplus.GetCollections(new PersilNxDiscrete(), PersilNextCollname, "{}", "{_id:0}").ToList();
            }
        }

        public List<PersilPositionWithNext> SavedPositionWithNext(string[] prokeys = null)
        {
            string pkeys = prokeys == null || prokeys.Length == 0 ? "" : string.Join(',', prokeys.Select(k => $"'{k}'"));
            var match = string.IsNullOrEmpty(pkeys) ? null : $"<$match:<keyProject:<$in:[{pkeys}]>>>".Replace("<", "{").Replace(">", "}");
            var stages = new[]{match,
        $@"<$lookup: <
		from: '{PersilNextCollname}',
		localField: 'key',
		foreignField: 'key',
		as: 'next'>>".Replace("<", "{").Replace(">", "}"),
        "{$unwind:{path: '$next',preserveNullAndEmptyArrays: true}}",
        "{$addFields:{next: '$next._step'}}",
        "{$project:{_id:0}}" }.Where(s => s != null).ToArray();

            return contextplus.GetDocuments(new PersilPositionWithNext(), PersilPositionCollname, stages).ToList();
        }

        public (bool OK, string error) SaveLastPositionDiscete()
        {
            try
            {
                var data = LastPositionDiscrete();
                var coll = contextplus.db.GetCollection<PersilPositionView>(PersilPositionCollname);
                coll.DeleteMany("{}");
                coll.InsertMany(data);
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool OK, string error) SaveNextStepDiscrete()
        {
            lock (this)
            {
                var ses = contextplus.db.Client.StartSession();
                ses.StartTransaction();
                try
                {
                    var res = SaveLastPositionDiscete();
                    if (!res.OK)
                        throw new Exception($"Save Last Position error: {res.error}");
                    var data = NextReadiesDiscrete(inclActive: true);
                    contextplus.db.GetCollection<BsonDocument>(PersilNextCollname).DeleteMany("{}");
                    var coll = contextplus.db.GetCollection<PersilNxDiscrete>(PersilNextCollname);
                    coll.InsertMany(data);

                    ses.CommitTransaction();
                    return (true, "");
                }
                catch (Exception ex)
                {
                    ses.AbortTransaction();
                    return (false, ex.Message);
                }
            }
        }

        /*		bool IsReady(string key)
				{
					var persil = contextplus.persils.FirstOrDefault(p => (p.en_state == null || p.en_state == StatusBidang.bebas) && p.invalid != true &&
													p.basic != null && p.basic.current != null && p.key==key);
					if (persil == null)
						return false;

					var assign = AssignHost.OpenedAssignments().Cast<Assignment>().FirstOrDefault(a=>a.details.Any(d=>d.keyPersil==key));
					var akey = key;
					var available = MainExistences.Where(m => m.available.IsExists()).FirstOrDefault(m=>m.key==key);
					var persilav = available!=null? (key, persil.Discriminator, a:available) : (null,null,null);

					var out_required = StepDocType.List.Where(s => s.invalid != true)
											.SelectMany(s => s.send.Select(r => (s.disc, s.step, r.keyDocType, chain: (string)null, r.ex, r.req)))
											.GroupBy(r => (r.disc, r.step, r.keyDocType))
											.Select(g => (g.Key.disc, g.Key.step, g.Key.keyDocType,
															exis: g.Select(d => new Existency(d.ex, d.req ? 1 : 0)).ToArray().Convert()));
					var docexists = persilavs.Join(out_requireds, p => (p.Discriminator, p.a.keyDocType), i => (i.disc, i.keyDocType), (p, i) => (p, i))
											.Select(x => (x.p.key, x.i.disc, x.i.step, x.i.keyDocType, chain: x.p.a.chainkey, exis: x.p.a.available.Sub(x.i.exis).IsExists()))
											.GroupBy(x => (x.key, x.disc, x.step))
											.Select(g => (g.Key.key, g.Key.disc, g.Key.step,
														combo: g.GroupBy(d => d.keyDocType).Select(g1 => (keyDocType: g1.Key,
																		 chains: g1.Select(d1 => (d1.chain, d1.exis)).ToArray())).ToArray()));
					//
					var stepreqs = StepDocType.List.Select(s => (s.disc, s.step,
									s.send.GroupBy(ss => ss.keyDocType).Select(g => (keyDocType: g.Key,
																	exis: g.Select(d => new Existency(d.ex, cnt: d.req ? 1 : 0)).ToArray().Convert()))));

					var bdocs = MainBundles.Join(persilavs, b => b.key, p => p.key, (b, p) => (b.key, b.doclist))
											.SelectMany(b => b.doclist.Select(d => (b.key, d)));

					// all requireds doc props
					var allspcreqs = bdocs.SelectMany(x => x.d.SpecialReqs.Select(d => (x.key, x.d, SpecialReqs: d))).ToList();
					var spcreqs = allspcreqs.Select(r => (r.key, r.d.keyDocType, r.SpecialReqs))
											.Select(x => (x.key, x.keyDocType, x.SpecialReqs.step, prop: x.SpecialReqs.prop.Key, val: x.SpecialReqs.prop.Value,
											x.SpecialReqs.exis));

					var props = MainProps.Join(bdocs, p => (p.key, p.keyDocType), x => (x.key, x.d.keyDocType), (p, x) => p);
					var reqsupp = spcreqs.Join(props, x => (x.key, x.keyDocType), p => (p.key, p.keyDocType),
									(x, p) => (x.key, x.keyDocType, x.step, x.prop, x.val, x.exis, p.props))
												.Where(x => x.props.Any(p => p.Key == x.prop && p.Value == x.val));
					var spcs = allspcreqs.Select((a, i) => (a, i)).Join(reqsupp, a => (a.a.key, a.a.d.keyDocType, a.a.SpecialReqs.step),
										r => (r.key, r.keyDocType, r.step), (a, r) => a.i).OrderByDescending(i => i).ToList();
					spcs.ForEach(i => allspcreqs.RemoveAt(i)); // remaining allspcreqs are the still required doc props

					// exclusion persils keys
					var missingreqs = allspcreqs.Select(s => s.key).Distinct();

					var allnextsteps = NextSteps(persils, assigns);
					// exclude the persils with that keys;
					docexists = docexists.Where(d => !missingreqs.Contains(d.key));
					var allgot = docexists.Where(x => x.combo.All(c => c.chains.All(p => p.exis)))
										.GroupBy(x => (x.key, x.disc))
										.Select(g => (g.Key.key, g.Key.disc, step: Assignment.GetLatest(g.Key.disc, g.Select(d => d.step).ToArray())))
										.Join(allnextsteps, d => (d.key, d.disc, d.step), n => (n.key, n.disc, n.next), (d, n) => d)
										.Join(persils, a => (a.key, a.disc), p => (p.key, p.Discriminator), (a, p) => (a, a.disc, p: p.basic.current))
										.GroupBy(x => (x.disc, x.p.keyDesa, x.p.keyPTSK, x.p.keyPenampung, x.a.step))
										.Select(g => (g.Key.disc, g.Key.keyDesa, g.Key.keyPTSK, g.Key.keyPenampung, g.Key.step, count: g.Count()))
										.Select(x => new PersilNx(x.keyDesa, x.keyPTSK, x.keyPenampung,
										Enum.TryParse<AssignmentCat>(x.disc, out AssignmentCat cat) ? cat : AssignmentCat.Unknown,
										x.step, x.count));
					return allgot;
				}*/

        static bool FilterDT(string disc, string keyDocType) => !Denials[disc].Contains(keyDocType);

        static Dictionary<string, string[]> Denials = new Dictionary<string, string[]> {
            { "PersilHibah",new[]{ "JDOK053", "JDOK011","JDOK012","JDOK013","JDOK014","JDOK015","JDOK016","JDOK017"} },
            { "persilSHM",new[]{ "JDOK053", "JDOK011","JDOK012","JDOK013","JDOK014","JDOK015","JDOK016","JDOK017"} },
            { "persilHGB",new[]{ "JDOK053", "JDOK011","JDOK012","JDOK013","JDOK014","JDOK015","JDOK016","JDOK017"} },
            { "persilSHP",new[]{ "JDOK053", "JDOK011","JDOK012","JDOK013","JDOK014","JDOK015","JDOK016","JDOK017"} },
            { "persilGirik",new[]{ "JDOK053"} },
        };
    }
    internal static class Helper
    {
        public static string ToJsonFilter(this string src)
            => src.Replace("<", "{").Replace(">", "}");
    }
}
