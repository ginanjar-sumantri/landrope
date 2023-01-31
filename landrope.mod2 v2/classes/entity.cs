using auth.mod;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
	[BsonKnownTypes(typeof(Persil), typeof(Group), typeof(namedentity2))]
	public class entity2 : entity
	{
		new public ExtLandropeContext MyContext() => _context<ExtLandropeContext>();

		public entity2() : base()
		{ }

		public entity2(BsonDocument doc)
			: base(doc)
		{
		}
	}

	public class namedentity2 : namedentity
	{
		public namedentity2() : base()
		{ }

		public namedentity2(BsonDocument doc)
			: base(doc)
		{
		}

	}
}
