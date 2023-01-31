using auth.mod;
using landrope.mod2;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod4
{
    public class entity4 : entity
    {
        new public LandropePayContext MyContext() => Injector<LandropePayContext>();

        public entity4() : base()
        { }

        public entity4(BsonDocument doc)
            : base(doc)
        {
        }
    }

    public class namedentity4 : namedentity
    {
        new public LandropePayContext MyContext() => Injector<LandropePayContext>();

        public namedentity4() : base()
        { }

        public namedentity4(BsonDocument doc)
            : base(doc)
        {
        }
    }
}
