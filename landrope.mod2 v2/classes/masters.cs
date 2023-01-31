using auth.mod;
using landrope.common;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
    [Entity("notaris", "masterdatas")]
    public class Notaris : namedentity
    {
        [BsonRequired]
        public string alamat { get; set; }
    }
    [BsonKnownTypes(typeof(PTSK))]
    [Entity("pt", "masterdatas")]
    public class Company : namedentity
    {
        [BsonRequired]
        public StatusPT status { get; set; }
        public string code { get; set; }
    }

    [Entity("jnsberkas", "masterdatas")]
    public class JnsBerkas : namedentity
    {
    }

    [Entity("mappingDocs", "masterdatas")]
    public class MappingDocs : namedentity
    {
        public JenisMappingDocument jenis { get; set; }
        public Document[] documents { get; set; }

        public class Document
        {
            public string jenisDocs { get; set; }
            public int?[] metadata { get; set; }
            public int? urutan { get; set; }
        }
        public int urutan { get; set; }
    }

    [Entity("internal", "masterdatas")]
    public class Internal : namedentity
    {
        public string salutation { get; set; }
        public string alamat { get; set; }
        public string[] tembusan { get; set; } = new string[0];
    }
}
