using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Reflection;

namespace Xtensions.Core.Swagger
{
    /// <summary>
    /// swagger helpers to be invoked in the configure services method of the app
    /// </summary>
    public static class SwaggerConfigureServices
    {
        /// <summary>
        /// custom AssSwaggerGen
        /// </summary>
        /// <param name="services"></param>
        /// <param name="ass"></param>
        /// <param name="apititle"></param>
        /// <param name="apiversion"></param>
        /// <param name="apidesc"></param>
        /// <param name="author"></param>
        /// <param name="email"></param>
        /// <param name="website"></param>
        /// <param name="license"></param>
        /// <param name="lictype"></param>
        public static void AddMySwaggerGen(this IServiceCollection services,
            Assembly ass,
            string apititle, int apiversion, string apidesc,
            string author, string email,
            string website, string license, string lictype)
        {
            var assName = ass.GetName().Name;
            var xmlBasedir = AppContext.BaseDirectory;
            if (assName != apititle) throw new Exception("apititle must match assembly name (case sensitive) and project name");
            services.AddSwaggerGen(options =>
            {

                options.DescribeAllParametersInCamelCase();

                //make it possible to order methods with [MethodOrder(n)]
                options.DocumentFilter<MethodOrderFilter>();

                /* Enable Swagger examples
                options.OperationFilter<ExamplesOperationFilter>();
                options.OperationFilter<DescriptionOperationFilter>();
                options.OperationFilter<AddResponseHeadersFilter>();
                options.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();*/


                options.SwaggerDoc($"v{apiversion}", new OpenApiInfo
                {
                    Version = $"v{apiversion}",
                    Title = apititle,
                    Description = apidesc,
                    TermsOfService = new Uri($"{license}"),
                    Contact = new OpenApiContact
                    {
                        Name = author,
                        Email = email,
                        Url = new Uri($"{website}"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = $"Use under {lictype}",
                        Url = new Uri($"{license}"),
                    }
                });


                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{assName}.xml";
                var xmlPath = Path.Combine(xmlBasedir, xmlFile);
                if (!File.Exists(xmlPath)) throw new Exception($"XML Comments : file not found ({xmlPath})");
                options.IncludeXmlComments(xmlPath, true);

                //options.OperationFilter<SwaggerFileOperationFilter>();


                // tag controller with:
                //    [ApiExplorerSettings(GroupName = "_test", IgnoreApi = false)]
                // so the methods are put in a group
                options.TagActionsBy(api =>
                {
                    if (api.GroupName != null)
                        return new[] { api.GroupName };
                    if (api.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                        return new[] { controllerActionDescriptor.ControllerName };
                    throw new InvalidOperationException("Unable to determine tag for endpoint.");
                });

                options.DocInclusionPredicate((name, api) => true);
            });
        }
    }
}
