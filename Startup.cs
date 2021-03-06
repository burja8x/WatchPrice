using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ros4
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAzureAppConfiguration();
            services.AddHealthChecks().AddCheck<WssHealthCheck>("Binance Websocket healthcheck");
            services.AddHealthChecks().AddSqlServer(Configuration.GetSection("WatchPrice:Settings:SQLConn").Value);
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAzureAppConfiguration();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health/ready");
                endpoints.MapHealthChecks("/health/live");
                endpoints.MapControllers();
            });

            Kline.KillOnError = Configuration.GetSection("WatchPrice:Settings:KillOnError").Value.Contains("true");
            Kline.endpoint = Configuration.GetSection("WatchPrice:Settings:WSSURL").Value;
            Data.sqlConnStr = Configuration.GetSection("WatchPrice:Settings:SQLConn").Value;
            Data.xurl = Configuration.GetSection("WatchPrice:Settings:xurl").Value;

            string text = File.ReadAllText("logConfig.json");
            text = text.Replace("CHANGE_ME_TCPSink_URI", Configuration.GetSection("Report:Settings:SerilogURI").Value);
            File.WriteAllText("logConfig1.json", text);
            var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("logConfig1.json")
                    .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            //app.UseSerilogRequestLogging();

            Core core = new Core();
        }
    }
}
