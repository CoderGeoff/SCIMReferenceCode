using System;
using System.Net.Http;

namespace ScimValidatorProxy.Controllers
{
    public interface IProxy { }

    public class Proxy : IProxy
    {
        private readonly Uri scimServiceBaseUrl;
        private readonly HttpClient httpClient = new HttpClient();

        public Proxy(Uri scimServiceBaseUrl)
        {
            this.scimServiceBaseUrl = scimServiceBaseUrl;
        }
    }
}