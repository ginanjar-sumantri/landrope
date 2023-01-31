#define test_
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
using MongoDB.Bson.Serialization.Attributes;
using GraphHost;
using System.Net.NetworkInformation;

namespace landrope.mod4
{
    public abstract class RequestInfo { }

    [BsonKnownTypes(typeof(StateRequest), typeof(MapRequest), typeof(PersilRequest), typeof(ProjectRequest), typeof(EnProsesRequest), typeof(LandSplitRequest))]
    public class UpdRequestData : namedentity4 { }

    public class UpdRequestData<T> : UpdRequestData where T : RequestInfo
    {
        public string instKey { get; set; }
        public string creator { get; set; }
        public DateTime created { get; set; }
        public string remark { get; set; }
        public T info { get; set; }

        //IGraphHostSvc
#if test
        GraphHostSvc graphhost => ContextService.services.GetService<IGraphHostSvc>() as GraphHostSvc;
#else
        GraphHostConsumer graph => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
#endif

        public UpdRequestData()
        {

        }

        public UpdRequestData AddKeys(user user, string discriminator, TypeState typeState = TypeState._)
        {
            var todoType = typeState == TypeState.bebas ? ToDoType.Land_Approval_Bebas : typeState == TypeState.belumbebas ? ToDoType.Land_Approval_BelumBebas : ToDoType.Land_Approval;

            this.key = MakeKey;
            this.created = DateTime.Now;
            this.creator = user.key;
            this.instKey = CreateGraphInstance(user, todoType, discriminator);

            return this;
        }

        public string GetDiscriminator(user user, string types)
        {
            var privs = user.privileges.Select(p => p.identifier).Where(x => x.Contains("CREATE_" + types)).FirstOrDefault();
            var substr = privs.Substring(7, (privs.Length - 7));

            return substr;
        }
#if test
        public string CreateGraphInstance(user user, ToDoType todo, string discriminator)
        {
            MyTracer.TraceInfo(MethodBase.GetCurrentMethod(), "Create Graphs line 41");
            if (this.instKey != null)
                return instKey;
            this.instKey = graphhost.Create(user, todo, discriminator)?.key;
            return this.instKey;
        }
#else
        public string CreateGraphInstance(user user, ToDoType todo, string discriminator)
        {
            MyTracer.TraceInfo(MethodBase.GetCurrentMethod(), "Create Graphs line 41");
            if (this.instKey != null)
                return instKey;
            this.instKey = graph.Create(user, todo, discriminator).GetAwaiter().GetResult()?.key;
            return this.instKey;
        }
#endif
        [BsonIgnore]
        GraphMainInstance _instance = null;

#if test
        [BsonIgnore]
        public GraphMainInstance instance
        {
            get
            {
                if (_instance == null)
                    _instance = graphhost?.Get(instKey);
                return _instance;
            }
        }
#else
        [BsonIgnore]
        public GraphMainInstance instance
        {
            get
            {
                if (_instance == null)
                    _instance = graph?.Get(instKey).GetAwaiter().GetResult();
                return _instance;
            }
        }
#endif

        [BsonIgnore]
        string _lastStatus = string.Empty;

        [BsonIgnore]
        public string lastStatus
        {
            get
            {
                if (instance != null)
                {
                    _lastStatus = instance.closed ? ToDoState.complished_.AsLandApprovalStatus() : instance?.Core?.nodes.OfType<GraphStatedNode>().FirstOrDefault(x => x.Active == true)?.status;
                }

                return _lastStatus;
            }
        }
    }
}