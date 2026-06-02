using System.Text;
using ComposeNowGateway.Models;
using ComposeNowGateway.Services.Auth;
using ComposeNowGateway.Services.Proxy;
using ComposeNowGateway.Services.Sessions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace ComposeNowGateway.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<GatewayOptions>(configuration.GetSection("Gateway"));
        services.AddControllers();
        services.AddHttpClient<ISiteAuthClient, SiteAuthClient>();
        services.AddScoped<IGatewayTokenValidator, GatewayTokenValidator>();
        services.AddScoped<IGatewaySessionStore, RedisGatewaySessionStore>();
        services.AddScoped<IWebSocketProxyService, WebSocketProxyService>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
            var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "gateway-redis";

            return ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { $"{redisHost}:6379" },
                Password = redisPassword,
                AbortOnConnectFail = false
            });
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                string signingKey = configuration["Jwt:SigningKey"]
                    ?? Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
                    ?? "compose-now-development-signing-key-change-me-please-32";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "ComposeNowSite",
                    ValidAudience = configuration["Jwt:Audience"] ?? "ComposeNowClients",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    NameClaimType = "login",
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });
        services.AddAuthorization();

        return services;
    }
}
