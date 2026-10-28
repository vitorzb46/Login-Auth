using LoginAuth.Data;
using LoginAuth.Data.Abstraction;
using LoginAuth.Model;

namespace LoginAuth.DependencyInjection;

static class DI
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDependencies()
        {
            services.AddScoped<DatabaseAbstract<Account>, Database>();
            return services;
        }
    }
}
