using auth.mod;
using flow.common;
using landrope.common;
using landrope.mod2;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.mod4
{
    [Entity("ProjectRequest", "update_request_data")]
    public class ProjectRequest : UpdRequestData<ProjectRequestInfo>
    {
        public ProjectRequest()
        {

        }
        public ProjectRequest(user user, ProjectApprovalCore core, Persil[] persils, (string key, int nomorTahap)[] tahaps)
        {
            var types = core.type == TypeState.bebas ? "PROJECT_SB" : core.type == TypeState.belumbebas ? "PROJECT_BB" : string.Empty;

            var discriminator = GetDiscriminator(user, types);
            AddKeys(user, discriminator, core.type);

            this.info = new ProjectRequestInfo(core, persils, tahaps);
        }
        public ProjectRequestView ToView(LandropePayContext context, (ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)[] locations, cmnItem[] ptsks)
        {
            var location = locations.FirstOrDefault(x => x.desa.key == this.info.keyDesa);
            var ptsk = ptsks.FirstOrDefault(x => x.key == this.info.keyPTSK);

            var view = new ProjectRequestView
            {
                _t = this.GetType().Name,
                key = this.key,
                identifier = this.identifier,
                instKey = this.instKey,
                status = this.lastStatus,
                created = this.created.ToLocalTime(),
                creator = context.users.FirstOrDefault(x => x.key == this.creator)?.FullName,
                remark = this.remark,
                info = new ProjectRequestInfoView(this.info.keyProject, this.info.keyDesa, this.info.keyPTSK, this.info.type,
                locations.FirstOrDefault(x => x.project.key == this.info.keyProject).project?.identity,
                locations.FirstOrDefault(x => x.desa.key == this.info.keyDesa).desa?.identity,
                ptsk?.name)
            };

            return view;
        }
        public void EditDetail(ProjectRequestCommad core, ProjectRequestDetail[] dtls)
        {
            (
                this.info.keyProject,
                this.info.keyDesa,
                this.info.keyPTSK,
                this.info.details,
                remark
                ) =
                (
                core.keyProject,
                core.keyDesa,
                core.KeyPTSK,
                dtls,
                core.remark
                );
        }
        public void Abort()
        {
            this.invalid = true;
        }
    }

    public class ProjectRequestInfo : RequestInfo
    {
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public TypeState type { get; set; }
        public ProjectRequestDetail[] details { get; set; } = new ProjectRequestDetail[0];
        public ProjectRequestInfo(ProjectApprovalCore core, Persil[] persils, (string key, int nomorTahap)[] tahaps)
        {
            (this.keyProject,
                this.keyDesa,
                this.keyPTSK,
                this.type) =
            (core.keyProject,
                core.keyDesa,
                core.keyPTSK,
                core.type);

            if (core.keyPersils.Count() > 0)
            {
                var lst = new List<ProjectRequestDetail>();
                foreach (var k in core.keyPersils)
                {
                    if (k != null && k != string.Empty)
                    {
                        var persil = persils.FirstOrDefault(x => x.key == k);
                        var nomorTahap = tahaps.FirstOrDefault(x => x.key == k).nomorTahap;
                        var detail = new ProjectRequestDetail(persil, nomorTahap);
                        lst.Add(detail);
                    }
                }

                details = lst.ToArray();
            }
        }

    }

    public class ProjectRequestDetail
    {
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string alashak { get; set; }
        public string noPeta { get; set; }
        public string group { get; set; }
        public JenisProses? proses { get; set; }
        public int tahap { get; set; }
        public int newTahap { get; set; }
        public double? luasSurat { get; set; }
        public double? luasDibayar { get; set; }
        public Persil persil(LandropePayContext context, string keyPersil) => context.persils.FirstOrDefault(p => p.key == keyPersil);
        public ProjectRequestDetailView ToView(Persil[] persils, (ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)[] villages, cmnItem[] ptsks)
        {
            var persil = persils.FirstOrDefault(x => x.key == keyPersil);
            var vill = villages.FirstOrDefault(x => x.desa.key == keyDesa);
            var ptsk = ptsks.FirstOrDefault(x => x.key == keyPTSK);

            var view = new ProjectRequestDetailView
            {
                keyPersil = this.keyPersil,
                IdBidang = this.IdBidang,
                Alashak = alashak,
                Desa = vill.desa?.identity,
                Project = vill.project?.identity,
                Group = this.group,
                PTSK = ptsk?.name,
                NoPeta = noPeta,
                StatusTanah = EnumHelpers.StatusTanahDesc(this.proses ?? JenisProses.standar),
                Tahap = this.tahap
            };

            return view;
        }
        public ProjectRequestDetail(Persil persil, int nomorTahap)
        {

            (this.keyPersil, this.IdBidang, this.keyProject, this.keyDesa, this.keyPTSK, this.alashak, this.noPeta, this.group, this.proses, this.tahap, this.luasSurat, this.luasDibayar) =
               (persil.key, persil.IdBidang, persil.basic?.current?.keyProject, persil.basic?.current?.keyDesa, persil.basic?.current?.keyPTSK, persil.basic?.current?.surat?.nomor, persil.basic?.current?.noPeta,
               persil.basic?.current?.group, persil.basic?.current?.en_proses, nomorTahap, persil.basic?.current?.luasSurat, persil.basic?.current?.luasDibayar);
        }
        public ProjectRequestDetail()
        {

        }
    }
}
