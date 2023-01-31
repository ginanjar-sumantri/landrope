using auth.mod;
using CadLoader;
using geo.shared;
using GeomHelper;
using landrope.mod;
using landrope.mod2;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Tracer;

namespace CadLoader
{
	public class processor
	{

		public processor(LandropeContext context, ExtLandropeContext contextex=null)
		{
			this.context = context;
			this.contextex = contextex;
		}

		LandropeContext context;
		ExtLandropeContext contextex;
		public string Process(string fname, string vk)
		{
			MyTracer.PushProc("Process", "Process");
			try
			{
				DxfFile dxf = new DxfFile(fname);
				var shapes = dxf.shapes;
				if (!shapes.Any())
				{
					var x = MyTracer.GetProcs("Process");
					return $"\"{Path.GetFileName(fname)}\"doesn't contain any Land Information - {x}";
				}

				MyTracer.PushProc("BackupAndClear");
				BackupAndClear(vk);
				var lands = new List<(Point CenterXY, LMapper map)>();
				shapes.ToList().ForEach(pl =>
				{
					MyTracer.ReplaceProc("Loop", "Procloop");
					if (DxfShape.BDlayernames.Contains(pl.layer))
					{
						MyTracer.PushProc("Batas SK 1");
						var vm = new VMapper(context, vk);
						ShapesXY shapesxy = new ShapesXY();
						MyTracer.ReplaceProc("Batas SK 2");
						shapesxy.Fill(pl.coords.Select(c => new ShapeXY { coordinates = c }));
						if (shapesxy.Count != 0)
						{
							MyTracer.ReplaceProc("Batas SK 3");
							var mom = shapesxy.GetAreaNCentroid();
							MyTracer.ReplaceProc("Batas SK 4");
							var polies = pl.coords.Select(pp => pp.Select(c => new UtmPoint { x = c.x, y = c.y }).ToArray()).ToArray();
							MyTracer.ReplaceProc("Batas SK 5");
							vm.PutUTMs(polies, new UtmPoint { x = mom.center.x, y = mom.center.y }, 48, true, mom.area);
						}
					}
					else
					{
						MyTracer.PushProc("Bidang 1");
						var p = pl as LabelledHatch;
						string kode, pemilik, penjual, surat, lsurat, lnib, lukur, nib;
						p.labels.TryGetValue("kode", out kode);
						p.labels.TryGetValue("pemilik", out pemilik);
						p.labels.TryGetValue("penjual", out penjual);
						p.labels.TryGetValue("surat", out surat);
						p.labels.TryGetValue("lsurat", out lsurat);
						p.labels.TryGetValue("lnib", out lnib);
						p.labels.TryGetValue("lukur", out lukur);
						p.labels.TryGetValue("nib", out nib);
						double dlsurat, dlukur, dlnib;
						double? nlsurat = null, nlukur = null, nlnib = null;
						if (double.TryParse(lsurat, out dlsurat))
							nlsurat = dlsurat;
						if (double.TryParse(lsurat, out dlukur))
							nlukur = dlukur;
						if (double.TryParse(lnib, out dlnib))
							nlnib = dlnib;
						MyTracer.ReplaceProc("Bidang 2");
						ShapesXY shapesxy = new ShapesXY();
						MyTracer.ReplaceProc("Bidang 3");
						var shps = pl.coords.Select(c => new ShapeXY { coordinates = c }).ToList();
						MyTracer.ReplaceProc("Bidang 3a");
						shapesxy.Fill(shps);
						MyTracer.ReplaceProc("Bidang 3b");
						if (shapesxy.Count != 0)
						{
							MyTracer.ReplaceProc("Bidang 4");
							var mom = shapesxy.GetAreaNCentroid();
							MyTracer.ReplaceProc("Bidang 5");
							var coords = p.coords.Select(c => c.Select(x => new UtmPoint { x = x.x, y = x.y }));
							MyTracer.ReplaceProc("Bidang 6");
							var lm = new LMapper(context, vk, coords, new UtmPoint { x = mom.center.x, y = mom.center.y }, 48,
																					true, kode, p.status, pemilik, nlukur, nlsurat, mom.area);
							MyTracer.ReplaceProc("Bidang 7");
							lands.Add((mom.center, lm));
							MyTracer.ReplaceProc("Bidang 8");
							//lm.Store();
						}
					}
					MyTracer.ClearTo("Procloop");
				});
				MyTracer.ReplaceProc("Reducing duplicates");
				LMapper.ReduceDuplicates(lands);
				MyTracer.ReplaceProc("Saving lands");
				lands.ForEach(l => l.map.Store());
				MyTracer.ClearTo("Process");
				return null;
			}
			catch (FileLoadException)
			{
				var proc = MyTracer.GetProcs("Process");
				return $"Error loading file \"{fname}\" - {proc}";
			}
			catch (Exception ex)
			{
				var proc = MyTracer.GetProcs("Process");
				return $"\"{Path.GetFileName(fname)}\" processing error: {ex.Message} - {proc}";
			}
			finally
			{
				MyTracer.ClearTo("Process");
			}
		}

