using flow.common;
using System;
using System.Linq;
using landrope.common;
using mongospace;
using GenWorkflow;
using System.Collections.Generic;
using landrope.mod2;

namespace landrope.mod4.classes
{
    [Entity("FUMarketing", "followUpMarketing")]
    public class FUMarketing : namedentity4
    {
        public string keyPersil { get; set; }
        public string keyManager { get; set; }
        public string keyWorker { get; set; }
        public DateTime? followUpDate { get; set; }
        public FollowUpResult? result { get; set; }
        public double? price { get; set; }
        public string note { get; set; }
        public DateTime? created { get; set; }
    }

    [Entity("BidangFollowUp", "bidangFollowUp")]
    public class BidangFollowUp : namedentity4
    {
        public string nomor { get; set; }
        public string keyManager { get; set; }
        public string keyWorker { get; set; }
        public string desaTemp { get; set; }
        public double? luas { get; set; }
        public category[] categories { get; set; } = new category[0];
        public string pemilik => categories.LastOrDefault()?.keyCategory.Skip(2).FirstOrDefault();
        public DateTime? followUpDate { get; set; }
        public FollowUpResult? result { get; set; }
        public double? price { get; set; }
        public string note { get; set; }
        public DateTime? created { get; set; }
        public string keyCreator { get; set; }
        public FollowUpEntries[] entries { get; set; }

        public BidangFollowUpView ToView(List<Worker> listWorker, List<Category> listCategory, double? sisaLuas=null)
        {
            this.categories = this.categories.OrderBy(cat => cat.tanggal).ToArray();
            string result = (this.result??FollowUpResult._).FollowUpResultDescription();
            string sales = listWorker.FirstOrDefault(x => x.key == keyWorker)?.ShortName;
            string manager = listWorker.FirstOrDefault(x => x.key == keyManager)?.ShortName;
            string category = this.categories.Count() != 0 ?
                             string.Join(" ", this.categories.LastOrDefault().keyCategory
                                                  .Select(c => listCategory.FirstOrDefault(lc => lc.key == c)?.shortDesc ?? "")
                                                  .ToList()) : "";

            string pemilik = listCategory.FirstOrDefault(lc => lc.key ==
                            this.categories.LastOrDefault()?.keyCategory.Skip(2)?.FirstOrDefault()
                             )?.shortDesc;

            BidangFollowUpView view = new BidangFollowUpView();
            (
                view.nomor, view.keyManager, view.manager, view.luas,
                view.keyWorker, view.worker, view.followUpDate, view.result,
                view.note, view.created, view.price, view.category,
                view.keyCategories, view.key, view.pemilik, view.resultEnum,
                view.desa, view.sisaLuas
            )
            =
            (
                this.nomor, this.keyManager, manager, this.luas,
                this.keyWorker, sales, Convert.ToDateTime(this.followUpDate).ToLocalTime(), result,
                this.note, Convert.ToDateTime(this.created).ToLocalTime(), this.price, category, 
                this.categories.LastOrDefault()?.keyCategory??new string[0], this.key, pemilik, this.result,
                this.desaTemp, sisaLuas
            );

            return view;
        }
    }

    public class FollowUpEntries
    {
        public string keyCreator { get; set; }
        public DateTime? created { get; set; }
        public string nomor { get; set; }
        public string keyManager { get; set; }
        public string keyWorker { get; set; }
        public string desaTemp { get; set; }
        public double? luas { get; set; }
        public DateTime? followUpDate { get; set; }
        public FollowUpResult? result { get; set; }
        public double? price { get; set; }
        public string note { get; set; }
    }

    public class BidangDocumentFollowUp
    {
        public string keyManager { get; set; }
        public string keyWorker { get; set; }
        public string RequestNo { get; set; }
        public string KeyPersil { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Group { get; set; }
        public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public string NomorPeta { get; set; }
        public double? LuasSurat { get; set; }
        public double? Satuan { get; set; }

        public BidangDocumentFollowUpView ToView()
        {
            BidangDocumentFollowUpView view = new BidangDocumentFollowUpView();
            (
                view.KeyPersil, view.IdBidang, view.Project, view.Desa,
                view.Group, view.Pemilik, view.AlasHak, view.NomorPeta,
                view.LuasSurat, view.Satuan
            )
            =
            (
                this.KeyPersil, this.IdBidang, this.Project, this.Desa,
                this.Group, this.Pemilik, this.AlasHak, this.NomorPeta,
                this.LuasSurat, this.Satuan
            );

            return view;
        }
    }

    [Entity("FollowUpDocument", "followUpDocument")]
    public class FollowUpDocument : namedentity4
    {
        public string keyDetail { get; set; }
        public DateTime? followUpDate { get; set; }
        public string note { get; set; }
        public DateTime? created { get; set; }
        public string keyCreator { get; set; }
        public FollowUpDocumentEntries[] entries { get; set; } = new FollowUpDocumentEntries[0];
    }

    public class DocumentsProperty
    {
        public string keyDocType { get; set; }
        public bool? UTJ { get; set; }
        public int totalSetUTJ { get; set; }
        public bool? DP { get; set; }
        public int totalSetDP { get; set; }
        public bool? Lunas { get; set; }
        public int totalSetLunas { get; set; }
        //public string[] noteSet { get; set; } = new string[0];
    }

    public class FollowUpDocumentEntries
    {
        public DateTime? created { get; set; }
        public string keyCreator { get; set; }
        public DateTime? followUpDate { get; set; }
        public string note { get; set; }
    }

    public class FollowUpDocList
    {
        public string key { get; set; }
        public string keyPersil { get; set; }
        public DateTime? followUpDate { get; set; }
        public string note { get; set; }
    }

    [Entity("DocSettings", "docSettings")]
    public class DocSettings : namedentity4
    {
        public string keyDetail { get; set; }
        public string keyCreator { get; set; }
        public DateTime created { get; set; }
        public DocSettingStatus status { get; set; }
        public DocumentsProperty[] docsSetting { get; set; } = new DocumentsProperty[0];
        public DocSettingsEntries[] entries { get; set; } = new DocSettingsEntries[0];

        public DocSettings AddEntry(DocSettingsEntries entry)
        {
            var listEntry = this.entries.ToList();
            listEntry.Add(entry);
            this.entries = listEntry.ToArray();
            return this;
        }
        public DocSettings UpdateDocSetting(DocumentsProperty[] entry, string creator)
        {
            this.docsSetting = entry;
            AddEntry(new DocSettingsEntries { 
                created = DateTime.Now,
                keyCreator = creator,
                docsSetting = entry
            });
            return this;
        }
    }

    public class DocSettingsEntries
    {
        public string keyCreator { get; set; }
        public DateTime created { get; set; }
        public DocumentsProperty[] docsSetting { get; set; } = new DocumentsProperty[0];
    }
}