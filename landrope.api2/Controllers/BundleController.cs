using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using APIGrid;
using auth.mod;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DynForm.shared;
using landrope.api2.Models;
using landrope.common;
using landrope.documents;
using landrope.engines;
using landrope.hosts;
using landrope.material;
using landrope.mod2;
using landrope.mod3;
using landrope.mod3.shared;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Tracer;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Serializers;
using BundlerConsumer;
using AssignerConsumer;
using GraphConsumer;
using landrope.consumers;
using MongoDB.Bson;

namespace landrope.api2.Controllers
{
	[Route("api/bundle")]
	[ApiController]
	[EnableCors(nameof(landrope))]
	public class BundleController : ControllerBase
	{
		mod3.LandropePlusContext context;// = Contextual.GetContextPlus();
		IServiceProvider services;
		BundlerHostConsumer bhost;
		AssignerHostConsumer ahost;
		GraphHostConsumer ghost;

		public BundleController(IServiceProvider services)
		{
			this.services = services;
			this.context = services.GetService<mod3.LandropePlusContext>();
			ghost = services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
			ahost = services.GetService<IAssignerHostConsumer>() as AssignerHostConsumer;
			bhost = services.GetService<IBundlerHostConsumer>() as BundlerHostConsumer;
		}

		[NeedToken("BUNDLE_VIEW,BUNDLE_FULL,BUNDLE_REVIEW,BPN_CREATOR,NOT_CREATOR,ARCHIVE_FULL")]
		[HttpGet("main/list")]
		public IActionResult MainList([FromQuery] string token, [FromQuery] AgGridSettings gs)
		{
			try
			{
				var data = context.GetCollections(new MainBundleView(), "material_main_bundle_core", "{}", "{_id:0}").ToList();
				//By Ricky pindah ke View dar view main_bundle_core_m_20221006-----
				//	.Where(x => !string.IsNullOrEmpty(x.project) || !string.IsNullOrEmpty(x.desa));

				//var persils = context.GetDocuments(new { key = "", nosurat = "" }, "persils_v2",
				//	"{$match:{en_state:{$in:[null,0,1]}}}", "{$project:{key:1,nosurat:'$basic.current.surat.nomor',_id:0}}").ToList();
				//data = data.Join(persils, b => b.key, p => p.key, (b, p) => b.SetNoSurat(p.nosurat)).ToList();
				//------------------------------------------------------------------
				return Ok(data.GridFeed(gs));
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}

		[NeedToken("BUNDLE_VIEW, BUNDLE_FULL, BUNDLE_REVIEW,ARCHIVE_FULL")]
		[HttpGet("main/get")]
		public IActionResult MainGet(string token, string key)
		{
			var bundle = bhost.MainGet(key).GetAwaiter().GetResult() as MainBundle;
			var curdir = ControllerContext.GetContentRootPath();
			var core = new MainBundleCore();
			bundle.ToCore(core);
			var layout = landrope.layout.LayoutMaster.LoadLayout(typeof(MainBundle).Name, curdir, new string[0]);
			var rcontext = new DynamicContext<MainBundleCore>(layout, core);
			return Ok(rcontext);
		}

		[NeedToken("BUNDLE_FULL")]
		[HttpPost("main/save")]
		[MainBundleMaterializer(Auto = false)]
		public IActionResult MainSave(string token, [FromBody] MainBundleCore core)
		{
			if (core == null)
				return new NotModifiedResult("Invalid data to save");

			MethodBase.GetCurrentMethod().SetKeyValue<MainBundleMaterializerAttribute>(core.key);

			var bundle = bhost.MainGet(core.key).GetAwaiter().GetResult() as MainBundle;
			if (bundle == null)
				return new NotModifiedResult("Invalid data to save");
			bundle.FromCore(core);
			bhost.MainUpdate(bundle).Wait();
			/*			context.mainBundles.Update(bundle);
						context.SaveChanges();*/
			//MethodBase.GetCurrentMethod().GetCustomAttribute<MainBundleMaterializerAttribute>()
			//	.ManualExecute(context, core.key);
			return Ok();
		}

		[NeedToken("BUNDLE_VIEW, BUNDLE_FULL, BUNDLE_REVIEW,ARCHIVE_FULL")]
		[HttpGet("main/dtl/list")]
		public IActionResult MainDtlList(string token, string bundkey, [FromQuery] AgGridSettings gs)
		{
			try
			{
				var bundle = bhost.MainGet(bundkey).GetAwaiter().GetResult() as MainBundle;

				if (bundle == null)
					return new UnprocessableEntityObjectResult($"bundle with key '{bundkey}' was not found");

				var ddata = bundle.doclist.ToArray();
				var data = ddata.Select(d => d.ToView()).ToArray();

				return Ok(data.GridFeed(gs));
				//return Ok(new
				//{
				//	total = 1,
				//	page = 1,
				//	records = data.Length,
				//	rows = data,
				//	tm = 0
				//});
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}

		[NeedToken("BUNDLE_VIEW,BUNDLE_FULL,BUNDLE_REVIEW,ARCHIVE_FULL,PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT")]
		[HttpGet("main/dtl/get")]
		public IActionResult MainGetDtl(string token, string bundkey, string dtkey)
		{
			var bundle = bhost.MainGet(bundkey).GetAwaiter().GetResult() as MainBundle;
			if (bundle == null)
				return new NoContentResult();
			var data = bundle.doclist.FirstOrDefault(d => d.keyDocType == dtkey);
			var layout = data.MakeLayout();
			var fdata = data.ToCore();
			var dcontext = new DynamicContext<RegisteredDocCore>(layout, fdata);

			return Ok(dcontext);
		}

		[NeedToken("BUNDLE_FULL")]
		[HttpPost("main/dtl/save")]
		[MainBundleMaterializer(Auto = false)]
		public IActionResult MainSaveDtl(string token, string bundkey, [FromBody] RegisteredDocCore core, LogActivityModul sourceModul = LogActivityModul.Bundle)
		{
			var user = context.FindUser(token);
			var timestamp = DateTime.Now;
			if (core == null)
				return new NotModifiedResult("Invalid data to save");
			MethodBase.GetCurrentMethod().SetKeyValue<MainBundleMaterializerAttribute>(bundkey);
			var bundle = bhost.MainGet(bundkey).GetAwaiter().GetResult() as MainBundle;
			if (bundle == null)
				return new NotModifiedResult("Invalid data to save");
			BundledDoc docex = bundle.doclist.FirstOrDefault(d => d.keyDocType == core.keyDocType);
			if (docex == null)
				return new NotModifiedResult("Invalid data to save");
			docex.FromCore(core, user, timestamp);
			var log = new LogBundle(core, user, bundle.key, timestamp, LogActivityType.Metadata, sourceModul);
			context.logBundle.Insert(log);
			context.SaveChanges();

			bhost.MainUpdate(bundle, true).Wait();
			/*			context.mainBundles.Update(bundle);
						context.SaveChanges();
			*/
			//MethodBase.GetCurrentMethod().GetCustomAttribute<MainBundleMaterializerAttribute>()
			//	.ManualExecute(context, bundkey);

			return Ok();
		}

		[NeedToken("TASK_VIEW, TASK_FULL, TASK_REVIEW, BUNDLE_VIEW, BUNDLE_FULL, BUNDLE_REVIEW")]
		[HttpGet("task/list/by-assign")]
		public IActionResult TaskList1([FromQuery] string token, [FromQuery] string asgnkey, [FromQuery] AgGridSettings gs)
		{
			var bhost = ControllerContext.HttpContext.RequestServices.GetService<IBundlerHostConsumer>();
			var bundles = bhost.TaskList(asgnkey).GetAwaiter().GetResult().Cast<TaskBundle>();
			var data = bundles.Select(b => b.ToView(bhost)).ToArray();
			return Ok(data.GridFeed(gs));
		}

		[NeedToken("BUNDLE_VIEW, BUNDLE_FULL, BUNDLE_REVIEW")]
		[HttpGet("task/list/by-main")]
		public IActionResult TaskList2([FromQuery] string token, [FromQuery] string mainkey, [FromQuery] AgGridSettings gs)
		{
			var bhost = ControllerContext.HttpContext.RequestServices.GetService<IBundlerHostConsumer>();
			var bundles = bhost.ChildrenList(mainkey).GetAwaiter().GetResult().Cast<TaskBundle>();
			var data = bundles.Select(b => b.ToView(bhost)).ToArray();
			return Ok(data.GridFeed(gs));
		}

		[NeedToken("BPN_CREATOR, NOT_CREATOR")]
		[HttpGet("list/parameters")]
		public IActionResult ParamList(string token, string key)
		{
			var persil = context.persils.FirstOrDefault(p => p.key == key);
			if (persil == null)
				return new UnprocessableEntityObjectResult("Bidang dimaksud tidak ada");
			var disc = persil.Discriminator;
			var steps = Assignment.StepOrders.TryGetValue(disc, out (int order, DocProcessStep step)[] stp) ? stp : null;
			if (steps == null)
				return new UnprocessableEntityObjectResult("Kategori bidang tidak diketahui");
			var dsteps = steps.OrderBy(s => s.order).Select(s => s.step);
			var dtypes = dsteps.Join(StepDocType.List, d => (disc, d), s => (s.disc, s._step), (d, s) => (d, s.send))
						.SelectMany(x => x.send.Select(s => (step: x.d, s.keyDocType)))
						.Join(DocType.List, x => x.keyDocType, d => d.key, (x, d) => (x.step, d.key, d.identifier, d.metadata));
			var props = dtypes.SelectMany(d => d.metadata.Where(m => m.req).Select(m => (d.key, metakey: m.key)));
			var param = new
			{
				process = dsteps.Select(s => new option { keyparent = null, key = s.ToString("g"), identity = s.GetName() }).ToArray(),
				doctypes = dtypes.Select(d => new option { keyparent = d.step.ToString("g"), key = d.key, identity = d.identifier }).ToArray(),
				props = props.Select(p => new option
				{
					keyparent = p.key,
					key = p.metakey.ToString("g"),
					identity = p.metakey.ToString("g").Replace("_", " ")
				}).ToArray()
			};
			return Ok(param);
		}

		/*		Dictionary<string,option[]> MakeDocExtras(string key, DocProcessStep step, string[] listname)
				{
					var persil = context.persils.FirstOrDefault(p => p.key == key);
					if (persil == null)
						throw new InvalidFilterCriteriaException("Bidang dimaksud tidak ada");

					var disc = persil.Discriminator;
					var steps = Assignment.StepOrders.TryGetValue(disc, out (int order, DocProcessStep step)[] stp) ? stp : null;
					if (steps == null)
						throw new InvalidOperationException("Kategori bidang tidak diketahui");
					if (!steps.Any(s=>s.step==step))
						throw new InvalidOperationException("Proses tidak dikenal");

					var dtypes = StepDocType.GetItem(step, disc)?.receive.Join(DocType.List.Where(s => s.invalid != true), s => s.keyDocType, d => d.key,
											(s, d) => (s.keyDocType, d.identifier,mkeys: d.metadata.Select(m => m.key).ToArray()));

					//.Select(r => r.keyDocType)
						//		.Join(DocType.List, x => x.keyDocType, d => d.key, (x, d) => (x.step, d.key, d.identifier, d.metadata));
					var props = dtypes.SelectMany(d => d.metadata.Where(m => m.req).Select(m => (d.key, metakey: m.key)));
					var param = new
					{
						process = dsteps.Select(s => new option { keyparent = null, key = s.ToString("g"), identity = s.GetName() }).ToArray(),
						doctypes = dtypes.Select(d => new option { keyparent = d.step.ToString("g"), key = d.key, identity = d.identifier }).ToArray(),
						props = props.Select(p => new option
						{
							keyparent = p.key,
							key = p.metakey.ToString("g"),
							identity = p.metakey.ToString("g").Replace("_", " ")
						}).ToArray()
					};
				}*/

		[NeedToken("BPN_CREATOR,NOT_CREATOR")]
		[HttpPost("doc/request/save")]
		[Consumes("application/json")]
		public IActionResult SaveRequest([FromQuery] string token, [FromQuery] string oper, [FromBody] DocRequestParam param)
		{
			user user;
			MainBundle bundle;
			BundledDoc doc;
			try
			{
				user = context.FindUser(token);
				bundle = bhost.MainGet(param.key).GetAwaiter().GetResult() as MainBundle;
				if (bundle == null)
					return new UnprocessableEntityObjectResult("Bundle tidak ada");
				doc = bundle.doclist.FirstOrDefault(d => d.keyDocType == param.keyDocType);
				if (doc == null)
					return new UnprocessableEntityObjectResult("Jenis dokumen tidak dikenali untuk bundle ini");
				return oper switch
				{
					"add" => Add(),
					"del" => Del(),
					_ => Edit()
				};
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}

			IActionResult Edit()
			{
				var d = doc.SpecialReqs.FirstOrDefault(s => s.step == param._step && s.prop.Key == param.propkey);
				if (d != null)
				{
					d.prop = new KeyValuePair<string, Dynamic>(param.propkey, new Dynamic(param.value));
					d.exis = param.exis;
					d.sec_exis = param.sec_exis;

					bhost.MainUpdate(bundle).Wait();
					/*					context.mainBundles.Update(bundle);
										context.SaveChanges();
					*/
					return Ok();
				}
				else
					return new UnprocessableEntityObjectResult("Request dokumen dimaksud tidak ada");
			}

			IActionResult Del()
			{
				var d = doc.SpecialReqs.FirstOrDefault(s => s.step == param._step && s.prop.Key == param.propkey);
				if (d != null)
				{
					doc.SpecialReqs.Remove(d);
					bhost.MainUpdate(bundle).Wait();
					/*					context.mainBundles.Update(bundle);
										context.SaveChanges();*/
					return Ok();
				}
				else
					return new UnprocessableEntityObjectResult("Request dokumen dimaksud tidak ada");
			}

			IActionResult Add()
			{
				var exists = doc.SpecialReqs.Any(s => s.step == param._step && s.prop.Key == param.propkey);
				if (exists)
					return new UnprocessableEntityObjectResult("Request dokumen dimaksud sudah ada");
				var req = new ProcessReq
				{
					step = param._step,
					prop = new KeyValuePair<string, Dynamic>(param.propkey, new Dynamic(param.value)),
					exis = param.exis,
					sec_exis = param.sec_exis
				};
				doc.SpecialReqs.Add(req);
				bhost.MainUpdate(bundle).Wait();
				/*				context.mainBundles.Update(bundle);
								context.SaveChanges();*/
				return Ok();
			}
		}

		/*		[HttpGet("save-lastpos")]
				public IActionResult SaveLastPosition()
				{
					var host = HostServicesHelper.GetBundleHost(ControllerContext.HttpContext.RequestServices);
					(var OK, var error) = host.SaveLastPositionDiscete();
					if (OK)
						return Ok();
					return new UnprocessableEntityObjectResult(error);
				}
		*/
		[HttpGet("save-nextproc")]
		public IActionResult SavePersilNexts()
		{
			bhost.ReloadData().GetAwaiter().GetResult();
			ahost.AssignReload();
			(var OK, var error) = bhost.SaveNextStepDiscrete().GetAwaiter().GetResult();
			if (OK)
				return Ok();
			return new UnprocessableEntityObjectResult(error);
		}

		[NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT,MAP_FULL,MAP_REVIEW")]
		[HttpGet("main/dtl/list-deal")]
		public IActionResult MainDtlListDeal(string token, string bundkey, [FromQuery] AgGridSettings gs)
		{
			try
			{
				var user = context.FindUser(token);
				var privs = user.privileges.Select(p => p.identifier).ToArray();
				var bundle = bhost.MainGet(bundkey).GetAwaiter().GetResult() as MainBundle;

				if (bundle == null)
					return new UnprocessableEntityObjectResult($"bundle with key '{bundkey}' was not found");

				var privScanDocs = context.GetDocuments(new DocsAccess(), "static_collections", "{$match: {'_t': 'praDealDocAcceess'}}", "{$project: {_id:0,_t:0 }}").ToList();
				var privScanDoc = privScanDocs.Where(a => a.privs.Intersect(privs).Any()).FirstOrDefault();
				var allowedDocs = privScanDoc?.jnsDoks ?? new string[0];

				var ddata = bundle.doclist.ToArray();
				var data = ddata.Select(d => d.ToView(allowedDocs)).ToArray();

				PreBundle preBundle = bhost.PreBundleGet("templatePreBundle").GetAwaiter().GetResult() as PreBundle;
				var preBundleDocList = preBundle.doclist.Select(doc => doc.keyDocType).ToList();

				data = data.Where(x => preBundleDocList.Contains(x.keyDocType)).ToArray();

				return Ok(data.GridFeed(gs));
			}
			catch (UnauthorizedAccessException exa)
			{
				return new ContentResult { StatusCode = int.Parse(exa.Message) };
			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}
	}
}