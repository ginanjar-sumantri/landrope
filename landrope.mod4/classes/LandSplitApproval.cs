using auth.mod;
using landrope.common;
using landrope.mod2;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.mod4
{
    [Entity("LandSplitRequest", "update_request_data")]
    public class LandSplitRequest : UpdRequestData<LandSplitRequestInfo>
    {
        public LandSplitRequest()
        {

        }
        public LandSplitRequest(user user, Persil[] persils, LandSplitApprovalCore core, (string key, int nomorTahap)[] tahaps)
        {
            var types = "LANDSPLIT";

            var discriminator = GetDiscriminator(user, types);
            AddKeys(user, discriminator);

            this.info = new LandSplitRequestInfo(core, persils, tahaps);
        }

        public LandSplitRequestView ToView(LandropePayContext context, (ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)[] locations, cmnItem[] ptsks)
        {
            var viewDtls = this.info.details.Select(d => new LandSplitRequestDetailView
            {
                keyPersil = d.keyPersil,
                IdBidang = d.IdBidang,
                Project = locations.FirstOrDefault(x => x.project.key == d.keyProject).project?.identity,
                Desa = locations.FirstOrDefault(x => x.desa.key == d.keyDesa).desa?.identity,
                Alashak = d.alashak,
                NoPeta = d.noPeta,
                PTSK = ptsks.FirstOrDefault(x => x.key == d.keyPTSK)?.name,
                StatusTanah = d?.proses?.ToDesc(),
                Tahap = d.tahap
            });

            var view = new LandSplitRequestView
            {
                _t = this.GetType().Name,
                key = this.key,
                identifier = this.identifier,
                instKey = this.instKey,
                status = this.lastStatus,
                created = this.created.ToLocalTime(),
                creator = context.users.FirstOrDefault(x => x.key == this.creator)?.FullName,
                remark = this.remark,
                info = new LandSplitRequestInfoView(this.info.keyPersil, this.info.IdBidang,
                                                    locations.FirstOrDefault(x => x.project.key == this.info.keyProject).project?.identity, this.info.keyProject,
                                                    locations.FirstOrDefault(x => x.desa.key == this.info.keyDesa).desa?.identity, this.info.keyDesa,
                                                    ptsks.FirstOrDefault(x => x.key == this.info.keyPTSK)?.name, this.info.keyPTSK,
                                                    this.info.type, viewDtls.ToArray()),
                IdBidang = this.info.IdBidang,
                project = locations.FirstOrDefault(x => x.project.key == this.info.keyProject).project?.identity,
                desa = locations.FirstOrDefault(x => x.desa.key == this.info.keyDesa).desa?.identity,
                ptsk = ptsks.FirstOrDefault(x => x.key == this.info.keyPTSK)?.name
            };

            return view;
        }

        public void EditDetail(LandSplitRequestCommad core, LandSplitRequestDetail[] dtls, Persil[] persils, (string key, int nomorTahap)[] tahaps)
        {
            var persil = persils.FirstOrDefault(x => x.key == core.keyPersil);

            (this.info.keyPersil, this.info.IdBidang, this.info.keyProject,
               this.info.keyDesa, this.info.keyPTSK, this.info.alashak,
               this.info.noPeta, this.info.luasSurat, this.info.luasDibayar,
               this.info.proses, this.remark, this.info.details, this.info.tahap) =
           (core.keyPersil, persil.IdBidang, persil?.basic?.current?.keyProject,
               persil?.basic?.current?.keyDesa, persil?.basic?.current?.keyPTSK, persil?.basic?.current?.surat?.nomor,
               persil?.basic?.current?.noPeta, persil?.basic?.current?.luasSurat, persil?.basic?.current?.luasDibayar,
               persil?.basic?.current?.en_proses, core.remark, dtls,
               tahaps.FirstOrDefault(x => x.key == persil.key).nomorTahap);
        }
    }

    public class LandSplitRequestInfo : RequestInfo
    {
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string alashak { get; set; }
        public string noPeta { get; set; }
        public JenisProses? proses { get; set; }
        public int? tahap { get; set; }
        public double? luasSurat { get; set; }
        public double? luasDibayar { get; set; }
        public TypeState type { get; set; }
        public LandSplitRequestDetail[] details { get; set; } = new LandSplitRequestDetail[0];
        public LandSplitRequestInfo(LandSplitApprovalCore core, Persil[] persils, (string key, int nomorTahap)[] tahaps)
        {
            var persil = persils.FirstOrDefault(x => x.key == core.keyPersil);

            (this.keyPersil, this.IdBidang, this.keyProject,
                this.keyDesa, this.keyPTSK, this.alashak,
                this.noPeta, this.luasSurat, this.luasDibayar,
                this.proses, this.type, this.tahap) =
            (core.keyPersil, persil.IdBidang, persil?.basic?.current?.keyProject,
                persil?.basic?.current?.keyDesa, persil?.basic?.current?.keyPTSK, persil?.basic?.current?.surat?.nomor,
                persil?.basic?.current?.noPeta, persil?.basic?.current?.luasSurat, persil?.basic?.current?.luasDibayar,
                persil?.basic?.current?.en_proses, core.type, tahaps.FirstOrDefault(x => x.key == persil.key).nomorTahap);

            if (core.keyPersils.Count() > 0)
            {
                var lst = new List<LandSplitRequestDetail>();
                foreach (var k in core.keyPersils)
                {
                    if (k != null && k != string.Empty)
                    {
                        var bidang = persils.FirstOrDefault(x => x.key == k);
                        var nomorTahap = tahaps.FirstOrDefault(x => x.key == k).nomorTahap;
                        var detail = new LandSplitRequestDetail(bidang, nomorTahap);
                        lst.Add(detail);
                    }
                }

                details = lst.ToArray();
            }
        }
    }

    public class LandSplitRequestDetail
    {
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string alashak { get; set; }
        public string noPeta { get; set; }
        public JenisProses? proses { get; set; }
        public int? tahap { get; set; }
        public double? luasSurat { get; set; }
        public double? luasDibayar { get; set; }
        public LandSplitRequestDetail(Persil persil, int nomorTahap)
        {

            (this.keyPersil, this.IdBidang, this.keyProject,
                this.keyDesa, this.keyPTSK, this.alashak,
                this.noPeta, this.proses,
                this.tahap, this.luasSurat, this.luasDibayar) =
            (persil.key, persil.IdBidang, persil.basic?.current?.keyProject,
               persil.basic?.current?.keyDesa, persil.basic?.current?.keyPTSK, persil.basic?.current?.surat?.nomor,
               persil.basic?.current?.noPeta, persil.basic?.current?.en_proses,
               nomorTahap, persil.basic?.current?.luasSurat, persil.basic?.current?.luasDibayar);
        }
    }
}
