using Microsoft.Extensions.FileProviders;
using System;
using System.Linq;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
//using mischelper;
using mongospace;

namespace landrope.docdb
{
	public class DatabaseFileInfo : IFileInfo
	{
		private string _viewPath;
		private byte[] _viewContent;
		private DateTimeOffset _lastModified;
		private bool _exists;

		public DatabaseFileInfo(IMongoDatabase db, string viewPath)
		{
			_viewPath = viewPath;
			GetView(db, viewPath);
		}
		public bool Exists => _exists;

		public bool IsDirectory => false;

		public DateTimeOffset LastModified => _lastModified;

		public long Length
		{
			get
			{
				using (var stream = new MemoryStream(_viewContent))
				{
					return stream.Length;
				}
			}
		}

		public string Name => Path.GetFileName(_viewPath);

		public string PhysicalPath => null;

		public Stream CreateReadStream()
		{
			return new MemoryStream(_viewContent);
		}

		private void GetView(IMongoDatabase db, string viewPath)
		{
			var stages = new[] {
				$"<$match:<key:'{viewPath}'>>".MongoJs(),
				"<$lookup:<from:'docTemplate',localField:'keyTemplate',foreignField:'key', as:'temp'>>".MongoJs(),
				"{$unwind: {path: '$temp', preserveNullAndEmptyArrays:true}}",
				"{$addFields:{'temp.lastReq':{$max:['$lastUpd','$temp.lastUpd']}}}",
				"{$replaceRoot:{newRoot:'$temp'}}",
				"{$project:{_id:0}}"
			};

			try
			{
				var layout = db.GetCollection<BsonDocument>("docLayout");
				var template = db.GetCollection<BsonDocument>("docTemplate");
				var data = layout.Aggregate<doctemplate>(PipelineDefinition<BsonDocument, doctemplate>.Create(stages)).ToList().FirstOrDefault();
				_exists = data!=null;
				if (_exists)
				{
					_viewContent = Encoding.ASCII.GetBytes(data.Html??"");
					_lastModified = new DateTimeOffset(data.lastUpd??DateTime.Now.AddDays(-7d));

					// update the layout & template last-request information
					var now = DateTimeOffset.UtcNow;
					layout.UpdateOne($"<key:'{viewPath}'>".MongoJs(), $"<$set:<lastReq:new Date('{now}')>>".MongoJs());
					template.UpdateOne($"<key:'{data.key}'>".MongoJs(), $"<$set:<lastReq:new Date('{now}')>>".MongoJs());
				}
			}
			catch (Exception ex)
			{
				_exists = false;
			}
		}
	}
}
