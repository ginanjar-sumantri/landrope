using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using encdec;

namespace landrope.api2.Controllers
{
	[Route("api/salt")]
	[ApiController]
	public class SaltController : ControllerBase
	{
		[HttpGet("get")]
		public IActionResult Get()
		{
			return Ok("S.A.L.T Get");
		}

		[HttpGet("test")]
		public IActionResult Test()
		{
			return Ok("S.A.L.T Test");
		}
	}
}
