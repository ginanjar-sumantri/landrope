using landrope.common;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.mod3
{
	public interface IDocControl
	{
		DateTime? Executed { get; set; }
		string note { get; set; }
		DocControlItem[] items { get; set; }
		void AddItem(DocControlItem item)
		{
			var lst = items.ToList();
			lst.Add(item);
			items = lst.ToArray();
		}

		void DelItem(string keyDoc)
		{
			var item = items.FirstOrDefault(i => i.keyDoc == keyDoc);
			if (item != null)
			{
				var lst = items.ToList();
				lst.Remove(item);
				items = lst.ToArray();
			}
		}
	}

	public class DocControlItem
	{
		public string keyDoc { get; set; }
		public Existence existence { get; set; }
	}

	public interface IPeminjaman : IDocControl
	{
		DateTime DueDate { get; set; }
		DateTime? Returned { get; set; }
		string keyPengembali { get; set; }
	}

	public class AssDocControl : IDocControl
	{
		public DateTime? Executed { get;set; }
		public string note { get; set; }
		public DocControlItem[] items { get; set; } = new DocControlItem[0];
	}

	public class AssPeminjaman : AssDocControl, IPeminjaman
	{
		public DateTime DueDate { get; set; }
		public DateTime? Returned { get; set; }
		public string keyPengembali { get; set; }
	}

	public class AssPengeluaran : AssDocControl
	{

	}

	public class AssPengembalian : AssDocControl
	{

	}

	[Entity("docControl","controls")]
	[BsonKnownTypes(typeof(Peminjaman), typeof(Pengembalian))]
	public class DocControl : IDocControl
	{
		public DateTime Tanggal { get; set; }
		public string Nomor { get; set; }
		public string keyUser { get; set; }
		public DateTime? Executed { get; set; }
		public string note { get; set; }
		public DocControlItem[] items { get; set; } = new DocControlItem[0];
	}

	[Entity("peminjaman", "controls")]
	public class Peminjaman : DocControl, IPeminjaman
	{
		public DateTime DueDate { get; set; }
		public DateTime? Returned { get; set; }
		public string keyPengembali { get; set; }
	}

	[Entity("pengembalian", "controls")]
	public class Pengembalian : DocControl
	{

	}
}
