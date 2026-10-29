using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddJsonFile("appsettings.Testing.json", optional: false));

        builder.ConfigureTestServices(services =>
        {
            services.Configure<AuthenticationOptions>(options =>
            {
                if (options.Schemes is List<AuthenticationSchemeBuilder> schemes)
                {
                    schemes.RemoveAll(s => s.Name == NegotiateDefaults.AuthenticationScheme);
                    options.SchemeMap.Remove(NegotiateDefaults.AuthenticationScheme);
                }
            });
        });
    }
}
