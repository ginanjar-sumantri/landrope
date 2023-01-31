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
using landrope.common;
using landrope.engines;
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
using Microsoft.Extensions.DependencyInjection;
using landrope.hosts;
using landrope.mod3;
using BundlerConsumer;
using landrope.consumers;
using IdPetaGen;

namespace landrope.api2
{
	//[Route("/api/reporting")]
	[ApiController]
	public class MapController : ControllerBase
	{
		IServiceProvider services;
		ExtLandropeContext context;
		LandropePlusContext contextplus;
		BundlerHostConsumer bhost;

		public MapController(IServiceProvider services)
		{
			this.services = services;
			context = services.GetService<ExtLandropeContext>();
			contextplus = services.GetService<LandropePlusContext>();
			bhost = services.GetService<IBundlerHostConsumer>() as BundlerHostConsumer;
		}

#if !_LIVE_
		//[EnableCors(nameof(landrope))]
		//[HttpPost("/api/reporting/debug-map")]
		////[NeedToken("VIEW_MAP", true)]
		//public IActionResult gmap([FromQuery] string token, [FromQuery] string key)
		//{
		//	return Getmap((token, key));
		//}
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
				return Getmap2((param.token, param.key));
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}

		//[EnableCors(nameof(landrope))]
		//[HttpPost("/api/reporting/map2")]
		//public IActionResult Getmap2(string token, string key)
		//	=> Getmap((token, key));

		//[EnableCors(nameof(landrope))]
		//[HttpPost("/api/reporting/map-test")]
		//public IActionResult Getmap3(string token, string key)
		//	=> Getmap((token, key),true);

