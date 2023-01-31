#define _TEST
using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace IdPetaGen
{
	public class IDPetaGenerator
	{
#if _TEST_
		const string idcollname = "IDpeta_test";
#else
		const string idcollname = "IDpeta";
#endif
		public IDPetaGenerator()
		{
			client = new MongoClient(uri);
			db = client.GetDatabase("landrope_new");
		}

		const string uri = "mongodb://sa:M0ng0DB4dmin5@10.10.1.130:27017/admin";
		MongoClient client;
		IMongoDatabase db;

		public string GenerateNumber(string deskey)
		{
			var coll = db.GetCollection<IDPeta>(idcollname);
			if (coll == null)
				return null;
			IDPeta rec = null;

			lock (client) {
				try
				{
					rec = coll.Find($"{{key:'{deskey}'}}").FirstOrDefault();
					if (rec == null)
						return null;
					rec = rec.Increment();
					coll.FindOneAndReplace($"{{key:'{rec.key}'}}", rec);
				}
				catch (Exception ex)
				{
					return null;
				}
			}
			return $"{rec.code}{rec.last:0000}";
		}
	}

#if NETSTANDARD2_1
	internal record IDPeta
    {
		public ObjectId _id { get; set;}
		public string key { get; set;}
		public string code { get; set;}
		public int last { get; set; }


		internal IDPeta(ObjectId _id, string key, string code, int last)
        {
			this._id = _id;
			this.key = key;
			this.code = code;
			this.last = last;
        }
#else
	internal record IDPeta(ObjectId _id, string key, string code, int last)
	{
#endif
		public IDPeta Increment()
		{
			var last = this.last + 1;
			return this with { last = last };
		}
	}
}
