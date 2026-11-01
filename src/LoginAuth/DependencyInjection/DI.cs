using System.Text;
using LoginAuth.Data;
using LoginAuth.Data.Abstraction;
using LoginAuth.ErrorMessages;
using LoginAuth.Model;
using LoginAuth.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace LoginAuth.DependencyInjection;

static class DI
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDependencies(WebApplicationBuilder builder)
        {
            services.JwtAuthentication(services, builder);
            services.AddSingleton<PasswordHasher<string>>();
            services.AddSingleton<AccountPasswordHasher>();
            services.AddScoped<DatabaseAbstract<Account>, Database>();
            services.AddScoped<DatabaseAbstract<RefreshToken>, TokenTable>();
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
    }
}
