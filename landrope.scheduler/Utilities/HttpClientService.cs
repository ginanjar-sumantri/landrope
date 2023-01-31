using landrope.scheduler.Interfaces.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace landrope.scheduler.Utilities
{
    public class HttpClientService : IHttpClientService
    {
        HttpClient _client = new();

        public Task<HttpResponseMessage> DeleteAsync(string uri)
            => _client.DeleteAsync(uri);

        public Task<HttpResponseMessage> GetAsync(string uri)
            => _client.GetAsync(uri);

        public Task<HttpResponseMessage> PatchAsync(string uri, HttpContent content = null)
            => _client.PatchAsync(uri, content);

        public Task<HttpResponseMessage> PostAsync(string uri, HttpContent content = null)
            => _client.PostAsync(uri, content);

        public Task<HttpResponseMessage> PutAsync(string uri, HttpContent content = null)
            => _client.PutAsync(uri, content);
    }
}