		public class PersilInc
		{
			public string keyProject { get; set; }
			public string key { get; set; }
			public string IdBidang { get; set; }
		}

//		IActionResult Getmap((string token, string key) param, bool plain = false)
//		{
//			MyTracer.TraceInfo2(param.key);
//			try
//			{
//				var keys = param.key?.Split(',', ';', '|') ?? new string[0]; //project keys

//				/*List<PersilPositionWithNext> */
//				ParallelQuery<PersilPositionWithNext> pssteppings = new PersilPositionWithNext[0].AsParallel();
//				ParallelQuery<PersilBase> persils = new PersilBase[0].AsParallel();
//				ParallelQuery<MapData> mapinfos = new List<MapData>().AsParallel();
//				ParallelQuery<DBDesaMap> desamaps = new List<DBDesaMap>().AsParallel();
//				ParallelQuery<DBPersilMap> persilmaps = new List<DBPersilMap>().AsParallel();
//				ParallelQuery<PersilInc> persilincs = new List<PersilInc>().AsParallel();
//				ParallelQuery<string> excluded = new string[0].AsParallel();
//				var spss = new[] { new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today } }.AsParallel();

//				var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));
//				var task0 = Task.Run(() => pssteppings = bhost.SavedPositionWithNext(keys).GetAwaiter().GetResult().AsParallel());
//				var task1 = Task.Run(() => persils = contextplus.GetDocuments(new PersilBase(), "persils_v2",
//					$@"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>, en_state:<$not:<$in:[2,'2']>>,
//'basic.current.keyProject':<$in:[{prokeys}]>>>".Replace("<", "{").Replace(">", "}"),
//					"{$project:{_id:0,_t:1,key:1,IdBidang: 1,en_state: 1,deal: 1,created: 1,basic: '$basic.current'}}")
//					.ToList().AsParallel());
//				//var task2 = Task.Run(() => excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
//				//																			.ToList().Select(x => x.key).AsParallel());
//				var task3 = Task.Run(() => spss = contextplus.GetCollections(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
//																				"sps", "{date:{$ne:null}}", "{_id:0, keyPersil:1, step:1, date:1 }").ToList().AsParallel());


//				//List<(MapData map, LandStatus2 status)> data2 = new List<(MapData map, LandStatus2 status)>();

//				//persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList();
//				//var task0 = Task.Run(() => { persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList(); });
//				//var task1 = Task.Run(() => { persilsteps1 = context.GetCollections(new PersilSteps(), "material_status_hgb", "{}", "{_id:0}").ToList(); });
//				//var task2 = Task.Run(() => { persilsteps2 = context.GetCollections(new PersilSteps(), "material_status_shm", "{}", "{_id:0}").ToList(); });
//				//var task3 = Task.Run(() => { persilsteps3 = context.GetCollections(new PersilSteps(), "material_status_shp", "{}", "{_id:0}").ToList(); });
//				//var task4 = Task.Run(() => { persilsteps4 = context.GetCollections(new PersilSteps(), "material_status_hibah", "{}", "{_id:0}").ToList(); });

//				var findst = $"{{keyProject:{{$in:['{string.Join("','", keys)}']}}}}";
//				MyTracer.TraceInfo2(findst);
//				var task5 = Task.Run(() => { mapinfos = contextplus.GetCollections(new MapData(), "material_mapinfo", findst, "{_id:0}").ToList().AsParallel(); });
//				var task6 = Task.Run(() => { desamaps = contextplus.GetCollections(new DBDesaMap(), "material_desa_map", findst, "{_id:0,keyProject:0}").ToList().AsParallel(); });
//				var task7 = Task.Run(() => { persilmaps = contextplus.GetDocuments(new DBPersilMap(), "material_persil_map", 
//					$"{{$match:{findst}}}",
//					"{$lookup:{from:'vPersilsIncluded',localField:'key',foreignField:'key',as:'inc'}}",
//					"{$unwind:'$inc'}",
//					"{$project:{_id:0,keyProject:0,inc:0}}").ToList().AsParallel(); 
//				});
//				//var task7b = Task.Run(() => { persilincs = contextplus.GetCollections(new PersilInc(), "vPersilsIncluded", findst, "{_id:0,keyProject:0}").ToList().AsParallel(); });

//				Task.WaitAll(task0, task1, /*task2, */task3, /*task4, */task5, task6, task7/*,task7b*/);

//				ParallelQuery<PersilForMap> allstates = new PersilForMap[0].AsParallel();

//				var task8 = Task.Run(() => allstates = persils.GroupJoin(pssteppings, p => p.key, b => b.key, (p, sb) => p.ForMap(sb.FirstOrDefault()))
//												 .GroupJoin(excluded, p => p.key, x => x, (p, sx) => p.SetPending(sx.Any()))
//												 .GroupJoin(spss, p => (p.key, p.step, p.ongoing), s => (key: s.keyPersil, s.step, ongoing: true),
//												 (p, ss) => p.SetSPS(ss.Any())).ToArray().AsParallel());

//				ParallelQuery<(MapData m, maps.mod.Map desamap)> map_desa = new (MapData, maps.mod.Map)[0].AsParallel();
//				var task9 = Task.Run(() => map_desa = mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
//							(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
//											.ToArray().AsParallel());

//				Task.WaitAll(task8, task9);

//#if _DEBUG
//				var fs = new FileStream("coords.csv",FileMode.Create);
//				var sw = new StreamWriter(fs);
//				sw.WriteLine("key, latitude,Longitude");
//				var coords = persilmaps.Select(pm => (pm.key, shp: MapCompression.decode(pm.map.careas)))
//					.SelectMany(c => c.shp.SelectMany(x => x.AsArray.Select(y => (c.key, co:y)))).ToArray();
//				var stcoords = coords.Select(x => $"{x.key},{x.co[0]},{x.co[1]}").ToList();
//				stcoords.ForEach(c => sw.WriteLine(c));
//				sw.Flush();
//				fs.Flush();
//				sw.Close();
//				fs.Close();
//#endif
//				//var task6 = Task.Run(() =>
//				//{
//				//	data2 = context.GetCollections(new persilBelumBebas(), "belum_bebas", findst).ToList()
//				//	.Select(pbb => (map: new MapData
//				//	{
//				//		desa = pbb.desa,
//				//		key = pbb.key,
//				//		group = pbb.group,
//				//		keyDesa = pbb.keyDesa,
//				//		keyProject = pbb.keyProject,
//				//		luasUkur = pbb.luasUkur,
//				//		desaMap = pbb.dsmap,
//				//		map = new binaland.Map()
//				//		{
//				//			careas = MapCompression.encode(landrope.mod.landbase.decompress(pbb.careas)),
//				//			Area = pbb.luasUkur ?? 0,
//				//			Center = pbb.Center
//				//		},
//				//		pemilik = pbb.pemilik,
//				//		noPeta = pbb.noPeta,
//				//		_t = "persilgirik"
//				//	},
//				//	status: pbb.ls_status switch
//				//	{
//				//		LandStatus.Belum_Bebas__Sertifikat => LandStatus2.Belum_Bebas,
//				//		_ => LandStatus2.Kampung__Bengkok_Desa
//				//	})).ToList();
//				//});


//				var data1 = map_desa.Join(allstates, x => x.m.key, s => s.key,
//									(x, s) => (mapin: x.m,
//										status: s.step.ToLandState(s.ongoing, s.sps, s.category == AssignmentCat.Hibah, s.pending, s.deal, s.kampung, true),
//										desamap: x.desamap.ToBMap()));
//				var data2 = data1.Join(persilmaps.AsParallel(), l => l.mapin.key, pm => pm.key, (l, pm) => (l, pm)).ToList().AsParallel();
//				var promap = data2.GroupBy(a => a.l.mapin.keyProject)
//										.Select(g => new ProjectMap
//										{
//											key = g.Key,
//											desas = g.GroupBy(g => g.l.mapin.keyDesa)
//												.Select(x => new DesaMap
//												{
//													key = x.Key,
//													project = x.First().l.mapin.project,
//													nama = x.First().l.mapin.desa,
//													map = x.First().l.desamap,
//													lands = x.Select(xx => new landMap
//													{
//														key = xx.l.mapin.key,
//														type = xx.l.mapin._t,
//														meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == LandState.Belum_Bebas ? xx.l.mapin.deal : null),
//														map = xx.pm.map?.ToBMap(),
//														status = xx.l.status
//													}).ToArray()
//												}).ToArray()
//										}).ToArray().AsParallel();

//				var ftdata = promap.SelectMany(p => p.MakeFeatures()).ToArray();
//				var dsdata = ftdata.OfType<DesaFeature>().ToArray();
//				var lndata = ftdata.OfType<LandFeature>().ToArray();

//				//return Ok(new { d = dsdata, l = lndata });

//				if (plain)
//					return Ok(new { d = dsdata, l = dsdata });
//				var data = new { d = FeatureBase.Serialize64(dsdata), l = FeatureBase.Serialize64(lndata) };
//				return Ok(data);
//			}
//			catch (Exception ex)
//			{
//				MyTracer.TraceError2(ex);
//				Program.Log(ex.AllMessages());
//				return new UnprocessableEntityObjectResult(ex.Message);
//				//return new InternalErrorResult(ex.Message);
//			}
//		}

