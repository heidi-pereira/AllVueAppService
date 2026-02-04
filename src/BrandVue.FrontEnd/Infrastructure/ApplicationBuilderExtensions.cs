using BrandVue.EntityFramework;
using BrandVue.Middleware;
using Microsoft.AspNetCore.Builder;

namespace BrandVue.Infrastructure
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseSavantaExceptionHandling(this IApplicationBuilder app, string environment)
        {
            app.UseWhen(context => context.IsRequestedResourceApi(), appBuilder =>
            {
                appBuilder.AddSavantaApiExceptionHandling();
            });

            app.UseWhen(context => !context.IsRequestedResourceApi(), appBuilder =>
            {
                appBuilder.AddSavantaUiExceptionHandling(environment);
            });
        }

        private static void AddSavantaApiExceptionHandling(this IApplicationBuilder app)
        {
            app.UseMiddleware<ApiExceptionHandlingMiddleware>();
        }

        private static void AddSavantaUiExceptionHandling(this IApplicationBuilder app, string environment)
        {
            if (environment.Equals(AppSettings.DevEnvironmentName, StringComparison.OrdinalIgnoreCase))
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
        }
    }
}
