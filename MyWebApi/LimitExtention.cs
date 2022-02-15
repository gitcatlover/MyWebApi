using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace MyWebApi
{
    public static class LimitExtention
    {
        //private static readonly ILogger _logger = new LoggerConfiguration().CreateLogger();
        public static IApplicationBuilder UseIpLimit(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            try
            {
                if (AppSettingsHelper.GetApp(new string[] { "Middleware", "IpRateLimit", "Enabled" }))
                {
                    return app.UseIpRateLimiting();
                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        public static IServiceCollection AddIpPolicyRateLimit(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentException(nameof(services));
            }
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
            services.AddInMemoryRateLimiting();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            return services;
        }
    }
}