		//[NeedToken("VIEW_MAP", true)]
//		IActionResult Getmap_old((string token, string key) param, bool plain=false)
//		{
//			MyTracer.TraceInfo2(param.key);
//			try
//			{
//				var keys = param.key?.Split(',', ';', '|') ?? new string[0]; //project keys

//				/*List<PersilPositionWithNext> */
//				ParallelQuery<PersilPositionWithNext> pssteppings = new PersilPositionWithNext[0].AsParallel();
//				ParallelQuery<PersilBase> persils = new PersilBase[0].AsParallel();
//				ParallelQuery<MapData> mapinfos = new List<MapData>().AsParallel();
//				ParallelQuery<DBDesaMap> desamaps = new List<DBDesaMap>().AsParallel();
//				ParallelQuery<DBPersilMap> persilmaps = new List<DBPersilMap>().AsParallel();
//				ParallelQuery<string> excluded = new string[0].AsParallel();
//				var spss = new[] { new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today } }.AsParallel();

//				var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));
//				var task0 = Task.Run(()=> pssteppings = bhost.SavedPositionWithNext(keys).GetAwaiter().GetResult().AsParallel());
//				var task1 = Task.Run(()=> persils = contextplus.GetDocuments(new PersilBase(),"persils_v2",
//					$@"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>, en_state:<$not:<$in:[2,'2']>>,
//'basic.current.keyProject':<$in:[{prokeys}]>>>".Replace("<","{").Replace(">","}"),
//					"{$project:{_id:0,_t:1,key:1,IdBidang: 1,en_state: 1,deal: 1,created: 1,basic: '$basic.current'}}")
//					.ToList().AsParallel());
//				var task2 = Task.Run(()=> excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
//																							.ToList().Select(x => x.key).AsParallel());
//				var task3 = Task.Run(()=> spss = contextplus.GetCollections(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
//																				"sps", "{date:{$ne:null}}", "{_id:0, keyPersil:1, step:1, date:1 }").ToList().AsParallel());


//				//List<(MapData map, LandStatus2 status)> data2 = new List<(MapData map, LandStatus2 status)>();

//				//persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList();
//				//var task0 = Task.Run(() => { persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList(); });
//				//var task1 = Task.Run(() => { persilsteps1 = context.GetCollections(new PersilSteps(), "material_status_hgb", "{}", "{_id:0}").ToList(); });
//				//var task2 = Task.Run(() => { persilsteps2 = context.GetCollections(new PersilSteps(), "material_status_shm", "{}", "{_id:0}").ToList(); });
//				//var task3 = Task.Run(() => { persilsteps3 = context.GetCollections(new PersilSteps(), "material_status_shp", "{}", "{_id:0}").ToList(); });
//				//var task4 = Task.Run(() => { persilsteps4 = context.GetCollections(new PersilSteps(), "material_status_hibah", "{}", "{_id:0}").ToList(); });

//				var findst = $"{{keyProject:{{$in:['{string.Join("','", keys)}']}}}}";
//				MyTracer.TraceInfo2(findst);
//				var task5 = Task.Run(() => { mapinfos = contextplus.GetCollections(new MapData(), "material_mapinfo", findst, "{_id:0}").ToList().AsParallel(); });
//				var task6 = Task.Run(() => { desamaps = contextplus.GetCollections(new DBDesaMap(), "material_desa_map", findst, "{_id:0,keyProject:0}").ToList().AsParallel(); });
//				var task7 = Task.Run(() => { persilmaps = contextplus.GetCollections(new DBPersilMap(), "material_persil_map", findst, "{_id:0,keyProject:0}").ToList().AsParallel(); });

//				Task.WaitAll(task0, task1, task2, task3, /*task4, */task5, task6, task7);

//				ParallelQuery<PersilForMap> allstates = new PersilForMap[0].AsParallel();

//				var task8= Task.Run(()=>allstates = persils.GroupJoin(pssteppings, p => p.key, b => b.key, (p, sb) => p.ForMap(sb.FirstOrDefault()))
//												.GroupJoin(excluded, p => p.key, x => x, (p, sx) => p.SetPending(sx.Any()))
//												.GroupJoin(spss, p => (p.key,p.step,p.ongoing), s=> (key:s.keyPersil,s.step,ongoing:true), 
//												(p, ss) => p.SetSPS(ss.Any())).ToArray().AsParallel());

//				ParallelQuery<(MapData m, maps.mod.Map desamap)> map_desa = new (MapData, maps.mod.Map)[0].AsParallel();
//				var task9 = Task.Run(()=>map_desa = mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
//							(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
//											.ToArray().AsParallel());

//				Task.WaitAll(task8, task9);

//#if _DEBUG
//				var fs = new FileStream("coords.csv",FileMode.Create);
//				var sw = new StreamWriter(fs);
//				sw.WriteLine("key, latitude,Longitude");
//				var coords = persilmaps.Select(pm => (pm.key, shp: MapCompression.decode(pm.map.careas)))
//					.SelectMany(c => c.shp.SelectMany(x => x.AsArray.Select(y => (c.key, co:y)))).ToArray();
//				var stcoords = coords.Select(x => $"{x.key},{x.co[0]},{x.co[1]}").ToList();
//				stcoords.ForEach(c => sw.WriteLine(c));
//				sw.Flush();
//				fs.Flush();
//				sw.Close();
//				fs.Close();
//#endif
//				//var task6 = Task.Run(() =>
//				//{
//				//	data2 = context.GetCollections(new persilBelumBebas(), "belum_bebas", findst).ToList()
//				//	.Select(pbb => (map: new MapData
//				//	{
//				//		desa = pbb.desa,
//				//		key = pbb.key,
//				//		group = pbb.group,
//				//		keyDesa = pbb.keyDesa,
//				//		keyProject = pbb.keyProject,
//				//		luasUkur = pbb.luasUkur,
//				//		desaMap = pbb.dsmap,
//				//		map = new binaland.Map()
//				//		{
//				//			careas = MapCompression.encode(landrope.mod.landbase.decompress(pbb.careas)),
//				//			Area = pbb.luasUkur ?? 0,
//				//			Center = pbb.Center
//				//		},
//				//		pemilik = pbb.pemilik,
//				//		noPeta = pbb.noPeta,
//				//		_t = "persilgirik"
//				//	},
//				//	status: pbb.ls_status switch
//				//	{
//				//		LandStatus.Belum_Bebas__Sertifikat => LandStatus2.Belum_Bebas,
//				//		_ => LandStatus2.Kampung__Bengkok_Desa
//				//	})).ToList();
//				//});


//				var data1 = map_desa.Join(allstates, x => x.m.key, s => s.key, 
//									(x, s) => (mapin: x.m,
//										status : s.step.ToLandState(s.ongoing,s.sps,s.category==AssignmentCat.Hibah,s.pending,s.deal,s.kampung, true), 
//										desamap: x.desamap.ToBMap()));
//				var data2 = data1.Join(persilmaps.AsParallel(), l => l.mapin.key, pm => pm.key, (l, pm) => (l, pm)).ToList().AsParallel();
//				var promap = data2.GroupBy(a => a.l.mapin.keyProject)
//										.Select(g => new ProjectMap
//										{
//											key = g.Key,
//											desas = g.GroupBy(g => g.l.mapin.keyDesa)
//												.Select(x => new DesaMap
//												{
//													key = x.Key,
//													project = x.First().l.mapin.project,
//													nama = x.First().l.mapin.desa,
//													map = x.First().l.desamap,
//													lands = x.Select(xx => new landMap
//													{
//														key = xx.l.mapin.key,
//														type = xx.l.mapin._t,
//														meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == LandState.Belum_Bebas ? xx.l.mapin.deal : null),
//														map = xx.pm.map?.ToBMap(),
//														status = xx.l.status
//													}).ToArray()
//												}).ToArray()
//										}).ToArray().AsParallel();

//				var ftdata = promap.SelectMany(p => p.MakeFeatures()).ToArray();
//				var dsdata = ftdata.OfType<DesaFeature>().ToArray();
//				var lndata = ftdata.OfType<LandFeature>().ToArray();

//				//return Ok(new { d = dsdata, l = lndata });

//				if (plain)
//					return Ok(new { d = dsdata, l = dsdata });
//				var data = new { d = FeatureBase.Serialize64(dsdata), l = FeatureBase.Serialize64(lndata) };
//				return Ok(data);
//			}
//			catch (Exception ex)
//			{
//				MyTracer.TraceError2(ex);
//				Program.Log(ex.AllMessages());
//				return new UnprocessableEntityObjectResult(ex.Message);
//				//return new InternalErrorResult(ex.Message);
//			}
//		}


