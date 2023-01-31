using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConChangeStream.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tracer;

namespace ConChangeStream;
public class ChangeStreamService : IHostedService, IDisposable
{
    private IMongoClient client;
    private readonly HttpClient httpClient;
    //private IMongoDatabase xdb;
    private ChangeStreamOperationType[] types = {
            ChangeStreamOperationType.Replace,
            ChangeStreamOperationType.Insert,
            ChangeStreamOperationType.Delete,
            ChangeStreamOperationType.Update
    };
    private List<MaterializedConfig> materializedConfigs = new();
    private List<ViewDeconstruct> viewDeconstructs = new();
    private IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor;
    private readonly ILogger<ChangeStreamService> logger;
    private readonly IConfiguration  configuration;
    public ChangeStreamService(
        ILogger<ChangeStreamService> _logger,
        IConfiguration _configuration,
        HttpClient _httpclient)
    {
        logger = _logger;
        configuration = _configuration;
        httpClient = _httpclient;
    }

    static readonly ChangeStreamOperationType[] ops = {
            ChangeStreamOperationType.Insert,
            ChangeStreamOperationType.Replace,
            ChangeStreamOperationType.Update
        };
    static readonly string change_stream_stored = "change_stream_stored";
    static string[] exceptDb = new string[] { "admin", "config", "local" };

