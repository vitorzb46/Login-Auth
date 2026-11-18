using LoginAuth.Services.Interfaces;

namespace LoginAuth.Tests;

public sealed class FakeEmailService : IEmailService
{
    public List<(string To, string Subject, string HtmlContent)> SentEmails { get; } = [];
    public TaskCompletionSource<bool> EmailSentSignal { get; private set; } = new();

    public Task<bool> SendEmailAsync(string to, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((to, subject, htmlContent));
        EmailSentSignal.TrySetResult(true);
        return Task.FromResult(true);
    }

    public void Reset()
    {
        SentEmails.Clear();
        EmailSentSignal = new TaskCompletionSource<bool>();
    }
}