		IActionResult Getmap2((string token, string key) param)
		{
			try
			{
				var keys = param.key?.Split(',', ';', '|') ?? new string[0]; //project keys
				//var keys = key?.Split(',', ';', '|') ?? new string[0]; //project keys

				/*List<PersilPositionWithNext> */
				ParallelQuery<PersilPositionWithNext> pssteppings = new PersilPositionWithNext[0].AsParallel();
				ParallelQuery<PersilBase> persils = new PersilBase[0].AsParallel();
				ParallelQuery<MapData> mapinfos = new List<MapData>().AsParallel();
				ParallelQuery<DBDesaMap> desamaps = new List<DBDesaMap>().AsParallel();
				ParallelQuery<DBPersilMap> persilmaps = new List<DBPersilMap>().AsParallel();
				ParallelQuery<string> excluded = new string[0].AsParallel();
				var spss = new[] { new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today } }.AsParallel();

				var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));
				var task0 = Task.Run(() => pssteppings = bhost.SavedPositionWithNext(keys).GetAwaiter().GetResult().AsParallel());
				var task1 = Task.Run(() => persils = contextplus.GetDocuments(new PersilBase(), "persils_v2",
					$@"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>, en_state:<$not:<$in:[2,'2',5,'5']>>,
'basic.current.keyProject':<$in:[{prokeys}]>>>".Replace("<", "{").Replace(">", "}"),
					"{$project:{_id:0,_t:1,key:1,IdBidang: 1,en_state: 1,deal: 1,created: 1,basic: '$basic.current'}}")
					.ToList().AsParallel());
				var task2 = Task.Run(() => excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
																							.ToList().Select(x => x.key).AsParallel());
				var task3 = Task.Run(() => spss = contextplus.GetCollections(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
																				"sps", "{date:{$ne:null}}", "{_id:0, keyPersil:1, step:1, date:1 }").ToList().AsParallel());


				//List<(MapData map, LandStatus2 status)> data2 = new List<(MapData map, LandStatus2 status)>();

				//persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList();
				//var task0 = Task.Run(() => { persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList(); });
				//var task1 = Task.Run(() => { persilsteps1 = context.GetCollections(new PersilSteps(), "material_status_hgb", "{}", "{_id:0}").ToList(); });
				//var task2 = Task.Run(() => { persilsteps2 = context.GetCollections(new PersilSteps(), "material_status_shm", "{}", "{_id:0}").ToList(); });
				//var task3 = Task.Run(() => { persilsteps3 = context.GetCollections(new PersilSteps(), "material_status_shp", "{}", "{_id:0}").ToList(); });
				//var task4 = Task.Run(() => { persilsteps4 = context.GetCollections(new PersilSteps(), "material_status_hibah", "{}", "{_id:0}").ToList(); });

				var findst = $"{{keyProject:{{$in:['{string.Join("','", keys)}']}}}}";
				var task5 = Task.Run(() =>
				{
					try
					{
						mapinfos = contextplus.GetCollections(new MapData(), "material_mapinfo", findst, "{_id:0}").ToList().AsParallel();
					}
					catch (Exception ex)
					{

						MyTracer.TraceError2(ex);
					}

				});
				var task6 = Task.Run(() =>
				{
					try
					{
						desamaps = contextplus.GetCollections(new DBDesaMap(), "material_desa_map", findst, "{_id:0,keyProject:0}").ToList().AsParallel();
					}
					catch (Exception ex)
					{

						MyTracer.TraceError2(ex);
					}

				});
				var task7 = Task.Run(() => {
					try
					{
						persilmaps = contextplus.GetCollections(new DBPersilMap(), "material_persil_map", findst, "{_id:0,keyProject:0}").ToList().AsParallel();
					}
					catch (Exception ex)
					{

						MyTracer.TraceError2(ex);
					}

				});

				Task.WaitAll(task0, task1, task2, task3, /*task4, */task5, task6, task7);

				ParallelQuery<PersilForMap> allstates = new PersilForMap[0].AsParallel();

				var task8 = Task.Run(() => allstates = persils.GroupJoin(pssteppings, p => p.key, b => b.key, (p, sb) => p.ForMap(sb.FirstOrDefault()))
												 .GroupJoin(excluded, p => p.key, x => x, (p, sx) => p.SetPending(sx.Any()))
												 .GroupJoin(spss, p => (p.key, p.step, p.ongoing), s => (key: s.keyPersil, s.step, ongoing: true),
												 (p, ss) => p.SetSPS(ss.Any())).ToArray().AsParallel());

				ParallelQuery<(MapData m, maps.mod.Map desamap)> map_desa = new (MapData, maps.mod.Map)[0].AsParallel();
				var task9 = Task.Run(() => map_desa = mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
							(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
											.ToArray().AsParallel());

				Task.WaitAll(task8, task9);

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


				var data1 = map_desa.Join(allstates, x => x.m.key, s => s.key,
									(x, s) => (mapin: x.m,
										status: s.step.ToLandState(s.ongoing, s.sps, s.category == AssignmentCat.Hibah, s.pending, s.deal, s.kampung, true),
										desamap: x.desamap.ToBMap()));
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
														//meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == landrope.common.LandState.Belum_Bebas ? xx.l.mapin.deal : null),
														meta = ((MetaData)xx.l.mapin).SetDeal(null,null),
														map = xx.pm.map?.ToBMap(),
														status = xx.l.status
													}).ToArray()
												}).ToArray()
										}).ToArray().AsParallel();

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
				return new UnprocessableEntityObjectResult(ex.Message);
				//return new InternalErrorResult(ex.Message);
			}
		}

