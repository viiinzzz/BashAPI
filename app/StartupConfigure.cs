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

namespace BashAPI
{
    /// <summary>
    /// The WebAPI Startup class
    /// </summary>
    public partial class Startup
    {

        /// <summary>
        /// method to configure the app
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseRouteDebugger("/routes");
            }
            else app.UseHsts();
            app.UseHttpsRedirection();


            if (env.IsProduction() || env.IsStaging() || env.IsEnvironment("Staging_2"))
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            
            app.UseRouting();
            app.UseAuthorization();

            app.UseMySwagger(nameof(BashAPI), major);

            app.UseEndpoints(endpoints => {
                endpoints.MapHealthChecks("/health")
                    //.RequireHost("www.contoso.com:5001")
                    ;
                endpoints.MapControllers();
                endpoints.MapSwagger();
            });

        }
    }
}
