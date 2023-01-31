using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace landrope.scheduler.Interfaces.Common
{
    public interface IHttpClientService
    {
        Task<HttpResponseMessage> DeleteAsync(string uri);
        Task<HttpResponseMessage> GetAsync(string uri);
        Task<HttpResponseMessage> PatchAsync(string uri, HttpContent content = null);
        Task<HttpResponseMessage> PostAsync(string uri, HttpContent content = null);
        Task<HttpResponseMessage> PutAsync(string uri, HttpContent content = null);
    }
}
