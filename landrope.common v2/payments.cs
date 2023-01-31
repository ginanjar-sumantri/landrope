using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text;

namespace landrope.common
{
	public class cmnGroupUTJ : cmnBase
  {
    public DateTime? tanggal { get; set; }
    public double total { get; set; }
    public Payment bayar { get; set; }
  }

   public class cmnGroupDP : cmnBase
  {
    public List<Rfp> utama { get; set; }
    public List<Rfp> lurah { get; set; }
    public List<Rfp> mediator { get; set; }
    public double? bebanPBB { get; set; }
    public double? bebanPPh { get; set; }
    public double? bebanLainnya { get; set; }
  }

  public class cmnGroupPelunasan : cmnBase
  {
    public Rfp utama { get; set; }
    public Rfp mediator{ get; set; }
  }
}
