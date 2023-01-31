using System;
using System.Collections.Generic;
using System.Linq;
using landrope.docdb.Models;
using landrope.mod3;
using landrope.mod2;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using landrope.common;

namespace landrope.docdb.Repo
{
    public class AssignmentRepo
    {
        public ReportSuratTugasDyn GetDataSuratTugas(LandropePlusContext context, string assignKey, string layout)
        {

            if (layout == null)
                return new ReportSuratTugasDyn();

            var layoutDoc = context.GetDocuments(new doclayout(), "docLayout",
                "{$match: {key: '" + layout + "'}}",
                "{$project: {_id: 0}}"
                ).FirstOrDefault();
            var data = new ReportSuratTugasDyn(layoutDoc);

            if (assignKey == null)
                return new ReportSuratTugasDyn();

            var assignment = context.GetCollections(new Assignment(), "assignments", "{_t:'assignment', key: '" + assignKey + "' }", "{_id:0}").FirstOrDefault();
            var villages = context.GetVillage(assignment.keyDesa);

            var notaris = context.GetCollections(new Notaris(), "masterdatas", "{_t: 'notaris', invalid:{$ne:true}}", "{_id:0}").ToList();
            var internals = context.GetCollections(new Internal(), "masterdatas", "{_t: 'internal', invalid:{$ne:true}}", "{_id:0}").ToList();
            var ptsk = context.GetCollections(new PTSK(), "masterdatas", "{_t: 'ptsk', invalid:{$ne:true}}", "{_id:0}").ToList();


            var bidangsAssigned = assignment.details.Select(bid => bid.keyPersil);
            string keyPersils = string.Join(",", bidangsAssigned);
            keyPersils = string.Join(",", keyPersils.Split(',').Select(x => string.Format("'{0}'", x)).ToList());

            var bayars = context.GetDocuments(new { keyPersil = "", tahap = 0 }, "bayars",
                                            "{$match: {$and: [{'bidangs': {$ne:[]}}, {'invalid': {$ne:true}} ]} }",
                                            "{$unwind: '$bidangs'}",
                                            "{$match: {'bidangs.keyPersil': {$in: [" + keyPersils + "]}}}",
                                            "{$project: { _id:0, tahap:'$nomorTahap', keyPersil:'$bidangs.keyPersil' }}"
                                            ).ToList();

            List<(string key, Dynamic value)> bundleShm = GetDataFromBundle(context, MetadataKey.Nomor.ToString("g"), "JDOK036");

            List<(string key, Dynamic value)> bundleShgb = GetDataFromBundle(context, MetadataKey.Nomor.ToString("g"), "JDOK037");

            var persils = context.db.GetCollection<Persil>("persils_v2").Find("{key:{$in :[" + keyPersils + "]}}").ToList();

            var summaries = persils.Select(p => new SummaryBidangSertifikat()
            {
                IdBidang = p.IdBidang,
                Seller = p.basic.current.pemilik,
                Tahap = bayars.FirstOrDefault(b => b.keyPersil == p.key).tahap,
                Shm = Convert.ToString(bundleShm.FirstOrDefault(s => s.key == p.key).value ?? null),
                Shgb = Convert.ToString(bundleShgb.FirstOrDefault(s => s.key == p.key).value ?? null),
                LuasSurat = p.basic.current.luasSurat.GetValueOrDefault(0),
                Satuan = p.basic.current.satuan.GetValueOrDefault(0),
                NilaiTransaksi = p.basic.current.luasSurat.GetValueOrDefault(0) * p.basic.current.satuan.GetValueOrDefault(0)
            });

            var pic = notaris.Select(n => (n.key, n.identifier)).Union(internals.Select(i => (i.key, i.identifier))).ToList();
            string picName = pic != null ? pic.FirstOrDefault(x => x.key == data.PIC).identifier : "";

            (
                data.NomorSurat, data.TanggalSurat, data.PIC, data.Summaries,
                data.Project, data.Desa
            )
                =
            (
                assignment.identifier, assignment.created, picName, summaries.ToArray(),
                villages.project?.identity, villages.desa?.identity
            );

            return data;
        }

