using GeomHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using netDxf;
using netDxf.Entities;
using netDxf.Header;
using netDxf.IO;
using System.Linq;
//using Google.Type;
using System.Text.RegularExpressions;
using System.Reflection.Emit;
using System.Linq.Expressions;
using System.Drawing;
using landrope.mod;
using System.Net.NetworkInformation;
using landrope.common;
using System.Security.AccessControl;
//using TriangleNet.Geometry;

namespace CadLoader
{
	using point = geo.shared.Point;

	public class DxfShape
	{
		public string layer;
		public List<List<point>> coords;
		public virtual List<List<point>> koordinat
		{
			get => coords;
			set
			{
				coords = value;
			}
		}

		public static string[] BDlayernames = new string[] { "BATAS SK", "BATAS DESA" };

	}

	public class LabelledHatch : DxfShape
	{
		public List<List<Segment>> edges;

		//public bool IsInside(point pt)
		//{
		//	var koord = koordinat;
		//	if (!koord.Any())
		//		return false;
		//	var Xminmax = (min: koord.Min(p => p.x), max: koord.Max(p => p.x));
		//	var Yminmax = (min: koord.Min(p => p.y), max: koord.Max(p => p.y));

		//	if (pt.x < Xminmax.min || pt.x > Xminmax.max || pt.y < Yminmax.min || pt.y > Yminmax.max)
		//		return false;

		//	var pt0 = new point(Xminmax.min - 10, pt.y);
		//	var seg0 = new Segment(pt0, pt);
		//	var cnt = edges.Where(e => e.IsIntersect(seg0)).Count();
		//	return cnt % 2 == 1;
		//}

		public T FindInsider<T>(IEnumerable<(T obj, point pt)> targets)
		{
			var segments = this.edges;
			if (!segments.Any())
				return (T)(object)null;
			var cands = segments.SelectMany(edges =>FindInsiderIn(targets, edges));
			return cands.FirstOrDefault();
		}

		private IEnumerable<T> FindInsiderIn<T>(IEnumerable<(T obj, point pt)> targets, List<Segment> edges)
		{
			if (!edges.Any())
				return new T[0];

			var Xminmax = (min: edges.Min(e => e.Pt1.x), max: edges.Max(e => e.Pt1.x));
			var Yminmax = (min: edges.Min(e => e.Pt1.y), max: edges.Max(e => e.Pt1.y));

			var focused = targets.Where(x =>
				(x.pt.x >= Xminmax.min && x.pt.x <= Xminmax.max && x.pt.y >= Yminmax.min && x.pt.y <= Yminmax.max));
			var segmenteds = focused.Select(x => (x.obj, x.pt, seg: new Segment(new point(Xminmax.min - 10, x.pt.y), x.pt)));
			var couples = segmenteds.Join(edges, x => 1, y => 1, (x, y) => (x.obj, edge: y, x.seg));
			var ispar = couples.Count() > 100;
			var candidates = (ispar ? couples.AsParallel().Where(c => c.edge.IsIntersect(c.seg)) :
													couples.Where(c => c.edge.IsIntersect(c.seg)))
								.GroupBy(c => c.obj).Select(g => (obj: g.Key, cnt: g.Count()))
								.Where(x => x.cnt % 2 == 1)
								.Select(x => x.obj);
			return candidates;
		}

		public Dictionary<string, string> labels;
		public (bool, bool) labelled = (false, false);
		public LandStatus status = LandStatus.Tanpa_status;
	}

	public class DxfPolyline : DxfShape
	{
		public void Union(DxfPolyline other)
		{
			coords.AddRange(other.coords);
		}
	}

	public interface ICadFile
	{
		IEnumerable<DxfShape> shapes { get; }
	}

	public class DxfFile : ICadFile
	{
		DxfDocument doc;

		static Regex rg1 = new Regex(@"\\pi[0-9\.]+;");
		static Regex rg2 = new Regex(@"\\f[A-Za-z0-9\s\-]+(\|[bic][01])+\|p[0-9]+;\xb2");
		static Regex rg3 = new Regex(@"\\C[0-9]+;");
		static Regex rg4 = new Regex(@"[\{\}]");
		static Regex rgls = new Regex(@"LS\s{0,}:\s{0,}([0-9]+)?\s?M?", RegexOptions.IgnoreCase);
		static Regex rglu = new Regex(@"LU\s{0,}:\s{0,}([0-9]+)?\s?M?", RegexOptions.IgnoreCase);
		static Regex rgnib = new Regex(@"nib\s{0,}([0-9]+)?", RegexOptions.IgnoreCase);

		public DxfShape singleplane
		{
			get
			{
				if (doc == null)
					return null;

				var polylines = doc.LwPolylines.Select(h => new
				{
					color = h.Color.IsByBlock ? h.Layer.Color.ToColor() : h.Color.ToColor(),
					layer = h.Layer?.Name ?? "",
					h.Vertexes
				});
				if (!polylines.Any())
					return null;

				var shape = new DxfShape { layer = polylines.First().layer };
				shape.coords = polylines.Select(p =>
				 p.Vertexes.Select(v => new point(v.Position.X, v.Position.Y)).ToList()).ToList();
				return shape;
			}
		}

