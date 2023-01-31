using auth.mod;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace landrope.mod2
{
	public class ExtLandropeContext : authEntities
	{
		static ExtLandropeContext()
		{
			Registrar.Register(MethodBase.GetCurrentMethod());
		}

		public ExtLandropeContext()
			: base()
		{
			InitColSets<ExtLandropeContext>();
			MongoEntities.PutDefault<ExtLandropeContext>(this);
		}

		public ExtLandropeContext(IConfigurationRoot configuration)
			: base(configuration)
		{
			InitColSets<ExtLandropeContext>();
			MongoEntities.PutDefault<ExtLandropeContext>(this);
		}

		public ExtLandropeContext(string url, string database)
			: base(url, database)
		{
			InitColSets<ExtLandropeContext>();
			MongoEntities.PutDefault<ExtLandropeContext>(this);
		}

		public override string ConfigRoot => "data";

		public class entitas
		{
			public string key { get; set; }
			public string identity { get; set; }
		}

		public entitas GetProject(string projkey)
		{
			var project = GetCollections(new entitas(),
						"maps", $"<key:'{projkey}'>".MongoJs(),
						"{key:1,identity:1,_id:0}").ToList().FirstOrDefault();
			return (project);
		}

		public List<entitas> GetProjects() =>
			GetCollections(new entitas(),
						"maps", "{invalid:{$ne:true}}",
						"{key:1,identity:1,_id:0}").ToList();

		public (entitas project, entitas desa) GetVillage(string vilkey)
		{
			var project = GetCollections(new { project = new entitas(), village = new entitas() },
						"villages", $"<'village.key':'{vilkey}'>".MongoJs(), 
						"{project:1,'village.key':1,'village.identity':1,_id:0}").ToList().FirstOrDefault();
			return (project?.project, project?.village);
		}

		public List<(entitas project, entitas desa)> GetVillages()
		{
			var projects = GetCollections(new { project = new entitas(), village = new entitas() },
						"villages", "{}",
						"{project:1,'village.key':1,'village.identity':1,_id:0}").ToList();
			return projects.Select(p => (p.project, p.village)).ToList();
		}

		public CollSet<Persil> persils;
		public CollSet<PersilGirik> persilGiriks;
		public CollSet<PersilHGB> persilHGBs;
		public CollSet<PersilSHM> persilSHMs;
		public CollSet<PersilSHP> persilSHPs;
		public CollSet<PersilHibah> persilHibahs;
		public CollSet<Notaris> notarists;
		public CollSet<Company> companies;
		public CollSet<JnsBerkas> jnsberkases;
		public CollSet<Group> groups;
		public CollSet<persilMap> persilmaps;
		public CollSet<PTSK> ptsk;
		public CollSet<Category> categorys;
		public CollSet<MainProject> mainprojects;
		public CollSet<Worker> workers;
		public CollSet<PersilCategories> persilCat;
	}
}
