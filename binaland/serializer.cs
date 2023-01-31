using Binaron.Serializer;
using landrope.mod.shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace binaland
{
	public static class serializer
	{
		public static byte[] serial_bin(Shapes shps)
		{
			var strm = new MemoryStream();
			Binaron.Serializer.BinaronConvert.Serialize(shps, strm);
			strm.Seek(0, SeekOrigin.Begin);
			var buff = new byte[strm.Length];
			strm.Read(buff, 0, buff.Length);
			return buff;
		}

		public static Shapes deserial_bin(byte[] data)
		{
			var strm = new MemoryStream(data);
			return Binaron.Serializer.BinaronConvert.Deserialize<Shapes>(strm);
		}
	}
}
