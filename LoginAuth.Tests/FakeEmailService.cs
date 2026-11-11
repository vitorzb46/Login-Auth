using LoginAuth.Services.Interfaces;

namespace LoginAuth.Tests;

public sealed class FakeEmailService : IEmailService
{
    public List<(string To, string Subject, string HtmlContent)> SentEmails { get; } = new();
    public TaskCompletionSource<bool> EmailSentSignal { get; } = new();

    public Task<bool> SendEmailAsync(string to, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((to, subject, htmlContent));
        EmailSentSignal.TrySetResult(true);
        return Task.FromResult(true);
    }
}