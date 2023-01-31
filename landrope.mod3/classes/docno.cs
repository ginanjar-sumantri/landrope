using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using mongospace;
using System.Linq;
using MongoDB.Bson;
using System.ComponentModel;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace landrope.mod3
{
	[Entity("docno", "docno")]
	public partial class docno : entity3
	{
		[BsonRequired]
		public string format { get; set; }
		[BsonRequired]
		public string reset { get; set; }
		public docnodtl[] details { get; set; } = new docnodtl[0];

		public docnodtl AddDtl(DateTime period)
		{
			List<docnodtl> lst = details.ToList();
			docnodtl dtl = new docnodtl { moyear = period, lastno = 0 };
			lst.Add(dtl);
			details = lst.ToArray();
			return dtl;
		}

		static string[] romans = new[] { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };
		public string Generate(DateTime dt, bool save=true, params string[] codes)
		{
			DateTime dtref = reset.ToUpper() switch
			{
				"Y" => new DateTime(dt.Year, 1, 1),
				"M" => new DateTime(dt.Year, dt.Month, 1),
				"D" => dt.Date,
				"H" => dt.Date + new TimeSpan(dt.Hour, 0, 0),
				"N" => dt.Date + new TimeSpan(dt.Hour, dt.Minute, 0),
				"S" => dt.Date + new TimeSpan(dt.Hour, dt.Minute, dt.Second),
				_ => new DateTime(1900, 1, 1)
			};

			int num;

			docnodtl nodtl = details.FirstOrDefault(d => d.moyear == dtref.Date);
			if (nodtl == null)
				nodtl = AddDtl(dtref.Date);
			num = nodtl.NextNumber();

			if (save)
			{
				MyContext().docnoes.Update(this);
				MyContext().SaveChanges();
			}

			string result = this.format;

			MatchCollection mcoll = Regex.Matches(result, @"\{\s*[A-CyYmMdDhHnNsS0#rR]+\s*\}");
			foreach (Match mc in mcoll)
			{
				string st = mc.Value;
				string fmt = st.Substring(1, st.Length - 2);
				result = fmt[0] switch
				{
					'A' => result.Replace(st, codes.Length > 0 ? codes[0] : ""),
					'B' => result.Replace(st, codes.Length > 1 ? codes[1] : ""),
					'C' => result.Replace(st, codes.Length > 2 ? codes[2] : ""),
					'0' or '#' => result.Replace(st, string.Format("{0:" + fmt + "}", num)),
					'r' or 'R' => result.Replace(st, romans[dt.Month]),
					'M' or 'm' => result.Replace(st, dt.ToString(fmt.ToUpper())),
					'Y' or 'y' => result.Replace(st, dtref.ToString(fmt.ToLower())),
					'd' or 'D' => result.Replace(st, dtref.ToString(fmt.ToLower())),
					'h' or 'H' => result.Replace(st, dtref.ToString(fmt.ToUpper())),
					'n' or 'N' => result.Replace(st, dtref.ToString(fmt.ToLower().Replace("n", "m"))),
					's' or 'S' => result.Replace(st, dtref.ToString(fmt.ToLower())),
					_ => result
				};
			}
			return result;
		}
	}

	public partial class docnodtl
	{
		[BsonDateTimeOptions(Kind = DateTimeKind.Local)]
		public DateTime moyear { get; set; }
		public int lastno { get; set; } = 0;

		public int NextNumber()
		{
			int num = this.lastno + 1;
			this.lastno = num;
			return num;
		}

	}

}
