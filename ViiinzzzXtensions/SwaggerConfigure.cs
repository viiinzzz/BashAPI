using Microsoft.AspNetCore.Builder;

namespace Xtensions.Core.Swagger
{
    /// <summary>
    /// swagger helpers to be invoked in the configure method of the app
    /// </summary>
    public static class SwaggerConfigure
    {
        /// <summary>
        /// custom UseSwagger
        /// </summary>
        public static void UseMySwagger(this IApplicationBuilder app, string apiname, int apiversion)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
                //c.RouteTemplate = $"{apiname}/swagger/v{apiversion}/swagger.json";
                c.RouteTemplate = $"swagger/v{apiversion}/swagger.json";
            });
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                //c.SwaggerEndpoint($"/{apiname}/swagger/v{apiversion}/swagger.json", apiname);
                //c.RoutePrefix = $"{apiname}/swagger";// string.Empty;
                c.SwaggerEndpoint($"/swagger/v{apiversion}/swagger.json", apiname);
                c.RoutePrefix = string.Empty;


                c.InjectStylesheet("/swagger/ui/custom.css");
            });
        }
    }
}