    #region Hosted Service
    public Task StartAsync(CancellationToken cancellationToken)
    {
        MakeConnection();
        ReadConfig();
        CheckConfig();

        Task.Run(async () =>
          {
              cursor = await client.WatchAsync();
              await cursor.ForEachAsync(async x =>
              {
                  if (!(x.CollectionNamespace.FullName).Contains(change_stream_stored))
                  {
                      WriteDetectedLog(new {
                          CollectionNamespace = x.CollectionNamespace.FullName,
                          DocumentKey = x.DocumentKey,
                          OperationType = $"{x.OperationType:g}"
                      });
                      
                      MaterializedConfig config = materializedConfigs
                                                    .FirstOrDefault(f => string.Equals($"{f.database}.{f.collection}", x.CollectionNamespace.FullName));
                      if (config != null)
                      {
                          var log = new
                          {
                              Info = "Detected",
                              Database = x.CollectionNamespace.DatabaseNamespace.DatabaseName,
                              CollectionNamespace = x.CollectionNamespace.FullName,
                              DocumentKey = x.DocumentKey,
                              OperationType = x.OperationType.ToString(),
                              Destination = ""
                          };
                          //MyTracer.Info(JsonConvert.SerializeObject(log));

                          var database = client.GetDatabase(x.CollectionNamespace.DatabaseNamespace.DatabaseName);

                          BsonDocument documentChanged = database.GetCollection<BsonDocument>(config.collection).Find(x.DocumentKey).FirstOrDefault();
                          var inMater = ops.Contains(x.OperationType);

                          foreach (Merge merge in config.merges)
                          {
                              if (!inMater)
                              {
                                  //Console.WriteLine($"Delete Document 1111: '{merge.key}':'{key}' ");
                                  //var dest = database.GetCollection<BsonDocument>(merge.destination);
                                  //dest.FindOneAndDelete($"{{'{merge.key}':'{key}'}}");
                              }
                              else
                              {
                                  var collName = merge.view;
                                  var key = documentChanged.GetValue(merge.key)?.ToString();
                                  List<BsonDocument> stages = new();

                                  if (string.IsNullOrEmpty(merge.view)) // collection to collection
                                  {
                                      collName = config.collection;
                                  }
                                  else // collection to view
                                  {
                                      var viewDeconstruct = viewDeconstructs
                                                          .FirstOrDefault(f => f.db == x.CollectionNamespace.DatabaseNamespace.DatabaseName && f.view == merge.view);
                                      stages = viewDeconstruct.stages.ToList();
                                      collName = viewDeconstruct.source;
                                  }

                                  if (merge.specific)
                                      stages.Insert(0, BsonDocument.Parse($"<$match: <'{merge.key}':'{key}'>>".Replace("<", "{").Replace(">", "}")));

                                  stages.Add(BsonDocument.Parse("{$project:{_id:0}}"));

                                  if (!string.IsNullOrEmpty(merge.db))
                                      stages.Add(BsonDocument.Parse($"<$merge:<into: <db:'{merge.db}', coll: '{merge.destination}'>, on: '{merge.key}', whenMatched: 'replace', whenNotMatched:'insert' >>".Replace("<", "{").Replace(">", "}")));
                                  else
                                      stages.Add(BsonDocument.Parse($"<$merge:<into: '{merge.destination}', on: '{merge.key}', whenMatched: 'replace', whenNotMatched:'insert' >>".Replace("<", "{").Replace(">", "}")));

                                  var materialized = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);

                                  // change_stream_stored
                                  var filter = new BsonDocument(){
                                                    { "collname",  x.CollectionNamespace.CollectionName},
                                                    { "key", key}
                                                  };
                                  var findSelf = new BsonDocument();

                                  findSelf = database.GetCollection<BsonDocument>(change_stream_stored).Find(filter).FirstOrDefault();
                                  if (string.IsNullOrEmpty(merge.db))
                                      findSelf = null;

                                  if (findSelf == null)
                                  {
                                      var store = new BsonDocument()
                                              {
                                                { "collname", merge.destination},
                                                { "key", key},
                                                { "expiredAt", DateTime.Now},
                                      };

                                      if (!string.IsNullOrEmpty(merge.db))
                                          await client.GetDatabase(merge.db).GetCollection<BsonDocument>(change_stream_stored).InsertOneAsync(store);
                                      //else
                                      //    await database.GetCollection<BsonDocument>(change_stream_stored).InsertOneAsync(store);

                                      try
                                      {
                                          await database.GetCollection<BsonDocument>(collName)
                                                  .AggregateAsync(materialized);

                                          if (!string.IsNullOrEmpty(merge.urlendpoint))
                                          {
                                              string url = $"{merge.urlendpoint}?key={key}";

                                              HttpRequestMessage request = new()
                                              {
                                                  RequestUri = new Uri(url),
                                                  Method = HttpMethod.Get
                                              };

                                              httpClient.SendAsync(request).Wait();
                                          }

                                          //log = $"Change Executed => DocumentKey : {x.DocumentKey}; OperationType:{x.OperationType:g}; CollectionNamespace:{x.CollectionNamespace.FullName}; destination : {merge.db}.{merge.destination}";
                                          //Console.WriteLine(log);

                                          var writeToLog = new
                                          {
                                              Info = "Executed",
                                              Database = x.CollectionNamespace.DatabaseNamespace.DatabaseName,
                                              CollectionNamespace = x.CollectionNamespace.FullName,
                                              DocumentKey = x.DocumentKey,
                                              OperationType = x.OperationType.ToString(),
                                              Destination = string.IsNullOrEmpty(merge.db) ? merge.destination : $"{merge.db}.{merge.destination}"
                                          };

                                          MyTracer.Info(JsonConvert.SerializeObject(writeToLog));
                                      }
                                      catch(Exception ex)
                                      {
                                          var writeToLog = new
                                          {
                                              Info = "Error",
                                              Database = x.CollectionNamespace.DatabaseNamespace.DatabaseName,
                                              CollectionNamespace = x.CollectionNamespace.FullName,
                                              DocumentKey = x.DocumentKey,
                                              OperationType = x.OperationType.ToString(),
                                              Destination = string.IsNullOrEmpty(merge.db) ? merge.destination : $"{merge.db}.{merge.destination}"
                                          };

                                          MyTracer.Error(ex);
                                          MyTracer.Info(JsonConvert.SerializeObject(writeToLog));
                                      }
                                  }
                                  else
                                  {
                                      //viewDeconstructs.Select(sv => sv.db)
                                      viewDeconstructs.Select(sv => sv.db)
                                      .Distinct()
                                      .ToList()
                                      .ForEach(f =>
                                      {
                                          client.GetDatabase(f)
                                                  .GetCollection<BsonDocument>(change_stream_stored)
                                                  .FindOneAndDelete(filter);
                                      });
                                      //Console.WriteLine($"Remove Stored : {filter.GetValue("collname")?.ToString()}");
                                  }
                              }
                          }
                      }
                  }
              });

              while (!cancellationToken.IsCancellationRequested)
              {

              }
          }, cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("ChangeStreamService Stop Watching...");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Console.WriteLine("ChangeStreamService Disposable Service...");
        cursor?.Dispose();
    }

    #endregion
    private async void ReadConfig()
    {
        //string dirPath = Path.Combine(Directory.GetCurrentDirectory(), "MaterializedConfig");
        string dirPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterializedConfig");
        if (Directory.Exists(dirPath))
        {
            foreach (string file in Directory.EnumerateFiles(dirPath, "*.json"))
            {
                string contents = File.ReadAllText(file);
                materializedConfigs.AddRange(JsonConvert.DeserializeObject<MaterializedConfig[]>(contents));
            }
        }
        await Task.Delay(0);
    }

