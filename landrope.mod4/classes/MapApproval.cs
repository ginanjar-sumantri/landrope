using System;
using System.Collections.Generic;
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

namespace landrope.mod4
{
    [Entity("MapRequest", "update_request_data")]
    public class MapRequest : UpdRequestData<MapRequestInfo>
    {
        public MapRequest()
        {

        }

        public MapRequest(MapApprovalReqCore core, user user, string keyPersil)
        {
            AddKeys(user, "ADD_MAP");
            (this.info)
            =
            (new MapRequestInfo(core, keyPersil));
        }

        public MapRequest(user user, string[] keyPersils)
        {
            AddKeys(user, "ADD_MAP");
            (this.info) = (new MapRequestInfo(keyPersils));
        }

        public UpdMapRequestView ToView(List<(ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)> villages,
                                        List<user> listUser,
                                        List<Persil> listPersil,
                                        List<LogWorklist> listWorklistHist,
                                        GraphMainInstance[] instances = null
                                       )
        {
            List<Persil> persils = listPersil.Where(x => this.info.keyPersils.Contains(x.key)).ToList() ?? new List<Persil>();
            Persil persil = listPersil.FirstOrDefault(x => x.key == this.info.keyPersil);
            if (persil != null)
                persils.Add(persil);

            string creator = listUser.FirstOrDefault(lu => lu.key == this.creator)?.FullName;
            var entries = listWorklistHist.Count != 0 ?
                                     listWorklistHist.FirstOrDefault(log => log.key == this.instKey)?.entries : new LogWorklistEntry[0];
            string note = entries != null ? entries.LastOrDefault(e => e.state == ToDoState.created_)?.reason : "";


            var infoBidang = persils.Count() != 0 ? 
                persils.Select(p => new InfoBidang()
                {
                    keyPersil = p.key,
                    idBidang = p.IdBidang,
                    keyProject = p.basic?.current?.keyProject,
                    project = villages.FirstOrDefault(v => v.project.key == p.basic?.current?.keyProject).project?.identity,
                    keyDesa = p.basic?.current?.keyDesa,
                    desa = villages.FirstOrDefault(v => v.desa.key == p.basic?.current?.keyProject).desa?.identity
                }).ToArray()  : new InfoBidang[0]  ;
            
            UpdMapRequestView view = new UpdMapRequestView()
            {
                _t = this.GetType().Name,
                identifier = this.identifier,
                key = this.key,
                instKey = this.instKey,
                state = this.instance?.lastState?.state ?? ToDoState.unknown_,
                status = this.instance?.lastState?.state.AsLandApprovalStatus(),
                created = Convert.ToDateTime(this.created).ToLocalTime(),
                creator = creator,
                info = new MapRequestInfoView()
                {
                    bydesa = this.info.bydesa,
                    requestType = Enum.GetName(typeof(JenisReqMapApproval), this.info.requestType),
                    note = note,
                    bidangs = infoBidang
                }
            };
            return view;
        }
    }

    public class MapRequestInfo : RequestInfo
    {
        public bool bydesa { get; set; }
        public string keyPersil { get; set; }
        public string[] keyPersils { get; set; } = new string[0];
        public string idBidang { get; set; }
        public JenisReqMapApproval requestType { get; set; }

        public MapRequestInfo()
        {

        }
        public MapRequestInfo(MapApprovalReqCore core, string keyPersil)
        {
            (this.bydesa, this.idBidang, this.requestType, this.keyPersil)
             =
            (core.bydesa, core.idBidang, core.requestType, keyPersil);
        }

        public MapRequestInfo(string[] keyPersils)
        {
            (this.bydesa, this.idBidang, this.requestType, this.keyPersil, this.keyPersils)
                =
            (false, null, 0, null, keyPersils);
        }
    }

    public class DetailMap
    {
        public string key { get; set; }
        public byte[] careas { get; set; }

        public MapApprovalDetail ToMapApproveDet(string keyReq, string idBidang, string keyDesa)
        {
            string locationJson = Encoding.ASCII.GetString(this.careas).Replace("'", "\"");
            var locationObj = JsonSerializer.Deserialize<List<Shape>>(locationJson);

            MapApprovalDetail detail = new MapApprovalDetail()
            {
                keyRequest = keyReq,
                keyPersil = this.key,
                IdBidang = idBidang,
                keyDesa = keyDesa,
                map = locationObj.ToArray()
            };
            return detail;
        }

        public Map ToMap()
        {
            Map result = new Map();
            if (this.careas == null)
            {
                result.map = new Shape[0];
                return result;
            }

            string locationJson = Encoding.ASCII.GetString(this.careas).Replace("'", "\"");
            var locationObj = JsonSerializer.Deserialize<List<Shape>>(locationJson);

            result.map = locationObj.ToArray();

            return result;
        }
    }

    public class geoPoint
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    public class Shape
    {
        public geoPoint[] coordinates { get; set; }
    }

    public class MapApprovalDetail
    {
        public string keyRequest { get; set; }
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string keyDesa { get; set; }
        public Shape[] map { get; set; }
    }

    public class Map
    {
        public Shape[] map { get; set; } = new Shape[0];
    }

    public class ObjectByKeyDesa
    {
        public string idBidang { get; set; }
        public JenisBerkas? en_state { get; set; }
        public JenisProses? en_proses { get; set; }
        public DateTime? created { get; set; }
        public double luasSurat { get; set; }
        public double luasDibayar { get; set; }
        public string nomorSurat { get; set; }
        public string namaSurat { get; set; }
        public byte[] careas { get; set; }
    }
}