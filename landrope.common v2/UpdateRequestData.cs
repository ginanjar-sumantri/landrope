using auth.mod;
using flow.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.common
{
    public abstract class RequestInfoView { }

    public class UpdRequestDataView
    {

    }

    public class UpdRequestDataView<T> : UpdRequestDataView where T : RequestInfoView
    {
        public string _t { get; set; }
        public string identifier { get; set; }
        public bool isCreator { get; set; }
        public string key { get; set; }
        public string instKey { get; set; }
        public string creator { get; set; }
        public DateTime created { get; set; }
        public string status { get; set; }
        public ToDoState state { get; set; }
        public routes[] rou { get; set; }
        public string remark { get; set; }
        public T info { get; set; }

        public UpdRequestDataView<T> SetStatus(ToDoState state)
        {
            this.status = state.AsStatus();
            return this;
        }

        public UpdRequestDataView<T> SetCreator(bool IsCreator)
        {
            this.isCreator = IsCreator; return this;
        }

        public UpdRequestDataView<T> SetRoutes((string key, string todo, ToDoVerb verb, ToDoControl[] cmds)[] routes)
        {
            var lst = new List<routes>();
            foreach (var item in routes)
            {
                var r = new routes();
                r.routekey = item.key;
                r.todo = item.todo;
                r.verb = item.verb;
                r.cmds = item.cmds;
                lst.Add(r);
            }
            rou = lst.ToArray();

            return this;
        }

        public UpdRequestDataView SetState(ToDoState state) { this.state = state; return this; }
    }

    public class RequestData
    {
        public string req { get; set; }
        public string key { get; set; }
        public bool? invalid { get; set; }
        public string identifier { get; set; }
        public string instKey { get; set; }
        public string creator { get; set; }
        public DateTime created { get; set; }
    }

    public class UpdateRequestCommand
    {
        public string _t { get; set; }
        public string reqKey { get; set; }
        public string routeKey { get; set; }
        public ToDoControl control { get; set; }
        public ToDoVerb verb { get; set; }
        public string remark { get; set; }
        public string reason { get; set; }
    }

    public class LogWorkListCore
    {
        public string instKey { get; set; }
        public string keyCreator { get; set; }
        public DateTime created { get; set; }
        public ToDoState state { get; set; }
        public ToDoVerb verb { get; set; }
        public string reason { get; set; }

        public LogWorkListCore() { }
        public LogWorkListCore(string instKey, string keyCreator, DateTime created,
                           ToDoState state, ToDoVerb verb, string reason)
        {
            (this.instKey, this.keyCreator, this.created, this.state, this.verb, this.reason)
                =
            (instKey, keyCreator, created, state, verb, reason);
        }
    }

    public class StateLogger
    {
        public string key;
        public string instKey;
    }

    public class StateHistoryView
    {
        public string creator { get; set; }
        public DateTime created { get; set; }
        public string state { get; set; }
        public string verb { get; set; }
        public string reason { get; set; }
    }
}