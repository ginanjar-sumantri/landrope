#define _PROBE2
#define _NOT_PARALLEL_
#define _STAGING

using BundlerConsumer;
using encdec;
using landrope.common;
using landrope.mod2;
using landrope.mod3;
using maps.mod;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using mischelper;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tracer;
using mongohelper;
using brotenc;
using api2m.shared;
using MongoDB.Bson;
//using SharpMap.Data.Providers;

namespace landrope.api2.Controllers
{
	//[Route("/api/reporting")]
	[ApiController]
	public class MapController : ControllerBase
	{
		IServiceProvider services;
		LandropePlusContext contextplus;
		BundlerHostConsumer bhost;

		public MapController(IServiceProvider services)
		{
			this.services = services;
			//context = services.GetService<ExtLandropeContext>();
			contextplus = services.GetService<LandropePlusContext>();
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("/api/reporting/debug-map")]
		public IActionResult gmap([FromQuery] string token, [FromQuery] string key, bool csv = true, bool cutoff = false, int appno = 1, bool clean = true, bool xtra=false)
		{
			var ret = xtra? GetmapBX(key, cutoff, appno, clean) : GetmapB(key, cutoff, appno, clean);
			if (csv)
			{
				var ldata = ret.ls.Select(l => new CsvStruct
				{
					project = l.data.project,
					desa = l.data.desa,
					IdBidang = l.data.IdBidang,
					luas = l.data.luas,
					state = (int)l.State,
					status = l.status,
					sisa = l.data.sisa,
					overlap = l.data.overlap,
					keluar = l.keluar,
					claim = l.claim,
					damai = l.damai,
					damaiB = l.damaiB,
					kulit = l.kulit,
					proses = l.data.proses,
					nomor = l.data.nomor
				}).ToArray();
				var sb = MakeCsv(ldata, false);
				var file = new FileContentResult(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv");
				file.FileDownloadName = $"Map-data-{DateTime.Now:yyyyMMddHHmm}.csv";
				return file;
			}
			var ops = new JsonSerializerSettings { Formatting = Formatting.None };
			var data = new DL(ret.ds, ret.ls);
			var ser = JsonConvert.SerializeObject(data, typeof(DL), ops);
			return new ContentResult { ContentType = "application/json", Content = ser };
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("/api/reporting/map-bydesa")]
		public IActionResult gmapD([FromQuery] string token, [FromQuery] string deskeys, bool csv = true, bool cutoff = false, int appno = 1, bool clean = true)
		{
			var ret = GetmapBD(deskeys, cutoff, appno, clean);
			if (csv)
			{
				var ldata = ret.ls.Select(l => new CsvStruct
				{
					project = l.data.project,
					desa = l.data.desa,
					IdBidang = l.data.IdBidang,
					luas = l.data.luas,
					state = (int)l.State(),
					status = l.status(),
					keluar = l.keluar,
					claim = l.claim,
					damai = l.damai,
					damaiB = l.damaiB,
					kulit = l.kulit,
					proses = l.data.proses,
					nomor = l.data.nomor
				}).ToArray();
				var sb = MakeCsv(ldata, false);
				var file = new FileContentResult(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv");
				file.FileDownloadName = $"Map-data-bydesa{DateTime.Now:yyyyMMddHHmm}.csv";
				return file;
			}
			var ops = new JsonSerializerSettings { Formatting = Formatting.None };
			var data = new DLL(ret.ds, ret.ls);
			var ser = JsonConvert.SerializeObject(data, typeof(DL), ops);
			return new ContentResult { ContentType = "application/json", Content = ser };
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("/api/reporting/comp-map")]
		public IActionResult gmapcomp([FromQuery] string token, [FromQuery] string key, bool cutoff = false, int appno = 1, bool clean = true, bool xtra=false)
		{
			var ret = xtra? GetmapBX(key, cutoff, appno, clean) : GetmapB(key, cutoff, appno, clean);
			var cret = new DL(ret.ds, ret.ls);
			return new ContentResult { ContentType = "application/octet-stream", Content = BrotEnc.ObjToOctet(cret) };
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("/api/reporting/comp-map-bydesa")]
		public IActionResult gmapcompD([FromQuery] string token, [FromQuery] string deskeys, bool cutoff = false, int appno = 1, bool clean = true)
		{
			var ret = GetmapBD(deskeys, cutoff, appno, clean);
			var cret = new DLL(ret.ds, ret.ls);
			return new ContentResult { ContentType = "application/octet-stream", Content = BrotEnc.ObjToOctet(cret) };
		}

		IActionResult ReturnCompressed(object obj) =>
			new ContentResult { ContentType = "application/octet-stream", Content = BrotEnc.ObjToOctet(obj) };

		[EnableCors(nameof(landrope))]
		[HttpPost("/api/reporting/new-map")]
		public IActionResult gmap2([FromQuery] string token, [FromQuery] string key, bool csv = true, bool cutoff = false)
		{
			var ret = GetmapC(key, cutoff);
			if (csv)
			{
				var ldata = ret.ls.Select(l => new CsvStruct
				{
					project = l.data.project,
					desa = l.data.desa,
					IdBidang = l.data.IdBidang,
					luas = l.data.luas,
					state = (int)l.State,
					status = l.status,
					sisa = l.data.sisa,
					overlap = l.data.overlap,
					proses = l.data.proses,
				}).ToArray();
				var sb = MakeCsv(ldata, false);
				var file = new FileContentResult(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv");
				file.FileDownloadName = $"Map-data-{DateTime.Now:yyyyMMddHHmm}.csv";
				return file;
			}
			var ops = new JsonSerializerSettings { Formatting = Formatting.None };
			var data = new DL(ret.ds, ret.ls);
			var ser = JsonConvert.SerializeObject(data, typeof(DL), ops);
			return new ContentResult { ContentType = "application/json", Content = ser };
		}

		[EnableCors(nameof(landrope))]
		[HttpPost("/api/reporting/map")]
		//[NeedToken("VIEW_MAP", true)]
		public IActionResult map([FromQuery] string p)
		{
			//MyTracer.TraceInfo2($"p={p}");
			//Program.Log($"p={p}");
			Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

			try
			{
				var json = Transporter.Decrypt(p);
				MyTracer.TraceInfo2($"json={json}");
				Program.Log($"json={json}");
				var param = JsonConvert.DeserializeAnonymousType(json, new { token = "", key = "" });
				return Getmap(param.key);
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}

		//[EnableCors(nameof(landrope))]
		//[HttpPost("/api/reporting/map2")]
		//public IActionResult Getmap2(string token, string key)
		//	=> Getmap(key);

		[EnableCors(nameof(landrope))]
		[HttpPost("/api/reporting/map-test")]
		public IActionResult Getmap3(string token, string key, bool cutoff = false)
			=> Getmap(key, false, cutoff);

		IActionResult Getmap(string key, bool plain = false, bool cutoff = false)
		{
			var ret = GetmapB(key, cutoff);
			if (plain)
			{
				var ops = new JsonSerializerSettings { Formatting = Formatting.None };
				var data = new DL(ret.ds, ret.ls);
				var ser = JsonConvert.SerializeObject(data, typeof(DL), ops);
				return new ContentResult { ContentType = "application/json", Content = ser };
			}
			var dl = FeatureBase.Serialize64(ret.ls);
			var dx = FeatureBase.Serialize64(ret.ds);
			return Ok(new { d = dx, l = dl });
		}

		async Task<List<PersilPositionWithNext>> SavedPositionWithNext(string[] prokeys = null) =>
			await SavedPositionWithNext(contextplus,prokeys);

		public static async Task<List<PersilPositionWithNext>> SavedPositionWithNext(
			LandropePlusContext contextplus,string[] prokeys = null)
		{
			string pkeys = prokeys == null || prokeys.Length == 0 ? "" : string.Join(',', prokeys.Select(k => $"'{k}'"));
			var match = string.IsNullOrEmpty(pkeys) ? null : $"<$match:<keyProject:<$in:[{pkeys}]>>>".Replace("<", "{").Replace(">", "}");
			var stages = new[]{match,
		"{$lookup: {from: 'persilNextStep',localField: 'key',foreignField: 'key',as: 'next'}}",
		"{$unwind:{path: '$next',preserveNullAndEmptyArrays: true}}",
		"{$addFields:{next: '$next._step'}}",
		"{$project:{_id:0}}" }.Where(s => s != null).ToArray();

			return await contextplus.GetDocumentsAsync(new PersilPositionWithNext(), "persilLastPos", stages);
		}

		internal class PersilCateg
		{
			public string key { get; set; }
			public string keyProject { get; set; }
			public string[] cats { get; set; }
		}

		(DesaFeature[] ds, LandFeature[] ls) GetmapB(string key, bool cutoff = false, int appno = 1, bool clean = true)
		{
			var persil_collection = "persils_v2";// "persils_v2";
			var mapinfo_name = "vMapInfo";

			MyTracer.TraceInfo2(key);
			try
			{
				contextplus.SwitchDB(cutoff, appno);
				var keys = key?.Split(',', ';', '|') ?? new string[0]; //project keys

				var prokeys = string.Join(',', keys.Select(k => $"'{k.Trim()}'"));

				var task0 = SavedPositionWithNext(keys);//.GetAwaiter().GetResult()
				var task1 = contextplus.GetDocumentsAsync(new PersilBase(), persil_collection,
					$"<$match:<invalid:<$ne:true>, en_state:<$not:<$in:[2,'2']>>,'basic.current.keyProject':<$in:[{prokeys}]>>>".MongoJs(),
					"{$project:{_id:0,_t:1,key:1,IdBidang: 1,en_state: 1, en_proses:'$basic.current.en_proses' ,deal: 1,created: 1,basic: '$basic.current'}}");

				var task3 = contextplus.GetDocumentsAsync(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
																				"sps", "{$match:{date:{$ne:null}}}", "{$project:{_id:0, keyPersil:1, step:1, date:1 }}");


				var lastproj = "{_id:0}";
				var findst = $"{{keyProject:{{$in:[{prokeys}]}}}}";
				var stg_findst = $"{{$match:{findst} }}";
				var stg_lastproj = $"{{$project:{lastproj} }}";
				//				var psmaps = contextplus.GetCollections(new DBPersilMap(), "material_persil_map", findst, "{ _id:0,keyProject:0}").ToList();

				var stg_unclean = "{$addFields: {incl_keluar:true}}";

				var task4a = clean ? contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vLandStatus_new", findst, lastproj) :
													contextplus.db.ExecuteSPAsync<maps.mod.LandStatus>("vLandStatus_new",
															(pre: stg_findst, post: stg_lastproj), (pre: stg_unclean, post: null));
				var task4b = contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vBintang_new", findst, lastproj);
				var task4c = contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vDamai_new", findst, lastproj);
				var task4d = contextplus.GetCollectionsAsync(new maps.mod.KulitStatus(), "vKulit_new", findst, lastproj);

				var task5 = contextplus.GetCollectionsAsync(new MapData(), "vMapInfo", findst, "{_id:0}");
				var task6 = contextplus.GetCollectionsAsync(new DBDesaMap(), "vDesaMap", findst, "{_id:0,keyProject:0}");
				var task7 = contextplus.GetCollectionsAsync(new DBPersilMap(), "vPersilMap", findst, "{ _id:0,keyProject:0}");
				var task7a = contextplus.GetCollectionsAsync(new { project = new { key = "", identity = "" }, village = new { key = "", identity = "" } },
											"villages2", $"<'project.key':<$in:[{prokeys}]>>".MongoJs(), "{ _id:0, project:1,'village.key':1,'village.identity':1 }");
				var task7b = contextplus.GetCollectionsAsync(new PersilCateg(), "vmCategs_new", findst, lastproj);


				//Task.WaitAll(task0, task1, task3, task4a, task4b, task4c, task5, /*task6, */task7, task7a, task7b);
				//Task.WaitAll(task0, task1, task3, task4a, task4b, task4c, task5 ,task7, task7b, task6, task7a);
				var persils = task1.Result;
				var pskeys = persils.Select(p => p.key).ToArray();

				var lsto = task4a.Result.Join(pskeys, p => p.key, k => k, (p, k) => p).ToArray();
				var lst = lsto.ToArray().ToList();
				var lstb = task4b.Result.Join(pskeys, p => p.key, k => k, (p, k) => p).ToList();
				lstb.ForEach(x =>
					x.state = maps.mod.LandState.Overlap// | (x.kategori.Contains("amai") ? maps.mod.LandState._Damai : maps.mod.LandState.___)
				);
				lst.AddRange(lstb);
				var lstc = task4c.Result;
				lstc.ForEach(x => x.state = (x.kategori == "damai*" ? (maps.mod.LandState._Damai | maps.mod.LandState.Overlap) :
											maps.mod.LandState._Damai));
				lst.AddRange(lstc);
				var landstatus = lst
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif
				var kulits = task4d.Result;

#if _NOT_PARALLEL_
				List<PersilPositionWithNext> pssteppings = task0.Result;
				var pbelum = lsto.Where(p => p.status.Contains("elum"))// p.en_state == StatusBidang.belumbebas || p.en_state == StatusBidang.kampung)
					.Select(p => p.IdBidang).ToArray();
				var psudah = lsto.Select(p => p.IdBidang).Except(pbelum).ToArray();

				var corrects0 = pssteppings.Join(pbelum, s => s.IdBidang, b => b, (s, b) => s).ToArray();
				var remains = pssteppings.Join(psudah, s => s.IdBidang, b => b, (s, b) => s).ToList();

				var corrects = corrects0.Where(x => x.step != DocProcessStep.Belum_Bebas).ToArray();
				remains.AddRange(corrects0.Where(x => x.step == DocProcessStep.Belum_Bebas));

				var adjs = corrects.Any();
#if _PROBE2_
				if (adjs)
					MyTracer.TraceInfo2($"unmatched step: {string.Join(",", corrects.Select(c => c.IdBidang))}");
#endif
				foreach (var x in corrects)
				{
					x.step = DocProcessStep.Belum_Bebas;
					x.next = DocProcessStep.Baru_Bebas;
					x.position = "Belum Bebas";
				}

#if _PROBE2_
				if (adjs)
				{
					var states = corrects.Select(p => $"{p.IdBidang},{p.step},{p.position}").ToArray();
					MyTracer.TraceInfo2($"corrected steps: {string.Join(",", states)}");
				}
#endif
				if (adjs)
				{
					pssteppings = remains;
					pssteppings.AddRange(corrects);
				}

#if _PROBE2_
				MyTracer.TraceInfo2($"step: {pssteppings.FirstOrDefault(p => p.IdBidang == "B0030017")?.position}");
#endif
				var spss = task3.Result;
				var mapinfos1 = task5.Result;
				var persilmaps = task7.Result;
				var locations = task7a.Result;
				var desamaps = task6.Result.Join(locations, d => d.key, l => l.village.key, (d, l) => d).ToArray();
				//var desamaps = rawdesamaps.Join(locations, d => d.key, l => l.village.key, (d, l) => d).ToArray();
				var categs = task7b.Result;

#else
						var pssteppings = task0.Result.AsParallel();
						var persils = task1.Result.AsParallel();
						var spss = task3.Result.AsParallel();
						var mapinfos1 = task5.Result.AsParallel();
						var desamaps = task6.Result.AsParallel();
						var persilmaps = task7.Result.AsParallel();
				var locations = task7a.Result.AsParallel();
#endif


#if _PROBE_
				var bulky1 = landstatus.FirstOrDefault(l => l.IdBidang == "F0210151"); ;
#endif

				var mapinfos = new List<MapData2>();

#if _NOT_PARALLEL_
				;
#else
						.AsParallel();
#endif
				//var psls = lsto.Join(persils, l => l.key, p => p.key, (l, p) =>
				var psls = landstatus.Join(persils, l => l.key, p => p.key, (l, p) =>
				(p, lS: l.luasSurat, lB: l.luasDibayar, en_state: l.status.Contains("udah") ? StatusBidang.bebas : StatusBidang.belumbebas)).ToArray();
				var task8 = Task.Run(() => psls.GroupJoin(pssteppings, p => p.p.key, b => b.key,
													(p, sb) => p.p.ForMap(sb.FirstOrDefault(), p.en_state, p.lS, p.lB))
												 //.GroupJoin(excluded, p => p.key, x => x, (p, sx) => p.SetPending(sx.Any()))
												 .GroupJoin(spss, p => (p.key, p.step, p.ongoing), s => (key: s.keyPersil, s.step, ongoing: true),
												 (p, ss) => p.SetSPS(ss.Any())).ToList());

				var task9 = Task.Run(() =>
				{
					var landstplus = landstatus.Join(locations, m => m.keyDesa, l => l.village.key,
						(m, l) => (mapin: m, locs: new[] { l.project.identity, l.village.identity })).ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif
					var landstplusK = kulits.Join(locations, m => m.keyDesa, l => l.village.key,
						(m, l) => (mapinK: m, project: l.project.identity, desa:l.village.identity)).ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif

					mapinfos = landstplus.GroupJoin(mapinfos1, s => s.mapin.key, i => i.key, (s, si) => new MapData2(si.FirstOrDefault(), s))
								.ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif

					return (mapinfos, landstplusK);

					//mapd = mapinfos.AsParallel().GroupJoin(desamaps, m => m.keyDesa, d => d.key,
					//		(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
					//						.ToArray();
					//return mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
					//		(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
					//						.ToList();
				});

				Task.WaitAll(task8, task9);

				var allstates = task8.Result ?? new List<PersilForMap>();
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				var victim = allstates.FirstOrDefault();// (p => p.IdBidang == "B0030017");
				MyTracer.TraceInfo2($"step-2: {victim?.step},position:{victim?.position}");
#endif



				var (map_infos, map_infosK) = task9.Result
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				MyTracer.TraceInfo2($"pass step-2");
#endif

#if _PROBE_
						var bulky2 = allstates.FirstOrDefault(l => l.IdBidang== "F0210151");

						var bulky2a = map_desa.FirstOrDefault(l => l.m.IdBidang == "F0210151"); ;
#endif

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

#if _PROBE2_
				MyTracer.TraceInfo2($"{nameof(map_infos)} is null? {map_infos == null}; {nameof(allstates)} is null? {allstates == null}");
				MyTracer.TraceInfo2($"into step-3");
#endif
				var data1 = map_infos.GroupJoin(allstates, x => x.key, s => s.key,
									(x, ss) => (x, s: ss.FirstOrDefault()))
									.Select(y => (mapin: y.x,
											status: (y.s?.step.ToLandStateEx(y.s?.ongoing == true, y.s?.sps == true, y.s?.category == AssignmentCat.Hibah,
															y.s?.pending == true, y.s?.deal == true, y.s?.kampung == true) ?? maps.mod.LandState.Belum_Bebas)
															| y.x.state,
											nomor: y.s?.nomor
										//desamap: y.x.desamap?.ToBMap()
										)).ToArray()
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				var victim2 = data1.FirstOrDefault(p => p.mapin.IdBidang == "B0030017");
				if (victim2.mapin != null)
					MyTracer.TraceInfo2($"step-3: {victim2.status},{victim2.mapin.state}");
#endif

#if _PROBE_
						var bulky3 = data1.Where(l => !string.IsNullOrWhiteSpace(l.mapin.IdBidang)).GroupBy(l => l.mapin.IdBidang.Substring(0, 1))
							.Select(g => (key: g.Key, sample: g.First()))
							.ToArray();
#endif

				var data1a = data1.GroupJoin(persilmaps, l => l.mapin.key, pm => pm.key,
							(l, spm) => (l, pm: spm.FirstOrDefault())).ToList()
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;
#if _PROBE2_
				var victim3 = data1a.FirstOrDefault(p => p.l.mapin.IdBidang == "B0030017");
				if (victim3.l.mapin != null)
					MyTracer.TraceInfo2($"step-4: {victim3.l.status},{victim3.l.mapin.state}");
#endif

#if _PROBE_
						var bulky4 = data2.Where(l => !string.IsNullOrWhiteSpace(l.l.mapin.IdBidang)).GroupBy(l => l.l.mapin.IdBidang.Substring(0, 1))
							.Select(g => (key: g.Key, sample: g.First()))
							.ToArray();
#endif
				var data2 = data1a.GroupJoin(categs, d => d.l.mapin.key, c => c.key,
						(d, cs) => (d.l, d.pm, cat: cs.FirstOrDefault()?.cats ?? new string[0])).ToList();

				//				var coords = data2.Select(d => (d.l.mapin.key, careas: Encoding.ASCII.GetString(d.pm?.map?.careas ?? new byte[0]))).ToArray();
				//#if !_NOT_PARALLEL_
				//				.AsParallel()
				//#endif
				//				;

#if _PROBE2_
				var victim4a = categs.FirstOrDefault(p => p.cats.Any());
				MyTracer.TraceInfo2($"step-5a: {string.Join(',', victim4a.cats)}");
				var victim4 = data2.FirstOrDefault(p => p.cat?.Any() ?? false);
				MyTracer.TraceInfo2($"step-5: {string.Join(',', victim4.cat)}");
#endif

				//var promap = data2.GroupBy(a => a.l.mapin.keyProject)
				//						.Select(g => new ProjectMap
				//						{
				//							key = g.Key,
				//							desas = g.GroupBy(g => g.l.mapin.keyDesa)
				//								.Select(x => new DesaMap
				//								{
				//									key = x.Key,
				//									project = x.First().l.mapin.project,
				//									nama = x.First().l.mapin.desa,
				//									map = x.First().l.desamap,
				//									lands = x.Select(xx => new landMap
				//									{
				//										key = xx.l.mapin.key,
				//										type = xx.l.mapin._t,
				//										meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == maps.mod.LandState.Deal ? xx.l.mapin.deal : null)
				//														.SetNomor(xx.l.nomor).SetCats(xx.cat),
				//										map = xx.pm?.map?.ToBMap(),
				//										status = xx.l.status ?? maps.mod.LandState.___
				//									}).ToArray()
				//								}).ToArray()
				//						}).ToArray()
				//#if !_NOT_PARALLEL_
				//				.AsParallel()
				//#endif
				//;

				var dslist = lst.GroupBy(l => l.keyDesa).Select(g => g.Key).ToArray();
				//var ftdata = promap.SelectMany(p => p.MakeFeatures()).ToArray();
				var dsdata = MakeDesaFeatures(); //ftdata.OfType<DesaFeature>().ToArray();
				var lndata = MakeLandFeatures(); //ftdata.OfType<LandFeature>().ToArray();
				var lndataK = MakeLandFeaturesK(); //ftdata.OfType<LandFeature>().ToArray();
				return (dsdata, lndata.Concat(lndataK).ToArray());

				DesaFeature[] MakeDesaFeatures() =>
					locations.Join(dslist, l => l.village.key, d => d, (l, d) => l).GroupJoin(desamaps, l => l.village.key, d => d.key, (l, ds) =>
													 ds.DefaultIfEmpty(new DBDesaMap { key = l.village.key, map = new maps.mod.Map() })
														.Select(y => new DesaFeature
														{
															key = y.key,
															nama = l.village.identity,
															project = l.project.identity,
															prokey = l.project.key,
															deskey = y.key,
															shapes = y.map?.ToBMap().GetShape() ?? Array.Empty<geo.shared.XPointF[]>(),
														}).First()).ToArray();


				LandFeature[] MakeLandFeatures() =>
					data2.Select(l => new LandFeature
					{
						key = l.l.mapin.key,
						prokey = l.l.mapin.keyProject,
						deskey = l.l.mapin.keyDesa,
						keluar = l.l.mapin.keluar,
						claim = l.l.mapin.claim,
						damai = l.l.mapin.damai,
						damaiB = l.l.mapin.damaiB,
						kulit = l.l.mapin.kulit,
						data = ((MetaData)l.l.mapin).SetDeal(l.l.status == maps.mod.LandState.Deal ? l.l.mapin.deal : null)
											.SetCats(l.cat).SetNomor<MetaData>(l.l.nomor),
						state = l.l.status,
						shapes = l.pm?.map == null ? new geo.shared.XPointF[0][] : l.pm.map.ToBMap().GetShape(),
					}).ToArray();

				LandFeature[] MakeLandFeaturesK() =>
					map_infosK.Select(l => new LandFeature
					{
						key = l.mapinK.key,
						prokey = l.mapinK.keyProject,
						deskey = l.mapinK.keyDesa,
						keluar = false,
						claim = false,
						damai = false,
						damaiB = false,
						kulit = true,
						data = new MetaData(l.mapinK,l.project,l.desa),
						shapes = l.mapinK.GetShapes()?.Select(s => s.coordinates.Select(c => (geo.shared.XPointF)c).ToArray()).ToArray() ?? Array.Empty<geo.shared.XPointF[]>(),
					}).ToArray();

			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				Program.Log(ex.AllMessages());
				return (Array.Empty<DesaFeature>(), Array.Empty<LandFeature>());
			}
		}

		(DesaFeature[] ds, LandFeature[] ls) GetmapBX(string key, bool cutoff = false, int appno = 1, bool clean = true)
		{
			var persil_collection = "vPersils_xtra";// "persils_v2";
			var mapinfo_name = "vMapInfo";

			MyTracer.TraceInfo2(key);
			try
			{
				contextplus.SwitchDB(cutoff, appno);
				var keys = key?.Split(',', ';', '|') ?? new string[0]; //project keys

				var prokeys = string.Join(',', keys.Select(k => $"'{k.Trim()}'"));

				var task0 = SavedPositionWithNext(keys);//.GetAwaiter().GetResult()
				var task1 = contextplus.GetDocumentsAsync(new PersilBase(), persil_collection,
					$"<$match:<invalid:<$ne:true>, en_state:<$not:<$in:[2,'2']>>,'basic.current.keyProject':<$in:[{prokeys}]>>>".MongoJs(),
					"{$project:{_id:0,_t:1,key:1,IdBidang: 1,en_state: 1, en_proses:'$basic.current.en_proses' ,deal: 1,created: 1,basic: '$basic.current'}}");

				var task3 = contextplus.GetDocumentsAsync(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
																				"sps", "{$match:{date:{$ne:null}}}", "{$project:{_id:0, keyPersil:1, step:1, date:1 }}");


				var lastproj = "{_id:0}";
				var findst = $"{{keyProject:{{$in:[{prokeys}]}}}}";
				var stg_findst = $"{{$match:{findst} }}";
				var stg_lastproj = $"{{$project:{lastproj} }}";
				//				var psmaps = contextplus.GetCollections(new DBPersilMap(), "material_persil_map", findst, "{ _id:0,keyProject:0}").ToList();

				var stg_unclean = "{$addFields: {incl_keluar:true}}";

				var task4a = clean ? contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vLandStatus_new_xtra", findst, lastproj) :
													contextplus.db.ExecuteSPAsync<maps.mod.LandStatus>("vLandStatus_new_xtra",
															(pre: stg_findst, post: stg_lastproj), (pre: stg_unclean, post: null));
				var task4b = contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vBintang_new", findst, lastproj);
				var task4c = contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vDamai_new", findst, lastproj);
				var task4d = contextplus.GetCollectionsAsync(new maps.mod.KulitStatus(), "vKulit_new", findst, lastproj);

				var task5 = contextplus.GetCollectionsAsync(new MapData(), "vMapInfo", findst, "{_id:0}");
				var task6 = contextplus.GetCollectionsAsync(new DBDesaMap(), "vDesaMap", findst, "{_id:0,keyProject:0}");
				var task7 = contextplus.GetCollectionsAsync(new DBPersilMap(), "vPersilMap", findst, "{ _id:0,keyProject:0}");
				var task7a = contextplus.GetCollectionsAsync(new { project = new { key = "", identity = "" }, village = new { key = "", identity = "" } },
											"villages2", $"<'project.key':<$in:[{prokeys}]>>".MongoJs(), "{ _id:0, project:1,'village.key':1,'village.identity':1 }");
				var task7b = contextplus.GetCollectionsAsync(new PersilCateg(), "vmCategs_new", findst, lastproj);


				//Task.WaitAll(task0, task1, task3, task4a, task4b, task4c, task5, /*task6, */task7, task7a, task7b);
				//Task.WaitAll(task0, task1, task3, task4a, task4b, task4c, task5 ,task7, task7b, task6, task7a);
				var persils = task1.Result;
				var pskeys = persils.Select(p => p.key).ToArray();

				var lsto = task4a.Result.Join(pskeys, p => p.key, k => k, (p, k) => p).ToArray();
				var lst = lsto.ToArray().ToList();
				var lstb = task4b.Result.Join(pskeys, p => p.key, k => k, (p, k) => p).ToList();
				lstb.ForEach(x =>
					x.state = maps.mod.LandState.Overlap// | (x.kategori.Contains("amai") ? maps.mod.LandState._Damai : maps.mod.LandState.___)
				);
				lst.AddRange(lstb);
				var lstc = task4c.Result;
				lstc.ForEach(x => x.state = (x.kategori == "damai*" ? (maps.mod.LandState._Damai | maps.mod.LandState.Overlap) :
											maps.mod.LandState._Damai));
				lst.AddRange(lstc);
				var landstatus = lst
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif
				var kulits = task4d.Result;

#if _NOT_PARALLEL_
				List<PersilPositionWithNext> pssteppings = task0.Result;
				var pbelum = lsto.Where(p => p.status.Contains("elum"))// p.en_state == StatusBidang.belumbebas || p.en_state == StatusBidang.kampung)
					.Select(p => p.IdBidang).ToArray();
				var psudah = lsto.Select(p => p.IdBidang).Except(pbelum).ToArray();

				var corrects0 = pssteppings.Join(pbelum, s => s.IdBidang, b => b, (s, b) => s).ToArray();
				var remains = pssteppings.Join(psudah, s => s.IdBidang, b => b, (s, b) => s).ToList();

				var corrects = corrects0.Where(x => x.step != DocProcessStep.Belum_Bebas).ToArray();
				remains.AddRange(corrects0.Where(x => x.step == DocProcessStep.Belum_Bebas));

				var adjs = corrects.Any();
#if _PROBE2_
				if (adjs)
					MyTracer.TraceInfo2($"unmatched step: {string.Join(",", corrects.Select(c => c.IdBidang))}");
#endif
				foreach (var x in corrects)
				{
					x.step = DocProcessStep.Belum_Bebas;
					x.next = DocProcessStep.Baru_Bebas;
					x.position = "Belum Bebas";
				}

#if _PROBE2_
				if (adjs)
				{
					var states = corrects.Select(p => $"{p.IdBidang},{p.step},{p.position}").ToArray();
					MyTracer.TraceInfo2($"corrected steps: {string.Join(",", states)}");
				}
#endif
				if (adjs)
				{
					pssteppings = remains;
					pssteppings.AddRange(corrects);
				}

#if _PROBE2_
				MyTracer.TraceInfo2($"step: {pssteppings.FirstOrDefault(p => p.IdBidang == "B0030017")?.position}");
#endif
				var spss = task3.Result;
				var mapinfos1 = task5.Result.Join(persils,m=>m.key,p=>p.key,(m,p)=>m.SetIdBidang(p.IdBidang)).ToArray();
				var persilmaps = task7.Result;
				var locations = task7a.Result;
				var desamaps = task6.Result.Join(locations, d => d.key, l => l.village.key, (d, l) => d).ToArray();
				//var desamaps = rawdesamaps.Join(locations, d => d.key, l => l.village.key, (d, l) => d).ToArray();
				var categs = task7b.Result;

#else
						var pssteppings = task0.Result.AsParallel();
						var persils = task1.Result.AsParallel();
						var spss = task3.Result.AsParallel();
						var mapinfos1 = task5.Result.AsParallel();
						var desamaps = task6.Result.AsParallel();
						var persilmaps = task7.Result.AsParallel();
				var locations = task7a.Result.AsParallel();
#endif


#if _PROBE_
				var bulky1 = landstatus.FirstOrDefault(l => l.IdBidang == "F0210151"); ;
#endif

				var mapinfos = new List<MapData2>();

#if _NOT_PARALLEL_
				;
#else
						.AsParallel();
#endif
				//var psls = lsto.Join(persils, l => l.key, p => p.key, (l, p) =>
				var psls = landstatus.Join(persils, l => l.key, p => p.key, (l, p) =>
				(p, lS: l.luasSurat, lB: l.luasDibayar, en_state: l.status.Contains("udah") ? StatusBidang.bebas : StatusBidang.belumbebas)).ToArray();
				var task8 = Task.Run(() => psls.GroupJoin(pssteppings, p => p.p.key, b => b.key,
													(p, sb) => p.p.ForMap(sb.FirstOrDefault(), p.en_state, p.lS, p.lB))
												 //.GroupJoin(excluded, p => p.key, x => x, (p, sx) => p.SetPending(sx.Any()))
												 .GroupJoin(spss, p => (p.key, p.step, p.ongoing), s => (key: s.keyPersil, s.step, ongoing: true),
												 (p, ss) => p.SetSPS(ss.Any())).ToList());

				var task9 = Task.Run(() =>
				{
					var landstplus = landstatus.Join(locations, m => m.keyDesa, l => l.village.key,
						(m, l) => (mapin: m, locs: new[] { l.project.identity, l.village.identity })).ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif
					var landstplusK = kulits.Join(locations, m => m.keyDesa, l => l.village.key,
						(m, l) => (mapinK: m, project: l.project.identity, desa: l.village.identity)).ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif

					mapinfos = landstplus.GroupJoin(mapinfos1, s => s.mapin.key, i => i.key, (s, si) => new MapData2(si.FirstOrDefault(), s))
								.ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif

					return (mapinfos, landstplusK);

					//mapd = mapinfos.AsParallel().GroupJoin(desamaps, m => m.keyDesa, d => d.key,
					//		(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
					//						.ToArray();
					//return mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
					//		(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
					//						.ToList();
				});

				Task.WaitAll(task8, task9);

				var allstates = task8.Result ?? new List<PersilForMap>();
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				var victim = allstates.FirstOrDefault();// (p => p.IdBidang == "B0030017");
				MyTracer.TraceInfo2($"step-2: {victim?.step},position:{victim?.position}");
#endif



				var (map_infos, map_infosK) = task9.Result
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				MyTracer.TraceInfo2($"pass step-2");
#endif

#if _PROBE_
						var bulky2 = allstates.FirstOrDefault(l => l.IdBidang== "F0210151");

						var bulky2a = map_desa.FirstOrDefault(l => l.m.IdBidang == "F0210151"); ;
#endif

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

#if _PROBE2_
				MyTracer.TraceInfo2($"{nameof(map_infos)} is null? {map_infos == null}; {nameof(allstates)} is null? {allstates == null}");
				MyTracer.TraceInfo2($"into step-3");
#endif
				var data1 = map_infos.GroupJoin(allstates, x => x.key, s => s.key,
									(x, ss) => (x, s: ss.FirstOrDefault()))
									.Select(y => (mapin: y.x,
											status: (y.s?.step.ToLandStateEx(y.s?.ongoing == true, y.s?.sps == true, y.s?.category == AssignmentCat.Hibah,
															y.s?.pending == true, y.s?.deal == true, y.s?.kampung == true) ?? maps.mod.LandState.Belum_Bebas)
															| y.x.state,
											nomor: y.s?.nomor
										//desamap: y.x.desamap?.ToBMap()
										)).ToArray()
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				var victim2 = data1.FirstOrDefault(p => p.mapin.IdBidang == "B0030017");
				if (victim2.mapin != null)
					MyTracer.TraceInfo2($"step-3: {victim2.status},{victim2.mapin.state}");
#endif

#if _PROBE_
						var bulky3 = data1.Where(l => !string.IsNullOrWhiteSpace(l.mapin.IdBidang)).GroupBy(l => l.mapin.IdBidang.Substring(0, 1))
							.Select(g => (key: g.Key, sample: g.First()))
							.ToArray();
#endif

				var data1a = data1.GroupJoin(persilmaps, l => l.mapin.key, pm => pm.key,
							(l, spm) => (l, pm: spm.FirstOrDefault())).ToList()
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;
#if _PROBE2_
				var victim3 = data1a.FirstOrDefault(p => p.l.mapin.IdBidang == "B0030017");
				if (victim3.l.mapin != null)
					MyTracer.TraceInfo2($"step-4: {victim3.l.status},{victim3.l.mapin.state}");
#endif

#if _PROBE_
						var bulky4 = data2.Where(l => !string.IsNullOrWhiteSpace(l.l.mapin.IdBidang)).GroupBy(l => l.l.mapin.IdBidang.Substring(0, 1))
							.Select(g => (key: g.Key, sample: g.First()))
							.ToArray();
#endif
				var data2 = data1a.GroupJoin(categs, d => d.l.mapin.key, c => c.key,
						(d, cs) => (d.l, d.pm, cat: cs.FirstOrDefault()?.cats ?? new string[0])).ToList();

				//				var coords = data2.Select(d => (d.l.mapin.key, careas: Encoding.ASCII.GetString(d.pm?.map?.careas ?? new byte[0]))).ToArray();
				//#if !_NOT_PARALLEL_
				//				.AsParallel()
				//#endif
				//				;

#if _PROBE2_
				var victim4a = categs.FirstOrDefault(p => p.cats.Any());
				MyTracer.TraceInfo2($"step-5a: {string.Join(',', victim4a.cats)}");
				var victim4 = data2.FirstOrDefault(p => p.cat?.Any() ?? false);
				MyTracer.TraceInfo2($"step-5: {string.Join(',', victim4.cat)}");
#endif

				//var promap = data2.GroupBy(a => a.l.mapin.keyProject)
				//						.Select(g => new ProjectMap
				//						{
				//							key = g.Key,
				//							desas = g.GroupBy(g => g.l.mapin.keyDesa)
				//								.Select(x => new DesaMap
				//								{
				//									key = x.Key,
				//									project = x.First().l.mapin.project,
				//									nama = x.First().l.mapin.desa,
				//									map = x.First().l.desamap,
				//									lands = x.Select(xx => new landMap
				//									{
				//										key = xx.l.mapin.key,
				//										type = xx.l.mapin._t,
				//										meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == maps.mod.LandState.Deal ? xx.l.mapin.deal : null)
				//														.SetNomor(xx.l.nomor).SetCats(xx.cat),
				//										map = xx.pm?.map?.ToBMap(),
				//										status = xx.l.status ?? maps.mod.LandState.___
				//									}).ToArray()
				//								}).ToArray()
				//						}).ToArray()
				//#if !_NOT_PARALLEL_
				//				.AsParallel()
				//#endif
				//;

				var dslist = lst.GroupBy(l => l.keyDesa).Select(g => g.Key).ToArray();
				//var ftdata = promap.SelectMany(p => p.MakeFeatures()).ToArray();
				var dsdata = MakeDesaFeatures(); //ftdata.OfType<DesaFeature>().ToArray();
				var lndata = MakeLandFeatures(); //ftdata.OfType<LandFeature>().ToArray();
				var lndataK = MakeLandFeaturesK(); //ftdata.OfType<LandFeature>().ToArray();
				return (dsdata, lndata.Concat(lndataK).ToArray());

				DesaFeature[] MakeDesaFeatures() =>
					locations.Join(dslist, l => l.village.key, d => d, (l, d) => l).GroupJoin(desamaps, l => l.village.key, d => d.key, (l, ds) =>
													 ds.DefaultIfEmpty(new DBDesaMap { key = l.village.key, map = new maps.mod.Map() })
														.Select(y => new DesaFeature
														{
															key = y.key,
															nama = l.village.identity,
															project = l.project.identity,
															prokey = l.project.key,
															deskey = y.key,
															shapes = y.map?.ToBMap().GetShape() ?? Array.Empty<geo.shared.XPointF[]>(),
														}).First()).ToArray();


				LandFeature[] MakeLandFeatures() =>
					data2.Select(l => new LandFeature
					{
						key = l.l.mapin.key,
						prokey = l.l.mapin.keyProject,
						deskey = l.l.mapin.keyDesa,
						keluar = l.l.mapin.keluar,
						claim = l.l.mapin.claim,
						damai = l.l.mapin.damai,
						damaiB = l.l.mapin.damaiB,
						kulit = l.l.mapin.kulit,
						data = ((MetaData)l.l.mapin).SetDeal(l.l.status == maps.mod.LandState.Deal ? l.l.mapin.deal : null)
											.SetCats(l.cat).SetNomor<MetaData>(l.l.nomor),
						state = l.l.status,
						shapes = l.pm?.map == null ? new geo.shared.XPointF[0][] : l.pm.map.ToBMap().GetShape(),
					}).ToArray();

				LandFeature[] MakeLandFeaturesK() =>
					map_infosK.Select(l => new LandFeature
					{
						key = l.mapinK.key,
						prokey = l.mapinK.keyProject,
						deskey = l.mapinK.keyDesa,
						keluar = false,
						claim = false,
						damai = false,
						damaiB = false,
						kulit = true,
						data = new MetaData(l.mapinK, l.project, l.desa),
						shapes = l.mapinK.GetShapes()?.Select(s => s.coordinates.Select(c => (geo.shared.XPointF)c).ToArray()).ToArray() ?? Array.Empty<geo.shared.XPointF[]>(),
					}).ToArray();

			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				Program.Log(ex.AllMessages());
				return (Array.Empty<DesaFeature>(), Array.Empty<LandFeature>());
			}
		}

		(DesaFeature[] ds, LandFeatureLight[] ls) GetmapBD(string deskeys, bool cutoff = false, int appno = 1, bool clean = true)
		{
			var persil_collection = "persils_v2";// "persils_v2";
			var mapinfo_name = "vMapInfo";

			MyTracer.TraceInfo2(deskeys);
			try
			{
				contextplus.SwitchDB(cutoff, appno);
				var dkeys = deskeys?.Split(',', ';', '|') ?? new string[0]; //project keys

				deskeys = string.Join(',', dkeys.Select(k => $"'{k.Trim()}'"));

				var task1 = contextplus.GetDocumentsAsync(new PersilBase(), persil_collection,
					$"<$match:<invalid:<$ne:true>, en_state:<$not:<$in:[2,'2']>>,'basic.current.keyDesa':<$in:[{deskeys}]>>>".MongoJs(),
					"{$project:{_id:0,_t:1,key:1,IdBidang: 1,en_state: 1, en_proses:'$basic.current.en_proses' ,deal: 1,created: 1,basic: '$basic.current'}}");

				var lastproj = "{_id:0}";
				var findst = $"{{keyDesa:{{$in:[{deskeys}]}}}}";
				var stg_findst = $"{{$match:{findst} }}";
				var stg_lastproj = $"{{$project:{lastproj} }}";
				//				var psmaps = contextplus.GetCollections(new DBPersilMap(), "material_persil_map", findst, "{ _id:0,keyProject:0}").ToList();

				var stg_unclean = "{$addFields: {incl_keluar:true}}";

				var task4a = clean ? contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vLandStatus_new", findst, lastproj) :
													contextplus.db.ExecuteSPAsync<maps.mod.LandStatus>("vLandStatus_new",
															(pre: stg_findst, post: stg_lastproj), (pre: stg_unclean, post: null));
				var task4b = contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vBintang_new", findst, lastproj);
				var task4c = contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vDamai_new", findst, lastproj);
				var task4d = contextplus.GetCollectionsAsync(new maps.mod.KulitStatus(), "vKulit_new", findst, lastproj);

				var task5 = contextplus.GetCollectionsAsync(new MapData(), "vMapInfo", findst, "{_id:0}");
				var task6 = contextplus.GetCollectionsAsync(new DBDesaMapD(), "vDesaMapD", findst, "{_id:0,keyProject:0}");
				var task7 = contextplus.GetCollectionsAsync(new DBPersilMap(), "vPersilMapD", findst, "{ _id:0,keyProject:0,keyDesa:0}");
				var task7a = contextplus.GetCollectionsAsync(new { project = new { key = "", identity = "" }, village = new { key = "", identity = "" } },
											"villages2", $"<'village.key':<$in:[{deskeys}]>>".MongoJs(), "{ _id:0, project:1,'village.key':1,'village.identity':1 }");
				var task7b = contextplus.GetCollectionsAsync(new PersilCateg(), "vmCategs_new", findst, lastproj);


				//Task.WaitAll(task0, task1, task3, task4a, task4b, task4c, task5, /*task6, */task7, task7a, task7b);
				//Task.WaitAll(task0, task1, task3, task4a, task4b, task4c, task5 ,task7, task7b, task6, task7a);
				var persils = task1.Result;
				var pskeys = persils.Select(p => p.key).ToArray();

				var lsto = task4a.Result.Join(pskeys, p => p.key, k => k, (p, k) => p).ToArray();
				var lst = lsto.ToArray().ToList();
				var lstb = task4b.Result.Join(pskeys, p => p.key, k => k, (p, k) => p).ToList();
				lstb.ForEach(x =>
					x.state = maps.mod.LandState.Overlap// | (x.kategori.Contains("amai") ? maps.mod.LandState._Damai : maps.mod.LandState.___)
				);
				lst.AddRange(lstb);
				var lstc = task4c.Result;
				lstc.ForEach(x => x.state = (x.kategori == "damai*" ? (maps.mod.LandState._Damai | maps.mod.LandState.Overlap) :
											maps.mod.LandState._Damai));
				lst.AddRange(lstc);
				var landstatus = lst
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif
				var kulits = task4d.Result;

#if _NOT_PARALLEL_
				var pbelum = lsto.Where(p => p.status.Contains("elum"))// p.en_state == StatusBidang.belumbebas || p.en_state == StatusBidang.kampung)
					.Select(p => p.IdBidang).ToArray();
				var psudah = lsto.Select(p => p.IdBidang).Except(pbelum).ToArray();

				var mapinfos1 = task5.Result;
				var persilmaps = task7.Result;
				var locations = task7a.Result;
				var desamaps = task6.Result.Join(locations, d => d.key, l => l.village.key, (d, l) => d).ToArray();
				//var desamaps = rawdesamaps.Join(locations, d => d.key, l => l.village.key, (d, l) => d).ToArray();
				var categs = task7b.Result;

#else
						var pssteppings = task0.Result.AsParallel();
						var persils = task1.Result.AsParallel();
						var spss = task3.Result.AsParallel();
						var mapinfos1 = task5.Result.AsParallel();
						var desamaps = task6.Result.AsParallel();
						var persilmaps = task7.Result.AsParallel();
				var locations = task7a.Result.AsParallel();
#endif


#if _PROBE_
				var bulky1 = landstatus.FirstOrDefault(l => l.IdBidang == "F0210151"); ;
#endif

				var mapinfos = new List<MapData2>();

#if _NOT_PARALLEL_
				;
#else
						.AsParallel();
#endif
				//var psls = lsto.Join(persils, l => l.key, p => p.key, (l, p) =>
				var psls = landstatus.Join(persils, l => l.key, p => p.key, (l, p) =>
				(p, lS: l.luasSurat, lB: l.luasDibayar, en_state: l.status.Contains("udah") ? StatusBidang.bebas : StatusBidang.belumbebas)).ToArray();
				var task8 = Task.Run(() => psls.Select(p => p.p.ForMap(null, p.en_state, p.lS, p.lB)).ToList());

				var task9 = Task.Run(() =>
				{
					var landstplus = landstatus.Join(locations, m => m.keyDesa, l => l.village.key,
						(m, l) => (mapin: m, locs: new[] { l.project.identity, l.village.identity })).ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif
					var landstplusK = kulits.Join(locations, m => m.keyDesa, l => l.village.key,
						(m, l) => (mapinK: m, project: l.project.identity, desa: l.village.identity)).ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif

					mapinfos = landstplus.GroupJoin(mapinfos1, s => s.mapin.key, i => i.key, (s, si) => new MapData2(si.FirstOrDefault(), s))
								.ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif

					return (mapinfos, landstplusK);

					//mapd = mapinfos.AsParallel().GroupJoin(desamaps, m => m.keyDesa, d => d.key,
					//		(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
					//						.ToArray();
					//return mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
					//		(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
					//						.ToList();
				});

				//Task.WaitAll(task8, task9);

				var allstates = task8.Result ?? new List<PersilForMap>();
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				var victim = allstates.FirstOrDefault();// (p => p.IdBidang == "B0030017");
				MyTracer.TraceInfo2($"step-2: {victim?.step},position:{victim?.position}");
#endif



				var (map_infos, map_infosK) = task9.Result
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				MyTracer.TraceInfo2($"pass step-2");
#endif

#if _PROBE_
						var bulky2 = allstates.FirstOrDefault(l => l.IdBidang== "F0210151");

						var bulky2a = map_desa.FirstOrDefault(l => l.m.IdBidang == "F0210151"); ;
#endif

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

#if _PROBE2_
				MyTracer.TraceInfo2($"{nameof(map_infos)} is null? {map_infos == null}; {nameof(allstates)} is null? {allstates == null}");
				MyTracer.TraceInfo2($"into step-3");
#endif
				var data1 = map_infos.GroupJoin(allstates, x => x.key, s => s.key,
									(x, ss) => (x, s: ss.FirstOrDefault()))
									.Select(y => (mapin: y.x,
											status: (y.s?.step.ToLandStateEx(y.s?.ongoing == true, y.s?.sps == true, y.s?.category == AssignmentCat.Hibah,
															y.s?.pending == true, y.s?.deal == true, y.s?.kampung == true) ?? maps.mod.LandState.Belum_Bebas)
															| y.x.state,
											nomor: y.s?.nomor
										//desamap: y.x.desamap?.ToBMap()
										)).ToArray()
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				var victim2 = data1.FirstOrDefault(p => p.mapin.IdBidang == "B0030017");
				if (victim2.mapin != null)
					MyTracer.TraceInfo2($"step-3: {victim2.status},{victim2.mapin.state}");
#endif

#if _PROBE_
						var bulky3 = data1.Where(l => !string.IsNullOrWhiteSpace(l.mapin.IdBidang)).GroupBy(l => l.mapin.IdBidang.Substring(0, 1))
							.Select(g => (key: g.Key, sample: g.First()))
							.ToArray();
#endif

				var data1a = data1.GroupJoin(persilmaps, l => l.mapin.key, pm => pm.key,
							(l, spm) => (l, pm: spm.FirstOrDefault())).ToList()
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;
#if _PROBE2_
				var victim3 = data1a.FirstOrDefault(p => p.l.mapin.IdBidang == "B0030017");
				if (victim3.l.mapin != null)
					MyTracer.TraceInfo2($"step-4: {victim3.l.status},{victim3.l.mapin.state}");
#endif

#if _PROBE_
						var bulky4 = data2.Where(l => !string.IsNullOrWhiteSpace(l.l.mapin.IdBidang)).GroupBy(l => l.l.mapin.IdBidang.Substring(0, 1))
							.Select(g => (key: g.Key, sample: g.First()))
							.ToArray();
#endif
				var data2 = data1a.GroupJoin(categs, d => d.l.mapin.key, c => c.key,
						(d, cs) => (d.l, d.pm, cat: cs.FirstOrDefault()?.cats ?? new string[0])).ToList();

				//				var coords = data2.Select(d => (d.l.mapin.key, careas: Encoding.ASCII.GetString(d.pm?.map?.careas ?? new byte[0]))).ToArray();
				//#if !_NOT_PARALLEL_
				//				.AsParallel()
				//#endif
				//				;

#if _PROBE2_
				var victim4a = categs.FirstOrDefault(p => p.cats.Any());
				MyTracer.TraceInfo2($"step-5a: {string.Join(',', victim4a.cats)}");
				var victim4 = data2.FirstOrDefault(p => p.cat?.Any() ?? false);
				MyTracer.TraceInfo2($"step-5: {string.Join(',', victim4.cat)}");
#endif

				//var promap = data2.GroupBy(a => a.l.mapin.keyProject)
				//						.Select(g => new ProjectMap
				//						{
				//							key = g.Key,
				//							desas = g.GroupBy(g => g.l.mapin.keyDesa)
				//								.Select(x => new DesaMap
				//								{
				//									key = x.Key,
				//									project = x.First().l.mapin.project,
				//									nama = x.First().l.mapin.desa,
				//									map = x.First().l.desamap,
				//									lands = x.Select(xx => new landMap
				//									{
				//										key = xx.l.mapin.key,
				//										type = xx.l.mapin._t,
				//										meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == maps.mod.LandState.Deal ? xx.l.mapin.deal : null)
				//														.SetNomor(xx.l.nomor).SetCats(xx.cat),
				//										map = xx.pm?.map?.ToBMap(),
				//										status = xx.l.status ?? maps.mod.LandState.___
				//									}).ToArray()
				//								}).ToArray()
				//						}).ToArray()
				//#if !_NOT_PARALLEL_
				//				.AsParallel()
				//#endif
				//;

				var dslist = lst.GroupBy(l => l.keyDesa).Select(g => g.Key).ToArray();
				//var ftdata = promap.SelectMany(p => p.MakeFeatures()).ToArray();
				var dsdata = MakeDesaFeatures(); //ftdata.OfType<DesaFeature>().ToArray();
				var lndata = MakeLandFeatures(); //ftdata.OfType<LandFeature>().ToArray();
				var lndataK = MakeLandFeaturesK(); //ftdata.OfType<LandFeature>().ToArray();
				return (dsdata, lndata.Concat(lndataK).ToArray());

				DesaFeature[] MakeDesaFeatures() =>
					locations.Join(dslist, l => l.village.key, d => d, (l, d) => l).GroupJoin(desamaps, l => l.village.key, d => d.key, (l, ds) =>
													 ds.DefaultIfEmpty(new DBDesaMap { key = l.village.key, map = new maps.mod.Map() })
														.Select(y => new DesaFeature
														{
															key = y.key,
															nama = l.village.identity,
															project = l.project.identity,
															prokey = l.project.key,
															deskey = y.key,
															shapes = y.map?.ToBMap().GetShape() ?? Array.Empty<geo.shared.XPointF[]>(),
														}).First()).ToArray();


				LandFeatureLight[] MakeLandFeatures() =>
					data2.Select(l => new LandFeatureLight
					{
						key = l.l.mapin.key,
						prokey = l.l.mapin.keyProject,
						deskey = l.l.mapin.keyDesa,
						keluar = l.l.mapin.keluar,
						claim = l.l.mapin.claim,
						damai = l.l.mapin.damai,
						damaiB = l.l.mapin.damaiB,
						kulit = l.l.mapin.kulit,
						data = (MetaDataBase)l.l.mapin,
						state = l.l.status,
						shapes = l.pm?.map == null ? new geo.shared.XPointF[0][] : l.pm.map.ToBMap().GetShape(),
					}).ToArray();

				LandFeatureLight[] MakeLandFeaturesK() =>
					map_infosK.Select(l => new LandFeatureLight
					{
						key = l.mapinK.key,
						prokey = l.mapinK.keyProject,
						deskey = l.mapinK.keyDesa,
						keluar = false,
						claim = false,
						damai = false,
						damaiB = false,
						kulit = true,
						data = new MetaDataBase(l.mapinK, l.project, l.desa),
						shapes = l.mapinK.GetShapes()?.Select(s => s.coordinates.Select(c => (geo.shared.XPointF)c).ToArray()).ToArray() ?? Array.Empty<geo.shared.XPointF[]>(),
					}).ToArray();

			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				Program.Log(ex.AllMessages());
				return (Array.Empty<DesaFeature>(), Array.Empty<LandFeatureLight>());
			}
		}

		(DesaFeature[] ds, LandFeature[] ls) GetmapC(string key, bool cutoff = false)
		{
			var persil_collection = "vPersil_and_rincik";// "persils_v2";
			var mapinfo_name = "material_mapinfo";

			MyTracer.TraceInfo2(key);
			try
			{
				contextplus.SwitchDB(cutoff);
				var keys = key?.Split(',', ';', '|') ?? new string[0]; //project keys

				var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));

				var task0 = SavedPositionWithNext(keys);//.GetAwaiter().GetResult()
				var task1 = contextplus.GetDocumentsAsync(new PersilBase(), persil_collection,
					$"<$match:<invalid:<$ne:true>, en_state:<$not:<$in:[2,'2']>>,'basic.current.keyProject':<$in:[{prokeys}]>>>".MongoJs(),
					"{$project:{_id:0,_t:1,key:1,IdBidang: 1,en_state: 1, en_proses:'$basic.current.en_proses' ,deal: 1,created: 1,basic: '$basic.current'}}");

				var task3 = contextplus.GetDocumentsAsync(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
																				"sps", "{$match:{date:{$ne:null}}}", "{$project:{_id:0, keyPersil:1, step:1, date:1 }}");

				var findst = $"{{keyProject:{{$in:['{string.Join("','", keys)}']}}}}";
				var task4a = contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vLandStatus3", findst, "{_id:0}");
				var task4b = contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vBintang_and_rincik", findst, "{_id:0}");
				var task4c = contextplus.GetCollectionsAsync(new maps.mod.LandStatus(), "vDamai", findst, "{_id:0}");

				var task5 = contextplus.GetCollectionsAsync(new MapData(), mapinfo_name, findst, "{_id:0}");
				var task6 = contextplus.GetCollectionsAsync(new DBDesaMap(), "material_desa_map", findst, "{_id:0,keyProject:0}");
				var task7 = contextplus.GetCollectionsAsync(new DBPersilMap(), "material_persil_map", findst, "{ _id:0,keyProject:0}");
				var task7a = contextplus.GetCollectionsAsync(new { project = new { key = "", identity = "" }, village = new { key = "", identity = "" } },
											"villages2", $"<'project.key':<$in:[{prokeys}]>>".MongoJs(), "{ _id:0, project:1,'village.key':1,'village.identity':1 }");
				var task7b = contextplus.GetCollectionsAsync(new { key = "", keyProject = "", cats = new string[0] },
											"vPersilCategs", findst, "{_id:0}");


				Task.WaitAll(task0, task1, task3, task4a, task4b, task4c, task5, task6, task7, task7a, task7b);

				var persils = task1.Result;
				var pskeys = persils.Select(p => p.key).ToArray();
				var lsto = task4a.Result.Join(pskeys, p => p.key, k => k, (p, k) => p).ToArray();
				var lst = lsto.ToArray().ToList();
				var lstb = task4b.Result.Join(pskeys, p => p.key, k => k, (p, k) => p).ToList();
				lstb.ForEach(x =>
					x.state = maps.mod.LandState.Overlap | (x.kategori.Contains("amai") ? maps.mod.LandState._Damai : maps.mod.LandState.___)
				);
				lst.AddRange(lstb);
				var lstc = task4c.Result;
				lstc.ForEach(x => x.state = maps.mod.LandState._Damai);
				lst.AddRange(lstc);
				var landstatus = lst
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif

#if _NOT_PARALLEL_
				List<PersilPositionWithNext> pssteppings = task0.Result;
				var pbelum = lsto.Where(p => p.status.Contains("elum"))// p.en_state == StatusBidang.belumbebas || p.en_state == StatusBidang.kampung)
					.Select(p => p.IdBidang).ToArray();
				var psudah = lsto.Select(p => p.IdBidang).Except(pbelum).ToArray();

				var corrects0 = pssteppings.Join(pbelum, s => s.IdBidang, b => b, (s, b) => s).ToArray();
				var remains = pssteppings.Join(psudah, s => s.IdBidang, b => b, (s, b) => s).ToList();

				var corrects = corrects0.Where(x => x.step != DocProcessStep.Belum_Bebas).ToArray();
				remains.AddRange(corrects0.Where(x => x.step == DocProcessStep.Belum_Bebas));

				var adjs = corrects.Any();
#if _PROBE2_
				if (adjs)
					MyTracer.TraceInfo2($"unmatched step: {string.Join(",", corrects.Select(c => c.IdBidang))}");
#endif
				foreach (var x in corrects)
				{
					x.step = DocProcessStep.Belum_Bebas;
					x.next = DocProcessStep.Baru_Bebas;
					x.position = "Belum Bebas";
				}

#if _PROBE2_
				if (adjs)
				{
					var states = corrects.Select(p => $"{p.IdBidang},{p.step},{p.position}").ToArray();
					MyTracer.TraceInfo2($"corrected steps: {string.Join(",", states)}");
				}
#endif
				if (adjs)
				{
					pssteppings = remains;
					pssteppings.AddRange(corrects);
				}

#if _PROBE2_
				MyTracer.TraceInfo2($"step: {pssteppings.FirstOrDefault(p => p.IdBidang == "B0030017")?.position}");
#endif
				var spss = task3.Result;
				var mapinfos1 = task5.Result;
				var persilmaps = task7.Result;
				var locations = task7a.Result;
				var desamaps = task6.Result.Join(locations, d => d.key, l => l.village.key, (d, l) => d).ToArray();
				var categs = task7b.Result;

#else
						var pssteppings = task0.Result.AsParallel();
						var persils = task1.Result.AsParallel();
						var spss = task3.Result.AsParallel();
						var mapinfos1 = task5.Result.AsParallel();
						var desamaps = task6.Result.AsParallel();
						var persilmaps = task7.Result.AsParallel();
				var locations = task7a.Result.AsParallel();
#endif


#if _PROBE_
				var bulky1 = landstatus.FirstOrDefault(l => l.IdBidang == "F0210151"); ;
#endif

				var mapinfos = new List<MapData2>();

#if _NOT_PARALLEL_
				;
#else
						.AsParallel();
#endif
				var psls = lsto.Join(persils, l => l.key, p => p.key, (l, p) =>
				(p, lS: l.luasSurat, lB: l.luasDibayar, en_state: l.status.Contains("udah") ? StatusBidang.bebas : StatusBidang.belumbebas)).ToArray();
				var task8 = Task.Run(() => psls.GroupJoin(pssteppings, p => p.p.key, b => b.key,
													(p, sb) => p.p.ForMap(sb.FirstOrDefault(), p.en_state, p.lS, p.lB))
												 //.GroupJoin(excluded, p => p.key, x => x, (p, sx) => p.SetPending(sx.Any()))
												 .GroupJoin(spss, p => (p.key, p.step, p.ongoing), s => (key: s.keyPersil, s.step, ongoing: true),
												 (p, ss) => p.SetSPS(ss.Any())).ToList());

				var task9 = Task.Run(() =>
				{
					var landstplus = landstatus.Join(locations, m => m.keyDesa, l => l.village.key,
						(m, l) => (mapin: m, locs: new[] { l.project.identity, l.village.identity })).ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif
					mapinfos = landstplus.GroupJoin(mapinfos1, s => s.mapin.key, i => i.key, (s, si) => new MapData2(si.FirstOrDefault(), s))
								.ToList()
#if _NOT_PARALLEL_
						;
#else
						.AsParallel();
#endif
					return mapinfos;

					//mapd = mapinfos.AsParallel().GroupJoin(desamaps, m => m.keyDesa, d => d.key,
					//		(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
					//						.ToArray();
					//return mapinfos.GroupJoin(desamaps, m => m.keyDesa, d => d.key,
					//		(m, d) => (m, desamap: d.DefaultIfEmpty(new DBDesaMap { key = m.keyDesa, map = new maps.mod.Map() }).First().map))
					//						.ToList();
				});

				Task.WaitAll(task8, task9);

				var allstates = task8.Result ?? new List<PersilForMap>();
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				var victim = allstates.FirstOrDefault();// (p => p.IdBidang == "B0030017");
				MyTracer.TraceInfo2($"step-2: {victim?.step},position:{victim?.position}");
#endif



				var map_infos = task9.Result
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				MyTracer.TraceInfo2($"pass step-2");
#endif

#if _PROBE_
						var bulky2 = allstates.FirstOrDefault(l => l.IdBidang== "F0210151");

						var bulky2a = map_desa.FirstOrDefault(l => l.m.IdBidang == "F0210151"); ;
#endif

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

#if _PROBE2_
				MyTracer.TraceInfo2($"{nameof(map_infos)} is null? {map_infos == null}; {nameof(allstates)} is null? {allstates == null}");
				MyTracer.TraceInfo2($"into step-3");
#endif
				var data1 = map_infos.GroupJoin(allstates, x => x.key, s => s.key,
									(x, ss) => (x, s: ss.FirstOrDefault()))
									.Select(y => (mapin: y.x,
											status: (y.s?.step.ToLandStateEx(y.s?.ongoing == true, y.s?.sps == true, y.s?.category == AssignmentCat.Hibah,
															y.s?.pending == true, y.s?.deal == true, y.s?.kampung == true) ?? maps.mod.LandState.Belum_Bebas)
															| y.x.state,
											nomor: y.s?.nomor
										//desamap: y.x.desamap?.ToBMap()
										)).ToArray()
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				var victim2 = data1.FirstOrDefault(p => p.mapin.IdBidang == "B0030017");
				if (victim2.mapin != null)
					MyTracer.TraceInfo2($"step-3: {victim2.status},{victim2.mapin.state}");
#endif

#if _PROBE_
						var bulky3 = data1.Where(l => !string.IsNullOrWhiteSpace(l.mapin.IdBidang)).GroupBy(l => l.mapin.IdBidang.Substring(0, 1))
							.Select(g => (key: g.Key, sample: g.First()))
							.ToArray();
#endif

				var data1a = data1.GroupJoin(persilmaps, l => l.mapin.key, pm => pm.key,
							(l, spm) => (l, pm: spm.FirstOrDefault())).ToList()
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;
#if _PROBE2_
				var victim3 = data1a.FirstOrDefault(p => p.l.mapin.IdBidang == "B0030017");
				if (victim3.l.mapin != null)
					MyTracer.TraceInfo2($"step-4: {victim3.l.status},{victim3.l.mapin.state}");
#endif

#if _PROBE_
						var bulky4 = data2.Where(l => !string.IsNullOrWhiteSpace(l.l.mapin.IdBidang)).GroupBy(l => l.l.mapin.IdBidang.Substring(0, 1))
							.Select(g => (key: g.Key, sample: g.First()))
							.ToArray();
#endif
				var data2 = data1a.GroupJoin(categs, d => d.l.mapin.key, c => c.key,
						(d, cs) => (d.l, d.pm, cat: cs.FirstOrDefault()?.cats ?? new string[0])).ToList();
#if !_NOT_PARALLEL_
				.AsParallel()
#endif
				;

#if _PROBE2_
				var victim4a = categs.FirstOrDefault(p => p.cats.Any());
				MyTracer.TraceInfo2($"step-5a: {string.Join(',', victim4a.cats)}");
				var victim4 = data2.FirstOrDefault(p => p.cat?.Any() ?? false);
				MyTracer.TraceInfo2($"step-5: {string.Join(',', victim4.cat)}");
#endif

				//var promap = data2.GroupBy(a => a.l.mapin.keyProject)
				//						.Select(g => new ProjectMap
				//						{
				//							key = g.Key,
				//							desas = g.GroupBy(g => g.l.mapin.keyDesa)
				//								.Select(x => new DesaMap
				//								{
				//									key = x.Key,
				//									project = x.First().l.mapin.project,
				//									nama = x.First().l.mapin.desa,
				//									map = x.First().l.desamap,
				//									lands = x.Select(xx => new landMap
				//									{
				//										key = xx.l.mapin.key,
				//										type = xx.l.mapin._t,
				//										meta = ((MetaData)xx.l.mapin).SetDeal(xx.l.status == maps.mod.LandState.Deal ? xx.l.mapin.deal : null)
				//														.SetNomor(xx.l.nomor).SetCats(xx.cat),
				//										map = xx.pm?.map?.ToBMap(),
				//										status = xx.l.status ?? maps.mod.LandState.___
				//									}).ToArray()
				//								}).ToArray()
				//						}).ToArray()
				//#if !_NOT_PARALLEL_
				//				.AsParallel()
				//#endif
				//;

				var dslist = lst.GroupBy(l => l.keyDesa).Select(g => g.Key).ToArray();
				//var ftdata = promap.SelectMany(p => p.MakeFeatures()).ToArray();
				var dsdata = MakeDesaFeatures(); //ftdata.OfType<DesaFeature>().ToArray();
				var lndata = MakeLandFeatures(); //ftdata.OfType<LandFeature>().ToArray();
				return (dsdata, lndata);


				DesaFeature[] MakeDesaFeatures() =>
					locations.Join(dslist, l => l.village.key, d => d, (l, d) => l).GroupJoin(desamaps, l => l.village.key, d => d.key, (l, ds) =>
													 ds.DefaultIfEmpty(new DBDesaMap { key = l.village.key, map = new maps.mod.Map() })
														.Select(y => new DesaFeature
														{
															key = y.key,
															nama = l.village.identity,
															project = l.project.identity,
															prokey = l.project.key,
															deskey = y.key,
															shapes = y.map?.ToBMap().GetShape() ?? Array.Empty<geo.shared.XPointF[]>(),
														}).First()).ToArray();


				LandFeature[] MakeLandFeatures() =>
					data2.Select(l => new LandFeature
					{
						key = l.l.mapin.key,
						prokey = l.l.mapin.keyProject,
						deskey = l.l.mapin.keyDesa,
						keluar = l.l.mapin.keluar,
						claim = l.l.mapin.claim,
						damai = l.l.mapin.damai,
						data = ((MetaData)l.l.mapin).SetDeal(l.l.status == maps.mod.LandState.Deal ? l.l.mapin.deal : null)
											.SetCats(l.cat).SetNomor<MetaData>(l.l.nomor),
						state = l.l.status,
						shapes = l.pm?.map == null ? new geo.shared.XPointF[0][] : l.pm.map.ToBMap().GetShape()
					}).ToArray();
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				Program.Log(ex.AllMessages());
				return (Array.Empty<DesaFeature>(), Array.Empty<LandFeature>());
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("/api/reporting/locations")]
		public IActionResult GetLocationList()
		{
			try
			{
				var data = contextplus.GetCollections(new { value = "", text = "", desas = new ListItem[0] }, "vUsedLocations", "{}").ToList()
									.OrderBy(l => l.text);
				return new JsonResult(data);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("/api/reporting/map-projects")]
		public IActionResult GetProjectsForMap(bool cutoff = false, int appno = 1)
		{
			try
			{
				contextplus.SwitchDB(cutoff, appno);
				var data = contextplus.GetCollections(new { value = "", text = "", desas = new ListItem[0] }, "vMapsProject", "{}").ToList()
									.OrderBy(l => l.text);
				return new JsonResult(data);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("/api/reporting/boundaries")]
		public IActionResult GetBoundaries(string keys, bool cutoff = false, int appno = 1)
		{
			try
			{
				var result = DoGetBoundaries(keys, cutoff, appno);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("/api/reporting/comp-boundaries")]
		public IActionResult GetBoundariesComp(string keys, bool cutoff = false, int appno = 1)
		{
			try
			{
				var result = DoGetBoundaries(keys, cutoff, appno);
				return ReturnCompressed(result);
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}

		Boundary[] DoGetBoundaries(string keys, bool cutoff = false, int appno = 1)
		{
			contextplus.SwitchDB(cutoff, appno);

			if (!string.IsNullOrWhiteSpace(keys))
			{
				var arkeys = string.Join(',', keys.Split(',', ';', '|').Select(s => $"'{s}'"));
				var result = contextplus.db.ExecuteSP<Boundary>("vBoundaries", $"{{$match:{{key:{{$in:[{arkeys}]}} }} }}", null);
				return result.OrderBy(b => b.project).ToArray();
			}
			return contextplus.GetCollections(new Boundary(), "vBoundaries", "{}").ToList()
								.OrderBy(l => l.project).ToArray();
		}

		public class CatNode
		{
			public string cat { get; set; }
			public CatNode[] children { get; set; }
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("/api/reporting/category-tree")]
		public IActionResult GetCategTree(bool cutoff = false, int appno = 1)
		{
			try
			{
				contextplus.SwitchDB(cutoff, appno);
				var data = contextplus.GetCollections(new { path = "" }, "vCategPath", "{}").ToList();
				return new JsonResult(data.Select(d => d.path).ToArray());
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}


		[EnableCors(nameof(landrope))]
		[HttpGet("/api/reporting/map-bintang")]
		public IActionResult GetMapBintang(string token, string keyproject, string keydesa, int appno = 2, bool cutoff = false)
		{
			var output = GMB(keyproject, keydesa, appno, cutoff);
			return new OkObjectResult(output);
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("/api/reporting/comp-map-bintang")]
		public IActionResult GetMapBintangComp(string token, string keyproject, string keydesa, int appno = 2, bool cutoff = false)
		{
			var output = GMB(keyproject, keydesa, appno, cutoff);
			return new ContentResult { ContentType = "application/octet-stream", Content = BrotEnc.ObjToOctet(output) };
		}

		BintangMapInfo[] GMB(string keyproject, string keydesa, int appno, bool cutoff)
		{
			var stages = new List<string> {
				"{$match:{en_state:{$in:[null,0,1]},'basic.current.en_proses':4}}",
				@"{$lookup: {
							 from: 'persilMaps_hibah',
							 localField: 'key',
							 foreignField: 'key',
							 as: 'pmap'
						 }}",
				"{$unwind: '$pmap'}",
				"{$addFields: {map:{$arrayElemAt: ['$pmap.entries.map.careas',-1]}}}",
				"{$project: {key:1,IdBidang:1,map:1,keyDesa:'$basic.current.keyDesa',_id:0,en_state:1}}"
			};

			var arkeypro = keyproject == null ? null : String.Join(",", keyproject.Split(new[] { ',', ';', '|', '/' }).Select(s => $"'{s}'").ToArray());
			var arkeydes = keydesa == null ? null : String.Join(",", keydesa.Split(new[] { ',', ';', '|', '/' }).Select(s => $"'{s}'").ToArray());

			var filter = (arkeypro != null) ?
				$@"<$match:<'basic.current.keyProject':<$in:[{arkeypro}]>>>".MongoJs() :
			(arkeydes != null) ?
				$@"<$match:<'basic.current.keyDesa':<$in:[{arkeydes}]>>>".MongoJs() : null;

			if (filter != null)
				stages.Insert(0, filter);

			contextplus.SwitchDB(cutoff, appno);

			var result = contextplus.GetDocuments(new BintangMapRead(), "persils_v2_hibah", stages.ToArray());
			return result.Select(r => new BintangMapInfo(r)).ToArray();
		}

		static JenisProses[] LinkReq = new JenisProses[] { JenisProses.hibah, JenisProses.overlap };
		[EnableCors(nameof(landrope))]
		[HttpPost("/api/update/bidang/proses")]

		public IActionResult UpdateProses(string token, [FromBody] UpdateProcessInfo data, int appno = 2, bool cutoff = false)
		{
			try
			{
				contextplus.SwitchDB(cutoff, appno);
				var (tokenok, res, user) = AuthController.CheckToken(contextplus, token, "CREATE_ADD_MAP,APPR1_ADD_MAP");
				if (!tokenok)
					return res;

				var impdata = contextplus.GetCollections(new { key = "" }, "securities", "{identifier:/import/}", "{key:1,_id:0}").ToList().FirstOrDefault();
				if (impdata == null)
					return new UnprocessableEntityObjectResult($"Internal process Error. User database error. Please contact support.");
				var importerkey = impdata.key;

				var persil = contextplus.persils.FirstOrDefault(p => p.IdBidang == data.IdBidang);
				if (persil == null)
					return new UnprocessableEntityObjectResult($"No Object with IdBidang '{data.IdBidang}' found");
				var basic = persil.basic.current;
				if (basic == null)
					basic = persil.basic.entries.LastOrDefault()?.item;
				if (basic == null)
					return new UnprocessableEntityObjectResult($"Object '{data.IdBidang}' has invalid properties. Aborted");
				var bintang = new { key = "" };
				var reqBintang = LinkReq.Contains((JenisProses)data.newproses);
				if (reqBintang)
				{
					bintang = contextplus.GetCollections(bintang, "persils_v2_hibah", $"{{IdBidang:'{data.linkId}'}}", "{key:1,_id:0}").ToList().FirstOrDefault();
					if (bintang == null)
						return new UnprocessableEntityObjectResult($"No Bintang Object with IdBidang '{data.linkId}' found. Aborted");
				}

				basic.en_proses = (JenisProses)data.newproses;

				var val = new ValidatableEntry<PersilBasic>
				{
					created = DateTime.Now,
					approved = true,
					en_kind = ChangeKind.Update,
					keyCreator = user.key,
					keyReviewer = importerkey,
					reviewed = DateTime.Now,
					item = basic
				};

				persil.basic.entries.Add(val);
				persil.basic.current = basic;

				var trx = contextplus.db.Client.StartSession();
				trx.StartTransaction();
				try
				{
					if (reqBintang)
					{
						var info = new RincikHibahInfo(persil.key, bintang.key, data.note);
						contextplus.db.GetCollection<RincikHibahInfo>("persils_rincik_hibah")?.InsertOne(info);
					}
					else
						contextplus.db.GetCollection<RincikHibahInfo>("persils_rincik_hibah")?.FindOneAndUpdate($"{{keyRincik:'{persil.key}'}}", $"{{$set:{{keyBintang:null,note:'{data.note}'}}}}");

					contextplus.persils.Update(persil);
					contextplus.SaveChangesNoTrans();

					trx.CommitTransaction();
				}
				catch (Exception)
				{
					trx.AbortTransaction();
					throw;
				}

				return new OkResult();
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}

		// todo list:
		//
		//
		//endpoint: /api/request/map/info/by-desa?token=xxx&keyDesa=zzz
		//result:
		//class InfoBidang
		//{
		//	public string idBidang { get; set; }
		//	public bool bebas { get; set; }
		//	public string kind { get; set; }
		//	public int luasSurat { get; set; }
		//	public int luasDibayar { get; set; }
		//	public DateTime created { get; set; }
		//	public Surat surat { get; set; }
		//	public Map[] map { get; set; }
		//}
		//class Surat
		//{
		//	public string nomor { get; set; }
		//	public string nama { get; set; }
		//}
		//class Map
		//{
		//	public geoPoint[] coordinates { get; set; }
		//}
		//class geoPoint
		//{
		//	public float longitude { get; set; }
		//	public float latitude { get; set; }
		//}
		//
		//
		//endpoint: /api/request/map/by-bidang?token=xxx&idBidang=zzz
		//result:
		//

		//
		// /api/request/map/kulit/desa


		public static StringBuilder MakeCsv<T>(T[] outdata, bool doublehead = true)
		{
			var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.ToArray();
			var sb = new StringBuilder();
			var propreg = props.Where(p => p.PropertyType.Name != "Dictionary`2" && p.SetMethod != null);
			var headers = propreg.Select(p => (px: true, p, p.Name, Label: p.GetCustomAttribute<CsvLabelAttribute>()))
										.Where(p => p.Label == null || !p.Label.Avoid)
										.ToList();
			var propdict = props.Where(p => p.PropertyType.Name == "Dictionary`2");
			var fixcols = headers.Count;

			var prheaders = headers.Select(p => "").ToList();

			var pkeys = new List<(bool px, PropertyInfo p, string Name, CsvLabelAttribute Label)>();
			var prkeys = new List<string>();
			foreach (var d in outdata)
			{
				var dicts = propdict.Select(r => (pi: r, d: r.GetValue(d) as Dictionary<(int ord, DocProcessStep head), (double p, double a)>)).ToArray();
				var dkeys = dicts.SelectMany(dd => dd.d.Keys.Select(xk => (dd.pi, h: $"{xk.head}", p: $"{dd.d[xk].p:#,##0}")));
				dkeys = dkeys.Where(dk => !pkeys.Select(p => p.Name).Contains(dk.h)).ToArray();
				pkeys.AddRange(dkeys.Select(d => (false, d.pi, d.h, (CsvLabelAttribute)null)));
				prkeys.AddRange(dkeys.Select(d => d.p));
			}
			headers.AddRange(pkeys);
			prheaders.AddRange(prkeys);
			var dictcols = headers.Count - fixcols;
			var range = Enumerable.Range(fixcols, dictcols).ToArray();

			sb.AppendJoin(",", headers.Select(h => h.Label?.Text == null ? h.Name : h.Label.Text));
			sb.AppendLine();
			if (doublehead)
			{
				sb.AppendJoin(",", prheaders);
				sb.AppendLine();
			}
			foreach (var d in outdata)
			{
				var datas = headers.Select(r => r.p.GetValue(d)).ToArray();
				var normdatas = datas.Take(fixcols).Select(dd => dd?.ToString()).ToList();
				var dictdatas = datas.Skip(fixcols).ToArray();
				var pvals = new List<string>();
				var dicts = propdict.Select(r => r.GetValue(d) as Dictionary<(int ord, DocProcessStep head), (double p, double a)>).ToArray();
				var dvals = dicts.SelectMany(dd => dd.Keys.Select(xk => (ord: xk.ord + fixcols, val: dd[xk])).Select(dv => (dv.ord, dv.val.a)));
				var adds = range.GroupJoin(dvals, r => r, d => d.ord, (r, sd) => sd.FirstOrDefault())
										.Select(x => x.a).ToArray();
				normdatas.AddRange(adds.Select(a => a.ToString()));

				sb.AppendJoin(",", normdatas);
				sb.AppendLine();
			}

			return sb;
		}
	}

	public class CsvStruct
	{
		public string project { get; set; }
		public string desa { get; set; }
		public string IdBidang { get; set; }
		public double? luas { get; set; }
		public int state { get; set; }
		public string status { get; set; }
		public double? sisa { get; set; }
		public double? overlap { get; set; }
		public string proses { get; set; }
		public string nomor { get; set; }
		public bool? keluar { get; set; }
		public bool? claim { get; set; }
		public bool? damai { get; set; }
		public bool? damaiB { get; set; }
		public bool? kulit { get; set; }
	}


}