		public string BatchProcess(string listfname)
		{
			try
			{
				var strm = new StreamReader(listfname, true);
				var data = new[] { new { desakey = "", file = "" } }.ToArray();
				var st = strm.ReadToEnd().Trim();
				var ldata = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(st, data).ToList();

				var stats = new StringBuilder();
				ldata.ForEach(d =>
				{
					Console.Write($"Processing map file \"{d.file}\"...");
					var str = Process(d.file, d.desakey);
					Console.WriteLine($"Done{(str != null ? " with error!" : "")}");
					stats.AppendLine(str);
				});
				return stats.ToString();
			}

			catch (FileNotFoundException)
			{
				return $"Unable to find file \"{listfname}\"";
			}
			catch (FileLoadException)
			{
				return $"Error loading file \"{listfname}\"";
			}
			catch (Exception ex)
			{
				return $"File \"{listfname}\" error: {ex.Message}";
			}
		}

		public void BackupAndClear(string vkey)
		{
			MyTracer.PushProc(MethodBase.GetCurrentMethod(), "BNC");
			try
			{
				MyTracer.PushProc("100");
				var obj = context.GetCollections(new { project = new { key = "", identity = "" }, village = new Village() },
																		"villages", $"{{'village.key':'{vkey}'}}").FirstOrDefault();
				if (obj == null)
					return;
				MyTracer.ReplaceProc("200");
				var list = context.GetCollections(new Land(),
																		"lands", $"{{vilkey:'{vkey}'}}", "{_id:0}").ToList();
				if (!list.Any()) // no needs for backup
					return;

				MyTracer.ReplaceProc("300");
				var data = new { stamp = DateTime.Now, data = new { owner = obj, data = list } };
				context.db.GetCollection<BsonDocument>("backup").InsertOne(data.ToBsonDocument());

				MyTracer.ReplaceProc("400");
				context.db.GetCollection<Land>("lands").DeleteMany($"{{vilkey:'{vkey}'}}");
			}
			finally
			{
				MyTracer.ClearTo("BNC");
			}
		}

		public string ProcessSingle(string fname, string persilkey, user user, int size)
		{
			MyTracer.PushProc("ProcessSingle-file", "ProcessSingle");
			try
			{
				DxfFile dxf = new DxfFile(fname);
				var filename = Path.GetFileName(fname);
				return DoProcessSingle(dxf, filename, persilkey, user,size);
			}
			catch (FileLoadException)
			{
				var proc = MyTracer.GetProcs("Process");
				//MyTracer
				return $"Error loading file \"{fname}\" - {proc}";
			}
			catch (Exception ex)
			{
				var proc = MyTracer.GetProcs("Process");
				return $"\"{Path.GetFileName(fname)}\" processing error: {ex.Message} - {proc}";
			}
			finally
			{
				MyTracer.ClearTo("ProcessSingle");
			}
		}

		public string ProcessSingle(Stream strm, string filename, string persilkey, user user, int size)
		{
			MyTracer.PushProc("ProcessSingle-stream", "ProcessSingle");
			try
			{
				DxfFile dxf = new DxfFile(strm);
				return DoProcessSingle(dxf, filename, persilkey, user, size);
			}
			catch (Exception ex)
			{
				var proc = MyTracer.GetProcs("Process");
				return $"Stream processing error: {ex.Message} - {proc}";
			}
			finally
			{
				MyTracer.ClearTo("ProcessSingle");
			}
		}

		private string DoProcessSingle(DxfFile dxf, string sourcename, string persilkey, user user, int size)
		{
			var shape = dxf.singleplane;
			if (shape == null)
			{
				var x = MyTracer.GetProcs("ProcessSingle");
				//return $"\"{Path.GetFileName(fname)}\"doesn't contain any map - {x}";
			}

			var plm = new PlMapper(context, contextex, persilkey);
			ShapesXY shapesxy = new ShapesXY();
			shapesxy.Fill(shape.coords.Select(c => new ShapeXY { coordinates = c }));
			if (shapesxy.Count != 0)
			{
				var mom = shapesxy.GetAreaNCentroid();
				var polies = shape.coords.Select(pp => pp.Select(c => new UtmPoint { x = c.x, y = c.y }).ToArray()).ToArray();
				plm.PutUTMs(polies, new UtmPoint { x = mom.center.x, y = mom.center.y }, 48, true, mom.area);
				plm.Store(sourcename,user,dxf,size);
			}
			return null;
		}


