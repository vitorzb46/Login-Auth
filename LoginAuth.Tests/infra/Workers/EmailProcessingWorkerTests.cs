using System.Threading.Channels;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Interfaces;
using LoginAuth.Model;
using LoginAuth.Services.Interfaces;
using LoginAuth.Worker;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LoginAuth.Tests.infra.Workers;

public class EmailProcessingWorkerTests
{
    private ApiWebApplicationFactory _factory;
    private HttpClient _client;
    private IServiceScope _scope;
    private DatabaseAbstract _db;
    private Channel<IEmailEvent> _channel;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiWebApplicationFactory();
        _client = _factory.CreateClient();
    }
    
    [SetUp]
    public async Task SetUp()
    {
        _scope = _factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<DatabaseAbstract>();
        _channel = _scope.ServiceProvider.GetRequiredService<Channel<IEmailEvent>>();

        await _db.ExecuteAsync("DELETE FROM EmailConfirmation; DELETE FROM Account;");       
    }

    [Test]
    public async Task Worker_WhenEventIsPublished_ShouldSaveDbAndSendEmail()
    {
        var userId = 99;
        var email = "worker_test@test.com";

        var acc = _db.Add(new Account
        {
            Id = userId,
            Email = email,
            PasswordHash = "x",
            EmailConfirmed = 0,
            CreatedAt = DateTime.UtcNow
        });
        Assert.That(acc, Is.Not.Null);

        var eventToPublish = new EmailRegistrationEvent(userId, email);
        var emailService = _factory.Services.GetRequiredService<IEmailService>() as FakeEmailService;
        Assert.That(emailService, Is.Not.Null);
        
        await _channel.Writer.WriteAsync(eventToPublish);
        Task completedTask = await Task.WhenAny(emailService.EmailSentSignal.Task, Task.Delay(5000));
        
        using (Assert.EnterMultipleScope())
        {
            Assert.That(completedTask, Is.EqualTo(emailService.EmailSentSignal.Task));            
            Assert.That(emailService.SentEmails, Has.Count.EqualTo(1));
            var sent = emailService.SentEmails.First();
            Assert.That(sent.To, Is.EqualTo(email));
            Assert.That(sent.HtmlContent, Does.Contain("Your account activation code is"));
            var acc2 = _db.FindByID<EmailConfirmation>(userId);
            Assert.That(acc2, Is.Not.Null);
            Assert.That(acc2?.Code.Length, Is.EqualTo(6));
        }
    }

    [Test]
    public async Task Worker_ProcessPasswordRecovery_ShouldSaveTokenAndSendEmail()
    {
        // Arrange
        // NÃO removemos o Worker! Precisamos dele rodando para processar a fila.
        using var localFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Apenas garantimos que o FakeEmailService seja usado
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService, FakeEmailService>(); 
            });
        });
        
        using var localClient = localFactory.CreateClient();
        using var scope = localFactory.Services.CreateScope();
        var channel = scope.ServiceProvider.GetRequiredService<Channel<IEmailEvent>>();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseAbstract>();
        
        // Pega a instância do FakeEmailService que foi injetada
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>() as FakeEmailService;
        Assert.That(emailService, Is.Not.Null);

        var userId = 100;
        var email = "recovery@test.com";
        var eventToPublish = new PasswordRecoveryEvent(userId, email);
        
        // Act
        await channel.Writer.WriteAsync(eventToPublish);
        
        // Agora esperamos o Worker (que está vivo) consumir a mensagem e chamar o FakeEmailService
        var completedTask = await Task.WhenAny(emailService.EmailSentSignal.Task, Task.Delay(3000));
        Assert.That(completedTask, Is.EqualTo(emailService.EmailSentSignal.Task), "O worker não processou o e-mail a tempo.");
        
        // Assert
        using (Assert.EnterMultipleScope())
        {                    
            var sent = emailService.SentEmails.First();
            Assert.That(sent.To, Is.EqualTo(email));
            Assert.That(sent.Subject, Does.Contain("Password recovery"));
            Assert.That(sent.HtmlContent, Does.Contain("reset-password?token="));
            var savedReset = db.FindByID<PasswordReset>(userId);
            Assert.That(savedReset, Is.Not.Null);
            Assert.That(savedReset?.Token, Is.Not.Empty);
        }
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}