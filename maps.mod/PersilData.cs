using maps.mod;
using System;
using Salar.Bois;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;

[assembly:AssemblyVersion("5.2.*")]

namespace maps.mod
{
	public record PersilData : MetaData
	{
		static BoisSerializer boiser = new BoisSerializer();
		static PersilData()
		{
			BoisSerializer.Initialize(typeof(MetaData), typeof(PersilData));
		}
		public bool keluar { get; set; }
		public bool claim { get; set; }
		public bool damai { get; set; }
		public bool damaiB { get; set; }
		public bool kulit { get; set; }
		public string status { get; set; }

		static maps.mod.LandState notBebas(maps.mod.LandState x) => (maps.mod.LandState)((int)x & ~(int)maps.mod.LandState.Sudah_Bebas_);

		public void SetState(maps.mod.LandState state)
		{
			var State = kulit ? LandState.Kulit_ :
			(state & (maps.mod.LandState.Overlap | maps.mod.LandState._Damai)) != maps.mod.LandState.___ ? notBebas(state) :
			(state & maps.mod.LandState.Sudah_Bebas_) != maps.mod.LandState.___ ? state :
			(state & maps.mod.LandState.Bebas_Flag) switch
			{
				maps.mod.LandState.Kampung__Bengkok_Desa or maps.mod.LandState.Deal or maps.mod.LandState.Ditunda => notBebas(state),
				maps.mod.LandState.Proses_Overlap => state | maps.mod.LandState.Overlap,
				maps.mod.LandState.Belum_Bebas => state,
				_ => state | maps.mod.LandState.Sudah_Bebas_
			};
			status = State.Describe3();
		}

		public PersilData() { }
		public static PersilData FromMetaData(MetaData other)
		{
			var json = JsonConvert.SerializeObject(other);
			var newdata = JsonConvert.DeserializeObject<PersilData>(json);
			return newdata;
			//var strm = new MemoryStream();
			//boiser.Serialize(other, strm);
			//var buff = strm.GetBuffer();
			//strm = new MemoryStream(buff);
			//return boiser.Deserialize<PersilData>(strm);
		}

		public static PersilData[] FromMetaDatas(MetaData[] data)
		{
			var json = JsonConvert.SerializeObject(data);
			var newdata = JsonConvert.DeserializeObject<PersilData[]>(json);
			return newdata;
		}

	}
}