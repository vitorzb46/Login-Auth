using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Services.Interfaces;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace LoginAuth.Worker;

public sealed class EmailProcessingWorker(Channel<EmailRegistrationEvent> channel,
                                          IServiceProvider serviceProvider,
                                          ILogger<EmailProcessingWorker> logger) : BackgroundService
{
    private readonly Channel<EmailRegistrationEvent> _channel = channel;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<EmailProcessingWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var registration in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                string verificationCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                DateTime expirationTime = DateTime.UtcNow.AddMinutes(15);
                using (var scope = _serviceProvider.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var db = scope.ServiceProvider.GetRequiredService<DatabaseAbstract>();

                    string query = @"INSERT INTO EmailConfirmation (AccountId, Code, ExpiresAt)
                                     VALUES (@Id, Code, ExpiresAt)
                                     ON CONFLICT (AccountId)
                                     DO UPDATE SET

                                        Code = excluded.Code,
                                        ExpiresAt = excluded.ExpiresAt";

                    var isValid = await db.ExecuteAsync(query, new { Id = registration.UserId, Code = verificationCode, ExpiresAt = expirationTime });
                    if (!isValid) return;

                    string htmlBody = $@"
                <p>Your account activation code is: <strong>{verificationCode}</strong></p>
                <p>Enter this code on our <a href='http://localhost:5265/email/confirmation'>Email Confirmation page</a> to verify your account.</p>
                <p style='color: #666; font-size: 12px;'>Note: This code is valid for 15 minutes.</p>";


                    await emailService.SendEmailAsync(
                        to: registration.Email,
                        subject: "Account confirmation - Activation code",
                        htmlContent: htmlBody,
                        cancellationToken: stoppingToken
                    );
                }

                string message = $"Code successfully sent to {registration.Email}.";
                _logger.LogInformation(message);
            }
            catch (Exception ex)
            {
                string message = $"Failure to sent activation code to user {registration.UserId}";
                _logger.LogError(ex, message);
            }
        }
    }
}