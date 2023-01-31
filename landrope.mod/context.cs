using MongoDB.Bson;
using MongoDB.Driver;
using mongospace;
using auth.mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Tracer;
using Microsoft.Extensions.Configuration;

namespace landrope.mod
{
	public class LandropeContext : authEntities
	{
		public LandropeContext()
			:base()
		{
			MongoEntities.PutDefault<LandropeContext>(this);
		}

		public LandropeContext(IConfigurationRoot configuration)
			: base(configuration)
		{
			MongoEntities.PutDefault<LandropeContext>(this);
		}


		public LandropeContext(string url, string database)
			: base(url, database)
		{
			MongoEntities.PutDefault<LandropeContext>(this);
		}

		public IFindFluent<Project,Project> Projects(FilterDefinition<Project> filter)
		{
			return db.GetCollection<Project>("maps").Find(filter);
		}

		public IFindFluent<Project,T> Find<T>(T sample, FilterDefinition<Project> filter, ProjectionDefinition<Project,T> projections)
		{
			return db.GetCollection<Project>("maps").Find(filter).Project<T>(projections);
		}

		public static PipelineDefinition<BsonDocument, TOut> CreatePipeline<TOut>(IEnumerable<TOut> sample, params string[] stages)
								=> PipelineDefinition<BsonDocument, TOut>.Create(stages);

		//public List<T> GetDocuments<T>(T sample, string collname, params string[] stages)
		//					=> db.GetCollection<BsonDocument>(collname).Aggregate(
		//										PipelineDefinition<BsonDocument, T>.Create(stages)).ToList();

		//public IFindFluent<T,T> GetCollections<T>(T sample, string collname, FilterDefinition<T> filter, ProjectionDefinition<T> projects=null)
		//{
		//	var ff = db.GetCollection<T>(collname).Find(filter);
		//	if (projects!=null)
		//		ff = ff.Project<T>(projects);
		//	return ff;
		//}

		public void Insert(Project doc)
		{
			db.GetCollection<Project>("maps").InsertOne(doc);
		}

		public void InsertMany(Project[] docs)
		{
			db.GetCollection<Project>("maps").InsertMany(docs);
		}

		public (long match, long updated) Update(Project doc, UpdateDefinition<Project> def)
		{
			var res = db.GetCollection<Project>("maps").UpdateOne(new BsonDocument("key", doc.key), def);
			return (res.MatchedCount, res.ModifiedCount);
		}

		public long Delete(Project doc)
		{
			var res = db.GetCollection<Project>("maps").DeleteOne(new BsonDocument("key", doc.key));
			return res.DeletedCount;
		}
	}
}
