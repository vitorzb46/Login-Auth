namespace LoginAuth.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlContent, CancellationToken cancellationToken = default);
}
