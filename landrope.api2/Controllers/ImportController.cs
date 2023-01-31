using ExcelDataReader;
using landrope.common;
using landrope.mod3;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using auth.mod;
using landrope.mod3.shared;
using GraphConsumer;
using landrope.consumers;
using GenWorkflow;
using graph.mod;
using HttpAccessor;
using core.helpers;
using flow.common;
using MongoDB.Driver;
using AssignerConsumer;
using landrope.mod2;
using Microsoft.Extensions.Configuration;
using landrope.mod.cross;
using BundlerConsumer;

namespace landrope.api2.Controllers
{
    [ApiController]
    [Route("api/import")]
    [EnableCors(nameof(landrope))]

    static class Helpers
    {
        public static string ToMongo(this string st) =>
            st.Replace("<", "{").Replace(">", "}");

        public static AssignmentCat Category(this string cat)
            => cat switch
            {
                "girik" => AssignmentCat.Girik,
                "shm" => AssignmentCat.SHM,
                "hgb" => AssignmentCat.HGB,
                "shp" => AssignmentCat.SHP,
                "hibah" => AssignmentCat.Hibah,
                _ => AssignmentCat.Unknown
            };

        public static DocProcessStep? ToDocProcessStep(this string step)
            => step switch
            {
                //Belum_Bebas = 0,
                "PJB" => DocProcessStep.Akta_Notaris,
                "NIB PERORANGAN" => DocProcessStep.PBT_Perorangan,
                "SPH" => DocProcessStep.SPH,
                "PBT PT" => DocProcessStep.PBT_PT,
                "SK" => DocProcessStep.SK_BPN,
                "CETAK BUKU" => DocProcessStep.Cetak_Buku,
                "AJB" => DocProcessStep.AJB,
                //AJB_Hibah = 8,
                //SHM_Hibah = 9,
                "PENURUNAN HAK" => DocProcessStep.Penurunan_Hak,
                "PENINGKATAN HAK" => DocProcessStep.Peningkatan_Hak,
                "BALIK NAMA" => DocProcessStep.Balik_Nama,

                "PENGAKUAN HAK" => DocProcessStep.Pengakuan_Hak,
                "BALIK NAMA AHLI WARIS" => DocProcessStep.Balik_Nama_Ahli_Waris,
                "SURAT UKUR" => DocProcessStep.Surat_Ukur,
                "Ganti Blanko" => DocProcessStep.Ganti_Blanko,
                "PENGANTAR SK" => DocProcessStep.Pengantar_SK,
                "KUASA MENJUAL" => DocProcessStep.Kuasa_Menjual,
                "KESEPAKATAN BERSAMA" => DocProcessStep.Kesepakatan_Bersama,
                "SK KANTAH" => DocProcessStep.SK_Kantah,
                "SK KANWIL" => DocProcessStep.SK_Kanwil,

                //GPS_Dan_Ukur = 13,
                //Riwayat_Tanah = 14,
                //Bayar_PPh = 15,
                //Validasi_PPh = 16,
                //Bayar_BPHTB = 17,
                //Validasi_BPHTB = 18,
                //Bayar_UTJ = 19,
                //Bayar_DP = 20,
                //Pelunasan = 21,
                //Baru_Bebas = 22,
                //Proses_Hibah = 23
                _ => null
            };
    }
    public class ImportController : ControllerBase
    {
        IServiceProvider services;
        LandropePlusContext contextplus = Contextual.GetContextPlus();
        GraphHostConsumer ghost;
        BundlerHostConsumer bhost;
        AssignerHostConsumer ahost;
        GraphContext gcontext;
        LandropeCrossContext contextcross;
        string name;
        List<Failures> failures = new List<Failures>();

        public ImportController(IServiceProvider services)
        {
            this.services = services;
            contextplus = services.GetService<LandropePlusContext>();
            ghost = services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
            ahost = services.GetService<IAssignerHostConsumer>() as AssignerHostConsumer;
            bhost = services.GetService<IBundlerHostConsumer>() as BundlerHostConsumer;

            MakeConnectionGraph();
        }

        [HttpPost("import-assign")]
        public IActionResult ImportAssign(IFormFile file, string token)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                var user = contextplus.users.FirstOrDefault(x => x.identifier == "importer");
                var ColumnInfoes = ColumnFacts.Select(x => x.many ? (ColInfo)new ColInfoM(x.kind, x.caption) : new ColInfoS(x.kind, x.caption)).ToArray();

                name = file.FileName;
                var strm = file.OpenReadStream();
                var data = new byte[strm.Length];
                strm.Read(data, 0, data.Length);

                Stream stream = new MemoryStream(data);

                var reader = ExcelReaderFactory.CreateReader(stream).AsDataSet();

