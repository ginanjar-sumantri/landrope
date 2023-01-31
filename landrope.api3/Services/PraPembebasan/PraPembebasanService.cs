using landrope.common;
using landrope.mod;
using landrope.mod2;
using landrope.mod4;
using Microsoft.Extensions.DependencyInjection;
using mongospace;
using SyncfusionTemplates.PdfTemplates.PraPembebasan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace landrope.api3.Services.PraPembebasan
{
    public class PraPembebasanService : IPraPembebasanService
    {
        LandropeContext _context;

        public PraPembebasanService(IServiceProvider serviceProvider)
        {
            _context = serviceProvider.GetService<LandropeContext>();
        }

        public async Task<PraPembebasanCore> Get(string key)
        {
            var res = await _context.GetDocumentsAsync(new PraPembebasanCore(), "praDeals", new[] {
                $"< $match: < key: '{key}' > >".MongoJs(),
                @"{ $project: { _id: 0,
                        key: '$key',
                        keyReff: '$keyReff',
                } }"
            });

            return res.FirstOrDefault();
        }

        public async Task<byte[]> GetDetailDocumentReportPdf(string key)
        {
            var data = await GetDetailDocumentReportData(key);

            if (data.Count() < 1)
                throw new Exception("Tidak ada data.");

            IEnumerable<ReportDetail> docTypes = await _context.GetDocumentsAsync(new ReportDetail("", ""), "jnsDok",
                "{$project : { _id :0, Identity:'$key', value:'$identifier' }}");

            return await new PraPembebasanDetailDocumentReportPdfTemplate(data.Select(x => x.ToVieModel(docTypes))).Generate();
        }

        async Task<List<PraBebasDetailDocumentReportModel>> GetDetailDocumentReportData(string key)
        {
            string[] keys = new[] { $"'{key}'" };

            var praDeal = await Get(key);

            if (praDeal == null)
                throw new Exception("Data tidak ditemukan");

            while (!string.IsNullOrEmpty(praDeal?.keyReff))
            {
                keys.Add($"'{praDeal.keyReff}'");
                praDeal = await Get(praDeal.keyReff);
            }

            string[] query = new[] {
                    "{ $unwind: '$details' }",
                    $"< $match: < $or: [< key: < $in: [{string.Join(",", keys)}] > >, < keyReff: < $in: [{string.Join(",", keys)}] > >] > >".MongoJs(),

                    "{ $addFields: { sKey: { $ifNull: ['$details.keyPersil', '$details.key' ] } } }",
                    "{ $lookup: { from: 'bundles', localField: 'sKey', foreignField: 'key', as: 'bundle' } }",
                    "{ $unwind: { path: '$bundle', preserveNullAndEmptyArrays: true } }",
                    "{ $lookup: { from: 'persils_v2', localField: 'details.keyPersil', foreignField: 'key', as: 'persil' } }",
                    "{ $unwind: { path: '$persil', preserveNullAndEmptyArrays: true } }",

                    "{ $lookup: { from: 'villages', localField: 'details.keyDesa', foreignField: 'village.key', as: 'md' } }",
                    "{ $unwind: { path: '$md', preserveNullAndEmptyArrays: true } }",
                    @"{ $group: {
                            _id: '$details.key',
                            Key: { $first: '$key' },
                            IdBidang: { $first: '$persil.IdBidang' },
                            AlasHak: { $first: '$details.alasHak' },
                            LuasSurat: { $first: '$details.luasSurat' },
                            Pemilik: { $first: '$details.pemilik' },
                            Alias: { $first: '$details.alias' },
                            Project: { $first: '$md.project.identity' },
                            Desa: { $first: '$md.village.identity' },
                            Group: { $first: '$details.group' },
                            Docs: { $first: '$bundle.doclist' }
                        }
                    }",
                    "{ $project: { _id: 0 } }",
                    "{ $sort: { Key: 1 } }"
            };
            // JenisAlasHak: ??,
            // JenisPemilik: ??,

            return await _context.GetDocumentsAsync(new PraBebasDetailDocumentReportModel(), "praDeals", query);
        }
    }
}