//		[EnableCors(nameof(landrope))]
//		[HttpPost("/api/reporting/map3")]
//		public IActionResult Getmap4(string token, string key)
//		{
//			try
//			{
//				//var keys = param.key?.Split(',', ';', '|') ?? new string[0]; //project keys
//				var keys = key?.Split(',', ';', '|') ?? new string[0]; //project keys

//				/*List<PersilPositionWithNext> */
//				ParallelQuery<PersilPositionWithNext> pssteppings = new PersilPositionWithNext[0].AsParallel();
//				ParallelQuery<PersilBase> persils = new PersilBase[0].AsParallel();
//				ParallelQuery<MapData> mapinfos = new List<MapData>().AsParallel();
//				ParallelQuery<DBDesaMap> desamaps = new List<DBDesaMap>().AsParallel();
//				ParallelQuery<DBPersilMap> persilmaps = new List<DBPersilMap>().AsParallel();
//				ParallelQuery<string> excluded = new string[0].AsParallel();
//				var spss = new[] { new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today } }.AsParallel();

//				var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));
//				var task0 = Task.Run(() => pssteppings = bhost.SavedPositionWithNext(keys).GetAwaiter().GetResult().AsParallel());
//				var task1 = Task.Run(() => persils = contextplus.GetDocuments(new PersilBase(), "persils_v2",
//					$@"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>, en_state:<$not:<$in:[2,'2']>>,
//'basic.current.keyProject':<$in:[{prokeys}]>>>".Replace("<", "{").Replace(">", "}"),
//					"{$project:{_id:0,_t:1,key:1,IdBidang: 1,en_state: 1,deal: 1,created: 1,basic: '$basic.current'}}")
//					.ToList().AsParallel());
//				var task2 = Task.Run(() => excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
//																							.ToList().Select(x => x.key).AsParallel());
//				var task3 = Task.Run(() => spss = contextplus.GetCollections(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
//																				"sps", "{date:{$ne:null}}", "{_id:0, keyPersil:1, step:1, date:1 }").ToList().AsParallel());


//				//List<(MapData map, LandStatus2 status)> data2 = new List<(MapData map, LandStatus2 status)>();

