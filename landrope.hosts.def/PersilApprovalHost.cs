using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using landrope.common;
using landrope.engines;
using landrope.mod4;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace landrope.hosts
{
    public class PersilApprovalHost : IPersilRequestHost
    {
        IConfiguration configuration;
        IServiceProvider services;
        LandropePayContext contextPay;
        ObservableCollection<PersilRequest> persilApprovals = new ObservableCollection<PersilRequest>();

        public PersilApprovalHost(IConfiguration config, IServiceProvider services)
        {
            this.configuration = config;
            this.services = services;
            contextPay = services.GetService<LandropePayContext>();
            persilApprovals = new ObservableCollection<PersilRequest>(contextPay.persilApproval.Query(a => a.invalid != true));
            persilApprovals.CollectionChanged += bidangs_CollectionChanged;
        }

        public List<IPersilApproval> OpenReqNewPersil()
            => persilApprovals.Where(a => a.invalid != true).ToList<IPersilApproval>();

        public List<IPersilApproval> OpenReqNewPersilbyInstance(string[] instanceKeys)
            => persilApprovals.Where(a => a.invalid != true && instanceKeys.Contains(a.instKey)).ToList<IPersilApproval>();

        public IPersilApproval GetPersilAppByKey(string key)
            => persilApprovals.FirstOrDefault(a => a.key == key);

        public void Add(PersilRequest data)
        {
            persilApprovals.Add(data);
            contextPay.persilApproval.Insert(data);
            contextPay.SaveChanges();
        }

        public bool Update(PersilRequest data, bool save = true)
        {
            if (data == null)
                return false;
            var pos = persilApprovals.IndexOf(data);
            if (pos == -1)
                return false;
            contextPay.persilApproval.Update(data);
            if (save)
                contextPay.SaveChanges();
            return true;
        }

        private void bidangs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    persilApprovals = new ObservableCollection<PersilRequest>(contextPay.persilApproval.Query(a => a.invalid != true));
                    persilApprovals.CollectionChanged += bidangs_CollectionChanged;
                    break;
            }
        }
    }
}
