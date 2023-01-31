using auth.mod;
using landrope.mod2;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod3
{
	public class entity3 : entity
	{
		new public LandropePlusContext MyContext() => Injector<LandropePlusContext>();

		public entity3() : base()
		{ }

		public entity3(BsonDocument doc)
			: base(doc)
		{
		}
	}

	public class namedentity3 : namedentity
	{
		new public LandropePlusContext MyContext() => Injector<LandropePlusContext>();

		public namedentity3() : base()
		{ }

		public namedentity3(BsonDocument doc)
			: base(doc)
		{
		}
	}
}
