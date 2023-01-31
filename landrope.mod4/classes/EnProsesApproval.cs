using landrope.common;
using mongospace;
using System.Linq;
using auth.mod;
using flow.common;
using System.Collections.Generic;
using landrope.mod2;

namespace landrope.mod4
{
    [Entity("EnProsesRequest", "update_request_data")]
    public class EnProsesRequest : UpdRequestData<EnProsesRequestInfo>
    {
        public EnProsesRequest()
        {

        }

        public EnProsesRequest(user user, EnProsesApprovalCore core, List<Persil> listPersil, List<(int? tahap, string keyPersil)> tahapBayar)
        {
            var types = core.type == TypeState.bebas ? "PROSES_SB" : core.type == TypeState.belumbebas ? "PROSES_BB" : string.Empty;

            var discriminator = GetDiscriminator(user, types);
            AddKeys(user, discriminator, core.type);

            this.info = new EnProsesRequestInfo(core, listPersil, tahapBayar);
        }

        public EnProsesRequest UpdateInfo(EnProsesApprovalCore core, List<Persil> listPersil, List<(int? tahap, string keyPersil)> tahapBayar)
        {
            this.info = new EnProsesRequestInfo(core, listPersil, tahapBayar);
            return this;
        }

        public EnProsesApprovalView ToView(List<user> users, List<(ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)> listVillage, List<PTSK> listPtsk)
        {
            var infoDetail = this.info.details
                        .Select(pd => new EnProasesApprovalDetailView()
                        {
                            keyPersil = pd.keyPersil,
                            idBidang = pd.IdBidang,
                            nomorPeta = pd.noPeta,
                            alasHak = pd.alasHak,
                            project = listVillage.FirstOrDefault(lV => lV.desa.key == pd.keyDesa).project?.identity,
                            desa = listVillage.FirstOrDefault(lV => lV.desa.key == pd.keyDesa).desa?.identity,
                            ptsk = listPtsk.FirstOrDefault(lp => lp.key == pd.keyPtsk)?.identifier,
                            group = pd.group,
                            statusTanah = pd?.en_proses?.ToDesc(),
                            tahap = pd.tahap
                        }).ToArray();

            var view = new EnProsesApprovalView
            {
                _t = this.GetType().Name,
                key = this.key,
                identifier = this.identifier,
                instKey = this.instKey,
                status = this.lastStatus,
                created = this.created.ToLocalTime(),
                creator = users.FirstOrDefault(x => x.key == this.creator)?.FullName,
                remark = this.remark,
                info = new EnProsesApprovalInfoView(this.info.en_proses, this.info.type, infoDetail)
            };

            return view;
        }

        public void Abort()
        {
            this.invalid = true;
        }
    }

    public class EnProsesRequestInfo : RequestInfo
    {
        public JenisProses en_proses { get; set; }
        public TypeState type { get; set; }
        public EnProsesRequestInfoDetail[] details { get; set; } = new EnProsesRequestInfoDetail[0];

        public EnProsesRequestInfo()
        {

        }

        public EnProsesRequestInfo(EnProsesApprovalCore core, List<Persil> listPersil, List<(int? tahap, string keyPersil)> tahapBayar)
        {
            (this.en_proses, this.type, this.details)
                =
            (core.en_proses, core.type,

            core.keyPersils.Select(p => new EnProsesRequestInfoDetail()
            {
                keyPersil = p,
                IdBidang = listPersil.FirstOrDefault(lp => lp.key == p)?.IdBidang,
                keyProject = listPersil.FirstOrDefault(lp => lp.key == p)?.basic?.current?.keyProject,
                keyDesa = listPersil.FirstOrDefault(lp => lp.key == p)?.basic?.current?.keyDesa,
                keyPtsk = listPersil.FirstOrDefault(lp => lp.key == p)?.basic?.current?.keyPTSK,
                alasHak = listPersil.FirstOrDefault(lp => lp.key == p)?.basic?.current?.surat?.nomor,
                noPeta = listPersil.FirstOrDefault(lp => lp.key == p)?.basic?.current?.noPeta,
                group = listPersil.FirstOrDefault(lp => lp.key == p)?.basic?.current?.group,
                en_proses = listPersil.FirstOrDefault(lp => lp.key == p)?.basic?.current?.en_proses,
                tahap = tahapBayar.FirstOrDefault(tB => tB.keyPersil == p).tahap,
                luasSurat = listPersil.FirstOrDefault(lp => lp.key == p)?.basic?.current?.luasSurat,
                luasDibayar = listPersil.FirstOrDefault(lp => lp.key == p)?.basic?.current?.luasDibayar,
            }).ToArray());
        }
    }

    public class EnProsesRequestInfoDetail
    {
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPtsk { get; set; }
        public string alasHak { get; set; }
        public string noPeta { get; set; }
        public string group { get; set; }
        public JenisProses? en_proses { get; set; }
        public int? tahap { get; set; }
        public int newTahap { get; set; }
        public double? luasSurat { get; set; }
        public double? luasDibayar { get; set; }
    }

    public class EnProsesPersilDetail
    {
        public string keyRequest { get; set; }
        public string keyPersil { get; set; }
        public string idBidang { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string ptsk { get; set; }
        public string alasHak { get; set; }
        public string noPeta { get; set; }
        public string group { get; set; }
        public JenisProses statusTanah { get; set; }
    }
}
