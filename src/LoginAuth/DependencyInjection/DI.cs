using LoginAuth.Data;
using LoginAuth.Data.Abstraction;
using LoginAuth.ErrorMessages;
using LoginAuth.Interfaces;
using LoginAuth.Security;
using LoginAuth.Services;
using LoginAuth.Services.Interfaces;
using LoginAuth.Worker;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.Channels;
using System.Threading.RateLimiting;

namespace LoginAuth.DependencyInjection;

static class DI
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDependencies(WebApplicationBuilder builder)
        {
            Authentication_Authorization(services);
            JwtAuthentication(services, builder);
            RateLimiter(services);
            Serilog(builder);
            Telemetry(services);
            HealthChecks(services);

            services.AddHttpClient<IEmailService, ResendEmailService>(ResendApi(builder));
            services.AddHostedService<EmailProcessingWorker>();

            services.AddSingleton(ChannelOptions());
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<PasswordHasher<string>>();
            services.AddSingleton<AccountPasswordHasher>();
            services.AddScoped<DatabaseAbstract, Database>();
            services.AddSingleton<Tokens>();
            services.AddSingleton<TwoFactorsAuth>();
            return services;
        }

        private static void Serilog(WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, services, loggerConfig) =>
            {
                loggerConfig
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithSpan()
                    .WriteTo.Console()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
            });
        }

        private static void Telemetry(IServiceCollection sc)
        {
            sc.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter())
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddConsoleExporter());
        }

        private static void HealthChecks(IServiceCollection sc)
        {
            sc.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
                .AddCheck("database", () =>
                {
                    try { return HealthCheckResult.Healthy(); }
                    catch { return HealthCheckResult.Unhealthy(); }
                }, tags: ["ready"]);
        }

        private static Channel<IEmailEvent> ChannelOptions()
        {
            return Channel.CreateUnbounded<IEmailEvent>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
        }

        private static void Authentication_Authorization(IServiceCollection sc)
        {
            sc.AddOpenApi();
            sc.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
            sc.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy.
                options.FallbackPolicy = options.DefaultPolicy;
            });
        }

        private static Action<HttpClient> ResendApi(WebApplicationBuilder builder)
        {
            return client =>
            {
                client.BaseAddress = new Uri("https://resend.com");

                var apiKey = builder.Configuration["Resend:ApiKey"];
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            };
        }

        private static void JwtAuthentication(IServiceCollection sc, WebApplicationBuilder builder)
        {
            string secretKey = builder.Configuration["Jwt:SecretKey"]
                ?? throw new NullReferenceException(SystemErrorMessage.SecretKeyNotFound);

            byte[] key = Encoding.ASCII.GetBytes(secretKey);
            sc.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        }

        private static void RateLimiter(IServiceCollection sc)
        {
            sc.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((long)Math.Ceiling(retryAfter.TotalSeconds)).ToString(NumberFormatInfo.InvariantInfo);
                    }
                    await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
                };

                options.AddPolicy("user-policy", httpContext =>
                {
                    var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? httpContext.User.FindFirst("sub")?.Value;

                    if (!string.IsNullOrEmpty(userId))
                    {
                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: $"user_{userId}",
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 200,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 4,
                                QueueLimit = 0
                            });
                    }

                    // Fallback - Not logged in
                    var notUserIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"ip_{notUserIp}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        });
                });

                options.AddFixedWindowLimiter("fixed-policy", opt =>
                {
                    opt.PermitLimit = 5; // 10 requests in border effect
                    opt.Window = TimeSpan.FromMinutes(1);
                });
            });
        }
    }
}
