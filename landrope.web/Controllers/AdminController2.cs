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
//using GridMvc.Server;

namespace landrope.web.Controllers
{
	[ApiController]
	[Route("api/admin")]
	
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'AdminController2'
	public class AdminController2 : ControllerBase
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'AdminController2'
	{
		ExtLandropeContext context = Contextual.GetContextExt();

		//[NeedToken("PRABEBAS_VIEW,PASKABEBAS_VIEW")]
		[HttpGet("group/list")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'AdminController2.GetGroupList(AgGridSettings)'
		public IActionResult GetGroupList(AgGridSettings gs)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'AdminController2.GetGroupList(AgGridSettings)'
		{
			try
			{
				var data = context.db.GetCollection<Group>("groups")
											.Find("{invalid:{$ne:true}}")
											.ToList();
				return new JsonResult(data.GridFeed(gs));
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		[HttpGet("group/detail")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'AdminController2.GetGroupItem(string, AgGridSettings)'
		public IActionResult GetGroupItem(string key, AgGridSettings gs)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'AdminController2.GetGroupItem(string, AgGridSettings)'
		{
			try
			{
				var data = context.db.GetCollection<Group>("groups")
											.Aggregate().Match($"{{key:'{key}'}}")
											.Unwind("{'$keyLands}")
											.Lookup("expersils", "keyLands", "key", "persil")
											.ReplaceRoot<Persil>("$persil").ToList()
											.Select(p=> new { p.key, p.basic.current?.pemilik, p.basic.current?.namaSurat, p.basic.current?.luasSurat, 
												p.basic.current?.alasHak, p.basic.current?.note })
											.ToList();
				return new JsonResult(data.GridFeed(gs));
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}

		//[HttpGet("group/grid")]
		//public IActionResult GetGroupGrid(AgGridSettings gs)
		//{
		//	try
		//	{
		//		var data = context.db.GetCollection<Group>("groups")
		//									.Find("{invalid:{$ne:true}}")
		//									.ToList();
		//		IGridServer<Group> server = new GridServer<Group>(data, Request.Query, true, "groupsGrid")
		//												.WithPaging(gs.pageSize)
		//												.WithGridItemsCount()
		//												.Sortable()
		//												.Filterable()
		//												.WithMultipleFilters()
		//												.WithGridItemsCount();

		//		var items = server.ItemsToDisplay;
		//		return Ok(items);
		//	}
		//	catch (Exception ex)
		//	{
		//		return new InternalErrorResult(ex.Message);
		//	}
		//}
	}
}
