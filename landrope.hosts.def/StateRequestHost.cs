using landrope.engines;
using landrope.mod4;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using landrope.common;
using System.Linq;
using System.Collections.Specialized;

namespace landrope.hosts
{
    public class StateRequestHost : IStateRequestHost
    {
        IConfiguration configuration;
        IServiceProvider services;
        LandropePayContext contextpay;
        ObservableCollection<StateRequest> UpdStateRequests = new ObservableCollection<StateRequest>();

        public StateRequestHost(IConfiguration config, IServiceProvider services)
        {
            this.configuration = config;
            this.services = services;
            contextpay = services.GetService<LandropePayContext>();

            UpdStateRequests = new ObservableCollection<StateRequest>(contextpay.stateRequests.Query(a => a.invalid != true));
            UpdStateRequests.CollectionChanged += UpdStateRequest_CollectionChanged;
        }

        public List<IStateRequest> OpenedStateRequest()
            => UpdStateRequests.Where(a => a.invalid != true).ToList<IStateRequest>();

        public List<IStateRequest> OpenedStateRequest(string[] instkeys)
            => UpdStateRequests.Where(a => a.invalid != true && instkeys.Contains(a.instKey)).ToList<IStateRequest>();

        public IStateRequest GetStateRequest(string key)
            => UpdStateRequests.FirstOrDefault(a => a.key == key);

        public void Add(StateRequest data)
        {
            UpdStateRequests.Add(data);
            contextpay.stateRequests.Insert(data);
            contextpay.SaveChanges();
        }

        public bool Update(StateRequest data, bool save = true)
        {
            if (data == null)
                return false;
            var pos = UpdStateRequests.IndexOf(data);
            if (pos == -1)
                return false;
            contextpay.stateRequests.Update(data);
            if (save)
                contextpay.SaveChanges();
            return true;
        }

        private void UpdStateRequest_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    UpdStateRequests = new ObservableCollection<StateRequest>(contextpay.stateRequests.Query(a => a.invalid != true));
                    UpdStateRequests.CollectionChanged += UpdStateRequest_CollectionChanged;
                    break;
            }
        }
    }
}
