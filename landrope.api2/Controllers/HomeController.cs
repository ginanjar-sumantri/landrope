using ClosedXML.Excel;
using DynForm.shared;
using landrope.api2.Models;
using landrope.common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace landrope.api2.Controllers
{
	public class HomeController : ControllerBase
	{
		[HttpGet("/excel/candidate")]
		public IActionResult candidate()
		{
			try
			{
				var model = new candidateModel();
				model.Get();
				var dt = new DataTable();

				var type = typeof(PersilNextReady);
				var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
				var xcols = props.Select(p => (p, attr: p.GetCustomAttribute<GridColumnAttribute>()))
																						.Where(x => x.attr != null)
																						.Select((x, i) => (i, x.p, x.attr.Caption, x.attr.Width)).ToArray();

				foreach (var xc in xcols)
				{
					var col = new DataColumn(xc.p.Name, xc.p.PropertyType);
					col.AllowDBNull = true;
					col.Caption = xc.Caption;
					dt.Columns.Add(col);
				}
				var lcol = new DataColumn("detail", typeof(string));
				lcol.Caption = "Detail Bidang";
				dt.Columns.Add(lcol);


				var path = $"{Request.Scheme}://{Request.Host}/{Request.PathBase}";
				if (!path.EndsWith("/"))
					path += "/";
				var lobjs = new List<object>();
				foreach (var i in model.Items)
				{//.ForEach(i => {
					lobjs.Clear();
					foreach (var xc in xcols)
						lobjs.Add(xc.p.GetValue(i));
					lobjs.Add("Download");
					dt.Rows.Add(lobjs.ToArray());
				}//);


				var outstrm = new MemoryStream();
				var doc = new XLWorkbook();
				var ws = doc.Worksheets.Add(dt, "Kandidat Penugasan");
				var xitems = model.Items.Select((n, i) => (n, i)).ToArray();
				foreach (var x in xitems)
				{
					var cell = ws.Row(x.i + 2).Cell(xcols.Length + 1);
					cell.Hyperlink.ExternalAddress =
							new Uri($"{path}excel/cand/dtl/{x.n.keyDesa}/{x.n.keyPTSK ?? x.n.keyPenampung}/{x.n.disc}/{x.n._step}");
					cell.SetValue<string>("Detail bidang");
				}
				/*			var xrow = ws.Rows().First();
							foreach (var xc in xcols)
							{
								var cell = xrow.Cell(xc.i + 1);
								cell.SetDataType(XLDataType.Text);
								cell.SetValue(xc.Caption);
							}*/
				doc.SaveAs(outstrm);
				outstrm.Seek(0, SeekOrigin.Begin);

				return new FileContentResult(outstrm.ToArray(), MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
			}
			catch(Exception ex)
			{
				var st = ex.Message;
				while(ex.InnerException!=null)
				{
					ex = ex.InnerException;
					st+=" > " + ex.Message;
				}
				return new UnprocessableEntityObjectResult(st);
			}
		}

		[HttpGet("/excel/cand/dtl/{keyDesa}/{keyComp}/{disc}/{_step}")]
		public IActionResult candidateDtl([FromRoute]string keyDesa, [FromRoute] string keyComp, [FromRoute] string disc, [FromRoute] string _step)
		{
			try
			{
				var step = Enum.TryParse<DocProcessStep>(_step, out DocProcessStep stp) ? stp : DocProcessStep.Belum_Bebas;
				if (step == DocProcessStep.Belum_Bebas)
					return new UnprocessableEntityObjectResult("Proses dimaksud tidak dikenali");

				disc = disc switch
				{
					"Hibah" => "PersilHibah",
					_ => "persil" + disc
				};

				var model = new candidateDtlModel();
				model.Get(keyDesa, keyComp, disc, step);
				var dt = new DataTable();

				var type = typeof(PersilView);
				var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
				var xcols = props.Select(p => (p, attr: p.GetCustomAttribute<GridColumnAttribute>()))
																						.Where(x => x.attr != null)
																						.Select((x, i) => (i, x.p, x.attr.Caption, x.attr.Width)).ToArray();

				foreach (var xc in xcols)
				{
					var col = new DataColumn(xc.p.Name,
							xc.p.PropertyType.Name.StartsWith("Nullable") ? xc.p.PropertyType.GenericTypeArguments[0] : xc.p.PropertyType);
					col.AllowDBNull = true;
					/*				col.ExtendedProperties.Add("number", numbertypes.Contains(col.DataType));*/
					col.Caption = xc.Caption;
					dt.Columns.Add(col);
				}


				var lobjs = new List<object>();
				foreach (var i in model.Items)
				{//.ForEach(i => {
					lobjs.Clear();
					foreach (var xc in xcols)
						lobjs.Add(xc.p.GetValue(i));
					dt.Rows.Add(lobjs.ToArray());
				}//);

				var numbertypes = new[] { typeof(double), typeof(decimal) };
				var datetypes = new[] { typeof(DateTime) };

				var numcols = xcols.Select(x => (x.i, t: x.p.PropertyType.Name.StartsWith("Nullable") ? x.p.PropertyType.GenericTypeArguments[0] : x.p.PropertyType))
								.Select(x => (x.i, n: numbertypes.Contains(x.t)))
								.Where(x => x.n).Select(x => x.i).ToArray();
				var ncols = xcols.Where(x => numcols.Contains(x.i));
				var datcols = xcols.Select(x => (x.i, t: x.p.PropertyType.Name.StartsWith("Nullable") ? x.p.PropertyType.GenericTypeArguments[0] : x.p.PropertyType))
								.Select(x => (x.i, n: datetypes.Contains(x.t)))
								.Where(x => x.n).Select(x => x.i).ToArray();
				var dcols = xcols.Where(x => datcols.Contains(x.i));

				var outstrm = new MemoryStream();
				var doc = new XLWorkbook();
				var ws = doc.Worksheets.Add(dt, "Detail Kandidat");
				foreach (var xc in ncols)
				{
					var column = ws.Column(xc.i + 1);
					column.Style.NumberFormat.SetFormat("#,##0");
				}
				foreach (var xc in dcols)
				{
					var column = ws.Column(xc.i + 1);
					column.Style.DateFormat.SetFormat("d mmm yyyy");
				}
				doc.SaveAs(outstrm);
				outstrm.Seek(0, SeekOrigin.Begin);

				return new FileContentResult(outstrm.ToArray(), MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
			}
			catch(Exception ex)
			{
				var st = ex.Message;
				while(ex.InnerException!=null)
				{
					ex = ex.InnerException;
					st+=" > " + ex.Message;
				}
				return new UnprocessableEntityObjectResult(st);
			}
		}
	}
}
