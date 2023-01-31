using binaland;
using ClipperLib;
using CoordHelper;
//using BlazorLeaflet.Models;
using geo.shared;
using landrope.common;
using landrope.mod.shared;
using MongoDB.Bson.Serialization.Attributes;
using protobsonser;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace maps.mod
{
	[Flags]
	public enum DrawFlag
	{
		PersilJenis = 1,
		PersilStatus = 2,
		PersilLand = 3,
		BatasDesa = 4,
		KulitDesa = 8,
		PersilDesa = 12
	}

	public class polyattribs
	{
		public Color? fillColor { get; set; }
		public Color? strokeColor { get; set; }
		public float? strokeWidth { get; set; }
		public float? hoverWidth { get; set; }

		public static polyattribs Empty => (null, null, 0, 0);
		public polyattribs() { }
		public polyattribs(Color? fill, Color? stroke, float? width, float? hover)
		{
			(fillColor, strokeColor, strokeWidth, hoverWidth) = (fill, stroke, width, hover);
		}

		[Flags]
		public enum AlterFlag
		{
			Color = 1, Width = 2
		}

		public void AlterFill(Color? color)
		{
			fillColor = color;
		}
		public void AlterHover(float? width)
		{
			hoverWidth = width;
		}
		public void AlterStroke(Color? color, float? width, AlterFlag flag)
		{
			if ((flag & AlterFlag.Color) == AlterFlag.Color)
				strokeColor = color;
			if ((flag & AlterFlag.Width) == AlterFlag.Width)
				strokeWidth = width;
		}

		public static implicit operator polyattribs((Color? fill, Color? stroke, float? strlwi, float? hov) tup)
			=> new polyattribs(tup.fill, tup.stroke, tup.strlwi, tup.hov);
	}

	//	public static Dictionary<PersilCat, polyattribs> catg_attribs = new Dictionary<PersilCat, polyattribs>
	//	{
	//		{PersilCat.Kampung,(Coloring.catg_colors[PersilCat.Kampung].fill,Coloring.catg_colors[PersilCat.Kampung].line,1,2) },
	//		{PersilCat.Belum,(Coloring.catg_colors[PersilCat.Belum].fill,Coloring.catg_colors[PersilCat.Belum].line,1,2)},
	//		{PersilCat.Sertifikat,(Coloring.catg_colors[PersilCat.Sertifikat].fill,Coloring.catg_colors[PersilCat.Sertifikat].line,1,2) },
	//		{PersilCat.Girik,(Coloring.catg_colors[PersilCat.Girik].fill,Coloring.catg_colors[PersilCat.Girik].line,1,2) },
	//		{PersilCat.Hibah,(Coloring.catg_colors[PersilCat.Hibah].fill,Coloring.catg_colors[PersilCat.Hibah].line,1,2) },
	//		{PersilCat.BelumSerti,(Coloring.catg_colors[PersilCat.BelumSerti].fill,Coloring.catg_colors[PersilCat.BelumSerti].line,1,2)},
	//		{PersilCat.BelumGirik,(Coloring.catg_colors[PersilCat.BelumGirik].fill,Coloring.catg_colors[PersilCat.BelumGirik].line,1,2)},
	//		{PersilCat.BelumHibah,(Coloring.catg_colors[PersilCat.BelumHibah].fill,Coloring.catg_colors[PersilCat.BelumHibah].line,1,2)},
	//	};

	//	public static Dictionary<PersilCat, polyattribs> catg_attribs2 = new Dictionary<PersilCat, polyattribs>
	//	{
	//		{PersilCat.Kampung,(Coloring.catg_colors2[PersilCat.Kampung].fill,Coloring.catg_colors2[PersilCat.Kampung].line,1,2) },
	//		{PersilCat.NonHibah,(Coloring.catg_colors2[PersilCat.NonHibah].fill,Coloring.catg_colors2[PersilCat.NonHibah].line,1,2)},
	//		{PersilCat.Hibah,(Coloring.catg_colors2[PersilCat.Hibah].fill,Coloring.catg_colors2[PersilCat.Hibah].line,1,2) },
	//		{PersilCat.BelumSerti,(Coloring.catg_colors2[PersilCat.BelumSerti].fill,Coloring.catg_colors2[PersilCat.BelumSerti].line,1,2)},
	//		{PersilCat.BelumGirik,(Coloring.catg_colors2[PersilCat.BelumGirik].fill,Coloring.catg_colors2[PersilCat.BelumGirik].line,1,2)},
	//		{PersilCat.BelumHibah,(Coloring.catg_colors2[PersilCat.BelumHibah].fill,Coloring.catg_colors2[PersilCat.BelumHibah].line,1,2)},
	//		{PersilCat.Deal,(Coloring.catg_colors2[PersilCat.Deal].fill,Coloring.catg_colors2[PersilCat.Deal].line,1,2)},
	//	};

	//	public static Dictionary<LandState, polyattribs> stat_attribs = new Dictionary<LandState, polyattribs>
	//	{
	//		{LandState.Kampung__Bengkok_Desa,(Coloring.stat_colors[LandState.Kampung__Bengkok_Desa].fill,Coloring.stat_colors[LandState.Kampung__Bengkok_Desa].line,1,2)},
	//		{LandState.Belum_Bebas,(Coloring.stat_colors[LandState.Belum_Bebas].fill,Coloring.stat_colors[LandState.Belum_Bebas].line,1,2)},
	//		{LandState.Baru_Bebas,(Coloring.stat_colors[LandState.Baru_Bebas].fill,Coloring.stat_colors[LandState.Baru_Bebas].line,1,2)},
	//		{LandState.PJB,(Coloring.stat_colors[LandState.PJB].fill,Coloring.stat_colors[LandState.PJB].line,1,2)},
	//		{LandState.PBT,(Coloring.stat_colors[LandState.PBT].fill,Coloring.stat_colors[LandState.PBT].line,1,2)},
	//		{LandState.SPH,(Coloring.stat_colors[LandState.SPH].fill,Coloring.stat_colors[LandState.SPH].line,1,2)},
	//		{LandState.PBT_PT,(Coloring.stat_colors[LandState.PBT_PT].fill,Coloring.stat_colors[LandState.PBT_PT].line,1,2)},
	//		{LandState.SK,(Coloring.stat_colors[LandState.SK].fill,Coloring.stat_colors[LandState.SK].line,1,2)},
	//		{LandState.HGB_PT,(Coloring.stat_colors[LandState.HGB_PT].fill,Coloring.stat_colors[LandState.HGB_PT].line,1,2)},
	//		{LandState.SHM,(Coloring.stat_colors[LandState.SHM].fill,Coloring.stat_colors[LandState.SHM].line,1,2)},
	//		{LandState.Penurunan_Hak,(Coloring.stat_colors[LandState.Penurunan_Hak].fill,Coloring.stat_colors[LandState.Penurunan_Hak].line,1,2)},
	//		{LandState.Penurunan__Peningkatan_Hak,(Coloring.stat_colors[LandState.Penurunan__Peningkatan_Hak].fill,Coloring.stat_colors[LandState.Penurunan__Peningkatan_Hak].line,1,2)},
	//		{LandState.AJB,(Coloring.stat_colors[LandState.AJB].fill,Coloring.stat_colors[LandState.AJB].line,1,2)}
	//	};

	//	public static Dictionary<bool, polyattribs> dsattribs = new Dictionary<bool, polyattribs>
	//	{
	//		{ false,(null,Color.Yellow,6,8) },
	//		{ true,(Color.White,Color.Blue,2,2) }
	//	};
	//}

	public class MapStack
	{
		public string key;
		public List<MapStack2> desas = new List<MapStack2>();
		public MapStack2 GetDesa(string key)
		{
			var stack = desas.FirstOrDefault(d => d.key == key);
			if (stack.key == null)
			{
				stack = new MapStack2 { key = key };
				desas.Add(stack);
			}
			return stack;
		}
		public LandFeature FindId(string id) => desas.FirstOrDefault(d => d.FindId(id).id != null)?.FindId(id) ?? default;
	}

	public class MapStack2
	{
		public string key;
		public List<MapStack3> categories = new List<MapStack3>();

		public MapStack3 GetCategory(PersilCat cat)
		{
			var stack = categories.FirstOrDefault(d => d.cat == cat);
			if (stack.cat == PersilCat.Unknown)
			{
				stack = new MapStack3 { cat = cat };
				categories.Add(stack);
			}
			return stack;
		}
		public LandFeature FindId(string id) => categories.FirstOrDefault(c => c.FindId(id).id != null)?.FindId(id) ?? default;
	}

	public class MapStack3
	{
		public PersilCat cat;
		public List<LandFeature> features = new List<LandFeature>();
		public LandFeature FindId(string id) => features.FirstOrDefault(f => f.id == id);
	}

	[BsonKnownTypes(typeof(DesaFeature), typeof(LandFeature))]
	public abstract class FeatureBase
	{
		public string key;
		public string id;
		public string prokey;
		public string deskey;
		[CsvLabel(true)]
		public geo.shared.XPointF[][] shapes { get; set; }
		//public Dictionary<DrawFlag, polyattribs> attribs = new Dictionary<DrawFlag, polyattribs>();
		[CsvLabel(true)]
		public polyattribs attribs { get; private set; } = null;// new polyattribs();

		public T SetAttribute<T>(polyattribs attrib) where T : FeatureBase
		{
			//this.attribs = attrib;
			return this as T;
		}

		//public abstract Polygon ToPolygon(params bool[] options);

		public virtual (geo.shared.XPointF min, geo.shared.XPointF max) GetBounds()
		{
			double? minX = null, minY = null, maxX = null, maxY = null;
			if (shapes == null)
				return collect();

			var xxs = shapes.SelectMany(ss => ss.Select(s => s.X));
			var yys = shapes.SelectMany(ss => ss.Select(s => s.Y));
			if (!xxs.Any())
				return collect();

			minX = xxs.Min();
			minY = yys.Min();
			maxX = xxs.Max();
			maxY = yys.Max();
			return collect();

			(geo.shared.XPointF min, geo.shared.XPointF max) collect()
			{
				if (minX == null || minY == null || maxX == null || maxY == null)
					return (null, null);
				return (new geo.shared.XPointF(minX.Value, minY.Value), new geo.shared.XPointF(maxX.Value, maxY.Value));
			}
		}
		public virtual string MakeContent() => "";

		static PolyTree FromPolygon(List<List<IntPoint>> poly)
		{
			if (poly.Count > 1) return PolyHelper.Xor(poly);
			var tree = new PolyTree();
			tree.Contour.AddRange(poly[0]);
			return tree;
		}

		public class Comparer : IEqualityComparer<FeatureBase>
		{
			public bool Equals(FeatureBase x, FeatureBase y)
				=> x.key == y.key;

			public int GetHashCode(FeatureBase obj)
				=> obj.key.GetHashCode();
		}

		public static Comparer comparer = new Comparer();

		public static Byte[] Serialize<T>(T[] feats) where T : FeatureBase =>
			BsonSupport.BsonSerialize(feats);
		public static String Serialize64<T>(T[] feats) where T : FeatureBase =>
			Convert.ToBase64String(BsonSupport.BsonSerialize(feats));
		public static T[] Deserialize<T>(byte[] data) where T : FeatureBase =>
			BsonSupport.BsonDeserialize<T[]>(data);
		public static T[] Deserialize64<T>(string st) where T : FeatureBase =>
			BsonSupport.BsonDeserialize<T[]>(Convert.FromBase64String(st));
	}

	[BsonDiscriminator("desaFeature")]
	[Serializable]
	public class DesaFeature : FeatureBase
	{
		public string project;
		public string nama;

		double? _area = null;

		public double Area
		{
			get
			{
				if (_area == null && shapes != null && shapes.Any())
					CalcArea();
				return _area ?? 0;
			}
		}

		void CalcArea()
		{
			if (shapes == null || !shapes.Any())
			{
				_area = 0;
				return;
			}

			var points = shapes.Select(S => S.Select(s => UtmConv.LatLon2UTM(s.X, s.Y, 48, true))
			.Select(p => PolyHelper.ut2ip(p.x, p.y))
			//.Select(p => PolyHelper.ut2ip(p))
			.ToList()).ToList();
			//var points = upoints.Select(S => S.Select(s => new IntPoint(
			//	PolyHelper.d2i(s.x), PolyHelper.d2i(s.y))).ToList()).ToList();
			var ptree = PolyHelper.Xor(points);
			_area = PolyHelper.GetArea(ptree);
		}


		public DesaFeature SetArea(double? area)
		{
			_area = area;
			return this;
		}

		//public override Polygon ToPolygon(bool[] options)
		//{
		//	//bool batas = (flag & DrawFlag.BatasDesa) == DrawFlag.BatasDesa;
		//	//bool kulit = (flag & DrawFlag.KulitDesa) == DrawFlag.KulitDesa;
		//	//if (!batas && !kulit)
		//	//	return null;
		//	//polyattribs attrib = attribs;// batas ? attribs[DrawFlag.BatasDesa] : attribs[DrawFlag.KulitDesa];

		//	var poly = new Polygon
		//	{
		//		DrawStroke = attribs.strokeWidth != null && attribs.strokeColor != null,
		//		Fill = attribs.fillColor != null,
		//		FillOpacity = (attribs.fillColor?.A ?? 0) / 255d,
		//		FillColor = Color.FromArgb(255, attribs.fillColor ?? Color.Black),
		//		StrokeColor = Color.FromArgb(255, attribs.strokeColor ?? Color.Black),
		//		StrokeOpacity = (attribs.strokeColor?.A ?? 0) / 255d,
		//		StrokeWidth = attribs.strokeWidth ?? 0,
		//		Shape = shapes.Select(xx=> xx.Select(x=>(PointF)x).ToArray()).ToArray(),
		//	};
		//	//poly.OnMouseOver += hoverHandler;
		//	//poly.OnMouseOut += outHandler;
		//	//poly.OnClick += clickHandler;
		//	//poly.OnDblClick += clickHandler;
		//	//poly.IsBubblingMouseEvents = false;

		//	if (options.Any() && options[0])
		//	{
		//		poly.Tooltip = new Tooltip
		//		{
		//			Direction = "auto",
		//			IsSticky = false,
		//			Opacity = 0.75,
		//			Content = MakeContent()
		//		};
		//	}

		//	this.id = poly.Id;

		//	return poly;
		//}

		public override string MakeContent() =>
$@"<b>Project&nbsp;:&nbsp;{this.project}<br/>
Desa&nbsp;:{nama}</b><br/>";
	}

	[BsonDiscriminator("landFeature")]
	[Serializable]
	public class LandFeature : FeatureBase
	{
		static maps.mod.LandState notBebas(maps.mod.LandState x) => (maps.mod.LandState)((int)x & ~(int)maps.mod.LandState.Sudah_Bebas_);
		public MetaData data { get; set; }
		[CsvLabel(true)]
		public bool keluar { get; set; }
		[CsvLabel(true)]
		public bool claim { get; set; }
		[CsvLabel(true)]
		public bool damai { get; set; }
		[CsvLabel(true)]
		public bool damaiB { get; set; }
		[CsvLabel(true)]
		public bool kulit { get; set; }
		[CsvLabel(true)]
		public LandState state { get; set; }
		public LandState State
		{
			get
			{
				if (kulit)
					return LandState.Kulit_;

				var norstate = state & ~LandState.Overlap_Or_Damai;
				if ((norstate & LandState.Ditunda_Or_Kampung) != LandState.___)
					return state;

				var ovrstate = state & LandState.Overlap;
				var clmstate = state & LandState._Damai;

				var bbsdealstate = norstate & LandState.Bebas_Or_Deal;
				if (bbsdealstate == LandState.___ && data.deal != null)
					norstate |= LandState.Deal;
				else if (bbsdealstate == LandState.Sudah_Bebas_)
					norstate &= ~LandState.Deal;


				//return kulit ? LandState.Kulit_ :
				//	(ovrstate & LandState._Damai) != LandState.___ ?
				//		norstate == LandState.Deal ? norstate | LandState.Deal : norstate
				//	 :
				//ovrstate != LandState.___ ? ovrstate :
				//(norstate & LandState.Bebas_Flag) switch
				//{
				if ((norstate & LandState.Bebas_Flag) == LandState.Proses_Overlap)
					ovrstate |= LandState.Overlap;

				return norstate | clmstate | ovrstate;
						//LandState.Belum_Bebas => state,
						//_ => state | LandState.Sudah_Bebas_
					//};
			}
		}

		public SimpleState? StateSimple =>
			keluar || kulit ? null :
			claim ? SimpleState.ClaimFlag :
			damai ? SimpleState.Damai_Std :

			damaiB ? SimpleState.Damai :

			(state & maps.mod.LandState.Overlap) != maps.mod.LandState.___ ? SimpleState.Bintang :

			(state & maps.mod.LandState.Sudah_Bebas_) == maps.mod.LandState.Sudah_Bebas_ ? SimpleState.Bebas :

			(state & maps.mod.LandState.Deal) == maps.mod.LandState.Deal || data.deal != null ? (
				(state & maps.mod.LandState.Overlap_Or_Damai) == maps.mod.LandState.Overlap_Or_Damai ? SimpleState.CalonDamai :
				SimpleState.Deal) :

			SimpleState.BelumBebas;

		public string status => State.Describe3();
		public maps.mod.LandState state3 => (State & maps.mod.LandState._Damai) != 0 ? maps.mod.LandState._Damai : State;

		public double LuasDesa { get; private set; } = 0;

		public LandFeature SetLuasDesa(double luas)
		{
			LuasDesa = luas;
			return this;
		}

		public override string MakeContent() =>
$@"Id Bidang:<b>{data.IdBidang}</b><br/>
No Peta:<b>{data.noPeta}</b><br/>
Project: {data.project}<br/>
Desa   : {data.desa}<br/>
" +
			state switch
			{
				maps.mod.LandState.Kampung__Bengkok_Desa =>
				$@"Luas&nbsp;:&nbsp;{data.luas:#,##0}M2<br/>
",
				maps.mod.LandState.Belum_Bebas =>
				$@"Luas&nbsp;:&nbsp;{data.luas:#,##0}M2<br/>
Pemilik&nbsp;:&nbsp;{data.pemilik ?? "-"}<br/>
Group&nbsp;:&nbsp;{data.group ?? "-"}<br/>
",
				_ =>
				$@"Luas&nbsp;:&nbsp;{data.luas:#,##0}M2<br/>
Pemilik&nbsp;:&nbsp;{data.pemilik ?? "-"}<br/>
Group&nbsp;:&nbsp;{data.group ?? "-"}<br/>
PT SK&nbsp;:&nbsp;{data.PTSK ?? "-"}<br/>
"
			} +
$"Status&nbsp;:&nbsp;{state.Describe()}";

		//public override Polygon ToPolygon(bool[] options)
		//{
		//	//bool byjenis = (flag & DrawFlag.PersilJenis) == DrawFlag.PersilJenis;
		//	//bool bystatus = (flag & DrawFlag.PersilStatus) == DrawFlag.PersilStatus;
		//	//if (!byjenis && !bystatus)
		//	//	return null;

		//	var poly = new Polygon
		//	{
		//		DrawStroke = attribs.strokeWidth != null && attribs.strokeColor != null,
		//		Fill = attribs.fillColor != null,
		//		FillOpacity = (attribs.fillColor?.A ?? 0) / 255d,
		//		FillColor = Color.FromArgb(255,attribs.fillColor ?? Color.Black),
		//		StrokeColor = Color.FromArgb(255, attribs.strokeColor ?? Color.Black),
		//		StrokeOpacity = (attribs.strokeColor?.A ?? 0) / 255d,
		//		StrokeWidth = attribs.strokeWidth ?? 0,
		//		Shape = shapes.Select(xx => xx.Select(x => (PointF)x).ToArray()).ToArray(),
		//	};
		//	if (options.Any() && options[0])
		//	{
		//		poly.Popup = new Popup
		//		{
		//			MinimumWidth = 100,
		//			MaximumWidth = 300,
		//			MaximumHeight = 500,
		//			CloseOnEscapeKey = true,
		//			ShowCloseButton = true,
		//			Content = MakeContent()
		//		};
		//	}
		//	else {
		//		poly.Tooltip = new Tooltip
		//		{
		//			Direction = "auto",
		//			IsSticky = false,
		//			Opacity = 0.75,
		//			Content = MakeContent()
		//		};
		//	}
		//	//poly.OnTooltipOpen += (s,a)=>hoverHandler((InteractiveLayer)s,null);
		//	//poly.OnMouseOver += hoverHandler;
		//	//poly.OnMouseOut += outHandler;
		//	//poly.OnClick += clickHandler;
		//	//poly.OnDblClick += clickHandler;
		//	//poly.IsBubblingMouseEvents = false;

		//	this.id = poly.Id;

		//	return poly;
		//}
	}

	[Serializable]
	public class LandFeatureLight : FeatureBase
	{
		static maps.mod.LandState notBebas(maps.mod.LandState x) => (maps.mod.LandState)((int)x & ~(int)maps.mod.LandState.Sudah_Bebas_);
		public MetaDataBase data { get; set; }
		[CsvLabel(true)]
		public bool keluar { get; set; }
		[CsvLabel(true)]
		public bool claim { get; set; }
		[CsvLabel(true)]
		public bool damai { get; set; }
		[CsvLabel(true)]
		public bool damaiB { get; set; }
		[CsvLabel(true)]
		public bool kulit { get; set; }
		[CsvLabel(true)]
		public maps.mod.LandState state { get; set; }
		public maps.mod.LandState State() =>
			claim ? LandState._Damai :
			damaiB ? LandState.Overlap_Or_Damai :
			damai ? LandState._Damai | LandState.Sudah_Bebas_ :
			(state | LandState.Overlap) == LandState.Overlap ? LandState.Overlap :
			state | LandState.Sudah_Bebas_;
		public string status() =>
			claim ? "Claim" :
			damaiB ? "Damai *" :
			damai ? "Damai" :
			kulit ? "Kulit Overlap" :
			(state | LandState.Sudah_Bebas_) == LandState.Sudah_Bebas_ ? "Sudah Bebas" : "Belum Bebas";
	}

	/* moved to landrope.api2.helpers */
	//public record LandDataBase(string key);

	//public record LandData(string key, string prokey, string deskey, MetaData data, XPointF[][] shapes) : LandDataBase(key);

	//public record LandDataState(string key, LandState state,
	//	bool kulit, bool claim, bool damai, bool damaiB) : LandDataBase(key)
	//{
	//	public maps.mod.LandState State() =>
	//		claim ? LandState._Damai :
	//		damaiB ? LandState.Overlap_Damai :
	//		damai ? LandState._Damai | LandState.Sudah_Bebas_ :
	//		(state | LandState.Overlap) == LandState.Overlap ? LandState.Overlap :
	//		state | LandState.Sudah_Bebas_;
	//	public string status() =>
	//		claim ? "Claim" :
	//		damaiB ? "Damai *" :
	//		damai ? "Damai" :
	//		kulit ? "Kulit Overlap" :
	//		(state | LandState.Sudah_Bebas_) == LandState.Sudah_Bebas_ ? "Sudah Bebas" : "Belum Bebas";
	//}

	//public record LandDataProcess(string key, string product, string luasprod, string next, bool prep, bool going) : LandDataBase(key);
}
