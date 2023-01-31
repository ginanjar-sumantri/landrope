# define test
using graph.mod;
using landrope.mod;
using landrope.mod2;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Tracer;
using landrope.common;
using MongoDB.Driver;
using GraphConsumer;
using GenWorkflow;
using flow.common;
using APIGrid;
using landrope.api3.Models;
using auth.mod;
using landrope.consumers;
using mongospace;
using BundlerConsumer;
using landrope.documents;
using MongoDB.Bson;
using landrope.mod3;
using DynForm.shared;
using Newtonsoft.Json;
using landrope.mod3.shared;
using FileRepos;
using System.Threading.Tasks;
using landrope.mod4.classes;
using landrope.mod4;
using System.Collections.ObjectModel;
using landrope.hosts;
using landrope.material;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using HttpAccessor;
using core.helpers;
using System.Text;
using landrope.mod.cross;
using MailSender;
using MailSender.Models;

namespace landrope.api3.Controllers
{
    [Route("api/list")]
    [ApiController]
    public class ListController : Controller
    {
        IServiceProvider services;
        LandropePlusContext contextplus = Contextual.GetContextPlus();
        private LandropePayContext contextpay = Contextual.GetContextPay();
        private IConfiguration configuration;


        public ListController(IServiceProvider service, IConfiguration iconfig)
        {
            this.services = service;
            configuration = iconfig;
        }

        //Add by Ricky 20220921
        [NeedToken("DOC_MANDATORY_VIEW")]
        [HttpGet("prabebas/docsetting")]
        public IActionResult GetDocsSetting([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var data = contextplus.GetCollections<FormDocSetting>(new FormDocSetting(), "APP_LIST_DocsSettingDetailPraDeal", "{}", "{_id:0}").ToList();
                data.Select(c => { c.statustext = c.status.ToString(); return c; }).ToList();

                var xlst = ExpressionFilter.Evaluate(data, typeof(List<FormDocSetting>), typeof(FormDocSetting), gs);
                var filteredData = xlst.result.Cast<FormDocSetting>().ToList();
                return Ok(filteredData.GridFeed(gs, null, null));
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
