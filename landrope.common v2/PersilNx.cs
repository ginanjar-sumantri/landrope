using DynForm.shared;
//using landrope.mcommon;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.common
{
	public interface ICore
	{

	}

	public interface ICoreDetail : ICore
	{
		ICore GetCore();
	}

	public class PersilStatBase
	{
		public string keyDesa { get; set; }
		public string keyProject { get; set; }
		public string keyPTSK { get; set; }
		public string keyPenampung { get; set; }
		public DocProcessStep _step { get; set; }
		public AssignmentCat cat { get; set; }
		[GridColumn(Caption = "Bidang", Width = 80)]
		public int count { get; set; }

		public PersilStatBase()
		{ }

		public PersilStatBase(string keyProject, string keyDesa, string keyPTSK, string keyPenampung, AssignmentCat cat, DocProcessStep _step, int count)
		{
			(this.keyProject, this.keyDesa, this.keyPTSK, this.keyPenampung, this.cat, this._step, this.count) =
			(keyProject, keyDesa, keyPTSK, keyPenampung, cat, _step, count);
		}
	}

	public class PersilStat : PersilStatBase
	{
		public double? luasSurat { get; set; }
		public double? luasDibayar { get; set; }
		public double? luasPBT { get; set; }
		public double? luasHGB { get; set; }
		public double? luasProp { get; set; }
		public string noPBT { get; set; }
		public string noHGB { get; set; }
		public DocProcessStep _step { get; set; }
		public DocProcessStep? _tostep { get; set; }

		public double? luas() => luasProp ?? luasDibayar ?? luasSurat;

		public PersilStat(string keyProject, string keyDesa, string keyPTSK, string keyPenampung, AssignmentCat cat, DocProcessStep _step, DocProcessStep? nextstep, int count)
				: base(keyProject, keyDesa, keyPTSK, keyPenampung, cat, _step, count)
		{
			this._step = _step;
			_tostep = nextstep;
		}
	}

	public class PersilNx : ICore
	{
		public string keyProject { get; set; }
		public string keyDesa { get; set; }
		public string keyPTSK { get; set; }
		public string keyPenampung { get; set; }
		public DocProcessStep _step { get; set; }
		public AssignmentCat cat { get; set; }
		[GridColumn(Caption = "Bidang", Width = 80)]
		public int count { get; set; }

		public PersilNx()
				: base()
		{

		}

		public PersilNx(string keyProject, string keyDesa, string keyPTSK, string keyPenampung, AssignmentCat cat, DocProcessStep _step, int count)
		{
			(this.keyProject, this.keyDesa, this.keyPTSK, this.keyPenampung, this.cat, this._step, this.count) =
					(keyProject, keyDesa, keyPTSK, keyPenampung, cat, _step, count);
		}

		public PersilNx(string keyProject, string keyDesa, (string PTSK, string Penampung) keyCompanies, AssignmentCat cat, DocProcessStep _step, int count)
		{
			(this.keyProject, this.keyDesa, this.keyPTSK, this.keyPenampung, this.cat, this._step, this.count) =
					(keyProject, keyDesa, keyCompanies.PTSK, keyCompanies.Penampung, cat, _step, count);
		}

		public PersilNx(PersilNextReady other)
				: this(other.keyProject, other.keyDesa, other.keyPTSK, other.keyPenampung, other.cat, other._step, other.count)
		{
		}
	}

	public class PersilNxDiscrete : PersilNx
	{
		public string key { get; set; }
		public string IdBidang { get; set; }

		public double? LuasSurat { get; set; }
		public double? LuasDibayar { get; set; }
		public double? LuasPBT { get; set; }
		public double? LuasHGB { get; set; }
		public string noPBT { get; set; }
		public string noHGB { get; set; }
		public double? LuasPropsPBT { get; set; }
		public double? LuasPropsHGB { get; set; }
		public bool ongoing { get; set; }

		public PersilNxDiscrete() { }
		public PersilNxDiscrete(PersilPositionView ppv, DocProcessStep next)
		{
			(key, keyProject, keyDesa, keyPTSK, keyPenampung, cat, _step, LuasSurat, LuasDibayar, IdBidang,
					LuasPBT, LuasHGB, noPBT, noHGB, LuasPropsPBT, LuasPropsHGB, ongoing) =
			(ppv.key, ppv.keyProject, ppv.keyDesa, ppv.keyPTSK, ppv.keyPenampung, ppv.category, next, ppv.luasSurat, ppv.luasDibayar,
			ppv.IdBidang, ppv.luasPBT, ppv.luasHGB, ppv.noPBT, ppv.noHGB, ppv.luasPropsPBT, ppv.luasPropsHGB, ppv.ongoing);
		}
	}

	public class PersilNextReady : PersilNx, ICoreDetail
	{
		[GridColumn(Caption = "Project", Width = 120)]
		public string project { get; set; }
		[GridColumn(Caption = "Desa", Width = 120)]
		public string desa { get; set; }
		[GridColumn(Caption = "Pemegang SK", Width = 100)]
		public string PTSK { get; set; }
		[GridColumn(Caption = "Penampung", Width = 100)]
		public string penampung { get; set; }
		[GridColumn(Caption = "Kategori", Width = 60)]
		public string disc { get; set; }
		[GridColumn(Caption = "Proses", Width = 100)]
		public string step
		{
			get => _step.ToString("g");
			set { _step = Enum.TryParse<DocProcessStep>(value, out DocProcessStep stp) ? stp : DocProcessStep.Belum_Bebas; }
		}

		public ICore GetCore() => new PersilNx(this);

		public PersilNextReady SetLocation((string keyProject, string keyDesa, string project, string desa) info)
		{
			this.project = info.project;
			this.desa = info.desa;
			return this;
		}
	}

	/*	public class PersilCrNx : PersilNxDiscrete
			{
					public DocProcessStep current { get; set; }

					public PersilCrNx() { }
					public PersilCrNx(PersilPositionView ppv, DocProcessStep curr)
							:base(ppv)
					{
							current = curr;
					}
			}
	*/
	public class PersilViewCore : ICore
	{
		public string key { get; set; }
	}

	public class PersilView : PersilViewCore, ICoreDetail
	{
		public string keyParent { get; set; }

		[GridColumn(Caption = "Id Bidang", Width = 80)]
		[JsonProperty(PropertyName = "IdBidang")]
		[System.Text.Json.Serialization.JsonPropertyName("IdBidang")]
		public string IdBidang { get; set; }

		[GridColumn(Caption = "No Peta", Width = 80)]
		public string noPeta { get; set; }

		[GridColumn(Caption = "Group Penjual", Width = 120)]
		public string group { get; set; }
		[GridColumn(Caption = "Nama", Width = 120)]
		[JsonIgnore]
		[BsonIgnore]
		public string xnama => string.Join("/", new[] { pemilik, alias, nama }.Where(s => !string.IsNullOrEmpty(s)));

		public string pemilik { get; set; }
		public string alias { get; set; }
		public string nama { get; set; }


		[GridColumn(Caption = "No. Alas Hak", Width = 120)]
		public string noSurat { get; set; }


		[GridColumn(Caption = "Luas Surat", Width = 100)]
		public double? luasSurat { get; set; }

		[GridColumn(Caption = "Luas Bayar", Width = 100)]
		public double? luasDibayar { get; set; }

		public ICore GetCore()
				=> this;
		//=> new PersilViewCore { key = this.key };
	}

	public class PersilViewTemp
	{
		[CsvLabel("Project")]
		public string project { get; set; }

		[CsvLabel("Desa")]
		public string desa { get; set; }

		[CsvLabel("Id Bidang")]
		public string idBidang { get; set; }

		[CsvLabel("Alashak")]
		public string? alashak { get; set; }

		[CsvLabel("No Peta")]
		public string? noPeta { get; set; }

		[CsvLabel("Nama Pemilik")]
		public string? NamaPemilik { get; set; }

		[CsvLabel("Luas Surat")]
		public double? luasSurat { get; set; }

		[CsvLabel("Luas Internak")]
		public double? luasInternal { get; set; }

		[CsvLabel("Luas Dibayar")]
		public double? luasdiBayar { get; set; }

	}

	public class PersilPositionView //: PersilViewCore
	{
		public string key { get; set; }
		public string keyProject { get; set; }
		public string keyDesa { get; set; }
		//[GridColumn(Caption = "Id Bidang", Width = 80)]
		//[JsonProperty(PropertyName = "IdBidang")]
		//[System.Text.Json.Serialization.JsonPropertyName("IdBidang")]
		public string IdBidang { get; set; }

		public AssignmentCat category { get; set; }
		public string keyPTSK { get; set; }
		public string keyPenampung { get; set; }

		//[GridColumn(Caption = "Luas Surat", Width = 100)]
		public double? luasSurat { get; set; }

		//[GridColumn(Caption = "Luas Bayar", Width = 100)]
		public double? luasDibayar { get; set; }

		//[GridColumn(Caption = "Status Akhir", Width = 100)]
		public DocProcessStep step { get; set; }
		/*		public DocProcessStep next { get; set; }
		*/
		[JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public string position
		{
			get => step.ToString().Replace("_", " ");
			set { step = Enum.TryParse<DocProcessStep>(value.Replace(" ", "_"), out DocProcessStep stp) ? stp : DocProcessStep.Belum_Bebas; }
		}

		/*		[JsonIgnore]
						[System.Text.Json.Serialization.JsonIgnore]
						public string todo => next.ToString().Replace("_", " ");
		*/
		public string noPBT { get; set; }
		public string noHGB { get; set; }
		public double? luasPBT { get; set; }
		public double? luasHGB { get; set; }
		public double? luasPropsPBT { get; set; }
		public double? luasPropsHGB { get; set; }
		public bool ongoing { get; set; }

		public PersilPositionView() { }
		public PersilPositionView(string key, string keyProject, string keyDesa, string IdBidang, double luasSurrat, double luasDibayar, DocProcessStep step,
										string noPBT, string noHGB)
		{
			(this.key, this.keyProject, this.keyDesa, this.IdBidang, this.luasSurat, this.luasDibayar, this.step, this.noPBT, this.noHGB) =
					(key, keyProject, keyDesa, IdBidang, luasSurat, luasDibayar, step, noPBT, noHGB);
		}

		public PersilPositionView(string key, AssignmentCat cat, string keyProject, string keyDesa, string keyPTSK, string keyPenampung,
										string IdBidang, double luasSurat, double luasDibayar, DocProcessStep step,
										string noPBT, string noHGB, double? luasPBT = null, double? luasHGB = null)
				: this(key, keyProject, keyDesa, IdBidang, luasSurat, luasDibayar, step, noPBT, noHGB)
		{
			(this.category, this.keyPTSK, this.keyPenampung, this.luasPBT, this.luasHGB) =
					(cat, keyPTSK, keyPenampung, luasPBT, luasHGB);
		}

		public PersilPositionView SetLuasPropsPBT(double luasTotalPBT)
		{
			luasPropsPBT = (luasPBT ?? 0) * (luasDibayar ?? luasSurat ?? 0) / luasTotalPBT;
			return this;
		}

		public PersilPositionView SetLuasPropsHGB(double luasTotalHGB)
		{
			luasPropsHGB = (luasHGB ?? 0) * (luasDibayar ?? luasSurat ?? 0) / luasTotalHGB;
			return this;
		}
	}

	public class PersilPositionWithNext : PersilPositionView
	{
		public PersilPositionWithNext() : base() { }
		public DocProcessStep? next { get; set; }
	}

	public class PersilForMap : PersilPositionWithNext
	{
		public string noPeta { get; set; }
		public string nomor { get; set; }
		public string nama { get; set; }
		public string group { get; set; }
		public bool kampung { get; set; }
		public bool pending { get; set; }
		public bool deal { get; set; }
		public bool sps { get; set; }
		public double? luasOverlap { get; set; }
		public PersilForMap SetPending(bool pending)
		{
			this.pending = pending;
			return this;
		}

		public PersilForMap SetSPS(bool sps)
		{
			this.sps = sps;
			return this;
		}
	}
}
