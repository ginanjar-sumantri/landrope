using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using auth.mod;
using binaland;
using DocumentFormat.OpenXml.Office.Word;
using encdec;
using geo.shared;
using GeomHelper;
using GraphConsumer;
//using landrope.api2.Models;
using landrope.common;
using landrope.mod;
using landrope.mod.shared;
using landrope.mod2;
using maps.mod;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using protobsonser;
using Tracer;

namespace landrope.web.Controllers
{
	//[Route("/api/reporting")]
	[ApiController]
	public class MapController : ControllerBase
	{
		ExtLandropeContext context = Contextual.GetContextExt();

#if DEBUG
		[EnableCors(nameof(landrope))]
		[HttpPost("/api/reporting/debug-map")]
		//[NeedToken("VIEW_MAP", true)]
		public IActionResult gmap([FromQuery] string token, [FromQuery] string key)
		{
			return Getmap((token, key));
		}
#endif

		[EnableCors(nameof(landrope))]
		[HttpPost("/api/reporting/map")]
		//[NeedToken("VIEW_MAP", true)]
		public IActionResult map([FromQuery] string p)
		{
			MyTracer.TraceInfo2($"p={p}");
			Program.Log($"p={p}");
			Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

			try
			{
				var json = Transporter.Decrypt(p);
				MyTracer.TraceInfo2($"json={json}");
				Program.Log($"json={json}");
				var param = JsonConvert.DeserializeAnonymousType(json, new { token = "", key = "" });
				return Getmap((param.token, param.key));
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}

		IActionResult Getmap((string token, string key) param)
		{
			try
			{
				var keys = param.key?.Split(',', ';', '|') ?? new string[0]; //project keys
				List<PersilSteps> persilsteps = new List<PersilSteps>();
				List<PersilSteps> persilsteps1 = new List<PersilSteps>();
				List<PersilSteps> persilsteps2 = new List<PersilSteps>();
				List<PersilSteps> persilsteps3 = new List<PersilSteps>();
				List<PersilSteps> persilsteps4 = new List<PersilSteps>();
				List<MapData> mapinfos = new List<MapData>();
				List<DBDesaMap> desamaps = new List<DBDesaMap>();
				List<DBPersilMap> persilmaps = new List<DBPersilMap>();
				//List<(MapData map, LandStatus2 status)> data2 = new List<(MapData map, LandStatus2 status)>();

				//persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList();
				var task0 = Task.Run(() => { persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList(); });
				var task1 = Task.Run(() => { persilsteps1 = context.GetCollections(new PersilSteps(), "material_status_hgb", "{}", "{_id:0}").ToList(); });
				var task2 = Task.Run(() => { persilsteps2 = context.GetCollections(new PersilSteps(), "material_status_shm", "{}", "{_id:0}").ToList(); });
				var task3 = Task.Run(() => { persilsteps3 = context.GetCollections(new PersilSteps(), "material_status_shp", "{}", "{_id:0}").ToList(); });
				var task4 = Task.Run(() => { persilsteps4 = context.GetCollections(new PersilSteps(), "material_status_hibah", "{}", "{_id:0}").ToList(); });

				var findst = $"{{keyProject:{{$in:['{string.Join("','", keys)}']}}}}";
				var task5 = Task.Run(() => { mapinfos = context.GetCollections(new MapData(), "material_mapinfo", findst, "{_id:0}").ToList(); });
				var task6 = Task.Run(() => { desamaps = context.GetCollections(new DBDesaMap(), "material_desa_map", findst, "{_id:0,keyProject:0}").ToList(); });
				var task7 = Task.Run(() => { persilmaps = context.GetCollections(new DBPersilMap(), "material_persil_map", findst, "{_id:0,keyProject:0}").ToList(); });

				Task.WaitAll(task0, task1, task2, task3, task4, task5, task6, task7);

#if _DEBUG
				var fs = new FileStream("coords.csv",FileMode.Create);
				var sw = new StreamWriter(fs);
				sw.WriteLine("key, latitude,Longitude");
				var coords = persilmaps.Select(pm => (pm.key, shp: MapCompression.decode(pm.map.careas)))
					.SelectMany(c => c.shp.SelectMany(x => x.AsArray.Select(y => (c.key, co:y)))).ToArray();
				var stcoords = coords.Select(x => $"{x.key},{x.co[0]},{x.co[1]}").ToList();
				stcoords.ForEach(c => sw.WriteLine(c));
				sw.Flush();
				fs.Flush();
				sw.Close();
				fs.Close();
#endif
				//var task6 = Task.Run(() =>
				//{
				//	data2 = context.GetCollections(new persilBelumBebas(), "belum_bebas", findst).ToList()
				//	.Select(pbb => (map: new MapData
				//	{
				//		desa = pbb.desa,
				//		key = pbb.key,
				//		group = pbb.group,
				//		keyDesa = pbb.keyDesa,
				//		keyProject = pbb.keyProject,
				//		luasUkur = pbb.luasUkur,
				//		desaMap = pbb.dsmap,
				//		map = new binaland.Map()
				//		{
				//			careas = MapCompression.encode(landrope.mod.landbase.decompress(pbb.careas)),
				//			Area = pbb.luasUkur ?? 0,
				//			Center = pbb.Center
				//		},
				//		pemilik = pbb.pemilik,
				//		noPeta = pbb.noPeta,
				//		_t = "persilgirik"
				//	},
				//	status: pbb.ls_status switch
				//	{
				//		LandStatus.Belum_Bebas__Sertifikat => LandStatus2.Belum_Bebas,
				//		_ => LandStatus2.Kampung__Bengkok_Desa
				//	})).ToList();
				//});


				persilsteps.AddRange(persilsteps1);
				persilsteps.AddRange(persilsteps2);
				persilsteps.AddRange(persilsteps3);
				persilsteps.AddRange(persilsteps4);

				var map_desa = mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
							(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
											.AsParallel();

				var allstates = persilsteps.Select(p => p.Convert()).AsParallel();
				var data1 = map_desa.Join(allstates, x => x.m.key, s => s.key, (x, s) => (mapin: x.m, s.status, desamap: x.desamap.ToBMap()));
				var data2 = data1.Join(persilmaps.AsParallel(), l => l.mapin.key, pm => pm.key, (l, pm) => (l, pm)).ToList().AsParallel();
				var promap = data2.GroupBy(a => a.l.mapin.keyProject)
										.Select(g => new ProjectMap
										{
											key = g.Key,
											desas = g.GroupBy(g => g.l.mapin.keyDesa)
												.Select(x => new DesaMap
												{
													key = x.Key,
													project = x.First().l.mapin.project,
													nama = x.First().l.mapin.desa,
													map = x.First().l.desamap,
													lands = x.Select(xx => new landMap
													{
														key = xx.l.mapin.key,
														type = xx.l.mapin._t,
														//meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == LandState.Belum_Bebas ? xx.l.mapin.deal : null),
                                                        meta = ((MetaData)xx.l.mapin).SetDeal(null, null),
                                                        map = xx.pm.map?.ToBMap(),
														status = xx.l.status
													}).ToArray()
												}).ToArray()
										}).ToArray();

				var ftdata = promap.SelectMany(p => p.MakeFeatures()).ToArray();
				var dsdata = ftdata.OfType<DesaFeature>().ToArray();
				var lndata = ftdata.OfType<LandFeature>().ToArray();

				var data = new { d = FeatureBase.Serialize64(dsdata), l = FeatureBase.Serialize64(lndata) };
				return Ok(data);
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				Program.Log(ex.AllMessages());
				return new NotModifiedResult(ex.Message);
				//return new InternalErrorResult(ex.Message);
			}
		}
	}


	//public class persilBelumBebas
	//{
	//	public string key { get; set; }
	//	public byte[] careas { get; set; }
	//	public geoPoint Center { get; set; }

	//	public string desa { get; set; }
	//	public binaland.Map dsmap { get; set; }
	//	public string pemilik { get; set; }
	//	public LandStatus ls_status { get; set; }
	//	public string noPeta { get; set; }
	//	public double? luasSurat { get; set; }
	//	public double? luasUkur { get; set; }
	//	public double? luasDibayar { get; set; }
	//	public string group { get; set; }
	//	public string keyDesa { get; set; }
	//	public string keyProject { get; set; }
	//}
}
