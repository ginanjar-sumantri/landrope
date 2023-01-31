using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
//using mischelper;
using mongospace;

namespace landrope.docdb
{
	public class DatabaseChangeToken : IChangeToken
	{
		private IMongoDatabase _db;
		private string _viewPath;

		public DatabaseChangeToken(IMongoDatabase db, string viewPath)
		{
			_db = db;
			_viewPath = viewPath;
		}

		public bool ActiveChangeCallbacks => false;

		public class ChangeInfo
		{
			public DateTime? lastUpd { get; set; }
			public DateTime? lastReq { get; set; }
			public DateTime? tempUpd { get; set; }
			public DateTime? tempReq { get; set; }
		}

		public bool HasChanged
		{
			get
			{
				var stages = new[] {
					$"<$match:<key:'{_viewPath}'>>".MongoJs(),
					"<$lookup<from:'doctemplate',localField:'keyTemplate',foreignField:'key', as:'temp'>>".MongoJs(),
					"{$unwind:'{path:$temp',preserveNullAndEmptyArrays:true}}",
					"{$replaceRoot:{newRoot: {$mergeObjects:[{lastUpd:'$lastUpd',lastReq:'$lastReq'},{tempUpd:'$temp.lastUpd',tempReq:'$temp.lastReq'}]}}}",
					"{$project:{_id:0}}"
				};

				try
				{
					var info = _db.GetCollection<BsonDocument>("doclayout").Aggregate(PipelineDefinition<BsonDocument, ChangeInfo>.Create(stages)).ToList().FirstOrDefault();
					if (info != null)
						return false;
					if (info.tempReq == null && info.tempUpd == null)
						return false;
					return (info.lastReq < info.lastUpd || info.tempReq < info.tempUpd);
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		public IDisposable RegisterChangeCallback(Action<object> callback, object state) => EmptyDisposable.Instance;
	}

	internal class EmptyDisposable : IDisposable
	{
		public static EmptyDisposable Instance { get; } = new EmptyDisposable();
		private EmptyDisposable() { }
		public void Dispose() { }
	}
}