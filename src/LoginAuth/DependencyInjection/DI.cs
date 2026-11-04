using LoginAuth.Data;
using LoginAuth.Data.Abstraction;
using LoginAuth.ErrorMessages;
using LoginAuth.Model;
using LoginAuth.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace LoginAuth.DependencyInjection;

static class DI
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDependencies(WebApplicationBuilder builder)
        {
            services.JwtAuthentication(services, builder);
            services.RateLimiter(services);

            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<PasswordHasher<string>>();
            services.AddSingleton<AccountPasswordHasher>();
            services.AddScoped<DatabaseAbstract<Account>, Database>();
            services.AddScoped<DatabaseAbstract<RefreshToken>, TokenTable>();
            services.AddSingleton<Tokens>();
            return services;
        }

        private IServiceCollection JwtAuthentication(IServiceCollection sc, WebApplicationBuilder builder)
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
            return sc;
        }

        private IServiceCollection RateLimiter(IServiceCollection sc)
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
            return sc;
        }
    }
}
