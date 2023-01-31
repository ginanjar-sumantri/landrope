using landrope.documents;
using System.Collections.Generic;
using System.Linq;
using landrope.common;

namespace landrope.mod4
{
    public class PraBebasDetailDocumentReportModel
    {
        public string Key { get; set; }
        public string IdBidang { get; set; }
        public string AlasHak { get; set; }
        public string Pemilik { get; set; }
        public string Alias { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Group { get; set; }
        public double? LuasSurat { get; set; }
        public IEnumerable<BundledDoc> Docs { get; set; }

        public PraBebasDetailDocumentReportModel() { }

        public PraBebasDetailDocumentReportViewModel ToVieModel(IEnumerable<ReportDetail> docTypes)
        {
            List<RegisteredDocView> results = new();

            if (Docs != null)
                foreach (var doc in Docs)
                {
                    var lastEntry = doc.entries.LastOrDefault();
                    RegisteredDocView tmpDoc = new();
                    string docName = docTypes.FirstOrDefault(x => x.Identity == doc.keyDocType)?.value;

                    if (lastEntry == null)
                    {
                        tmpDoc.docType = docName;
                        tmpDoc.keyDocType = doc.keyDocType;
                        tmpDoc.count = 0;
                        results.Add(tmpDoc);
                        continue;
                    }

                    int itemSet = 0;
                    foreach (var p in lastEntry.Item)
                    {
                        tmpDoc = new();

                        tmpDoc.docType = docName;
                        tmpDoc.keyDocType = doc.keyDocType;
                        tmpDoc.count = itemSet == 0 ? lastEntry.Item.Count() : 1;
                        tmpDoc.SetExistence(p.Value.exists.Select(s => new KeyValuePair<Existence, int>(s.ex, s.cnt)).ToDictionary(x => x.Key, x => x.Value));
                        tmpDoc.SetProperties(p.Value.props);

                        results.Add(tmpDoc);
                        itemSet++;
                    }
                }

            return new PraBebasDetailDocumentReportViewModel()
            {
                Key = this.Key,
                IdBidang = this.IdBidang,
                AlasHak = this.AlasHak,
                Pemilik = this.Pemilik,
                Alias = this.Alias,
                Project = this.Project,
                Desa = this.Desa,
                Group = this.Group,
                LuasSurat = this.LuasSurat.GetValueOrDefault(),
                Docs = results,
            };
        }
    }
}
