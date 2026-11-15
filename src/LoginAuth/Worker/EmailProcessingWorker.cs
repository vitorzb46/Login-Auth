using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Interfaces;
using LoginAuth.Services.Interfaces;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace LoginAuth.Worker;

public sealed class EmailProcessingWorker(Channel<IEmailEvent> channel,
                                          IServiceScopeFactory scopeFactory,
                                          ILogger<EmailProcessingWorker> logger) : BackgroundService
{
    private IEmailService? _emailService;
    private DatabaseAbstract? _db;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var emailEvent in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                _emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                _db = scope.ServiceProvider.GetRequiredService<DatabaseAbstract>();

                await (emailEvent switch
                {
                    EmailRegistrationEvent reg => ProcessRegistrationAsync(reg, stoppingToken),
                    PasswordRecoveryEvent pwd  => ProcessPasswordRecoveryAsync(pwd, stoppingToken),
                    _                          => HandleUnknownEvent()
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fail to proccess email event -> {ex.Message}");
            }
        }
    }

    private static Task HandleUnknownEvent()
    {
        Debug.WriteLine("Event type unknown.");
        return Task.CompletedTask;
    }

    private async Task ProcessPasswordRecoveryAsync(PasswordRecoveryEvent pwd,
                                                    CancellationToken stoppingToken)
    {
        string resetToken = Guid.NewGuid().ToString("N");
        DateTime expiration = DateTime.UtcNow.AddHours(1);
        string query = @"INSERT INTO PasswordReset (AccountId, Token, ExpiresAt)
                            VALUES (@Id, @Token, @ExpiresAt)
                            ON CONFLICT (AccountId) 
                            DO UPDATE SET 
                                Token = excluded.Token, ExpiresAt = excluded.ExpiresAt";
        string htmlBody = $@"
            <p>You requested a password reset.</p>
            <p><a href='http://localhost:5265/reset-password?token={resetToken}'>Click here to reset your password</a></p>
            <p>This link expires in 1 hours.</p>";
            
        await PersistCodeAndSendEmailAsync(
            emailEvent: pwd,
            query: query,
            queryParams: new { Id = pwd.UserId, Token = resetToken, ExpiresAt = expiration },
            subject: "Password recovery",
            htmlBody: htmlBody,
            stoppingToken: stoppingToken
        );
    }
    
    private async Task ProcessRegistrationAsync(EmailRegistrationEvent reg,
                                                CancellationToken stoppingToken)
    {
        try
        {
            string verificationCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            DateTime expirationTime = DateTime.UtcNow.AddMinutes(15);
            string query = @"INSERT INTO EmailConfirmation (AccountId, Code, ExpiresAt)
                                VALUES (@Id, @Code, @ExpiresAt)
                                ON CONFLICT (AccountId)
                                DO UPDATE SET
                                    Code = excluded.Code,
                                    ExpiresAt = excluded.ExpiresAt";
            string htmlBody = $@"
                <p>Your account activation code is: <strong>{verificationCode}</strong></p>
                <p>Enter this code on our <a href='http://localhost:5265/email/confirmation'>Email Confirmation page</a> to verify your account.</p>
                <p style='color: #666; font-size: 12px;'>Note: This code is valid for 15 minutes.</p>";

            await PersistCodeAndSendEmailAsync(
                emailEvent: reg,
                query: query,
                queryParams: new { Id = reg.UserId, Code = verificationCode, ExpiresAt = expirationTime },
                subject: "Account confirmation - Activation code",
                htmlBody: htmlBody,
                stoppingToken: stoppingToken
            );
        }
        catch (Exception ex)
        {
            string message = $"Failure to sent activation code to user {reg.UserId}";
            Debug.WriteLine(ex, message);
        }
    }

    private async Task PersistCodeAndSendEmailAsync<TEvent>(TEvent emailEvent,
                                                                 string query,
                                                                 object queryParams,
                                                                 string subject,
                                                                 string htmlBody,
                                                                 CancellationToken stoppingToken) where TEvent : IEmailEvent
    {
        if (_db is null || _emailService is null) return;
        
        var isValid = await _db.ExecuteAsync(query, queryParams);
        if (!isValid)
        {
            logger.LogWarning("Fail to persist code to {UserId}", emailEvent.UserId);
            return;
        }

        await _emailService.SendEmailAsync(
            to: emailEvent.Email,
            subject: subject,
            htmlContent: htmlBody,
            cancellationToken: stoppingToken
        );
    }
}