//				//persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList();
//				//var task0 = Task.Run(() => { persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList(); });
//				//var task1 = Task.Run(() => { persilsteps1 = context.GetCollections(new PersilSteps(), "material_status_hgb", "{}", "{_id:0}").ToList(); });
//				//var task2 = Task.Run(() => { persilsteps2 = context.GetCollections(new PersilSteps(), "material_status_shm", "{}", "{_id:0}").ToList(); });
//				//var task3 = Task.Run(() => { persilsteps3 = context.GetCollections(new PersilSteps(), "material_status_shp", "{}", "{_id:0}").ToList(); });
//				//var task4 = Task.Run(() => { persilsteps4 = context.GetCollections(new PersilSteps(), "material_status_hibah", "{}", "{_id:0}").ToList(); });

//				var findst = $"{{keyProject:{{$in:['{string.Join("','", keys)}']}}}}";
//                var task5 = Task.Run(() =>
//                {
//                    try
//                    {
//						mapinfos = contextplus.GetCollections(new MapData(), "material_mapinfo", findst, "{_id:0}").ToList().AsParallel();
//					}
//                    catch (Exception ex)
//                    {

//						MyTracer.TraceError2(ex);
//					}
					
//				});
//                var task6 = Task.Run(() =>
//                {
//                    try
//                    {
//						desamaps = contextplus.GetCollections(new DBDesaMap(), "material_desa_map", findst, "{_id:0,keyProject:0}").ToList().AsParallel();
//					}
//                    catch (Exception ex)
//                    {

//						MyTracer.TraceError2(ex);
//					}
					
//				});
//				var task7 = Task.Run(() => {
//                    try
//                    {
//						persilmaps = contextplus.GetCollections(new DBPersilMap(), "material_persil_map", findst, "{_id:0,keyProject:0}").ToList().AsParallel();
//					}
//                    catch (Exception ex)
//                    {

//						MyTracer.TraceError2(ex);
//					}
					
//				});

//				Task.WaitAll(task0, task1, task2, task3, /*task4, */task5, task6, task7);

//				ParallelQuery<PersilForMap> allstates = new PersilForMap[0].AsParallel();

//				var task8 = Task.Run(() => allstates = persils.GroupJoin(pssteppings, p => p.key, b => b.key, (p, sb) => p.ForMap(sb.FirstOrDefault()))
//												 .GroupJoin(excluded, p => p.key, x => x, (p, sx) => p.SetPending(sx.Any()))
//												 .GroupJoin(spss, p => (p.key, p.step, p.ongoing), s => (key: s.keyPersil, s.step, ongoing: true),
//												 (p, ss) => p.SetSPS(ss.Any())).ToArray().AsParallel());

//				ParallelQuery<(MapData m, maps.mod.Map desamap)> map_desa = new (MapData, maps.mod.Map)[0].AsParallel();
//				var task9 = Task.Run(() => map_desa = mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
//							(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
//											.ToArray().AsParallel());

//				Task.WaitAll(task8, task9);

//#if _DEBUG
//				var fs = new FileStream("coords.csv",FileMode.Create);
//				var sw = new StreamWriter(fs);
//				sw.WriteLine("key, latitude,Longitude");
//				var coords = persilmaps.Select(pm => (pm.key, shp: MapCompression.decode(pm.map.careas)))
//					.SelectMany(c => c.shp.SelectMany(x => x.AsArray.Select(y => (c.key, co:y)))).ToArray();
//				var stcoords = coords.Select(x => $"{x.key},{x.co[0]},{x.co[1]}").ToList();
//				stcoords.ForEach(c => sw.WriteLine(c));
//				sw.Flush();
//				fs.Flush();
//				sw.Close();
//				fs.Close();
//#endif
//				//var task6 = Task.Run(() =>
//				//{
//				//	data2 = context.GetCollections(new persilBelumBebas(), "belum_bebas", findst).ToList()
//				//	.Select(pbb => (map: new MapData
//				//	{
//				//		desa = pbb.desa,
//				//		key = pbb.key,
//				//		group = pbb.group,
//				//		keyDesa = pbb.keyDesa,
//				//		keyProject = pbb.keyProject,
//				//		luasUkur = pbb.luasUkur,
//				//		desaMap = pbb.dsmap,
//				//		map = new binaland.Map()
//				//		{
//				//			careas = MapCompression.encode(landrope.mod.landbase.decompress(pbb.careas)),
//				//			Area = pbb.luasUkur ?? 0,
//				//			Center = pbb.Center
//				//		},
//				//		pemilik = pbb.pemilik,
//				//		noPeta = pbb.noPeta,
//				//		_t = "persilgirik"
//				//	},
//				//	status: pbb.ls_status switch
//				//	{
//				//		LandStatus.Belum_Bebas__Sertifikat => LandStatus2.Belum_Bebas,
//				//		_ => LandStatus2.Kampung__Bengkok_Desa
//				//	})).ToList();
//				//});


//				var data1 = map_desa.Join(allstates, x => x.m.key, s => s.key,
//									(x, s) => (mapin: x.m,
//										status: s.step.ToLandState(s.ongoing, s.sps, s.category == AssignmentCat.Hibah, s.pending, s.deal, s.kampung, true),
//										desamap: x.desamap.ToBMap()));
//				var data2 = data1.Join(persilmaps.AsParallel(), l => l.mapin.key, pm => pm.key, (l, pm) => (l, pm)).ToList().AsParallel();
//				var promap = data2.GroupBy(a => a.l.mapin.keyProject)
//										.Select(g => new ProjectMap
//										{
//											key = g.Key,
//											desas = g.GroupBy(g => g.l.mapin.keyDesa)
//												.Select(x => new DesaMap
//												{
//													key = x.Key,
//													project = x.First().l.mapin.project,
//													nama = x.First().l.mapin.desa,
//													map = x.First().l.desamap,
//													lands = x.Select(xx => new landMap
//													{
//														key = xx.l.mapin.key,
//														type = xx.l.mapin._t,
//														meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == LandState.Belum_Bebas ? xx.l.mapin.deal : null),
//														map = xx.pm.map?.ToBMap(),
//														status = xx.l.status
//													}).ToArray()
//												}).ToArray()
//										}).ToArray().AsParallel();

