using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using System.Net;
using System.Linq;

namespace IdentitySample
{

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            builder.AddEnvironmentVariables("WebApp_");
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        /// <summary>
        /// see: https://github.com/aspnet/Identity/blob/79dbed5a924e96a22b23ae6c84731e0ac806c2b5/src/Microsoft.AspNetCore.Identity/IdentityServiceCollectionExtensions.cs#L46-L68
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {

            // sad but a giant hack :(
            // https://github.com/StackExchange/StackExchange.Redis/issues/410#issuecomment-220829614
            var redisHost = Configuration.GetValue<string>("Redis:Host");
            var redisPort = Configuration.GetValue<int>("Redis:Port");
            var redisIpAddress = Dns.GetHostEntryAsync(redisHost).Result.AddressList.Last();
            var redis = ConnectionMultiplexer.Connect($"{redisIpAddress}:{redisPort}");

            services.AddDataProtection()
                .PersistKeysToRedis(redis, "DataProtection-Keys");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IDataProtectionProvider provider)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            // To configure external authentication please see http://go.microsoft.com/fwlink/?LinkID=532715

            app.Run(async context =>
            {
                var protector = provider.CreateProtector("No purpose");
                await context.Response.WriteAsync((Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName) + "==");
                var data = context.Request.Query["d"].ToString();
                if (data != null)
                {
                    if (context.Request.Query["o"] == "d")
                    {
                        await context.Response.WriteAsync(protector.Unprotect(data));
                    }
                    else
                    {
                        await context.Response.WriteAsync(protector.Protect(data));
                      }
                }
            });
        }
    }
}