    private void MakeConnection()
    {
        try
        {
            string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "materialized.appsettings.json");
            string read = File.ReadAllText(filePath);

            MongoConnection mc = JsonConvert.DeserializeObject<MongoConnection>(read);
            client = new MongoClient($"{mc.protocol}://{mc.uid}:{mc.pwd}@{mc.server}/admin");
            Console.WriteLine("ChangeStreamService Start Watching...");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"ChangeStreamService Error: {ex.Message}");
            throw new Exception($"ChangeStreamService Error Message: {ex.Message}");
        }
    }
    private async void CheckConfig()
    {
        var listDatabaseServer = client.ListDatabaseNames().ToList().Where(w => !exceptDb.Contains(w));
        var listDatabaseConfig = materializedConfigs.SelectMany(sm =>
       {
           List<string> list = new() { sm.database };
           list.AddRange(sm.merges.Select(s => s.db).Where(w => !string.IsNullOrEmpty(w)));
           return list;
       }).Distinct();

        // check database is exists
        foreach (var dbName in listDatabaseConfig)
            if (listDatabaseServer.FirstOrDefault(a => a == dbName) == null)
                throw new Exception($"ChangeStreamService Error Message: Database '{dbName}' does not Exists.");

        // check collection is exists
        foreach (var dbName in listDatabaseConfig)
        {
            var mongoDatabase = client.GetDatabase(dbName);
            var collDatabase = (await mongoDatabase.ListCollectionsAsync()).ToList();
            var collConfig = materializedConfigs.Where(w => w.database == dbName).SelectMany(sm =>
            {
                List<string> coll = new() { sm.collection };
                coll.AddRange(sm.merges.Where(w => !string.IsNullOrEmpty(w.view)).Select(s => s.view));
                return coll;
            });

            // check collection
            foreach (var collName in collConfig)
                if (!collDatabase.Select(s => s.GetElement("name").Value.ToString()).Contains(collName))
                    throw new Exception($"ChangeStreamService Error Message: Collection '{dbName}.{collName}' does not Exists.");

            foreach (var collection in collDatabase)
            {
                var type = collection.GetElement("type").Value.ToString();
                if (type == "view")
                {
                    var opts = collection.GetElement("options").Value.AsBsonDocument;
                    var source = opts.GetElement("viewOn").Value.ToString();
                    var stages = opts.GetElement("pipeline").Value.AsBsonArray.ToArray().Select(x => (BsonDocument)x).ToArray();
                    viewDeconstructs.Add(new ViewDeconstruct()
                    {
                        db = dbName,
                        view = collection.GetElement("name").Value.ToString(),
                        source = source,
                        stages = stages
                    });
                }
            }

            // check views
            var viewConfig = materializedConfigs
                            .Where(w => w.database == dbName)
                            .SelectMany(sm => sm.merges.Where(w => !string.IsNullOrEmpty(w.view))
                            .Select(s => s.view));
            foreach (var view in viewConfig)
                if (!viewDeconstructs.Any(a => a.view == view))
                    throw new Exception($"ChangeStreamService Error Message: View '{dbName}.{view}' does not Exists.");

            // create change_stream_stored collection
            if (!collDatabase.Select(s => s.GetElement("name").Value.ToString()).Contains(change_stream_stored))
            {
                mongoDatabase.CreateCollection(change_stream_stored);
                _ = await mongoDatabase.GetCollection<BsonDocument>(change_stream_stored)
                        .Indexes
                        .CreateOneAsync(
                            new BsonDocument("expiredAt", 1),
                            new CreateIndexOptions() { ExpireAfter = TimeSpan.FromSeconds(0) }
                        );
            }
        }

        // make log directory
        string logDirPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterializedLogs");
        if(!Directory.Exists(logDirPath))
            Directory.CreateDirectory(logDirPath);

    }

    private void WriteDetectedLog(dynamic obj)
    {
        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterializedLogs", "changed.txt");
        if (!File.Exists(path))
            File.Create(path);

        //File.AppendAllText(path, JsonConvert.SerializeObject(obj) + Environment.NewLine);
    }

    private void WriteExecutedLog(dynamic obj)
    {

    }
    //private (string coll, BsonDocument[] stages) Deconstruct(IMongoDatabase db, string viewname)
    //{
    //	//Console.WriteLine($"ViewName: {viewname}");
    //	var coll = db.ListCollections(new ListCollectionsOptions { Filter = $"{{name:'{viewname}'}}" }).ToList();
    //	if(coll.Count() == 0)
    //		throw new Exception($"ChangeStreamService Error Message: View '{viewname}' does not Exists.");

    //	var root = coll[0];
    //	var type = root.GetElement("type").Value.ToString();
    //	if (type != "view")
    //		throw new Exception($"ChangeStreamService Error Message: '{viewname}' is not a view");
    //	//throw new InvalidOperationException($"ChangeStreamService Error Message: '{viewname}' is not a view");
    //	var opts = coll[0].GetElement("options").Value.AsBsonDocument;
    //	var source = opts.GetElement("viewOn").Value.ToString();
    //	var pipelines = opts.GetElement("pipeline").Value.AsBsonArray.ToArray().Select(x => (BsonDocument)x).ToArray();
    //	return (source, pipelines);
    //}
}
