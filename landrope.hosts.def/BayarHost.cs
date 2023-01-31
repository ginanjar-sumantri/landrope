using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using landrope.common;
using landrope.mod4;
using landrope.mod4.classes;
using landrope.engines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace landrope.hosts
{
    public class BayarHost : IBayarHost
    {
        IConfiguration configuration;
        IServiceProvider services;
        LandropePayContext contextpay;
        ObservableCollection<Bayar> Bayars = new ObservableCollection<Bayar>();

        public BayarHost(IConfiguration config, IServiceProvider services)
        {
            this.configuration = config;
            this.services = services;
            contextpay = services.GetService<LandropePayContext>();

            Bayars = new ObservableCollection<Bayar>(contextpay.bayars.Query(a => a.invalid != true));
            Bayars.CollectionChanged += Bayars_CollectionChanged;
        }

        public void Add(Bayar byr)
        {
            Bayars.Add(byr);
            contextpay.bayars.Insert(byr);
            contextpay.SaveChanges();
        }

        public bool Update(Bayar byr, bool save = true)
        {
            if (byr == null)
                return false;
            var pos = Bayars.IndexOf(byr);
            if (pos == -1)
                return false;
            contextpay.bayars.Update(byr);
            if (save)
                contextpay.SaveChanges();
            return true;
        }

        public List<IBayar> OpenedBayar()
            => Bayars.Where(a => a.invalid != true).ToList<IBayar>();

        public List<IBayar> OpenedBayar(string[] keys)
        {
            if (keys != null && keys.Length == 0)
                return new List<IBayar>();

            var all = keys == null || keys.Contains("*");
            var qry = Bayars.Where(a => a.invalid != true);
            if (all)
                return qry.ToList<IBayar>();

            //var result = qry.Where(x => keys.Contains(x.details.SelectMany(x => x.instkey)) || keys.Contains(x.deposits.SelectMany(x => x.instkey))).ToList<IBayar>();

            var dtls = qry.SelectMany(x => x.details, (y, z) => new { bayar = y, instkey = z.instkey }).Where(x => keys.Contains(x.instkey)).Select(x => x.bayar);
            var dpst = qry.SelectMany(x => x.deposits, (y, z) => new { bayar = y, instkey = z.instkey }).Where(x => keys.Contains(x.instkey)).Select(x => x.bayar);

            var result = dtls.Union(dpst).ToList<IBayar>();

            //var result = union.Where(x => keys.Contains(x.instkey)).Select(x => x.bayar).Distinct().ToList<IBayar>();

            return result;
        }

        public IBayar GetBayar(string key)
            => Bayars.FirstOrDefault(a => a.key == key);

        public IEnumerable<IBayar> GetBayarsByProject(string keyProject)
            => Bayars.Where(a => a.keyProject == keyProject);

        private void Bayars_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    Bayars = new ObservableCollection<Bayar>(contextpay.bayars.Query(a => a.invalid != true));
                    Bayars.CollectionChanged += Bayars_CollectionChanged;
                    break;
            }
        }

        public void Reload()
        {
            Bayars.Clear();
            Bayars = new ObservableCollection<Bayar>(contextpay.bayars.Query(a => a.invalid != true));
        }
        
    }
}