                failures = new List<Failures>();
                var table = reader.Tables.Cast<DataTable>().FirstOrDefault();
                if (table == null)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dapat diproses"
                    };
                    failures.Add(fail);

                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };

                }

                var firstrow = table.Rows[0].ItemArray.Select((o, i) => (o, i))
                        .Where(x => x.o != DBNull.Value).Select(x => (s: x.o?.ToString(), x.i))
                        .Where(x => !String.IsNullOrEmpty(x.s)).ToList();

                foreach (var (s, i) in firstrow)
                {
                    var col = ColumnInfoes.FirstOrDefault(c => c.captions.Contains(s));

                    if (col != null)
                        switch (col)
                        {
                            case ColInfoS cs: cs.number = i; break;
                            case ColInfoM cm: cm.numbers.Add(i); break;
                        }
                }

                var colNoSuratTugas = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.NoSuratTugas);
                var noNoSuratTugas = ((ColInfoS)colNoSuratTugas).number;
                if (ColumnInfoes.Where(c => c.exists).Count() < 2 || noNoSuratTugas == -1)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dipersiapkan dengan benar"
                    };
                    failures.Add(fail);

                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };
                }

                // var rows = table.Rows.Cast<DataRow>().Skip(5).Select((r, i) => (r, i))
                //.Where(x => x.r[noNoSuratTugas] != DBNull.Value).ToArray();

                var rows = table.Rows.Cast<DataRow>().Skip(5).Select((r, i) => (r, i)).ToArray();

                var ListImports = new List<AssignImport>();
                var locations = contextplus.GetVillages().ToArray().AsParallel();
                var allSPSs = contextplus.AllSPS();
                var persils = contextplus.GetCollections(new { key = "", IdBidang = "" }, "persils_v2", "{invalid:{$ne:true}}", "{key:1, IdBidang:1, _id:0}").ToList();

                var ptsks = contextplus.GetCollections(new PTSK(), "masterdatas", "{_t : 'ptsk'}", "{_id:0}").ToList().Select(x => new cmnItem { key = x.key, name = x.identifier }).ToArray();
                var pics = contextplus.GetCollections(new Notaris(), "masterdatas", "{_t : 'notaris'}", "{_id:0}").ToList().Select(x => new cmnItem { key = x.key, name = x.identifier }).ToArray();

                foreach (var (r, i) in rows)
                {
                    var acPersils = contextplus.GetCollections(new { keyPersil = "" }, "IM_PersilActiveInAssigment", "{}", "{}").ToList().Select(x => x.keyPersil).ToArray();

                    var objNoSuratTugas = r[noNoSuratTugas];
                    var noSuratTugas = objNoSuratTugas == DBNull.Value ? null : objNoSuratTugas.ToString();
                    if (string.IsNullOrWhiteSpace(noSuratTugas))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            Keterangan = "Nomor Surat Tugas kosong"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var _idBidang = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.IdBidang).Get<string>(r);
                    var _project = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Project).Get<string>(r);
                    var _desa = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Desa).Get<string>(r);
                    var _category = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Category).Get<string>(r);
                    var _step = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Step).Get<string>(r);
                    var _noSPS = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.NoSPS).Get<string>(r);
                    var _tglSPS = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TglSPS).Get<DateTime>(r);
                    var _pic = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.PIC).Get<string>(r);
                    var _pt = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.PT).Get<string>(r);

                    Action<string, string> chkerr = (_name, st) =>
                    {
                        if (!string.IsNullOrEmpty(st))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 6,
                                Identifier = noSuratTugas,
                                Keterangan = $"Error: {_name.Substring(1)}:{st}"
                            };
                            failures.Add(fail);
                        }
                    };

                    chkerr.Invoke(nameof(_idBidang), _idBidang.err);
                    chkerr.Invoke(nameof(_project), _project.err);
                    chkerr.Invoke(nameof(_desa), _desa.err);
                    chkerr.Invoke(nameof(_category), _category.err);
                    chkerr.Invoke(nameof(_step), _step.err);
                    chkerr.Invoke(nameof(_noSPS), _noSPS.err);
                    chkerr.Invoke(nameof(_tglSPS), _tglSPS.err);
                    chkerr.Invoke(nameof(_pic), _pic.err);
                    chkerr.Invoke(nameof(_pt), _pt.err);

                    var idBidang = _idBidang.data.Cast<string>().FirstOrDefault();
                    var project = _project.data.Cast<string>().FirstOrDefault();
                    var desa = _desa.data.Cast<string>().FirstOrDefault();
                    var category = _category.data.Cast<string>().FirstOrDefault();
                    var step = _step.data.Cast<string>().FirstOrDefault();
                    var noSPS = _noSPS.data.Cast<string>().FirstOrDefault();
                    var tglSPS = _tglSPS.data.Cast<DateTime?>().FirstOrDefault();
                    var pic = _pic.data.Cast<string>().FirstOrDefault();
                    var pt = _pt.data.Cast<string>().FirstOrDefault();

                    if (string.IsNullOrEmpty(idBidang))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            IdBidang = idBidang,
                            Identifier = noSuratTugas,
                            Keterangan = "Id Bidang Kosong"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var persilKey = persils.FirstOrDefault(x => x.IdBidang == idBidang)?.key;
                    if (string.IsNullOrEmpty(persilKey))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            IdBidang = idBidang,
                            Identifier = noSuratTugas,
                            Keterangan = "Bidang tidak ada dalam database"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    if (acPersils.Any(x => x == persilKey))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            IdBidang = idBidang,
                            Identifier = noSuratTugas,
                            Keterangan = "Bidang tersebut memiliki penugasan yang sedang aktif"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var keyproject = locations.FirstOrDefault(x => x.project.identity.ToLower().Trim().Contains(project.ToLower().Trim())).project?.key;
                    var keydesa = locations.FirstOrDefault(x => x.desa.identity.ToLower().Trim().Contains(desa.ToLower().Trim())).desa?.key;

                    if (string.IsNullOrEmpty(keyproject) || string.IsNullOrEmpty(keydesa))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            IdBidang = idBidang,
                            Identifier = noSuratTugas,
                            Keterangan = "Project/Desa tidak ditemukan dalam database"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var keyPIC = pics.FirstOrDefault(x => x.name.ToLower().Trim().Contains((pic ?? String.Empty).ToLower().Trim()))?.key;
                    var keyPTSK = ptsks.FirstOrDefault(x => x.name.ToLower().Trim().Contains((pt ?? String.Empty).ToLower().Trim()))?.key;

                    var cat = Helpers.Category(category);
                    if (cat == AssignmentCat.Unknown)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            IdBidang = idBidang,
                            Identifier = noSuratTugas,
                            Keterangan = "Category tidak valid"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var sstep = step.Replace("PROSES", "").Trim();
                    var stp = Helpers.ToDocProcessStep(sstep);
                    if (stp == null)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            IdBidang = idBidang,
                            Identifier = noSuratTugas,
                            Keterangan = "Jenis Proses Dokumen tidak valid"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var sps = allSPSs.FirstOrDefault(s => s.keyPersil == persilKey && s.step == stp);
                    if (sps != null)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            IdBidang = idBidang,
                            Identifier = noSuratTugas,
                            Keterangan = "SPS untuk bidang dan proses dimaksud sudah ada"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var exists = ListImports.FirstOrDefault(x => x.NoSuratTugas.Trim().ToLower() == noSuratTugas.Trim().ToLower());

                    if (exists == null)
                    {
                        var list = new List<AssignPersil>();
                        var newAssignDetailImport = new AssignPersil
                        {
                            row = i + 6,
                            key = persilKey,
                            IdBidang = idBidang,
                            noSPS = noSPS,
                            tglSPS = tglSPS
                        };

                        list.Add(newAssignDetailImport);

                        var newAssignImport = new AssignImport
                        {
                            NoSuratTugas = noSuratTugas,
                            keyProject = keyproject,
                            keyDesa = keydesa,
                            Category = cat,
                            Step = stp,
                            detail = list.ToArray()
                        };

                        ListImports.Add(newAssignImport);
                    }
                    else
                    {
                        if (exists?.keyProject != keyproject || exists?.keyDesa != keydesa)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 6,
                                IdBidang = idBidang,
                                Identifier = noSuratTugas,
                                Keterangan = "Penugasan tersebut memiliki bidang dengan Project dan Desa berbeda dalam satu penugasan"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        var index = ListImports.IndexOf(exists);
                        var details = exists.detail.ToList();

                        var dtExists = details.Any(x => x.IdBidang == idBidang);
                        if (dtExists)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 6,
                                IdBidang = idBidang,
                                Identifier = noSuratTugas,
                                Keterangan = "Bidang lebih dari satu dan mempunyai informasi yang sama dalam satu file"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        var newAssignDetailImport = new AssignPersil
                        {
                            row = i + 6,
                            key = persilKey,
                            IdBidang = idBidang,
                            noSPS = noSPS,
                            tglSPS = tglSPS
                        };

                        details.Add(newAssignDetailImport);
                        exists.detail = details.ToArray();

                        if (index != -1)
                            ListImports[index] = exists;
                    }
                }

                foreach (var item in ListImports)
                {
                    var assign = contextplus.GetCollections(new Assignment(), "assignment", "{invalid :{$ne : true}}", "{_id:0}").ToList().FirstOrDefault(x => x.identifier == item.NoSuratTugas);
                    if (assign == null)
                        NewAssign(item, user);
                    else
                    {
                        var close = ghost.Get(assign.instkey).GetAwaiter().GetResult();
                        if (!close.closed)
                            ExistsAssign(assign, item, user);
                        else
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Identifier = assign.identifier,
                                Keterangan = "Penugasan sudah closed/selesai"
                            };
                            failures.Add(fail);
                            continue;
                        }
                    }

                    var asgn = contextplus.GetCollections(new Assignment(), "assignment", "{invalid :{$ne : true}}", "{_id:0}").ToList().FirstOrDefault(x => x.key == assign.key);
                    var res = asgn.details.Select(d => bhost.DoMakeTaskBundle(token, asgn, d, null, user, false).GetAwaiter().GetResult()).ToArray();
                    if (res.Any(r => r.bundle == null))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Identifier = assign.identifier,
                            Keterangan = string.Join("|", res.Where(r => r.bundle == null).Select(r => r.reason))
                        };
                        failures.Add(fail);
                        continue;
                    }
                }

                var csv = MakeCsv(failures.ToArray());
                return new ContentResult { Content = csv.ToString(), ContentType = "text/csv" };
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void NewAssign(AssignImport item, user user)
        {
            var date = DateTime.Now;
            var ent = item.Step == null ? new Assignment(user) : new Assignment(item.Step.Value, user);
            ent.FromImport(item.Step.Value, item.Category, item.keyProject, item.keyDesa, string.Empty, string.Empty, string.Empty);
            ent.key = entity.MakeKey;
            ent.identifier = item.NoSuratTugas;
            var instkey = ent.instkey;

            contextplus.assignments.Insert(ent);
            contextplus.SaveChanges();

            foreach (var dt in item.detail)
            {
                //Persil Still Active in Assignment
                var stillActive = contextplus.GetCollections(new { keyPersil = "" }, "IM_PersilActiveInAssigment", "{}", "{}").ToList().Select(x => x.keyPersil).ToArray().Any(x => x == dt.key);
                if (stillActive)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Row = dt.row,
                        IdBidang = dt.IdBidang,
                        Identifier = item.NoSuratTugas,
                        Keterangan = "Bidang memiliki penugasan yang sedang aktif"
                    };
                    failures.Add(fail);
                    continue;
                }

                var asgn = contextplus.assignments.FirstOrDefault(x => x.key == ent.key);
                var persil = contextplus.persils.Query(x => x.IdBidang == dt.IdBidang).FirstOrDefault();

                var asgdtl = new AssignmentDtl();
                asgdtl.FromCore(string.Empty, persil.key);
                asgdtl.key = mongospace.MongoEntity.MakeKey;
                asgn.AddDetail(asgdtl);

                contextplus.assignments.Update(asgn);
                contextplus.SaveChanges();

                if (!string.IsNullOrEmpty(dt.noSPS) && dt.tglSPS != null)
                {
                    //Add SPS
                    var sps = new landrope.mod3.SPS
                    {
                        keyPersil = dt.key,
                        step = item.Step.Value,
                        date = date,
                        nomor = dt.noSPS
                    };
                    var ses = contextplus.db.Client.StartSession();
                    ses.StartTransaction();
                    contextplus.AddSPS(sps);
                    contextplus.SaveChanges();
                }
            }

            var assgn = contextplus.assignments.FirstOrDefault(x => x.key == ent.key);
            var keys = assgn.details.Select(d => d.key).ToArray();

            //Membentuk Dockeys untuk child
            ghost.RegisterDocs(instkey, keys, true).GetAwaiter().GetResult();

            var instance = ghost.Get(instkey).GetAwaiter().GetResult();

            var create = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(x => x._state == ToDoState.created_);
            create.Complete(user.key);
            create.Take(user.key, date);

            var issueApp = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(x => x._state == ToDoState.approvalIssued_);
            issueApp.Complete(user.key);
            issueApp.Take(user.key, date);

            var issue = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(x => x._state == ToDoState.issued_);
            issue.Complete(user.key);
            issue.Take(user.key, date);

            if (assgn.type == ToDoType.Proc_BPN)
            {
                var formFilling = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(x => x._state == ToDoState.formFilling_);
                formFilling.Complete(user.key);
                formFilling.Take(user.key, date);
            }

            var bundleTaken = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(x => x._state == ToDoState.bundleTaken_);
            bundleTaken.Complete(user.key);
            bundleTaken.Take(user.key, date);

            var accepted = instance.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault();
            accepted.Active = true;

            instance.lastState.state = ToDoState.accepted_;
            instance.lastState.time = date;

            ghost.Update(instance, gcontext).GetAwaiter().GetResult();

            instance = ghost.Get(assgn.instkey).GetAwaiter().GetResult();
            var nested = instance.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault();

            var template = ghost.GetTemplate(nested.tempname, nested.tempver).GetAwaiter().GetResult();

            //Membentuk Child
            nested.selfInstakey = null;
            nested.mainInstakey = assgn.instkey;
            if (!nested.instakeys.Any())
            {
                nested.templatekey = template.key;

                for (int i = 0; i < nested.DocKeys.Length; i++)
                {
                    var dockey = nested.DocKeys[i];
                    var insta = new GraphSubInstance(template, user, instance, nested);
                    insta.MorhpTemplate($"key{i + 1:000}");
                    var instakey = insta.key;
                    instance.SetChildren(insta);
                    nested.instakeys.Add(dockey, instakey);
                }
            }

            ghost.Update(instance, gcontext).GetAwaiter().GetResult();

            instance = ghost.Get(assgn.instkey).GetAwaiter().GetResult();
            nested = instance.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault();

            //Update states terima penugasan
            var states = instance.states.ToList();
            var state = new GraphState(date, ToDoState.accepted_);

            states.Add(state);
            instance.states = states.ToArray();

            var chains = instance.SubChains;
            var subs = instance.children;
            var subchs = subs.Join(chains, s => s.Key, c => c.Value, (s, c) => (sub: s.Value, dk: c.Key)).ToArray();

            var ghsub = new List<KeyValuePair<string, GraphSubInstance>>();
            var ghcloseds = new List<KeyValuePair<string, bool>>();

            foreach (var dt in assgn.details)
            {
                var chain = instance.SubChains.Where(x => x.Key == dt.key).FirstOrDefault();
                var child = instance.children.Where(x => x.Key == chain.Value).FirstOrDefault();

                var chAccepted = child.Value.Core.nodes.OfType<GraphNode>().FirstOrDefault(x => x._state == ToDoState.accepted_);

                var childStates = child.Value.states.ToList();

                var spsExists = item.detail.FirstOrDefault(x => x.key == dt.keyPersil);
                if (assgn.type == ToDoType.Proc_BPN)
                {
                    if (!string.IsNullOrEmpty(spsExists.noSPS) && spsExists.tglSPS != null)
                    {
                        chAccepted.Complete(user.key);
                        chAccepted.Take(user.key, date);

                        var chSPSPaid = child.Value.Core.nodes.OfType<GraphNode>().FirstOrDefault(x => x._state == ToDoState.spsPaid_);
                        chSPSPaid.Active = true;

                        var childState = new GraphState(date, ToDoState.spsPaid_);
                        childStates.Add(childState);
                        child.Value.states = childStates.ToArray();
                    }
                    else
                    {
                        chAccepted.Active = true;

                        var childState = new GraphState(date, ToDoState.accepted_);
                        childStates.Add(childState);
                        child.Value.states = childStates.ToArray();
                    }
                }
                else if (assgn.type == ToDoType.Proc_Non_BPN)
                {
                    chAccepted.Active = true;

                    var childState = new GraphState(date, ToDoState.accepted_);
                    childStates.Add(childState);
                    child.Value.states = childStates.ToArray();
                }

                ghsub.Add(child);
            }

            instance.children = ghsub.ToDictionary(x => x.Key, x => x.Value);
            ghost.Update(instance, gcontext).GetAwaiter().GetResult();

            ahost.AssignReload();
        }

        private void ExistsAssign(Assignment asgn, AssignImport item, user user)
        {
            var date = DateTime.Now;
            var instkey = asgn.instkey;
            foreach (var dt in item.detail)
            {
                //Persil Still Active in Assignment
                var stillActive = contextplus.GetCollections(new { keyPersil = "" }, "IM_PersilActiveInAssigment", "{}", "{}").ToList().Select(x => x.keyPersil).ToArray().Any(x => x == dt.key);
                if (stillActive)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Row = dt.row,
                        IdBidang = dt.IdBidang,
                        Identifier = item.NoSuratTugas,
                        Keterangan = "Bidang memiliki penugasan yang sedang aktif"
                    };
                    failures.Add(fail);
                    continue;
                }

                var persil = contextplus.persils.Query(x => x.IdBidang == dt.IdBidang).FirstOrDefault();

                var asgdtl = new AssignmentDtl();
                asgdtl.FromCore(string.Empty, persil.key);
                asgdtl.key = mongospace.MongoEntity.MakeKey;
                var dtlkey = asgdtl.key;
                asgn.AddDetail(asgdtl);

                contextplus.assignments.Update(asgn);
                contextplus.SaveChanges();

                if (!string.IsNullOrEmpty(dt.noSPS) && dt.tglSPS != null)
                {
                    //Add SPS
                    var sps = new landrope.mod3.SPS
                    {
                        keyPersil = dt.key,
                        step = item.Step.Value,
                        date = date,
                        nomor = dt.noSPS
                    };
                    var ses = contextplus.db.Client.StartSession();
                    ses.StartTransaction();
                    contextplus.AddSPS(sps);
                    contextplus.SaveChanges();
                }

                ghost.RegisterDoc(instkey, asgdtl.key, true).GetAwaiter().GetResult();

                var assgn = contextplus.assignments.FirstOrDefault(x => x.key == asgn.key);

                var instance = ghost.Get(instkey).GetAwaiter().GetResult();
                var nested = instance.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault();

                var template = ghost.GetTemplate(nested.tempname, nested.tempver).GetAwaiter().GetResult();

                nested.templatekey = template.key;

                //find index new detail
                var i = nested.DocKeys.ToList().FindIndex(x => x == asgdtl.key);

                //membentuk single child
                var dockey = nested.DocKeys[i];
                var insta = new GraphSubInstance(template, user, instance, nested);
                insta.MorhpTemplate($"key{i + 1:000}");
                var instakey = insta.key;
                instance.SetChildren(insta);
                nested.instakeys.Add(dockey, instakey);

                ghost.Update(instance, gcontext).GetAwaiter().GetResult();

                instance = ghost.Get(instkey).GetAwaiter().GetResult();
                nested = instance.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault();

                var ghsub = new List<KeyValuePair<string, GraphSubInstance>>();
                var ghcloseds = new List<KeyValuePair<string, bool>>();

                var chain = instance.SubChains.Where(x => x.Key == dtlkey).FirstOrDefault();
                var child = instance.children.Where(x => x.Key == chain.Value).FirstOrDefault();

                var chAccepted = child.Value.Core.nodes.OfType<GraphNode>().FirstOrDefault(x => x._state == ToDoState.accepted_);

                var childStates = child.Value.states.ToList();

                var spsExists = item.detail.FirstOrDefault(x => x.key == persil.key);
                if (!string.IsNullOrEmpty(spsExists.noSPS) && spsExists.tglSPS != null)
                {
                    chAccepted.Complete(user.key);
                    chAccepted.Take(user.key, date);

                    var chSPSPaid = child.Value.Core.nodes.OfType<GraphNode>().FirstOrDefault(x => x._state == ToDoState.spsPaid_);
                    chSPSPaid.Active = true;

                    var childState = new GraphState(date, ToDoState.spsPaid_);
                    childStates.Add(childState);
                    child.Value.states = childStates.ToArray();
                }
                else
                {
                    chAccepted.Active = true;

                    var childState = new GraphState(date, ToDoState.accepted_);
                    childStates.Add(childState);
                    child.Value.states = childStates.ToArray();
                }

                ghsub.Add(child);

                instance.children = ghsub.ToDictionary(x => x.Key, x => x.Value);
                ghost.Update(instance, gcontext).GetAwaiter().GetResult();

                ahost.AssignReload();
            }

        }

        [HttpPost("import-bundle")]
        public IActionResult ImportBundle(IFormFile file)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                //Make connecion to another db
                MakeConnectionCross();

                var ColumnInfoes = BundleColumnFacts.Select(x => x.many ? (ColInfo)new ColInfoM(x.kind, x.caption) : new ColInfoS(x.kind, x.caption)).ToArray();

                name = file.FileName;
                var strm = file.OpenReadStream();
                var data = new byte[strm.Length];
                strm.Read(data, 0, data.Length);

                Stream stream = new MemoryStream(data);

                var reader = ExcelReaderFactory.CreateReader(stream).AsDataSet();

                failures = new List<Failures>();
                var table = reader.Tables.Cast<DataTable>().FirstOrDefault();
                if (table == null)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dapat diproses"
                    };
                    failures.Add(fail);

                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };

                }

                var firstrow = table.Rows[0].ItemArray.Select((o, i) => (o, i))
                        .Where(x => x.o != DBNull.Value).Select(x => (s: x.o?.ToString(), x.i))
                        .Where(x => !String.IsNullOrEmpty(x.s)).ToList();

                foreach (var (s, i) in firstrow)
                {
                    var col = ColumnInfoes.FirstOrDefault(c => c.captions.Contains(s));

                    if (col != null)
                        switch (col)
                        {
                            case ColInfoS cs: cs.number = i; break;
                            case ColInfoM cm: cm.numbers.Add(i); break;
                        }
                }

                var colKeyPersil = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.KeyPersil);
                var noKeyPersil = ((ColInfoS)colKeyPersil).number;
                if (ColumnInfoes.Where(c => c.exists).Count() < 3 || noKeyPersil == -1)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dipersiapkan dengan benar"
                    };
                    failures.Add(fail);

                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };
                }

                var colDocType = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.DocType);
                var noDocType = ((ColInfoS)colDocType).number;
                if (ColumnInfoes.Where(c => c.exists).Count() < 3 || noDocType == -1)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dipersiapkan dengan benar"
                    };
                    failures.Add(fail);

                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };
                }

                var colTglLog = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TglLog);
                var noTglLog = ((ColInfoS)colTglLog).number;
                if (ColumnInfoes.Where(c => c.exists).Count() < 3 || noTglLog == -1)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dipersiapkan dengan benar"
                    };
                    failures.Add(fail);

                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };
                }

                var rows = table.Rows.Cast<DataRow>().Skip(1).Select((r, i) => (r, i)).ToArray();

                foreach (var (r, i) in rows)
                {
                    var objKeyPersil = r[noKeyPersil];
                    var okeyPersil = objKeyPersil == DBNull.Value ? null : objKeyPersil.ToString();
                    if (string.IsNullOrWhiteSpace(okeyPersil))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            Keterangan = "Key Persil Kosong"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var _keyPersil = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.KeyPersil).Get<string>(r);
                    var _docType = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.DocType).Get<string>(r);
                    var _tglLog = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TglLog).Get<DateTime>(r);

                    Action<string, string> chkerr = (_name, st) =>
                    {
                        if (!string.IsNullOrEmpty(st))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 6,
                                Identifier = okeyPersil,
                                Keterangan = $"Error: {_name.Substring(1)}:{st}"
                            };
                            failures.Add(fail);
                        }
                    };

                    chkerr.Invoke(nameof(_keyPersil), _keyPersil.err);
                    chkerr.Invoke(nameof(_docType), _docType.err);
                    chkerr.Invoke(nameof(_tglLog), _tglLog.err);

                    var keyPersil = _keyPersil.data.Cast<string>().FirstOrDefault();
                    var docType = _docType.data.Cast<string>().FirstOrDefault();
                    var tgllog = _tglLog.data.Cast<DateTime>().FirstOrDefault();

                    var BundleS2 = contextplus.mainBundles.Query(x => x.key == keyPersil).FirstOrDefault();
                    if (BundleS2 == null)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            Identifier = okeyPersil,
                            Keterangan = "Bundle tidak ditemukan"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var JDOCS2 = BundleS2.doclist.Where(x => x.keyDocType == docType).FirstOrDefault();
                    var index = BundleS2.doclist.IndexOf(JDOCS2);

                    //Get Bundles from Server 1
                    var BundleS1 = contextcross.GetCollections(new MainBundle(), "bundles", $"<key:'{keyPersil}'>".ToMongo(), "{_id:0}").FirstOrDefault();
                    if (BundleS1 == null)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            Identifier = okeyPersil,
                            Keterangan = "Bundle tidak ditemukan"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var JDOCS1 = BundleS1.doclist.Where(x => x.keyDocType == docType).FirstOrDefault();
                    if(JDOCS1.entries.Count() == 0)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            Identifier = okeyPersil,
                            Keterangan = "Bundle tidak memiliki properti untuk di copy"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var LogBundlesS1 = contextcross.logBundles.Query(x => x.keyPersil == keyPersil && x.keyDocType == docType && x.modul == LogActivityModul.Bundle).ToList()
                       .Where(x => (x.created ?? DateTime.Now).Date == tgllog.Date);

                    if (LogBundlesS1 == null || LogBundlesS1.Count() == 0)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 6,
                            Identifier = okeyPersil,
                            Keterangan = "Log bundle tidak ditemukan"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    BundleS2.doclist[index] = JDOCS1;
                    contextplus.mainBundles.Update(BundleS2);
                    contextplus.SaveChanges();

                    var listLog = new List<LogBundle>();
                    foreach (var item in LogBundlesS1)
                    {
                        var log = new LogBundle(item.keyPersil, item.keyDocType, item.keyCreator, item.created ?? DateTime.Now, item.activityType, item.modul);
                        listLog.Add(log);
                    }

                    contextplus.logBundle.Insert(listLog);
                    contextplus.SaveChanges();
                }

                bhost.ReloadData().GetAwaiter().GetResult();

                var csv = MakeCsv(failures.ToArray());
                return new ContentResult { Content = csv.ToString(), ContentType = "text/csv" };
            }
            catch (Exception)
            {

                throw;
            }
        }

        enum CollKind
        {
            //Penugasan
            NoSuratTugas,
            IdBidang,
            Project,
            Desa,
            Category,
            Step,
            NoSPS,
            TglSPS,
            PIC,
            PT,

            //Bundle
            KeyPersil,
            DocType,
            TglLog
        };

        static (CollKind kind, string caption, bool many)[] ColumnFacts = {
            (CollKind.NoSuratTugas, "NoSuratTugas", false),
            (CollKind.IdBidang, "IdBidang", false),
            (CollKind.Project, "project", false),
            (CollKind.Desa, "desa", false),
            (CollKind.Category, "Category", false),
            (CollKind.Step, "Step", false),
            (CollKind.NoSPS, "NoSPS", false),
            (CollKind.TglSPS, "TglSPS", false),
            (CollKind.PIC, "PIC", false),
            (CollKind.PT, "PT", false)
        };

        static (CollKind kind, string caption, bool many)[] BundleColumnFacts =
        {
            (CollKind.KeyPersil, "KeyPersil", false),
            (CollKind.DocType, "DocType", false),
            (CollKind.TglLog, "TglLog", false),
        };

        abstract class ColInfo
        {
            public CollKind kind;
            public string[] captions;

            public ColInfo(CollKind kind, string caption)
            {
                this.kind = kind;
                this.captions = caption.Split('|');
            }

            public abstract (T[] data, string err) Get<T>(DataRow row);
            public abstract bool exists { get; }
        }

        class ColInfoS : ColInfo
        {
            public int number;

            public override bool exists => number != -1;

            public ColInfoS(CollKind kind, string caption)
                : base(kind, caption)
            {
                number = -1;
            }

            public override (T[] data, string err) Get<T>(DataRow row)
            {
                if (number == -1)
                    return (new T[0], null);
                try
                {
                    var obj = row[number];
                    return (obj == DBNull.Value ? new T[0] : new T[] { (T)obj }, null);
                }
                catch (Exception ex)
                {
                    return (new T[0], ex.Message);
                }
            }
        }

        class ColInfoM : ColInfo
        {
            public List<int> numbers = new List<int>();

            public override bool exists => numbers.Any();

            public ColInfoM(CollKind kind, string caption)
                : base(kind, caption)
            {
            }

            public override (T[] data, string err) Get<T>(DataRow row)
            {
                try
                {
                    var Ts = new T[numbers.Count];
                    Array.Fill(Ts, default);
                    var objs = numbers.Select((n, i) => (i, obj: row[n])).Where(x => x.obj != DBNull.Value).ToArray();
                    foreach (var x in objs)
                        Ts[x.i] = (T)x.obj;
                    return (Ts, null);
                }
                catch (Exception ex)
                {
                    return (new T[0], ex.Message);
                }
            }
        }

        static StringBuilder MakeCsv<T>(T[] reportData)
        {
            var lines = new List<string>();
            var sb = new StringBuilder();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();
            var header = string.Join(",", props.ToList().Select(x => x.Name));
            lines.Add(header);

            var valueLines = reportData.Select(row => string.Join(",", header.Split(',')
                                       .Select(a => CsvFormatCorrection(row.GetType().GetProperty(a).GetValue(row, null)))
                                        ));
            lines.AddRange(valueLines);

            foreach (string item in lines)
            {
                sb.AppendLine(item);
            }

            return sb;
        }

        private static string CsvFormatCorrection(object objVal)
        {
            string value = objVal != null ? objVal.ToString() : "";
            string correctedFormat = "";
            correctedFormat = value.Contains(",") ? "\"" + value + "\"" : value;
            return correctedFormat;
        }

        private void MakeConnectionGraph()
        {
            var appsets = Config.AppSettings;
            if (appsets == null)
                throw new InvalidOperationException("Unable to retrieve database connection informations");

            string encoded, server, replica, ssl, uid, pwd, database, protocol, name;
            string dataset = "graph";

            bool enc = appsets.TryGet($"{dataset}:encoded", out encoded) ? encoded == "True" : false;
            if (!appsets.TryGet($"{dataset}:server", out server) || !appsets.TryGet($"{dataset}:replica", out replica) ||
                !appsets.TryGet($"{dataset}:ssl", out ssl) || !appsets.TryGet($"{dataset}:uid", out uid) ||
                !appsets.TryGet($"{dataset}:pwd", out pwd) ||
                !appsets.TryGet($"{dataset}:database", out database)
                )
                throw new Exception("Invalid Doc Repository connection informations");
            if (enc)
                pwd = encryption.Decode64(pwd);
            protocol = "mongodb";
            appsets.TryGet($"{dataset}:protocol", out protocol);

            string url = $"{protocol}://{uid}:{pwd}@{server}/admin?ssl={ssl}&authSource=admin";
            gcontext = new graph.mod.GraphContext(url, "admin");
            gcontext.ChangeDB(database);
        }

        private void MakeConnectionCross()
        {
            var config = (IConfigurationRoot)HttpAccessor.Config.configuration;
            contextcross = new LandropeCrossContext(config);
        }

        class Failures
        {
            public string File { get; set; }
            public int Row { get; set; }
            public string Identifier { get; set; }
            public string IdBidang { get; set; }
            public string Keterangan { get; set; }
        }

        class AssignImport
        {
            public string NoSuratTugas { get; set; }
            public string keyProject { get; set; }
            public string keyDesa { get; set; }
            public AssignmentCat Category { get; set; }
            public DocProcessStep? Step { get; set; }
            public AssignPersil[] detail { get; set; } = new AssignPersil[0];
        }

        class AssignPersil
        {
            public int row { get; set; }
            public string key { get; set; }
            public string IdBidang { get; set; }
            public string noSPS { get; set; }
            public DateTime? tglSPS { get; set; }
        }
    }
}
