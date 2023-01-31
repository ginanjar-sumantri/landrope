using auth.mod;
using landrope.common;
using landrope.documents;
using landrope.mod2;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod3
{
    [Entity("LogBundle", "logBundle")]
    public class LogBundle : namedentity3
    {

        public string keyPersil { get; set; }
        public string keyDocType { get; set; }
        public string keyCreator { get; set; }
        public DateTime? created { get; set; }
        public LogActivityType activityType { get; set; }
        public LogActivityModul modul { get; set; }

        public LogBundle(RegisteredDocCore core, user user, string bundKey, DateTime timeStamp, LogActivityType logActivity, LogActivityModul logActivityModul)
        {
            key = MakeKey;
            keyPersil = bundKey;
            keyDocType = core.keyDocType;
            keyCreator = user.key;
            created = timeStamp;
            activityType = logActivity;
            modul = logActivityModul;
        }

        public LogBundle(user user, string docType, string bundKey, DateTime timeStamp, LogActivityType logActivity, LogActivityModul logActivityModul)
        {
            key = MakeKey;
            keyPersil = bundKey;
            keyDocType = docType;
            keyCreator = user.key;
            created = timeStamp;
            activityType = logActivity;
            modul = logActivityModul;
        }

        public LogBundle(string bundKey, string docType, string keyCreator, DateTime created, LogActivityType logActivity, LogActivityModul logActivityModul)
        {
            key = MakeKey;
            this.keyPersil = bundKey;
            this.keyDocType = docType;
            this.keyCreator = keyCreator;
            this.created = created;
            this.activityType = logActivity;
            this.modul = logActivityModul;
        }

        public LogBundle()
        {

        }
    }

    [Entity("LogDeal", "logDeal")]
    public class LogDeal : namedentity3
    {
        public LogDeal()
        {
            this.key = MakeKey;
            this.identifier = "logDeal";
        }

        public string keyPersil { get; set; }
        public DateTime? tanggalKesepakatan { get; set; }
        public string[] jenisDokumen { get; set; } = new string[0];
    }

    [Entity("LogPreBundle", "logDeal")]
    public class LogPreBundle : namedentity3
    {
        public string keyPersil { get; set; }
        public string keyDocType { get; set; }
        public string keyCreator { get; set; }
        public DateTime? created { get; set; }
        public LogActivityType activityType { get; set; }
        public LogActivityModul modul { get; set; }

        public LogPreBundle()
        {

        }

        public LogPreBundle(user user, string docType, string bundKey, DateTime timeStamp, LogActivityType logActivity, LogActivityModul logActivityModul)
        {
            key = MakeKey;
            keyPersil = bundKey;
            keyDocType = docType;
            keyCreator = user.key;
            created = timeStamp;
            activityType = logActivity;
            modul = logActivityModul;
        }

        public LogPreBundle(RegisteredDocCore core, user user, string bundKey, DateTime timeStamp, LogActivityType logActivity, LogActivityModul logActivityModul)
        {
            key = MakeKey;
            keyPersil = bundKey;
            keyDocType = core.keyDocType;
            keyCreator = user.key;
            created = timeStamp;
            activityType = logActivity;
            modul = logActivityModul;
        }
    }
}