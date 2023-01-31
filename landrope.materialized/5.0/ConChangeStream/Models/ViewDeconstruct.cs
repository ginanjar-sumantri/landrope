using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConChangeStream.Models
{
    public class ViewDeconstruct
    {
        public string db { get; set; }
        public string view { get; set; }
        public string source { get; set; }
        public BsonDocument[] stages { get; set; }
    }
}
