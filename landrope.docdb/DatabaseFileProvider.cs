using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using MongoDB.Driver;
using Microsoft.Extensions.FileProviders.Composite;
using System.Collections.Generic;

namespace landrope.docdb
{
	public class DatabaseFileProvider : IFileProvider
	{
		private string _uri;
		private MongoClient client;
		private IMongoDatabase db;

		public DatabaseFileProvider(string uri, string database)
		{
			_uri = uri;
			client = new MongoClient(uri);
			db = client?.GetDatabase(database);
		}

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
			var sub = subpath switch
			{
				"/Areas" => "/",
				"/Pages" => "/",
				"/" => "",
				_ => null
			};
			return sub == null ? null: new CompositeDirectoryContents(new List<IFileProvider> {this}, sub);
        }

        public IFileInfo GetFileInfo(string subpath)
		{
			//var test =  subpath.Contains("Imports") ? subpath : "/Index.cshtml";
			var result = new DatabaseFileInfo(db, subpath);
			return result.Exists ? result as IFileInfo : new NotFoundFileInfo(subpath);
		}

        public IChangeToken Watch(string filter)
        {
            return new DatabaseChangeToken(db, filter);
        }
    }
}
