using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;

namespace Xtensions.Core.Swagger
{
    public static class Startup
    {
        public static void AddMySwaggerGen(this IServiceCollection services,
            string assName, string xmlBasedir,
            string apiversion, string apititle, string apidesc,
            string apiauthor, string apiurl, string apiemail, string apilic)
        {
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


                options.SwaggerDoc(apiversion, new OpenApiInfo
                {
                    Version = apiversion,
                    Title = apititle,
                    Description = apidesc,
                    TermsOfService = new Uri($"{apiurl}/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = apiauthor,
                        Email = apiemail,
                        Url = new Uri($"{apiurl}"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = apilic,
                        Url = new Uri($"{apiurl}/license"),
                    }
                });


                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{assName}.xml";
                var xmlPath = Path.Combine(xmlBasedir, xmlFile);
                options.IncludeXmlComments(xmlPath);

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