//				var ftdata = promap.SelectMany(p => p.MakeFeatures()).ToArray();
//				var dsdata = ftdata.OfType<DesaFeature>().ToArray();
//				var lndata = ftdata.OfType<LandFeature>().ToArray();

//				var data = new { d = FeatureBase.Serialize64(dsdata), l = FeatureBase.Serialize64(lndata) };
//				return Ok(data);
//			}
//			catch (Exception ex)
//			{
//				MyTracer.TraceError2(ex);
//				Program.Log(ex.AllMessages());
//				return new UnprocessableEntityObjectResult(ex.Message);
//				//return new InternalErrorResult(ex.Message);
//			}
//		}

		/// <summary>
		/// Generate No Peta by Desa's Key
		/// </summary>
		//[EnableCors(nameof(landrope))]
		//[HttpPost("/api/generate/no-peta")]
		//public IActionResult GenerateNoPeta(string token, string keyDesa)
  //      {
  //          try
  //          {
		//		var gen = services.GetService<IDPetaGenerator>() as IDPetaGenerator;
		//		var generatedNumber = gen.GenerateNumber(keyDesa);
		//		return Ok(generatedNumber);
  //          }
		//	catch (UnauthorizedAccessException exa)
		//	{
		//		return new ContentResult { StatusCode = int.Parse(exa.Message) };
		//	}
		//	catch (Exception ex)
		//	{
		//		MyTracer.TraceError2(ex);
		//		return new UnprocessableEntityObjectResult(ex.Message);
		//	}
		//}

//		[EnableCors(nameof(landrope))]
//		[HttpPost("/api/reporting/map2")]
//		public IActionResult Getmap2(string token, string key)
//		{
//			try
//			{
//				//var keys = param.key?.Split(',', ';', '|') ?? new string[0]; //project keys
//				var keys = key?.Split(',', ';', '|') ?? new string[0]; //project keys

//				/*List<PersilPositionWithNext> */
//				ParallelQuery<PersilPositionWithNext> pssteppings = new PersilPositionWithNext[0].AsParallel();
//				ParallelQuery<PersilBase> persils = new PersilBase[0].AsParallel();
//				ParallelQuery<MapData> mapinfos = new List<MapData>().AsParallel();
//				ParallelQuery<DBDesaMap> desamaps = new List<DBDesaMap>().AsParallel();
//				ParallelQuery<DBPersilMap> persilmaps = new List<DBPersilMap>().AsParallel();
//				ParallelQuery<string> excluded = new string[0].AsParallel();
//				var spss = new[] { new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today } }.AsParallel();

//				var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));
//				var task0 = Task.Run(() => pssteppings = bhost.SavedPositionWithNext(keys).GetAwaiter().GetResult().AsParallel());
//				var task1 = Task.Run(() => persils = contextplus.GetDocuments(new PersilBase(), "persils_v2",
//					$@"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>, en_state:<$not:<$in:[2,'2']>>,
//'basic.current.keyProject':<$in:[{prokeys}]>>>".Replace("<", "{").Replace(">", "}"),
//					"{$project:{_id:0,_t:1,key:1,IdBidang: 1,en_state: 1,deal: 1,created: 1,basic: '$basic.current'}}")
//					.ToList().AsParallel());
//				var task2 = Task.Run(() => excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
//																							.ToList().Select(x => x.key).AsParallel());
//				var task3 = Task.Run(() => spss = contextplus.GetCollections(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
//																				"sps", "{date:{$ne:null}}", "{_id:0, keyPersil:1, step:1, date:1 }").ToList().AsParallel());


//				//List<(MapData map, LandStatus2 status)> data2 = new List<(MapData map, LandStatus2 status)>();

//				//persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList();
//				//var task0 = Task.Run(() => { persilsteps = context.GetCollections(new PersilSteps(), "material_status_girik", "{}", "{_id:0}").ToList(); });
//				//var task1 = Task.Run(() => { persilsteps1 = context.GetCollections(new PersilSteps(), "material_status_hgb", "{}", "{_id:0}").ToList(); });
//				//var task2 = Task.Run(() => { persilsteps2 = context.GetCollections(new PersilSteps(), "material_status_shm", "{}", "{_id:0}").ToList(); });
//				//var task3 = Task.Run(() => { persilsteps3 = context.GetCollections(new PersilSteps(), "material_status_shp", "{}", "{_id:0}").ToList(); });
//				//var task4 = Task.Run(() => { persilsteps4 = context.GetCollections(new PersilSteps(), "material_status_hibah", "{}", "{_id:0}").ToList(); });

//				var findst = $"{{keyProject:{{$in:['{string.Join("','", keys)}']}}}}";
//				var task5 = Task.Run(() =>
//				{
//					try
//					{
//						mapinfos = contextplus.GetCollections(new MapData(), "material_mapinfo", findst, "{_id:0}").ToList().AsParallel();
//					}
//					catch (Exception ex)
//					{

//						MyTracer.TraceError2(ex);
//					}