		public string ProcessSingle(string fname, Persil persil, user user, int size)
		{
			MyTracer.PushProc("ProcessSingle-file", "ProcessSingle");
			try
			{
				DxfFile dxf = new DxfFile(fname);
				var filename = Path.GetFileName(fname);
				return DoProcessSingle(dxf, filename, persil, user, size);
			}
			catch (FileLoadException)
			{
				var proc = MyTracer.GetProcs("Process");
				return $"Error loading file \"{fname}\" - {proc}";
			}
			catch (Exception ex)
			{
				var proc = MyTracer.GetProcs("Process");
				return $"\"{Path.GetFileName(fname)}\" processing error: {ex.Message} - {proc}";
			}
			finally
			{
				MyTracer.ClearTo("ProcessSingle");
			}
		}

		public string ProcessSingle(Stream strm, string filename, Persil persil, user user, int size)
		{
			MyTracer.PushProc("ProcessSingle-stream", "ProcessSingle");
			try
			{
				DxfFile dxf = new DxfFile(strm);
				return DoProcessSingle(dxf, filename, persil, user, size);
			}
			catch (Exception ex)
			{
				var proc = MyTracer.GetProcs("Process");
				return $"Stream processing error: {ex.Message} - {proc}";
			}
			finally
			{
				MyTracer.ClearTo("ProcessSingle");
			}
		}

		private string DoProcessSingle(DxfFile dxf, string sourcename, Persil persil, user user, int size)
		{
			var shape = dxf.singleplane;
			if (shape == null)
			{
				var x = MyTracer.GetProcs("ProcessSingle");
				//return $"\"{Path.GetFileName(fname)}\"doesn't contain any map - {x}";
			}

			var plm = new PlMapper(context, contextex, persil);
			ShapesXY shapesxy = new ShapesXY();
			shapesxy.Fill(shape.coords.Select(c => new ShapeXY { coordinates = c }));
			if (shapesxy.Count != 0)
			{
				var mom = shapesxy.GetAreaNCentroid();
				var polies = shape.coords.Select(pp => pp.Select(c => new UtmPoint { x = c.x, y = c.y }).ToArray()).ToArray();
				plm.PutUTMs(polies, new UtmPoint { x = mom.center.x, y = mom.center.y }, 48, true, mom.area);
				plm.Store(sourcename, user, dxf, size);
			}
			return null;
		}


		public string ProcessSingle(string fname, PTSK ptsk)
		{
			MyTracer.PushProc("ProcessSingle-file", "ProcessSingle");
			try
			{
				DxfFile dxf = new DxfFile(fname);
				var filename = Path.GetFileName(fname);
				return DoProcessSingle(dxf, ptsk);
			}
			catch (FileLoadException)
			{
				var proc = MyTracer.GetProcs("Process");
				return $"Error loading file \"{fname}\" - {proc}";
			}
			catch (Exception ex)
			{
				var proc = MyTracer.GetProcs("Process");
				return $"\"{Path.GetFileName(fname)}\" processing error: {ex.Message} - {proc}";
			}
			finally
			{
				MyTracer.ClearTo("ProcessSingle");
			}
		}

		private string DoProcessSingle(DxfFile dxf, PTSK ptsk)
		{
			var shape = dxf.singleplane;
			if (shape == null)
			{
				var x = MyTracer.GetProcs("ProcessSingle");
				//return $"\"{Path.GetFileName(fname)}\"doesn't contain any map - {x}";
			}

			var ptsklm = new PTSKIMapper(context, contextex, ptsk);
			ShapesXY shapesxy = new ShapesXY();
			shapesxy.Fill(shape.coords.Select(c => new ShapeXY { coordinates = c }));
			if (shapesxy.Count != 0)
			{
				var mom = shapesxy.GetAreaNCentroid();
				var polies = shape.coords.Select(pp => pp.Select(c => new UtmPoint { x = c.x, y = c.y }).ToArray()).ToArray();
				ptsklm.PutUTMs(polies, 48, true);
				ptsklm.Store(ptsk);
			}
			return null;
		}
	}
}