		public IEnumerable<DxfShape> shapes
		{
			get
			{
				if (doc == null)
					return new List<DxfShape>();

				var hatches = doc.Hatches.Select(h => new
				{
					color = h.Color.IsByBlock ? h.Layer.Color.ToColor() : h.Color.ToColor(),
					layer = h.Layer?.Name ?? "",
					path = h.BoundaryPaths.Select(p=>p.Edges.OfType<HatchBoundaryPath.Line>().ToList())
				});
				var polylines = doc.LwPolylines.Select(h => new
				{
					color = h.Color.IsByBlock ? h.Layer.Color.ToColor() : h.Color.ToColor(),
					layer = h.Layer?.Name ?? "",
					h.Vertexes
				});

				var polsh = hatches.Select(h => new LabelledHatch
				{
					layer = h.layer,
					edges = h.path.Select(p=>p.Select(v => new Segment(v.Start.X, v.Start.Y, v.End.X, v.End.Y)).ToList()).ToList(),
					koordinat = h.path.Select(p=>p.Select(v => new point { x = v.Start.X, y = v.Start.Y }).ToList()).ToList(),
					labels = new Dictionary<string, string> { { "layer", h.layer }, { "color", $"{h.color.ToArgb():X8}" } },
					status = LandStatus.Tanpa_status
				}).Where(p => p.koordinat.Any()).ToList();
				polsh.ForEach(p =>
				{
					switch (p.layer)
					{
						case "HATCH BEBAS": p.status = LandStatus.Sudah_Bebas__murni; break;
						case "belum bebas shm": p.status = LandStatus.Belum_Bebas__Sertifikat; break;
						case "belum bebas hibah": p.status = LandStatus.Hibah__PBT_sudah_terbit; break;
						case "belum bebas hibah blm pbt": p.status = LandStatus.Hibah__PBT_belum_terbit; break;
						case "kampung": p.status = LandStatus.Kampung; break;
						case "bengkok desa": p.status = LandStatus.Kampung;break;// LandStatus.Bengkok_Desa; break;
					}
				});
				polsh = polsh.Where(p => p.status != LandStatus.Tanpa_status).ToList();
				var polshx = polylines.Where(p => p.layer == "HATCH BEBAS").Select(h => new LabelledHatch
				{
					layer = h.layer,
					//edges = h.path.Select(v => new Segment(v.Start.X, v.Start.Y, v.End.X, v.End.Y)).ToList(),
					koordinat = new[] { h.Vertexes.Select(v => new point { x = v.Position.X, y = v.Position.Y }).ToList() }.ToList(),
					labels = new Dictionary<string, string> { { "layer", h.layer }, { "color", $"{h.color.ToArgb():X8}" } },
					status = LandStatus.Sudah_Bebas__murni
				}).Where(p => p.koordinat.Any()).ToList();
				polshx.ForEach(pp =>
				{
					pp.edges = new List<List<Segment>>();
					pp.coords.ForEach(p =>
					{
						var koord = p.Skip(1).ToList();
						koord.Add(p[0]);
						var edge = p.Select((x, i) => (x, i)).Join(koord.Select((x, i) => (x, i)), x => x.x, y => y.x,
																			(x, y) => new Segment(x.x, y.x)).ToList();
						pp.edges.Add(edge);
					});
				});
				polsh.AddRange(polshx);

				var pols2 = polylines.Where(p => DxfShape.BDlayernames.Contains(p.layer)).Select(h => new DxfPolyline
				{
					layer = h.layer,
					coords = new[] { h.Vertexes.Select(v => new point { x = v.Position.X, y = v.Position.Y }).ToList() }.ToList(),
				}).Where(p => p.coords.SelectMany(c=>c.Select(cc=>cc)).Any()).ToList();
				if (pols2.Count() > 1)
				{
					var poly = pols2.First();
					pols2.Skip(1).ToList().ForEach(pp => poly.Union(pp));
					pols2 = pols2.Take(1).ToList();
				}

				var rgx1 = new Regex(@"(TEXT SURAT|NAMA PEMILIK)");

				var texts1 = doc.MTexts.Where(t => rgx1.IsMatch(t.Layer?.Name??""))
					.Select(t => new
					{
						pos = new point(t.Position.X, t.Position.Y),
						value = t.Value,
						match = parseText(t.Value)
					});
				var texts = texts1.Select(t => new
				{
					t.pos,
					owner = t.match["owner"],
					seller = t.match.TryGetValue("seller", out string cseller) ? cseller : null,
					surat = t.match["surat"],
					lsurat = t.match["lsurat"],
					lukur = t.match["lukur"],
					lnib = t.match.TryGetValue("lnib", out string clnib) ? clnib : null,
					nib = t.match["nib"]
				}).ToList();

				var rgx2 = new Regex(@"NO(MOR)?\s+PETA");
				var rgx2a = new Regex(@"\\pi[0-9\.t\,]+;");
				var texts2 = doc.MTexts.Where(t => rgx2.IsMatch(t.Layer?.Name??""))
					.Select(t => new
					{
						pos = new point(t.Position.X, t.Position.Y),
						code = rgx2a.Replace(t.Value.Replace(@"\P",""),"").Replace(@"\P", "")
					}).ToList();

				polsh.ForEach(p =>
				{
					var txt = texts.Any() ? p.FindInsider(texts.Select(t => (t, pt: t.pos))) : null;
					if (txt != null)
					{
						p.labels.Add("pemilik", txt.owner);
						p.labels.Add("penjual", txt.seller);
						p.labels.Add("surat", txt.surat);
						if (double.TryParse(txt.lsurat, out double ls))
							p.labels.Add("lsurat", txt.lsurat);
						if (double.TryParse(txt.lukur, out double lu))
							p.labels.Add("lukur", txt.lukur);
						if (double.TryParse(txt.lnib, out double ln))
							p.labels.Add("lnib", txt.lnib);
						p.labels.Add("nib", txt.nib);
						texts.Remove(txt);
					}
					var txt2 = texts2.Any() ? p.FindInsider(texts2.Select(t => (t, pt: t.pos))) : null;
					if (txt2 != null)
					{
						p.labels.Add("kode", txt2.code);
						texts2.Remove(txt2);
					}
				});
				//texts.ForEach(t =>
				//{
				//	var poly = polsh.FirstOrDefault(p => !p.labelled.Item1 && p.IsInside(t.pos));
				//	if (poly != null)
				//	{
				//		poly.labelled = (true, poly.labelled.Item2);
				//		poly.labels.Add("pemilik", t.owner);
				//		poly.labels.Add("penjual", t.seller);
				//		poly.labels.Add("surat", t.surat);
				//		if (double.TryParse(t.lsurat, out double ls))
				//			poly.labels.Add("lsurat", t.lsurat);
				//		if (double.TryParse(t.lukur, out double lu))
				//			poly.labels.Add("lukur", t.lukur);
				//		if (double.TryParse(t.lnib, out double ln))
				//			poly.labels.Add("lnib", t.lnib);
				//		poly.labels.Add("nib", t.nib);
				//	}
				//});
				//texts2.ForEach(t =>
				//{
				//	var poly = polsh.FirstOrDefault(p => !p.labelled.Item2 && p.IsInside(t.pos));
				//	if (poly != null)
				//	{
				//		poly.labelled = (poly.labelled.Item1, true);
				//		poly.labels.Add("kode", t.code);
				//	}
				//});
				return polsh.Cast<DxfShape>().Union(pols2.Cast<DxfShape>()).ToList();

				//nested function
				Dictionary<string, string> parseText(string value)
				{
					value = rg4.Replace(rg3.Replace(rg2.Replace(rg1.Replace(value, ""), ""), ""), "");
					string[] pvalues = value.Split(new[] { @"\P" }, StringSplitOptions.RemoveEmptyEntries);

					var result = new Dictionary<string, string>
					{
						{"owner","" },
						{"broker","" },
						{"surat","" },
						{"lsurat","" },
						{"lukur","" },
						{"lnib","" },
						{"nib","" }
					};
					if (pvalues.Length == 0)
						return result;

					result["owner"] = pvalues[0];
					if (pvalues.Length > 1)
						result["surat"] = pvalues[1];
					if (pvalues.Length > 2)
					{
						var Ms = rgls.Match(pvalues[2]);
						if (Ms.Success && Ms.Groups.Count > 1)
							result["lsurat"] = Ms.Groups[1].Value;
					}
					if (pvalues.Length > 3)
					{
						var Mu = rglu.Match(pvalues[3]);
						if (Mu.Success && Mu.Groups.Count > 1)
							result["lukur"] = Mu.Groups[1].Value;
					}
					if (pvalues.Length > 4)
					{
						var Mn = rgnib.Match(pvalues[4]);
						if (Mn.Success && Mn.Groups.Count > 1)
							result["nib"] = Mn.Groups[1].Value;
					}

					return result;
				}
			}
		}

		public void CheckVersion(DxfVersion ver)
		{
			if (ver < DxfVersion.AutoCad2000)
				throw new DxfVersionNotSupportedException("This version of DXF file is unsupported. Please convert to ACad 2000 or higher", ver);
		}

		public DxfFile(string filename)
		{
			CheckVersion(DxfDocument.CheckDxfFileVersion(filename));
			doc = DxfDocument.Load(filename);
		}

		public DxfFile(Stream filestrm)
		{
			CheckVersion(DxfDocument.CheckDxfFileVersion(filestrm));
			doc = DxfDocument.Load(filestrm);
		}

		public static bool Match(string filename) => DxfDocument.CheckDxfFileVersion(filename) >= DxfVersion.AutoCad2000;

		public static bool Match(Stream filestrm) => DxfDocument.CheckDxfFileVersion(filestrm) >= DxfVersion.AutoCad2000;

		public DateTime Created => doc.DrawingVariables.TdCreate;
		public DateTime LastUpdate => doc.DrawingVariables.TdUpdate;
		public string Updater => doc.DrawingVariables.LastSavedBy;
	}
}
