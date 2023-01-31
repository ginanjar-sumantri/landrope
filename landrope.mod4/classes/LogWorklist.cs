using flow.common;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.mod4
{
    [Entity("logWorklist", "logWorklist")]
    public class LogWorklist : entity4
    {
        public LogWorklistEntry[] entries { get; set; } = new LogWorklistEntry[0];

        public LogWorklist() {}
        public LogWorklist(string instKey)
        {
            this.key = instKey;
        }

        public void AddLogEntry(LogWorklistEntry entry)
        {
            var lst = entries.ToList();
            lst.Add(entry);
            this.entries = lst.ToArray();
        }
    }

    public class LogWorklistEntry
    {
        public string keyCreator { get; set; }
        public DateTime created { get; set; }
        public ToDoState state { get; set; }
        public ToDoVerb verb { get; set; }
        public string reason { get; set; }
    }
}
