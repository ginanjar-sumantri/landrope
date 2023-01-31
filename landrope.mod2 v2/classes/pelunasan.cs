using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
    public class pelunasan
    {
        public string keyCreator { get; set; }
        public DateTime created { get; set; }
        public double luasPelunasan { get; set; }
        public string reason { get; set; }
    }

    public class biayalainnya
    {
        public string identity { get; set; }
        public double? nilai { get; set; }
        public bool? fgLainnya { get; set; }
    }
}
