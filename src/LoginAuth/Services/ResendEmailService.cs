using System.Text.Json.Serialization;
using LoginAuth.Services.Interfaces;

namespace LoginAuth.Services;

public sealed class ResendEmailService(HttpClient httpClient, ILogger<ResendEmailService> logger) : IEmailService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ResendEmailService> _logger = logger;

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new ResendEmailRequest
            {
                From = "Login-Auth <onboarding@resend.dev>", 
                To = [to],
                Subject = subject,
                Html = htmlContent
            };

            var response = await _httpClient.PostAsJsonAsync("emails", payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string message = $"E-mail successfully sent to {to}.";
                _logger.LogInformation(message);
                return true;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            string message1 = $"Fail to send e-mail. Status: {response.StatusCode}. Awnser: {errorContent}";
            _logger.LogError(message1);
            return false;
        }
        catch (Exception ex)
        {
            string message = $"Internal error by trying to send e-mail to {to}.";
            _logger.LogError(ex, message);
            return false;
        }
    }

    private sealed class ResendEmailRequest
    {
        [JsonPropertyName("from")]
        public required string From { get; set; }

        [JsonPropertyName("to")]
        public required string[] To { get; set; }

        [JsonPropertyName("subject")]
        public required string Subject { get; set; }

        [JsonPropertyName("html")]
        public required string Html { get; set; }
    }

}
