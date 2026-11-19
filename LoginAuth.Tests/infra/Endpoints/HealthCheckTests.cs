using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LoginAuth.Tests.infra.Endpoints;

public sealed class HealthCheckTests
{
    private ApiWebApplicationFactory _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiWebApplicationFactory();
        _client = _factory.CreateClient();
    }
    
    [Test]
    public async Task HealthLive_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task HealthReady_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task HealthReady_WhenDatabaseIsDown_ShouldReturnServiceUnavailable()
    {
        using var localFactory = _factory.WithWebHostBuilder(ServicesConfig());

        using var localClient = localFactory.CreateClient();
        var response = await localClient.GetAsync("/health/ready");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable)); // 503


        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo("Unhealthy"));
    }

    private static Action<Microsoft.AspNetCore.Hosting.IWebHostBuilder> ServicesConfig()
    {
        return builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());

                services.AddHealthChecks()
                    .AddCheck("database", () =>
                        HealthCheckResult.Unhealthy("Database is down!"), tags: ["ready"]);
            });
        };
    }


    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory.Dispose();
        _client.Dispose();
    }

}
