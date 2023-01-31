using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class WorkerBase : CoreBase
    {
        public string key { get; set; }
        public string workerParent { get; set; }
        public string FullName { get; set; }
        public string ShortName { get; set; }
        public string InitialName { get; set; }
        public WorkerType type { get; set; }
    }

    public class WorkerView
    {
        public string key { get; set; }
        public string WorkerParent { get; set; }
        public string FullName { get; set; }
        public string ShortName { get; set; }
        public string InitialName { get; set; }
        public string Position { get; set; }
    }
}