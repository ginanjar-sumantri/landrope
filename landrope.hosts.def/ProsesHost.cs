using landrope.engines;
using landrope.mod4;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using landrope.common;
using System.Linq;

namespace landrope.hosts
{
    public class ProsesHost : IProsesHost
    {
        IConfiguration configuration;
        IServiceProvider services;
        LandropePayContext contextpay;
        ObservableCollection<Sertifikasi> Sertifikasis = new ObservableCollection<Sertifikasi>();
        ObservableCollection<Pajak> Pajaks = new ObservableCollection<Pajak>();

        public ProsesHost(IConfiguration config, IServiceProvider services)
        {
            this.configuration = config;
            this.services = services;
            contextpay = services.GetService<LandropePayContext>();


            Sertifikasis = new ObservableCollection<Sertifikasi>(contextpay.sertifikasis.Query(a => a.invalid != true));
            Sertifikasis.CollectionChanged += Sertifikasis_CollectionChanged;

            Pajaks = new ObservableCollection<Pajak>(contextpay.pajaks.Query(a => a.invalid != true));
            Pajaks.CollectionChanged += Pajaks_CollectionChanged;

        }

        public void AddSertifikasi(Sertifikasi sertifikasi)
        {
            Sertifikasis.Add(sertifikasi);
            contextpay.sertifikasis.Insert(sertifikasi);
            contextpay.SaveChanges();
        }

        public bool UpdateSertifikasi(Sertifikasi sertifikasi, bool save = true)
        {
            if (sertifikasi == null)
                return false;
            var pos = Sertifikasis.IndexOf(sertifikasi);
            if (pos == -1)
                return false;
            contextpay.sertifikasis.Update(sertifikasi);
            if (save)
                contextpay.SaveChanges();
            return true;
        }

        public List<ISertifikasi> OpenedSertifikasi()
            => Sertifikasis.Where(a => a.invalid != true).ToList<ISertifikasi>();

        public ISertifikasi GetSertifikasi(string key)
            => Sertifikasis.FirstOrDefault(a => a.key == key);

        public void AddPajak(Pajak pajak)
        {
            Pajaks.Add(pajak);
            contextpay.pajaks.Insert(pajak);
            contextpay.SaveChanges();
        }

        public bool UpdatePajak(Pajak pajak, bool save = true)
        {
            if (pajak == null)
                return false;
            var pos = Pajaks.IndexOf(pajak);
            if (pos == -1)
                return false;
            contextpay.pajaks.Update(pajak);
            if (save)
                contextpay.SaveChanges();
            return true;
        }

        public List<IPajak> OpenedPajak()
            => Pajaks.Where(a => a.invalid != true).ToList<IPajak>();

        public IPajak GetPajak(string key)
            => Pajaks.FirstOrDefault(a => a.key == key);

        private void Sertifikasis_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    Sertifikasis = new ObservableCollection<Sertifikasi>(contextpay.sertifikasis.Query(a => a.invalid != true));
                    Sertifikasis.CollectionChanged += Sertifikasis_CollectionChanged;
                    break;
            }
        }

        private void Pajaks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    Pajaks = new ObservableCollection<Pajak>(contextpay.pajaks.Query(a => a.invalid != true));
                    Pajaks.CollectionChanged += Pajaks_CollectionChanged;
                    break;
            }
        }
    }
}