        public ReportSuratTugasDyn GetDataSuratTugasGirik(LandropePlusContext context, string assignKey, string layout)
        {
            if (layout == null)
                return new ReportSuratTugasDyn();

            var layoutDoc = context.GetDocuments(new doclayout(), "docLayout",
                "{$match: {key: '" + layout + "'}}",
                "{$project: {_id: 0}}"
                ).FirstOrDefault();

            if (assignKey == null)
                return new ReportSuratTugasDyn();

            ReportSuratTugasDyn data = new ReportSuratTugasDyn();

            var assignment = context.GetCollections(new Assignment(), "assignments", "{_t:'assignment', key: '" + assignKey + "' }", "{_id:0}").FirstOrDefault();
            var villages = context.GetVillage(assignment.keyDesa);

            var notaris = context.GetCollections(new Notaris(), "masterdatas", "{_t: 'notaris', invalid:{$ne:true}}", "{_id:0}").ToList();
            var internals = context.GetCollections(new Internal(), "masterdatas", "{_t: 'internal', invalid:{$ne:true}}", "{_id:0}").ToList();
            var ptsk = context.GetCollections(new PTSK(), "masterdatas", "{_t: 'ptsk', invalid:{$ne:true}}", "{_id:0}").ToList();


            var bidangsAssigned = assignment.details.Select(bid => bid.keyPersil);
            string keyPersils = string.Join(",", bidangsAssigned);
            keyPersils = string.Join(",", keyPersils.Split(',').Select(x => string.Format("'{0}'", x)).ToList());

            var bayars = context.GetDocuments(new { keyPersil = "", tahap = 0 }, "bayars",
                                            "{$match:   {$and: [{'bidangs': {$ne:[]}}, {'invalid': {$ne:true}} ]} }",
                                            "{$unwind: '$bidangs'}",
                                            "{$match:   {'bidangs.keyPersil': {$in: [" + keyPersils + "]}}}",
                                            "{$project: { _id:0, tahap:'$nomorTahap', keyPersil:'$bidangs.keyPersil' }}"
                                            ).ToList();

            List<(string key, Dynamic value)> bundleShm = GetDataFromBundle(context, MetadataKey.Nomor.ToString("g"), "JDOK032");

            var persils = context.db.GetCollection<Persil>("persils_v2").Find("{key:{$in :[" + keyPersils + "]}}").ToList();

            var summaries = persils.Select(p => new SummaryBidangGirik()
            {
                IdBidang = p.IdBidang,
                Seller = !string.IsNullOrEmpty(p.basic.current.surat.nama) ? p.basic.current.surat.nama : p.basic.current.pemilik,
                AlasHak = p.basic.current.surat.nomor,
                Tahap = bayars.FirstOrDefault(b => b.keyPersil == p.key).tahap,

                LuasSurat = p.basic.current.luasSurat.GetValueOrDefault(0),
                Satuan = p.basic.current.satuan.GetValueOrDefault(0),
                NilaiTransaksi = p.basic.current.luasSurat.GetValueOrDefault(0) * p.basic.current.satuan.GetValueOrDefault(0)
            });

            var pic = notaris.Select(n => (n.key, n.identifier)).Union(internals.Select(i => (i.key, i.identifier))).ToList();
            string picName = pic != null ? pic.FirstOrDefault(x => x.key == data.PIC).identifier : "";

            (
                data.NomorSurat, data.TanggalSurat, data.PIC, data.Summaries,
                data.Project, data.Desa
            )
                =
            (
                assignment.identifier, assignment.created, picName, summaries.ToArray(),
                villages.project?.identity, villages.desa?.identity
            );

            return data;
        }

        public List<(string key, Dynamic value)> GetDataFromBundle(LandropePlusContext context, string dataType, string keyDocType)
        {
            var bundles = context.GetDocuments(new { key = "", doclist = new documents.BundledDoc() }, "bundles",
             "{$match:  {'_t' : 'mainBundle'}}",
             "{$unwind: '$doclist'}",
             "{$match : {$and : [{'doclist.keyDocType': '" + keyDocType + "'}, {'doclist.entries' : {$ne : []}}]}}",
             @"{$project : {
                                          _id: 0
                                        , key: 1
                                        , doclist: 1
                         }}").ToList();

            var bundleValue = bundles.Where(x => x.doclist.keyDocType == keyDocType)
                                        .Select(x => new
                                        {
                                            key = x.key,
                                            entries = x.doclist.entries.LastOrDefault().Item.FirstOrDefault().Value,
                                        })
                .Select(x => new
                {
                    key = x.key,
                    dyn = x.entries.props.TryGetValue(dataType, out Dynamic val) ? val : null
                })
                .Select(x => new
                {
                    key = x.key,
                    value = (Dynamic)x.dyn?.Value
                }).ToList();

            return bundleValue.Select(x => (x.key, x.value)).ToList();
        }
    }
}