//				});
//				var task6 = Task.Run(() =>
//				{
//					try
//					{
//						desamaps = contextplus.GetCollections(new DBDesaMap(), "material_desa_map", findst, "{_id:0,keyProject:0}").ToList().AsParallel();
//					}
//					catch (Exception ex)
//					{

//						MyTracer.TraceError2(ex);
//					}

//				});
//				var task7 = Task.Run(() => {
//					try
//					{
//						persilmaps = contextplus.GetCollections(new DBPersilMap(), "material_persil_map", findst, "{_id:0,keyProject:0}").ToList().AsParallel();
//					}
//					catch (Exception ex)
//					{

//						MyTracer.TraceError2(ex);
//					}

//				});

//				Task.WaitAll(task0, task1, task2, task3, /*task4, */task5, task6, task7);

//				ParallelQuery<PersilForMap> allstates = new PersilForMap[0].AsParallel();

//				var task8 = Task.Run(() => allstates = persils.GroupJoin(pssteppings, p => p.key, b => b.key, (p, sb) => p.ForMap(sb.FirstOrDefault()))
//												 .GroupJoin(excluded, p => p.key, x => x, (p, sx) => p.SetPending(sx.Any()))
//												 .GroupJoin(spss, p => (p.key, p.step, p.ongoing), s => (key: s.keyPersil, s.step, ongoing: true),
//												 (p, ss) => p.SetSPS(ss.Any())).ToArray().AsParallel());

//				ParallelQuery<(MapData m, maps.mod.Map desamap)> map_desa = new (MapData, maps.mod.Map)[0].AsParallel();
//				var task9 = Task.Run(() => map_desa = mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
//							(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
//											.ToArray().AsParallel());

//				Task.WaitAll(task8, task9);

//#if _DEBUG
//				var fs = new FileStream("coords.csv",FileMode.Create);
//				var sw = new StreamWriter(fs);
//				sw.WriteLine("key, latitude,Longitude");
//				var coords = persilmaps.Select(pm => (pm.key, shp: MapCompression.decode(pm.map.careas)))
//					.SelectMany(c => c.shp.SelectMany(x => x.AsArray.Select(y => (c.key, co:y)))).ToArray();
//				var stcoords = coords.Select(x => $"{x.key},{x.co[0]},{x.co[1]}").ToList();
//				stcoords.ForEach(c => sw.WriteLine(c));
//				sw.Flush();
//				fs.Flush();
//				sw.Close();
//				fs.Close();
//#endif
//				//var task6 = Task.Run(() =>
//				//{
//				//	data2 = context.GetCollections(new persilBelumBebas(), "belum_bebas", findst).ToList()
//				//	.Select(pbb => (map: new MapData
//				//	{
//				//		desa = pbb.desa,
//				//		key = pbb.key,
//				//		group = pbb.group,
//				//		keyDesa = pbb.keyDesa,
//				//		keyProject = pbb.keyProject,
//				//		luasUkur = pbb.luasUkur,
//				//		desaMap = pbb.dsmap,
//				//		map = new binaland.Map()
//				//		{
//				//			careas = MapCompression.encode(landrope.mod.landbase.decompress(pbb.careas)),
//				//			Area = pbb.luasUkur ?? 0,
//				//			Center = pbb.Center
//				//		},
//				//		pemilik = pbb.pemilik,
//				//		noPeta = pbb.noPeta,
//				//		_t = "persilgirik"
//				//	},
//				//	status: pbb.ls_status switch
//				//	{
//				//		LandStatus.Belum_Bebas__Sertifikat => LandStatus2.Belum_Bebas,
//				//		_ => LandStatus2.Kampung__Bengkok_Desa
//				//	})).ToList();
//				//});


//				var data1 = map_desa.Join(allstates, x => x.m.key, s => s.key,
//									(x, s) => (mapin: x.m,
//										status: s.step.ToLandState(s.ongoing, s.sps, s.category == AssignmentCat.Hibah, s.pending, s.deal, s.kampung),
//										desamap: x.desamap.ToBMap()));
//				var data2 = data1.Join(persilmaps.AsParallel(), l => l.mapin.key, pm => pm.key, (l, pm) => (l, pm)).ToList().AsParallel();
//				var promap = data2.GroupBy(a => a.l.mapin.keyProject)
//										.Select(g => new ProjectMap
//										{
//											key = g.Key,
//											desas = g.GroupBy(g => g.l.mapin.keyDesa)
//												.Select(x => new DesaMap
//												{
//													key = x.Key,
//													project = x.First().l.mapin.project,
//													nama = x.First().l.mapin.desa,
//													map = x.First().l.desamap,
//													lands = x.Select(xx => new landMap
//													{
//														key = xx.l.mapin.key,
//														type = xx.l.mapin._t,
//														meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == LandState.Belum_Bebas ? xx.l.mapin.deal : null),
//														map = xx.pm.map?.ToBMap(),
//														status = xx.l.status
//													}).ToArray()
//												}).ToArray()
//										}).ToArray().AsParallel();

//				var ftdata = promap.SelectMany(p => p.MakeFeatures()).ToArray();
//				var dsdata = ftdata.OfType<DesaFeature>().ToArray();
//				var lndata = ftdata.OfType<LandFeature>().ToArray();

//				var data = new { d = FeatureBase.Serialize64(dsdata), l = FeatureBase.Serialize64(lndata) };
//				return Ok(data);
//			}
//			catch (Exception ex)
//			{
//				MyTracer.TraceError2(ex);
//				Program.Log(ex.AllMessages());
//				return new UnprocessableEntityObjectResult(ex.Message);
//				//return new InternalErrorResult(ex.Message);
//			}
//		}
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


