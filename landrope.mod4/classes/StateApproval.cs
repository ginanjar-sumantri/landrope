using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using auth.mod;
using flow.common;
using GenWorkflow;
using landrope.consumers;
using GraphConsumer;
using landrope.common;
using mongospace;
using Microsoft.Extensions.DependencyInjection;
using Tracer;
using landrope.mod2;
using System.Collections.Generic;

namespace landrope.mod4
{
    [Entity("StateRequest", "update_request_data")]
    public class StateRequest : UpdRequestData<StateRequestInfo>, IStateRequest
    {
        public StateRequest()
        {

        }

        public StateRequest(user user, StateApprovalCore core)
        {
            var discriminator = core.type == TypeState.bebas ? "EDIT_SB" : core.type == TypeState.belumbebas ? "EDIT_BB" : string.Empty;

            AddKeys(user, discriminator);

            this.info = new StateRequestInfo(core);
        }

        public UpdStateRequestView ToView(LandropePayContext context)
        {
            var view = new UpdStateRequestView
            {
                _t = this.GetType().Name,
                key = this.key,
                identifier = this.identifier,
                instKey = this.instKey,
                status = this.instance?.lastState?.state.AsLandApprovalStatus(),
                created = this.created,
                creator = context.users.FirstOrDefault(x => x.key == this.creator)?.FullName,
                info = new StateRequestInfoView(this.info.request, this.info.type)
            };

            return view;
        }

        public void Abort()
        {
            this.invalid = true;
        }
    }

    public class StateRequestInfo : RequestInfo
    {
        public RequestState request { get; set; }
        public TypeState type { get; set; }
        public StateRequestDetail[] details { get; set; } = new StateRequestDetail[0];

        public StateRequestInfo(StateApprovalCore core)
        {
            (this.request, this.type) = (core.request, core.type);

            if (core.keyPersils.Count() > 0)
            {
                var lst = new List<StateRequestDetail>();
                foreach (var k in core.keyPersils)
                {
                    if (k != null && k != string.Empty)
                    {
                        var detail = new StateRequestDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = k };
                        lst.Add(detail);
                    }
                }

                details = lst.ToArray();
            }
        }

        public void AddDetail(StateRequestDetail dtl)
        {
            var lst = new List<StateRequestDetail>();
            if (details != null)
                lst = details.ToList();

            lst.Add(dtl);
            details = lst.ToArray();
        }

        public void EditDetail(StateRequestDetail[] dtls)
        {
            details = dtls.ToArray();
        }
    }

    public class StateRequestDetail
    {
        public string key { get; set; }
        public string keyPersil { get; set; }
        public Persil persil(LandropePayContext context, string keyPersil) => context.persils.FirstOrDefault(p => p.key == keyPersil);

        public StateRequestDetailView ToView(LandropePayContext context, (ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)[] villages)
        {
            var persil = this.persil(context, this.keyPersil);
            var vill = villages.FirstOrDefault(x => x.desa.key == persil.basic?.current?.keyDesa);

            var view = new StateRequestDetailView
            {
                key = this.key,
                keyPersil = this.keyPersil,
                IdBidang = persil.IdBidang,
                Alashak = persil.basic?.current?.surat?.nomor,
                Desa = vill.desa?.identity,
                Project = vill.project?.identity,
                Group = persil.basic?.current?.group,
                Pemilik = persil.basic?.current?.pemilik
            };

            return view;
        }
    }
}
