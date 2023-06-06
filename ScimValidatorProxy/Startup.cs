using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SCIM;
using ScimValidatorProxy.Controllers;

namespace ScimValidatorProxy
{
    public class Startup
    {
        private readonly IWebHostEnvironment environment;

        public IMonitor MonitoringBehavior { get; set; }
        public IProvider ProviderBehavior { get; set; }

        public Startup(IWebHostEnvironment env)
        {
            environment = env;
            MonitoringBehavior = new ConsoleMonitor();
        }

        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(IMonitor), MonitoringBehavior)
                    .AddSingleton(typeof(IProxy), new Proxy("49975", "49977"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseMiddleware<ForwardRequestToScimService>();
        }
    }
}
