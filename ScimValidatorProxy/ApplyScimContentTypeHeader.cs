using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ScimValidatorProxy
{
    public class ApplyScimContentTypeHeader
    {
        private readonly RequestDelegate next;

        public ApplyScimContentTypeHeader(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(ApplyHeader);
            await next(context);
            Task ApplyHeader()
            {
                if (context.Response.StatusCode == 200)
                {
                    context.Response.Headers["Content-Type"] = "application/scim+json";
                }

                return Task.CompletedTask;
            }
        }
    }
}