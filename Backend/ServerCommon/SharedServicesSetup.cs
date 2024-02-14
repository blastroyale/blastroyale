using Backend.Game.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ServerCommon.CommonServices;

namespace ServerCommon
{
    public static class SharedSetup
    {
        public static void SetupSharedServices(this IMvcBuilder builder, string appPath)
        {
            var services = builder.Services;
            var envConfig = new EnvironmentVariablesConfigurationService(appPath);
            if (envConfig.TelemetryConnectionString != null)
            {
                services.AddApplicationInsightsTelemetry(o => o.ConnectionString = envConfig.TelemetryConnectionString);
                services.AddSingleton<IMetricsService, AppInsightsMetrics>();
            }
            else
            {
                services.AddSingleton<IMetricsService, NoMetrics>();
            }
            services.AddSingleton<ILogger, ILogger>(l =>
            {
                return l.GetService<ILoggerFactory>()!.CreateLogger("Common");
            });

            builder.AddNewtonsoftJson(); // cloudscript specifically requires newtonsoft as it does not add [Serializable] attrs
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme()
                {
                    Name = "key",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKey",
                    BearerFormat = "ApiKey",
                    In = ParameterLocation.Query,
                    Description = "Api Key"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        new string[] { }
                    }
                });
            });
            
            services.AddSingleton<IBaseServiceConfiguration>(p => envConfig);
        }
    }
}