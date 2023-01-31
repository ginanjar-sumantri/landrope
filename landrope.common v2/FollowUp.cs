using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class BidangFollowUpView
    {
        public string key { get; set; }
        public string nomor { get; set; }
        public string keyManager { get; set; }
        public string manager { get; set; }
        public string keyWorker { get; set; }
        public string worker { get; set; }
        public double? luas { get; set; }
        public double? sisaLuas { get; set; }
        public string pemilik { get; set; }
        public string desa { get; set; }
        public DateTime? followUpDate { get; set; }
        public string result { get; set; }
        public FollowUpResult? resultEnum { get; set; }
        public string note { get; set; }
        public double? price { get; set; }
        public string category { get; set; }
        public string[] keyCategories { get; set; } = new string[0];
        public DateTime? created { get; set; }
    }

    public class FollowUpMarketingCore
    {
        public string nomor { get; set; }
        public string keyManager { get; set; }
        public string keyWorker { get; set; }
        public double? luas { get; set; }
        public string desa { get; set; }
        public DateTime? followUpDate { get; set; }
        public FollowUpResult? result { get; set; }
        public double? price { get; set; }
        public string note { get; set; }
        public string[] keyCategories { get; set; } = new string[0];
    }

    public class BidangDocumentFollowUpView
    {
        public string key { get; set; }
        public string RequestNo { get; set; }
        public string KeyPersil { get; set; }
        public string KeyManager { get; set; }
        public string Manager { get; set; }
        public string KeyWorker { get; set; }
        public string Worker { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Group { get; set; }
        public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public string NomorPeta { get; set; }
        public double? LuasSurat { get; set; }
        public double? Satuan { get; set; }

        public BidangDocumentFollowUpView FillWorker(string manager, string worker)
        {
            this.Manager = manager;
            this.Worker = worker;
            return this;
        }
    }

    public class FollowUpDocumentView
    {
        public string key { get; set; }
        public string keyDetail { get; set; }
        public string docName { get; set; }
        public string keyDocType { get; set; }
        public bool? mandatory { get; set; }
        public bool? DP { get; set; }
        public bool? Lunas { get; set; }
        public int jumlahSet { get; set; }
    }

    public class FollowUpDocumentCore
    {
        public string key { get; set; }
        public string keyDetail { get; set; }
        public DocsPropCore[] DocsProps { get; set; } = new DocsPropCore[0];
    }

    public class DocsPropCore
    {
        public string keyDocType { get; set; }
        public bool? UTJ { get; set; }
        public int totalSetUTJ { get; set; }
        public bool? DP { get; set; }
        public int totalSetDP { get; set; }
        public bool? Lunas { get; set; }
        public int totalSetLunas { get; set; }
    }

    public class FollowUpDocumentBidangCore
    {
        public string keyDetail { get; set; }
        public DateTime? followUpDate { get; set; }
        public string note { get; set; }
    }
}