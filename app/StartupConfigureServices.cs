using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using static Xtensions.Std.Copyright;
using Xtensions.Core.Swagger;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BashAPI
{
    /// <summary>
    /// The WebAPI Startup class
    /// </summary>
    public partial class Startup
    {
        /// <summary>
        /// the app settings
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// method to startup the app
        /// </summary>
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
        }


        /// <summary>
        /// method to configure the services
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            var PackageInfo = Configuration.GetSection("PackageInfo");
            var email = PackageInfo["email"];
            var website = PackageInfo["website"];
            var license = PackageInfo["license"];
            var lictype = PackageInfo["lictype"];

            var LogLevel = Configuration.GetSection("Logging").GetSection("LogLevel");
            var DefaultLogLevel = LogLevel["Default"];
            var MicrosoftLogLevel = LogLevel["Microsoft"];
            var LifetimeLogLevel = LogLevel["Microsoft.Hosting.Lifetime"];
            //Trace= 0, Debug = 1, Information = 2, Warning = 3, Error = 4, Critical = 5 et None = 6.

            services.AddHealthChecks()
                .AddCheck<MyHealthCheck>(
                    "my_health_check",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "example" })
                /*.AddCheck<MyHealthCheck>(
                    "example",
                    () => HealthCheckResult.Healthy("Example is OK!"),
                    tags: new[] { "example" },
                    args: new object[] { 5, "string" })*/
                ;
            services.AddControllers();
            services.AddMySwaggerGen(
               Assembly.GetExecutingAssembly(), nameof(BashAPI),
               major, pkgdesc, company,
               email, website, license, lictype);
        }

    }
}
