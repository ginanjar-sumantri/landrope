using System;
using landrope.mod.shared;
using maps.mod;

namespace api2m.shared
{
	public record DL(DesaFeature[] d, LandFeature[] l);
	public record DLL(DesaFeature[] d, LandFeatureLight[] l);


	public class Boundary
	{
		public string project { get; set; }
		public DsBoundary[] villages { get; set; }
	}

	public class DsBoundary
	{
		public string key { get; set; }
		public string name { get; set; }
		public byte[] careas { get; set; }
	}

	public class InfoBidang
	{
		public string idBidang { get; set; }
		public NewState state { get; set; }
		public int luasSurat { get; set; }
		public int luasDibayar { get; set; }
		public DateTime created { get; set; }
		public Surat surat { get; set; }
		public Shape[] map { get; set; }
	}
	public class Surat
	{
		public string nomor { get; set; }
		public string nama { get; set; }
	}
}
