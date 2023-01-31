//using System;
//using System.Collections.Generic;
//using System.Text;
//using flow.common;

//namespace landrope.common
//{

//    public class NewPersilView
//    {
//        public string jenisRequest { get; set; }
//        public string instKey { get; set; }
//        public string project { get; set; }
//        public string keyDesa { get; set; }
//        public string keyProject { get; set; }
//        public string desa { get; set; }
//        public string proses { get; set; }
//        public string jenisAlasHak { get; set; }
//        public double luas { get; set; }
//        public string pemilik { get; set; }
//        public string alasHak { get; set; }
//        public string nomorPeta { get; set; }
//        public string group { get; set; }
//        public string alias { get; set; }
//        public string status { get; set; }
//    }

//    public class NewPersilViewExt : NewPersilView
//    {
//        public string creator { get; set; }
//        public DateTime created { get; set; }
//        public ToDoState state { get; set; }
//        public DateTime? statustm { get; set; }
//        public bool isCreator { get; set; }
//        public routes[] rou { get; set; } = new routes[0];
//        public NewPersilViewExt SetState(ToDoState state)
//        {
//            this.state = state;
//            this.status = state.AsStatus();
//            return this;
//        }

//        public NewPersilViewExt SetCreator(bool IsCreator) 
//        { 
//            this.isCreator = IsCreator; return this;
//        }

//        public static NewPersilViewExt Upgrade(NewPersilView old)
//            => System.Text.Json.JsonSerializer.Deserialize<NewPersilViewExt>(
//               System.Text.Json.JsonSerializer.Serialize(old)
//        );

//        public NewPersilViewExt SetRoutes((string key, string todo, ToDoVerb verb, ToDoControl[] cmds)[] routes)
//        {
//            var lst = new List<routes>();
//            foreach (var item in routes)
//            {
//                var r = new routes();
//                r.routekey = item.key;
//                r.todo = item.todo;
//                r.verb = item.verb;
//                r.cmds = item.cmds;

//                lst.Add(r);
//            }

//            rou = lst.ToArray();

//            return this;
//        }

//    }

//    public class NewPersilCore
//    {
//        public bool IsBebas { get; set; }
//        public string keyCreator { get; set; }
//        public string keyProject { get; set; }
//        public string keyDesa { get; set; }
//        public JenisProses jenisProses { get; set; }
//        public JenisAlasHak jenisAlasHak { get; set; }
//        public double luas { get; set; }
//        public string pemilik { get; set; }
//        public string alasHak { get; set; }
//        public string nomorPeta { get; set; }
//        public string group { get; set; }
//        public string alias { get; set; }
//    }


//    public class ReqNewPersilCommand
//    {
//        public string reqKey { get; set; }
//        public string routeKey { get; set; }
//        public ToDoControl control { get; set; }
//        public string reason { get; set; }
//    }
//}