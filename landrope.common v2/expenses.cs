using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public enum ExpenseType
	{
		DP,
		Termin,
	}

	[Flags]
	public enum ExpensePost
	{
		Tanah_UTJ = 0x11,
		Tanah_DP = 0x12,
		Tanah_Pelunasan = 0x14,
		Taktis = 0x20,
		Notaris = 0x40,
		Pajak_PPh = 0x81,
		Pajak_BPHTB = 0x82,
		Lainnnya = 0x100
	}

	public class ExpenseCore
	{
		public string key { get; set; }
		public ExpensePost en_post { get; set; }

		public string post
		{
			get => en_post.ToString("g");
			set { if (Enum.TryParse<ExpensePost>(value, out ExpensePost et)) en_post = et; }
		}
		public ExpenseType en_type { get; set; }

		public string type
		{
			get => en_type.ToString("g");
			set { if (Enum.TryParse<ExpenseType>(value, out ExpenseType et)) en_type = et; }
		}

		PaymentRinci payment { get; set; }

	}

	public class ExpenseValidationCore
	{
		public string key { get; set; }
		public DateTime date { get; set; }
		public string keyUser { get; set; }
		public bool approved { get; set; }
		public string note { get; set; }
	}

}
