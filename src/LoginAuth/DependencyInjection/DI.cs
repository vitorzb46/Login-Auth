using LoginAuth.Data;
using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using LoginAuth.Security;
using Microsoft.AspNetCore.Identity;

namespace LoginAuth.DependencyInjection;

static class DI
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDependencies()
        {
            services.AddSingleton<PasswordHasher<string>>();
            services.AddSingleton<AccountPasswordHasher>();
            services.AddScoped<DatabaseAbstract<Account>, Database>();
            return services;
        }
    }
}
