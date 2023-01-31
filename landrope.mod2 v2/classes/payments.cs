using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text;

namespace landrope.mod2
{
	public class GroupUTJ : ValidatableItem
  {
    [BsonDateTimeOptions(Kind =DateTimeKind.Local, DateOnly = false /*true */)]
    public DateTime? tanggal { get; set; }
    public double total { get; set; }
    public Payment bayar { get; set; }
  }

   public class GroupDP : ValidatableItem
  {
    public List<Rfp> utama { get; set; }
    public List<Rfp> lurah { get; set; }
    public List<Rfp> mediator { get; set; }
    public double? bebanPBB { get; set; }
    public double? bebanPPh { get; set; }
    public double? bebanLainnya { get; set; }
  }

  public class GroupPelunasan : ValidatableItem
  {
    public Rfp utama { get; set; }
    public Rfp mediator{ get; set; }
    //public List<Rfp> utama { get; set; }
    //public List<Rfp> mediator { get; set; }
  }

}
