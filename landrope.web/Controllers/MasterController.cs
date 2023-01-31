using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using landrope.mod;
using MongoDB.Driver;
using Newtonsoft.Json;
using landrope.web.Models;
using landrope.mod2;
//using Google.Apis.Json;
using APIGrid;
using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;
using geo.shared;
using landrope.common;
using System.Reflection;
using mongospace;

namespace landrope.web.Controllers
{
	[ApiController]
	[Route("api/master")]


	public class MasterController : ControllerBase

	{
		ExtLandropeContext context = Contextual.GetContextExt();

		//[NeedToken("PRABEBAS_VIEW,PASKABEBAS_VIEW")]
		[EnableCors(nameof(landrope))]
		[HttpGet("locations")]
		public IActionResult GetLocationList()

		{
			try
			{
				var data = context.GetDocuments(new { value = "", text = "", desas = new ListItem[0] }, "maps",
"{$unwind: '$villages'}",
"{$group: { _id: '$key', text: {$first: '$identity'},desas: {$push: { value: '$villages.key',text: '$villages.identity'} } } }",
"{$project: { _id: 0,value: '$_id',text: 1, desas: 1} }"
									)
									.ToList().OrderBy(l => l.text);
				return new JsonResult(data);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}


		[EnableCors(nameof(landrope))]
		[HttpGet("projects")]
		public IActionResult GetProjectList()

		{
			LandropeContext mcontext = Contextual.GetContext();
			try
			{
				var data = mcontext.GetCollections(new { key = "", identity = "" }, "maps", "{}", "{_id:0,key:1,identity:1}").ToList()
											.Select(p => new cmnItem { key = p.key, name = p.identity }).OrderBy(p => p.name).ToList();
				return new JsonResult(data);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("desas")]
		public IActionResult GetDesaList(string prokey)

		{
			LandropeContext mcontext = Contextual.GetContext();
			try
			{
				var sample = new { key = "", proj = "", identity = "" };
				var data = (prokey == null) ?
					mcontext.GetCollections(sample, "villages", "{invalid:{$ne:true}}",
																					"{_id:0,key:'$village.key',proj:'$project.identity', identity:'$village.identity'}").ToList()
											.Select(p => new cmnItem { key = p.key, name = $"{p.proj}-{p.identity}" }).ToList() :
					mcontext.GetCollections(sample, "villages", $"{{'project.key':'{prokey}',invalid:{{$ne:true}}}}",
																				"{_id:0,key:'$village.key',proj:'$project.identity', identity:'$village.identity'}").ToList()
										.Select(p => new cmnItem { key = p.key, name = $"{p.identity}" }).ToList();
				return new JsonResult(data.OrderBy(d => d.name).ToArray());
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}


		[EnableCors(nameof(landrope))]
		[HttpGet("notarists")]
		public IActionResult GetNotarisList()

		{
			try
			{
				var data = context.db.GetCollection<Notaris>("masterdatas").Find("{_t:'notaris',invalid:{$ne:true}}").ToList()
											.Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();
				return new JsonResult(data);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("companies")]
		public IActionResult GetCompanyList(StatusPT status)

		{
			try
			{
				var data = context.db.GetCollection<Company>("masterdatas").Find("{_t:'pt',invalid:{$ne:true}}").ToList()
											.Where(p => p.status == status)
											.Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();
				return new JsonResult(data);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("ptsks")]
		public IActionResult GetPTSKList(StatusPT status)

		{
			try
			{
				var data = context.ptsk.Query(x => x.invalid != true).ToList()
											.Where(p => p.status == status)
											.Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();
				return new JsonResult(data);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("jenis-proses")]
		public IActionResult GetProcess()
			=> Ok(Enum.GetValues(typeof(JenisProses)).Cast<JenisProses>()
				 .Select(v => new cmnItem { key = $"{(int)v}", name = v.ToString("g") }).ToArray());

		[EnableCors(nameof(landrope))]
		[HttpGet("jenis-alashak")]
		public IActionResult GetJenisAlasHak()
			=> Ok(Enum.GetValues(typeof(JenisAlasHak)).Cast<JenisAlasHak>()
					.Where(v => v != JenisAlasHak.unknown)
					.Select(v => new cmnItem { key = $"{(int)v}", name = v.ToString("g") }).ToArray());

		[EnableCors(nameof(landrope))]
		[HttpGet("jenis-lahan")]
		public IActionResult GetLahan()
			=> Ok(Enum.GetValues(typeof(JenisLahan)).Cast<JenisLahan>()
					.Where(v => v != JenisLahan.unknown)
					.Select(v => new cmnItem { key = $"{(int)v}", name = v.ToString("g") }).ToArray());


		[EnableCors(nameof(landrope))]
		[HttpGet("persil-steps")]
		public IActionResult GetSteps(string jenisHak = null, bool hibah = false)
		{
			JenisAlasHak ah = JenisAlasHak.unknown;
			Enum.TryParse(jenisHak, out ah);

			Type type = null;
			switch (ah)
			{
				case JenisAlasHak.hgb: type = typeof(PersilHGB); break;
				case JenisAlasHak.shm: type = typeof(PersilSHM); break;
				case JenisAlasHak.shp: type = typeof(PersilSHP); break;
				case JenisAlasHak.girik when hibah:
					type = typeof(PersilGirik); break;
				case JenisAlasHak.girik when !hibah:
					type = typeof(PersilHibah); break;
			}
			if (type != null)
			{
				var attribs1 = type.GetProperties().Select(p => p.GetCustomAttributes(typeof(StepAttribute), true).FirstOrDefault())
													.Where(a => a != null).Cast<StepAttribute>().OrderBy(a => a.order)
													.Select(a => new { a.order, a.name, a.descr }).ToList();
				return Ok(attribs1);
			}
			var attribs = new[] { typeof(PersilGirik), typeof(PersilSHM), typeof(PersilSHP), typeof(PersilHGB), typeof(PersilHibah) }
												.SelectMany(t => t.GetProperties())
												.Select(p => p.GetCustomAttributes(typeof(StepAttribute), true).FirstOrDefault())
												.Where(a => a != null).Cast<StepAttribute>().OrderBy(a => a.order)
												.Select(a => new { a.order, a.name, a.descr }).Distinct().ToList();
			return Ok(attribs);
		}

		//static IEnumerable<(int order, string name, string descr)> GetStepsX(Persil persil)
		//{
		//  return persil.GetType().GetProperties().Select(p => p.GetCustomAttributes(typeof(StepAttribute), true)
		//                          .FirstOrDefault())
		//                    .Where(a => a != null).Cast<StepAttribute>().OrderBy(a => a.order)
		//                    .Select(a => (a.order, a.name, a.descr));
		//}

		//internal static IEnumerable<(int order, string name, string descr)> GetStepsX(ExtLandropeContext context, string key,
		//                auth.mod.user user, bool rejectonly = false)
		//{
		//  var persil = context.persils.FirstOrDefault(p => p.key == key);
		//  if (persil == null)
		//    return new (int order, string name, string descr)[0];
		//  if (user == null)
		//    return GetStepsX(persil);

		//  var privs = user.getPrivileges(null).Select(a => a.identifier);
		//  var steps = persil.GetType().GetProperties().Select(p => (p, attr: p.GetCustomAttributes(typeof(StepAttribute), true)
		//                          .FirstOrDefault() as StepAttribute))
		//                    .Where(x => x.attr != null &&
		//                        (!rejectonly || (((ValidatableShell)x.p.GetValue(persil))?.isRejected() ?? false))).OrderBy(x => x.attr.order)
		//                    .Select(x => new { x.attr.order, x.attr.name, x.attr.descr, privs = x.attr.privs.Split(',') })
		//                    .Where(s => s.privs.Intersect(privs).Any());
		//  return steps.Select(s => (s.order, s.name, s.descr)).ToList();
		//}

		[EnableCors(nameof(landrope))]
		[HttpGet("persil-steps/bykey")]
		public IActionResult GetSteps2(string key)
		{
			var steps = context.GetStepsX(key, null).Select(a => new { a.order, a.name, a.descr }).ToList();
			return Ok(steps);
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("persil-steps/bykey-ex")]
		public IActionResult GetStepsEx(string token, string key, bool rejectonly = false)
		{
			try
			{
				if (token == null)
					return new StatusCodeResult(403);
				var user = context.FindUser(token);
				if (user == null)
					return NoContent();

				var persil = context.persils.FirstOrDefault(p => p.key == key);
				if (persil == null)
					return NoContent();

				var steps = context.GetStepsX(key, user, rejectonly).Select(a => new { a.order, a.name, a.descr }).ToList();
				if (user != null)
				{
					int pos = 0;
					if (user.getPrivileges(null).Any(a => a.identifier == "MAP_FULL"))
						steps.Insert(pos++, new { order = 0, name = "map", descr = "Peta Bidang" });
					if (user.getPrivileges(null).Any(a => a.identifier == "MAP_REVIEW"))
					{
						var res = context.GetDocuments(new { reviewed = (DateTime?)null }, "persilMaps",
							$"{{$match: {{ key: '{key}'}} }}",
							"{$project: { key: '$key',entcount: {$size: '$entries'},entries: '$entries'} }",
							"{$match: { entcount: {$gt: 0} } }",
							"{$project: { last: {$cond:[{$gt:['$entcount', 1]},{$arrayElemAt:['$entries', -1]},{$arrayElemAt:['$entries', 0]} ]} } }",
							"{$project: { reviewed: '$last.reviewed',_id: 0} }"
						).FirstOrDefault();
						if (res != null && res.reviewed == null)
							steps.Insert(pos, new { order = 1, name = "mapval", descr = "Validasi Peta" });
					}
				}
				return Ok(steps);
			}
			catch (UnauthorizedAccessException ex1)
			{
				var stcode = ex1.Message;
				if (int.TryParse(stcode, out int code))
					return new StatusCodeResult(code);
				else
					return new UnauthorizedResult();
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		/// <summary>
		/// Get Workers By Type
		/// </summary>
		[EnableCors(nameof(landrope))]
		[HttpGet("workers/type")]
		public IActionResult GetWorkerByType(WorkerType type)
		{
			try
			{
				var data = context.db.GetCollection<Worker>("workers").Find("{_t:'worker',invalid:{$ne:true}}").ToList()
											.Where(p => p.type == type);
				var result = data.Select(x => x.ToView());
				return new JsonResult(result);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}


		/// <summary>
		/// Get Worker By they Parent
		/// </summary>
		[EnableCors(nameof(landrope))]
		[HttpGet("workers/parent")]
		public IActionResult GetWorkerByHierarchy([FromQuery] string token, string workerParent)
		{
			try
			{
				var data = context.db.GetCollection<Worker>("workers").Find("{_t:'worker',invalid:{$ne:true}}").ToList()
											.Where(p => p.WorkerParent == workerParent.Trim());
				var result = data.Select(x => x.ToView());
				return new JsonResult(result);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("internals")]
		public IActionResult GetInternalList()
		{
			try
			{
				var data = context.db.GetCollection<mod2.Internal>("masterdatas").Find("{_t:'internal',invalid:{$ne:true}}").ToList()
											.Select(p => new cmnItem { key = p.key, name = $"{p?.salutation}, {p?.identifier}" }).ToList();
				return new JsonResult(data);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("assign/pic/list")]
		public IActionResult GetAssignmentPicsList([FromQuery]string token )
		{
			try
			{
				var internals = context.db.GetCollection<mod2.Internal>("masterdatas")
										  .Find("{_t:'internal',invalid:{$ne:true}}").ToList()
										  .OrderBy(p => p.identifier)
										  .Select(p => new cmnItem { key = p.key, name = $"Internal - {p?.salutation} {p?.identifier}" }).ToList();

				var notarists = context.db.GetCollection<Notaris>("masterdatas")
									      .Find("{_t:'notaris',invalid:{$ne:true}}").ToList()
										  .OrderBy(p => p.identifier)
										  .Select(p => new cmnItem { key = p.key, name = $"Notaris - {p?.identifier}" }).ToList();

				var pics = internals.Union(notarists);

				return new JsonResult(pics);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("assign/pic/get")]
		public IActionResult GetAssignPicByKey([FromQuery] string token, [FromQuery] string picKey)
		{
			try
			{
				if (string.IsNullOrEmpty(picKey))
					return new UnprocessableEntityObjectResult("Mohon untuk mengisi parameter picKey");

				var internals = context.db.GetCollection<mod2.Internal>("masterdatas")
										  .Find("{_t:'internal',invalid:{$ne:true}, key:'"+ picKey +"'}").ToList()
										  .OrderBy(p => p.identifier)
										  .Select(p => new cmnItem { key = p.key, name = $"Internal - {p?.salutation} {p?.identifier}" }).ToList();

				var notarists = context.db.GetCollection<Notaris>("masterdatas")
										  .Find("{_t:'notaris',invalid:{$ne:true}, key:'" + picKey + "'}").ToList()
										  .OrderBy(p => p.identifier)
										  .Select(p => new cmnItem { key = p.key, name = $"Notaris - {p?.identifier}" }).ToList();

				var pics = internals.Union(notarists);

				return new JsonResult(pics);
			}
			catch (UnauthorizedAccessException exa)
			{
				return new ContentResult { StatusCode = int.Parse(exa.Message) };
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult($"Gagal {ex.Message}, harap beritahu support");
			}
		}

	}
}