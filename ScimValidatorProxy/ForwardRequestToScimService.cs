using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ScimValidatorProxy.Controllers;

namespace ScimValidatorProxy
{
    public class ForwardRequestToScimService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly RequestDelegate next;

        public ForwardRequestToScimService(IServiceProvider serviceProvider, RequestDelegate next)
        {
            this.serviceProvider = serviceProvider;
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var proxy = serviceProvider.GetService<IProxy>();
            await proxy.ForwardAsync(context);
            await next(context);
        }
    }
}