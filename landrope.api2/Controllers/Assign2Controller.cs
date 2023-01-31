using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DynForm.shared;
using landrope.common;
using landrope.mod;
using landrope.mod2;
using landrope.mod3;
using landrope.mod3.shared;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using mongospace;

namespace landrope.api2.Controllers
{
	[Route("/api/master/assign")]
	[ApiController]
	public class Assign2Controller : ControllerBase
	{
		ExtLandropeContext context = Contextual.GetContextExt();

		public IActionResult GetLocationList()

		{
			try
			{
				var data = context.GetDocuments(new { value = "", text = "", desas = new ListItem[0] }, "maps",
"{$unwind: '$villages'}",
"{$group: { _id: '$key', text: {$first: '$identity'},desas: {$push: { value: '$villages.key',text: '$villages.identity'} } } }",
"{$project: { _id: 0,value: '$_id',text: 1, desas: 1} }"
									)
									.ToList();
				return new JsonResult(data);
			}
			catch (Exception ex)
			{
				return new InternalErrorResult(ex.Message);
			}
		}


		[EnableCors(nameof(landrope))]
		[HttpGet("steps/by-cat")]
		public IActionResult GetSteps(AssignmentCat cat)
		{
			//(JenisProses pros, JenisAlasHak alh) = cat.Convert();
			var type = cat switch
			{
				AssignmentCat.Girik => typeof(PersilGirik),
				AssignmentCat.SHM => typeof(PersilSHM),
				AssignmentCat.HGB => typeof(PersilHGB),
				AssignmentCat.SHP => typeof(PersilSHP),
				AssignmentCat.Hibah  => typeof(PersilHibah),
				_ => typeof(Persil)
			};
			var attrib = type.GetCustomAttribute<EntityAttribute>();
			if (attrib == null)
				return new NoContentResult();
			var disc = attrib.Discriminator;

			var steps = context.GetStepsP(disc);
			var data = steps.Select(s => new ListItemEx<DocProcessStep>
			{
				value = (DocProcessStep)s,
				text = ((DocProcessStep)s).GetName()
			}).ToArray();

			return Ok(data);
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("steps")]
		public IActionResult GetStepsAsgn()
		{
			var steps = context.GetAllSteps();
			var res = steps.Select(s => new { cat=StrCats[s.disc],steps=s.steps.Cast<DocProcessStep>().ToArray() }).ToArray();

			return Ok(res);
		}

		[EnableCors(nameof(landrope))]
		[HttpGet("users")]
		public IActionResult GetUsers()
		{
			var users = context.users.Query(u => u.invalid != true).Select(u => new ListItem { value = u.key, text = u.FullName }).ToArray();
			var steps = Enum.GetValues(typeof(DocProcessStep)).Cast<DocProcessStep>()
									.GroupJoin(users, s => 1, u => 1, (s, su) => new { step = s, users = su.ToArray() }).ToArray();
			return Ok(steps);
		}
	}
}
