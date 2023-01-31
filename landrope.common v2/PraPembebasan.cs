using flow.common;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace landrope.common
{
    public class PraPembebasanCore : CoreBase
    {
        public string key { get; set; }
        public string keyReff { get; set; }
        public string manager { get; set; }
        public string sales { get; set; }
        public string mediator { get; set; }
        public double? luasDeal { get; set; }
        public DateTime? tanggalProses { get; set; }
    }

    public class DetailsPraBebasCore
    {
        #region Tab1
        public string reqKey { get; set; }
        public string detKey { get; set; }
        public string followUpKey { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string desa { get; set; }
        public string alasHak { get; set; }
        public string pemilik { get; set; }
        public string alias { get; set; }
        public string group { get; set; }
        public string notaris { get; set; }
        public double? luasSurat { get; set; }
        public double? hargaSatuan { get; set; }
        public JenisAlasHak jenisAlasHak { get; set; }
        public LandOwnershipType kepemilikan { get; set; }

        public DateTime? FileToNotary { get; set; }
        public DateTime? FileToPra { get; set; }
        public string[] Categories { get; set; }
        public string NotesBelumLengkap { get; set; }
        public DateTime? AdminToPraDP { get; set; }
        public DateTime? AdminToPraLunas { get; set; }
        #endregion

        #region Tab2
        public FormPraBebasBase Ppjb { get; set; }
        public FormPraBebasBase NibPerorangan { get; set; }
        public FormPraBebasBase CekSertifikat { get; set; }
        public FormPraBebasBase BukaBlokir { get; set; }
        public FormPraBebasBase RevisiNop { get; set; }
        public FormPraBebasBase RevisiGeoKpp { get; set; }
        public FormPraBebasBase BalikNama { get; set; }
        public FormPraBebasBase Shm { get; set; }
        #endregion

        #region Tab3
        public FormEndDates EndDates { get; set; }
        #endregion
    }

    public class PraBebasView : FlowPraDeals
    {
        public string key { get; set; }
        public string InstKey { get; set; }
        public string NoRequest { get; set; }
        public string keyReff { get; set; }
        public string KeyManager { get; set; }
        public string Manager { get; set; }
        public string KeySales { get; set; }
        public string Sales { get; set; }
        public string KeyMediator { get; set; }
        public string Mediator { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string Creator { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? tanggalProses { get; set; }
        public double? luasDeal { get; set; }

        public PraBebasView SetState(ToDoState state)
        {
            this.State = state;
            return this;
        }
        public PraBebasView SetStatus(string status, DateTime? time) { (this.Status, this.statustm) = (status ?? "", time); return this; }
        public PraBebasView SetVerb(ToDoVerb verb) { this.verb = verb; return this; }
        public PraBebasView SetTodo(string todo) { this.todo = todo; return this; }
        public PraBebasView SetCmds(ToDoControl[] cmds) { this.cmds = cmds; return this; }
        public PraBebasView SetCreator(bool IsCreator) { this.isCreator = IsCreator; return this; }
        public PraBebasView SetMilestones(DateTime? crea, DateTime? iss, DateTime? del, DateTime? acc, DateTime? clo)
        { (this.Created, this.issued, this.delegated, this.accepted, this.closed) = (crea, iss, del, acc, clo); ; return this; }

        public static PraBebasView Upgrade(PraBebasView old)
            => System.Text.Json.JsonSerializer.Deserialize<PraBebasView>(
                    System.Text.Json.JsonSerializer.Serialize(old)
                );
        public PraBebasView SetRoutes((string key, string todo, ToDoVerb verb, ToDoControl[] cmds)[] routes)
        {
            if (routes == null)
                return this;
            var lst = new List<route>();
            foreach (var item in routes)
            {
                var r = new route();
                r.routekey = item.key;
                r.todo = item.todo;
                r.verb = item.verb;
                r.cmds = item.cmds;

                lst.Add(r);
            }
            this.routes = lst.ToArray();
            return this;
        }

        public PraBebasView UpdateTime()
        {
            this.Created = Convert.ToDateTime(Created).ToLocalTime();
            this.tanggalProses = Convert.ToDateTime(tanggalProses).ToLocalTime();
            return this;
        }
    }

    public class PraDealsDetailView : FlowPraDeals
    {
        public string reqKey { get; set; }
        public string key { get; set; }
        public string followUpKey { get; set; }
        public string nomorFollowUp { get; set; }
        public string keyProject { get; set; }
        public string project { get; set; }
        public string keyDesa { get; set; }
        public string desa { get; set; }
        public DateTime? created { get; set; }
        public string AlasHak { get; set; }
        public string Pemilik { get; set; }
        public string Alias { get; set; }
        public string Group { get; set; }
        public string Status { get; set; }
        public string keyBundle { get; set; }
        public string keyNotaris { get; set; }
        public string notaris { get; set; }
        public double? luasSurat { get; set; }
        public string idBidang { get; set; }
        public DateTime? dealUTJ { get; set; }
        public DateTime? dealDP { get; set; }
        public DateTime? dealLunas { get; set; }
        public ReasonView[] reasons { get; set; } = new ReasonView[0];
        public string statusDeal { get; set; }
        public double? hargaSatuan { get; set; }
        public string jenisAlasHak { get; set; }
        public string kepemilikan { get; set; }

        public bool IsPersilEditable { get; set; } = false;

        public RegisteredDocCore[] doclist { get; set; } = new RegisteredDocCore[0];
        public PraDealsDetailView SetState(ToDoState state) { this.State = state; return this; }
        public PraDealsDetailView SetStatus(string status)
        {
            string[] statusPayment = new[] { "Approval SPK Pelunasan", "Approval SPK DP" };
            var persilAllowedStats = new[] { "baru dibuat", "diterbitkan", "review by analyst", "on hold" };
            bool anyRejectDP = this.reasons.Any(x => x.jenisBayar == "DP");
            bool anyRejectLunas = this.reasons.Any(x => x.jenisBayar == "Lunas");

            DateTime? rejectDPDate = this.reasons.Where(x => x.jenisBayar == "DP").OrderBy(x => x.reasonDate).LastOrDefault()?.reasonDate;
            DateTime? rejectLunasDate = this.reasons.Where(x => x.jenisBayar == "Lunas").OrderBy(x => x.reasonDate).LastOrDefault()?.reasonDate;

            if (status == "Update Document")
                status = this.keyBundle != null ? "Telah di Review Tim Analyst" : "Review By Analyst";

            (this.Status)
                =
            (
             this.dealLunas != null ? "Tuntas" :
             this.dealLunas == null && anyRejectLunas && !statusPayment.Contains(status) && (anyRejectDP ? (rejectLunasDate > rejectDPDate) : true) ? "Pengajuan Pelunasan ditolak" :
             this.dealDP != null && status != "Approval SPK Pelunasan" ? "Pengajuan DP Telah disetujui" :
             this.dealDP == null && anyRejectDP && !statusPayment.Contains(status) ? "Pengajuan DP ditolak" :
             status
            );

            if (persilAllowedStats.Contains(this.Status.ToLower()))
                this.IsPersilEditable = true;

            return this;
        }
        public PraDealsDetailView SetVerb(ToDoVerb verb) { this.verb = verb; return this; }
        public PraDealsDetailView SetTodo(string todo) { this.todo = todo; return this; }
        public PraDealsDetailView SetCmds(ToDoControl[] cmds) { this.cmds = cmds; return this; }
        public PraDealsDetailView SetCreator(bool IsCreator) { this.isCreator = IsCreator; return this; }
        public static PraDealsDetailView Upgrade(PraDealsDetailView old)
            => System.Text.Json.JsonSerializer.Deserialize<PraDealsDetailView>(
                    System.Text.Json.JsonSerializer.Serialize(old)
                );
        public PraDealsDetailView SetRoutes((string key, string todo, ToDoVerb verb, ToDoControl[] cmds)[] routes)
        {
            if (routes == null)
                return this;
           
            var lst = new List<route>();
            foreach (var item in routes)
            {
                if (lst.Any(l => l.routekey == item.key))
                    continue;
                if (this.dealDP != null && item.verb == ToDoVerb.dpPay_)
                    continue;
                if (this.dealUTJ != null && item.verb == ToDoVerb.utjPay_)
                    continue;
                var r = new route();
                r.routekey = item.key;
                r.todo = item.todo;
                r.verb = item.verb;
                r.cmds = item.cmds;
                lst.Add(r);
            }
            this.routes = lst.ToArray();
            return this;
        }
    }

    public class PraDealsDetailViewExt : PraDealsDetailView
    {
        public DateTime? FileToNotary { get; set; }
        public DateTime? FileToPra { get; set; }
        public CategoryL[] Categories { get; set; }
        public string NotesBelumLengkap { get; set; }
        public DateTime? AdminToPraDP { get; set; }
        public DateTime? AdminToPraLunas { get; set; }

        public FormPraBebasBase Ppjb { get; set; }
        public FormPraBebasBase NibPerorangan { get; set; }
        public FormPraBebasBase CekSertifikat { get; set; }
        public FormPraBebasBase BukaBlokir { get; set; }
        public FormPraBebasBase RevisiNop { get; set; }
        public FormPraBebasBase RevisiGeoKpp { get; set; }
        public FormPraBebasBase BalikNama { get; set; }
        public FormPraBebasBase Shm { get; set; }
        public FormEndDates EndDates { get; set; }
        public bool IsPersilEditable { get; set; } = false;
    }

    public class FlowPraDeals
    {
        public ToDoState State { get; set; }
        public DateTime? statustm { get; set; }
        public ToDoVerb verb { get; set; }
        public bool isCreator { get; set; }
        public string todo { get; set; }
        public route[] routes { get; set; }
        public ToDoControl[] cmds { get; set; } = new ToDoControl[0];
        public DateTime? issued { get; set; }
        public DateTime? delegated { get; set; }
        public DateTime? accepted { get; set; }
        public DateTime? closed { get; set; }
    }

    public class route
    {
        public string routekey { get; set; }
        public ToDoVerb verb { get; set; }
        public string todo { get; set; }
        public ToDoControl[] cmds { get; set; } = new ToDoControl[0];
    }

    public class PraBebasCommand
    {
        public string dkey { get; set; }
        public string rkey { get; set; }
        public ToDoControl control { get; set; }
        public string reason { get; set; }
    }

    public class PraBebasDetailCommand : PraBebasCommand
    {
        public string detKey { get; set; }
    }

    public class MatchBidangCore
    {
        public string reqKey { get; set; }
        public string detKey { get; set; }
        public string keyPersil { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }

    }

    public class UpdateDealCore
    {
        public string opr { get; set; }
        public string reqKey { get; set; }
        public string detKey { get; set; }
    }

    public class FormPraBebasBase
    {
        public string NomorSurat { get; set; }
        public DateTime? TanggalSurat { get; set; }
        public string Notaris { get; set; }
    }

    public class FormEndDates
    {
        public DateTime? CertEndDate { get; set; }
        public DateTime? OpenBlockEndDate { get; set; }
        public DateTime? NOPReviseEndDate { get; set; }
        public DateTime? GeoKppEndDate { get; set; }
        public DateTime? BalikNamaEndDate { get; set; }
    }

    public class ReturnPost
    {
        public bool isAlert { get; set; }
        public string returnValue { get; set; }

        public ReturnPost()
        {
        }

        public ReturnPost(string value)
        {
            this.returnValue = value;
        }
        public ReturnPost(bool isAlert, string value)
        {
            this.isAlert = isAlert;
            this.returnValue = value;
        }
    }

    public class ReasonView
    {
        public string reason { get; set; }
        public string jenisBayar { get; set; }
        public DateTime? reasonDate { get; set; }
        public string user { get; set; }
    }

    public class BidangsDealView
    {
        public string keyRequest { get; set; }
        public string keyDetail { get; set; }
        public string idBidang { get; set; }
        public string alasHak { get; set; }
        public double? luasSurat { get; set; }
        public string notaris { get; set; }
        public string pemilik { get; set; }
        public string desa { get; set; }
        public string group { get; set; }
    }

    public class MeasurmentReqCore
    {
        public string reason { get; set; }
        public string[] keyDetails { get; set; } = new string[0];

    }

    public class TableMeasurementRequestForm
    {
        public string nomorRequest { get; set; }
        public string pemilik { get; set; }
        public string alasHak { get; set; }
        public string desa { get; set; }
        public string group { get; set; }
    }

    public class MeasurementRequestFormView
    {
        public DateTime requestDate { get; set; }
        public string Reason { get; set; }
        public TableMeasurementRequestForm[] table { get; set; } = new TableMeasurementRequestForm[0];
        public string requestor { get; set; }
        public string mengetahui { get; set; }
    }

    public class HistoryMeasurementRequestView
    {
        public string key { get; set; }
        public string reason { get; set; }
        public string requestor { get; set; }
        public DateTime requestDate { get; set; }
    }

    public class PraBebasDetailDocumentReportViewModel
    {
        public string Key { get; set; }
        public string IdBidang { get; set; }
        public string AlasHak { get; set; }
        public string Pemilik { get; set; }
        public string Alias { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Group { get; set; }
        public double LuasSurat { get; set; }
        public List<RegisteredDocView> Docs { get; set; }
    }

    public class PraDealsDtRpt
    {
        public int No { get; set; }
        public string IdBidang { get; set; }
        public string Manager { get; set; }
        public string Sales { get; set; }
        public string NoKJB { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? TglKJB { get; set; }
        public string NamaNotaris { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? TglTandaTerimaNotaris { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? TglInputTandaTerimaNotaris { get; set; }
        public string KategoriGrup { get; set; }
        public string Grup { get; set; }
        public string NamaPenjual { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string AlasHak { get; set; }
        public string Sertifikat { get; set; }
        public double? Luas { get; set; }
        
    }

    public class TemplateMandatory
    {
        public JenisAlasHak jenis { get; set; }
        public LandOwnershipType[] owner { get; set; }
    }

    public class FormDocSetting
    {
        public string key { get; set; }
        public string keyDetail { get; set; }
        public string alasHak { get; set; }
        public string desa { get; set; }
        public string group { get; set; }
        public string idbidang { get; set; }
        public float luasSurat { get; set; }
        public string notaris { get; set; }
        public string project { get; set; }
        public DocSettingStatus status { get; set; }
        public string? statustext { get; set; }
        public DocSettingDetail[] docsSetting { get; set; }
    }

    public class DocSettingDetail
    {
        public string keyDocType { get; set; }
        public string jenisDoc { get; set; }
        public bool UTJ { get; set; }
        public bool DP { get; set; }
        public bool Lunas { get; set; }
        public int totalSetUTJ { get; set; }
        public int totalSetDP { get; set; }
        public int totalSetLunas { get; set; }
    }
}