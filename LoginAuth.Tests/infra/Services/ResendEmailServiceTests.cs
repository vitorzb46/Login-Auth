using System.Net;
using LoginAuth.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoginAuth.Tests.infra.Services;

public class ResendEmailServiceTests
{
    private class FakeHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode = statusCode;
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }

    [Test]
    public async Task SendEmailAsync_WhenResendReturnsOk_ShouldReturnTrue()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var service = new ResendEmailService(httpClient, NullLogger<ResendEmailService>.Instance);
        var result = await service.SendEmailAsync("test@test.com", "Subject", "<p>HTML</p>");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(handler.LastRequest, Is.Not.Null);
            Assert.That(handler.LastRequest!.RequestUri!.ToString(), Is.EqualTo("https://api.resend.com/emails"));
            Assert.That(service.ContentMessage.EmailSent, Is.True);
        }
    }

    [Test]
    public async Task SendEmailAsync_WhenResendFails_ShouldReturnFalse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var service = new ResendEmailService(httpClient, NullLogger<ResendEmailService>.Instance);
        var result = await service.SendEmailAsync("test@test.com", "Subject", "<p>HTML</p>");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(service.ContentMessage.EmailSent, Is.False);
        }
    }
}