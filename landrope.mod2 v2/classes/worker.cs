using auth.mod;
using landrope.common;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
    [Entity("worker", "workers")]
    public class Worker : namedentity
    {
        public Worker()
        {
            key = MakeKey;
        }

        public string WorkerParent { get; set; }
        public string FullName { get; set; }
        public string ShortName { get; set; }
        public string InitialName { get; set; }
        public WorkerType type { get; set; }

        public void FromCore(WorkerBase Core)
        {
            (FullName, ShortName, InitialName, type, WorkerParent) = (Core.FullName, Core.ShortName, Core.InitialName, Core.type, Core.workerParent);
        }

        public WorkerView ToView()
        {
            var view = new WorkerView();
            string position = Enum.GetName(typeof(WorkerType), type);
            (view.key, view.WorkerParent, view.ShortName, view.FullName, view.InitialName, view.Position)
               =
            (key, WorkerParent, ShortName, FullName, InitialName, position);
            return view;
        }
    }
}
