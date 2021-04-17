using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Xtensions.Core.Swagger
{
    /*
    
    in Startup -> ConfigureServices -> add:

        services.AddSwaggerGen(options =>
            {
                options.DocumentFilter<MethodOrderFilter>();
            }
        );

    then place order attribute before method declaration
    
        [MethodOrder(2)] //for example

     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MethodOrderAttribute : Attribute
    {
        public int Order { get; }

        public MethodOrderAttribute(int order)
        {
            this.Order = order;
        }
    }

    public class MethodOrderFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument openApiDoc, DocumentFilterContext context)
        {
            Dictionary<KeyValuePair<string, OpenApiPathItem>, int> paths = new Dictionary<KeyValuePair<string, OpenApiPathItem>, int>();
            foreach (var path in openApiDoc.Paths)
            {
                MethodOrderAttribute orderAttribute = context.ApiDescriptions.FirstOrDefault(x => x.RelativePath.Replace("/", string.Empty)
                    .Equals(path.Key.Replace("/", string.Empty), StringComparison.InvariantCultureIgnoreCase))?
                    .ActionDescriptor?.EndpointMetadata?.FirstOrDefault(x => x is MethodOrderAttribute) as MethodOrderAttribute;

                //if (orderAttribute == null)
                //    throw new ArgumentNullException("there is no order for operation " + path.Key);

                //int order = orderAttribute.Order;
                int order = orderAttribute?.Order ?? int.MaxValue;
                paths.Add(path, order);
            }

            var orderedPaths = paths.OrderBy(x => x.Value).ToList();
            openApiDoc.Paths.Clear();
            orderedPaths.ForEach(x => openApiDoc.Paths.Add(x.Key.Key, x.Key.Value));
        }

    }
}
