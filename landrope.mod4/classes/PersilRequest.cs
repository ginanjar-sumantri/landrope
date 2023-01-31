using auth.mod;
using flow.common;
using GenWorkflow;
using landrope.common;
using landrope.mod2;
using landrope.mod4;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;

namespace landrope.mod4
{
    [Entity("PersilRequest", "update_request_data")]
    public class PersilRequest : UpdRequestData<PersilRequestInfo>, IPersilApproval
    {
        public PersilRequest()
        {

        }

        public PersilRequest(user user, string keyPersil, string discriminator)
        {
            AddKeys(user, discriminator);
            (this.info) = (new PersilRequestInfo(keyPersil));
        }

        public PersilApprovalView ToView(List<user> listUser, 
                                         List<Persil> listPersil,
                                         user user,
                                         List<(ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)> villages
                                        )
        {
            Persil persil = listPersil.FirstOrDefault(p => p.key == this.info.keyPersil);

            ValidatableEntry<PersilBasic> lastEntry = persil.basic.entries.LastOrDefault();

            string project = villages.FirstOrDefault(v => v.project.key == lastEntry.item.keyProject).project?.identity;
            string desa = villages.FirstOrDefault(v => v.desa.key == lastEntry.item.keyDesa).desa?.identity;
            string creator = listUser.FirstOrDefault(lu => lu.key == this.creator)?.FullName;

            PersilApprovalView view = new PersilApprovalView()
            {
                _t = this.GetType().Name,
                state = this.instance.lastState.state,
                status = this.instance.lastState.state.AsLandApprovalStatus(),
                identifier = this.identifier,
                key = this.key,
                instKey = this.instKey,
                created = Convert.ToDateTime(this.created).ToLocalTime(),
                creator = creator,
                isCreator = this.creator == user.key,
                info = new PersilApprovalInfoView()
                {
                    keyPersil = this.info.keyPersil,
                    keyDesa = lastEntry.item.keyDesa,
                    keyProject = lastEntry.item.keyProject,
                    desa = desa,
                    project = project,
                    pemilik = lastEntry?.item?.pemilik,
                    jenisAlasHak = Enum.GetName(typeof(JenisAlasHak), lastEntry.item.en_jenis),
                    jenisProses = Enum.GetName(typeof(JenisProses), lastEntry.item.en_proses),
                    luas = lastEntry.item.luasSurat.GetValueOrDefault(0),
                    alasHak = lastEntry?.item?.surat?.nomor,
                    alias = lastEntry?.item?.alias,
                    nomorPeta = lastEntry?.item?.noPeta
                }
            };

            return view;
        }
    }

    public class PersilRequestInfo : RequestInfo
    {
        public string keyPersil { get; set; }

        public PersilRequestInfo()
        {

        }

        public PersilRequestInfo(string keyPersil)
        {
            this.keyPersil = keyPersil;
        }
    }
}
