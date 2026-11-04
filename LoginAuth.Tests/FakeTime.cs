using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LoginAuth.Tests;

public class FakeTime(ApiWebApplicationFactory factory)
{
    private readonly ApiWebApplicationFactory _factory = factory;

    public void AdvanceTime(TimeSpan timeToAdvance)
    {
        var fakeTime = _factory.Services.GetRequiredService<TimeProvider>() as FakeTimeProvider;
        Assert.That(fakeTime, Is.Not.Null);
        fakeTime.Advance(timeToAdvance);
    }
}