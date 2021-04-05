using Microsoft.AspNetCore.Builder;

namespace Xtensions.Core.Swagger
{
    public static class Configure
    {
        public static void UseMySwagger(this IApplicationBuilder app, string apiname,
            string endpoint = "/swagger/v1/swagger.json")
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint(endpoint, apiname);
                c.RoutePrefix = string.Empty;
            });
        }
    }
}
