using landrope.mod2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Collections.Specialized;
using System.Linq;

namespace landrope.hosts
{
    public class PTSKHost
    {
        IConfiguration configuration;
        IServiceProvider services;
        ExtLandropeContext contextex;
        ObservableCollection<PTSK> PTSKs = new ObservableCollection<PTSK>();

        public PTSKHost(IConfiguration config, IServiceProvider services)
        {
            this.configuration = config;
            this.services = services;

            contextex = services.GetService<ExtLandropeContext>();
            PTSKs = new ObservableCollection<PTSK>(contextex.ptsk.Query(x => x.invalid != true));
            PTSKs.CollectionChanged += PTSks_CollectionChanged;
        }

        public void Add(PTSK ptsk)
        {
            PTSKs.Add(ptsk);
            contextex.ptsk.Insert(ptsk);
            contextex.SaveChanges();
        }

        public bool Update(PTSK ptsk, bool save = true)
        {
            if (ptsk == null)
                return false;
            var pos = PTSKs.IndexOf(ptsk);
            if (pos == -1)
                return false;
            contextex.ptsk.Update(ptsk);
            if (save)
                contextex.SaveChanges();
            return true;
        }

        public List<PTSK> OpenedPTSK()
           => PTSKs.Where(x => x.invalid != true).ToList<PTSK>();

        public PTSK GetPTSK(string key)
            => PTSKs.FirstOrDefault(a => a.key == key);

        private void PTSks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    PTSKs = new ObservableCollection<PTSK>(contextex.ptsk.Query(x => x.invalid != true));
                    PTSKs.CollectionChanged += PTSks_CollectionChanged;
                    break;
            }
        }

    }
}
