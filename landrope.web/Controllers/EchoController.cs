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
using APIGrid;
using MongoDB.Driver.Encryption;
using mongospace;
using Microsoft.AspNetCore.Authentication;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
//using GridMvc.Server;

namespace landrope.web.Controllers
{
	[ApiController]
	[Route("api/echo")]


	public class EchoController : ControllerBase

	{
		//[NeedToken("PRABEBAS_VIEW,PASKABEBAS_VIEW")]
		[HttpPost("persil/basic")]
		public IActionResult Basic([FromBody]PersilBasic item)
			=>Ok(item);

		[HttpPost("persil/perjanjian")]
		public IActionResult Perjanjian([FromBody]ProsesPerjanjian item)
			=> Ok(item);

		//[HttpPost("persil/perjanjiangirik")]
		//public IActionResult PerjanjianGirik([FromBody]ProsesPerjanjianGirik item)
		//	=>Ok(item);

		//[HttpPost("persil/perjanjiansertifikat")]
		//public IActionResult PerjanjianSert([FromBody]ProsesPerjanjianSertifikat item)
		//	=> Ok(item);

		[HttpPost("persil/utj")]
		public IActionResult UTJ([FromBody]GroupUTJ item)
			=>Ok(item);

		[HttpPost("persil/dp")]
		public IActionResult DP([FromBody]GroupDP item)
			=>Ok(item);

		[HttpPost("persil/pelunasan")]
		public IActionResult Pelunasan([FromBody]GroupPelunasan item)
			=>Ok(item);

		[HttpPost("persil/pbt")]
		public IActionResult PBT([FromBody]ProsesPBT<NIB_PT> item)
			=> Ok(item);

		[HttpPost("persil/sph")]
		public IActionResult SPH([FromBody]ProsesSPH item)
			=> Ok(item);

		[HttpPost("persil/mohonskkantah")]
		public IActionResult MohonSK([FromBody]ProsesMohonSKKantah item)
			=> Ok(item);

		[HttpPost("persil/mohonskkanwil")]
		public IActionResult MohonSK([FromBody]ProsesMohonSKKanwil item)
			=> Ok(item);

		[HttpPost("persil/penurunanhak")]
		public IActionResult TurunHak([FromBody]ProsesTurunHak item)
			=> Ok(item);

		[HttpPost("persil/peningkatanhak")]
		public IActionResult NaikHak([FromBody]ProsesNaikHak item)
			=> Ok(item);

		[HttpPost("persil/baliknama")]
		public IActionResult BaliukNama([FromBody]ProsesBalikNama item)
			=> Ok(item);

		[HttpPost("persil/ajb")]
		public IActionResult AJB([FromBody]ProsesAJB item)
			=> Ok(item);

		[HttpPost("persil/cetakbuku")]
		public IActionResult CetakBuku([FromBody]ProsesCetakBukuHGB item)
			=> Ok(item);
	}
}
