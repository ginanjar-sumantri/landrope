//using Brotli;
using geo.shared;
using landrope.common;
using Newtonsoft.Json;
using SharpCompress.Writers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
//using BrotliStream = Brotli.BrotliStream;

namespace landrope.mod.shared
{
	internal class RegArea
	{
		public string key { get; set; }
		public LandState status { get; set; }
		public double Area { get; set; }
	}

	public class MapDataValue
	{
		public string key { get; set; }
		public Dictionary<int, double> Areas { get; set; }
		public Dictionary<string, gmapObject> SKs { get; set; }
		public Dictionary<int, gmapObject[]> Lands { get; set; }
	}

	public class Shape
	{
		public List<geoPoint> coordinates { get; set; }

		[JsonIgnore]
		public double[][] AsArray
		{
			get
			{
				var verts = coordinates.ToList();
				if (verts[0] != verts[verts.Count - 1])
					verts.Add(verts[0]);
				return verts.Select(pt => new double[] { pt.Latitude, pt.Longitude, 0 }).ToArray();
			}
		}
	}

	public class Shapes : List<Shape>
	{
		public gmapObject ToGmap()
		{
			geoPolygon poly = new geoPolygon();
			poly.coordinates = this.Select(a => a.AsArray).ToArray();

			geoFeature feat = new geoFeature();

			feat.geometry = poly;
			return feat;
		}
	}

	public static class MapCompression
	{
		public static byte[] encode(Shapes shps)
		{
			if (shps == null)
				return null;
			if (shps.Count == 0)
				return new byte[0];
			var str = JsonConvert.SerializeObject(shps);
			return ASCIIEncoding.ASCII.GetBytes(str);
		}

		public static Shapes decode(byte[] data)
		{
			if (data == null)
				return null;
			if (data.Length == 0)
				return new Shapes();

			var str = ASCIIEncoding.ASCII.GetString(data);
			return JsonConvert.DeserializeObject<Shapes>(str);
		}

		//public static byte[] raw_compress_br(byte[] input)
		//{
		//	using (System.IO.MemoryStream msInput = new System.IO.MemoryStream(input))
		//	using (System.IO.MemoryStream msOutput = new System.IO.MemoryStream())
		//	using (BrotliStream bs = new BrotliStream(msOutput, System.IO.Compression.CompressionMode.Compress))
		//	{
		//		bs.SetQuality(11);
		//		bs.SetWindow(12);
		//		msInput.CopyTo(bs);
		//		bs.Close();
		//		var output = msOutput.ToArray();
		//		return output;
		//	}
		//}

		//public static byte[] raw_decompress_br(byte[] input)
		//{
		//	if (input == null)
		//		return null;
		//	if (input.Length == 0 || input.Length == 2 && input[0] == '[' && input[1] == ']')
		//		return new byte[0];
		//	using (System.IO.MemoryStream msInput = new System.IO.MemoryStream(input))
		//	using (BrotliStream bs = new BrotliStream(msInput, System.IO.Compression.CompressionMode.Decompress))
		//	using (System.IO.MemoryStream msOutput = new System.IO.MemoryStream())
		//	{
		//		bs.CopyTo(msOutput);
		//		msOutput.Seek(0, System.IO.SeekOrigin.Begin);
		//		var output = msOutput.ToArray();
		//		return output;
		//	}
		//}

		//public static byte[] compress_br(Shapes shps)
		//{
		//	var input = ASCIIEncoding.ASCII.GetBytes(JsonConvert.SerializeObject(shps));
		//	return raw_compress_br(input);
		//}

		//public static Shapes decompress_br(byte[] data)
		//{
		//	var decomp = raw_decompress_br(data);
		//	if (decomp.Length==0)
		//		return new Shapes();
		//	return JsonConvert.DeserializeObject<Shapes>(ASCIIEncoding.ASCII.GetString(decomp));
		//}
	}

}
