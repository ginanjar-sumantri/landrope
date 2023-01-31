using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public interface IStateRequest { }
    public class ReqPersilStateView
    {
        public string key { get; set; }
        public string instkey { get; set; }
        public RequestState request { get; set; }
        public string lastState { get; set; }
        public string creator { get; set; }
        public DateTime created { get; set; }

    }

    public class ReqPersilStateDtlView
    {
        public string key { get; set; }
        public string keyPersil { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Alashak { get; set; }
        public string Group { get; set; }
        public string Pemilik { get; set; }
    }

    public class ReqPersilStateCore
    {
        public RequestState request { get; set; }

        public TypeState type { get; set; }

        public string[] keyPersils { get; set; } = new string[0];
    }
}
