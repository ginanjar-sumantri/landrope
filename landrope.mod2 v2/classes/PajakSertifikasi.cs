using auth.mod;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
    public class PajakSertifikasi
    {
        public double? LuasNIBBidang { get; set; }
        public PajakSertifikasiHistories[] histories { get; set; } = new PajakSertifikasiHistories[0];
    }

    public class PajakSertifikasiHistories
    {
        public PajakSertifikasiHistories(user user)
        {
            keyCreator = user.key;
        }
        public double? LuasNIBBidang { get; set; }
        public DateTime date { get; set; }
        public string keyCreator { get; set; }
    }
}
