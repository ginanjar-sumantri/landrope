using auth.mod;
using landrope.common;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
    [Entity("mainproject", "mainprojects")]
    public class MainProject : entity
    {
        public MainProject()
        {
        }
        public string identity { get; set; }
        public JenisProses en_proses { get; set; }
        public int nomorTahap { get; set; }
        public string[] projects { get; set; }
    }

}
