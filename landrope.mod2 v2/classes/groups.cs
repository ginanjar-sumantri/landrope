using auth.mod;
using mongospace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
  [Entity("group","groups")]
	public class Group : entity
  {
    public string keyOrigin { get; set; }
    public string keySK { get; set; }
    public string keyDesa { get; set; }
    public string penjual { get; set; }
    public string status { get; set; }

    [JsonIgnore]
    public string[] keyLands { get; set; } = new string[0];

    public GroupUTJ UTJ { get; set; }
#if (_INIT_MONGO_)
      = new GroupUTJ();
#endif
    public GroupDP DP { get; set; }
#if (_INIT_MONGO_)
      = new GroupDP();
#endif
    public GroupPelunasan pelunasan { get; set; }
#if (_INIT_MONGO_)
      = new GroupPelunasan();
#endif
  }
}
