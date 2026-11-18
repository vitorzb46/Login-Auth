using System.Threading.Channels;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Interfaces;
using LoginAuth.Model;
using LoginAuth.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Workers;

public class EmailProcessingWorkerTests
{
    private ApiWebApplicationFactory _factory;
    private HttpClient _client;
    private IServiceScope _scope;
    private DatabaseAbstract _db;
    private Channel<IEmailEvent> _channel;
    private FakeEmailService? _emailService;

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
        _emailService = _scope.ServiceProvider.GetRequiredService<IEmailService>() as FakeEmailService;
        await _db.ExecuteAsync("DELETE FROM EmailConfirmation; DELETE FROM Account;");        
    }

    [Test]
    public async Task Worker_WhenEventIsPublished_ShouldSaveDbAndSendEmail()
    {
        var email = "worker_test@test.com";
        var pwd = "dummy";
        var acc = _db.Add(new Account { Email = email, PasswordHash = pwd });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(acc, Is.Not.Null);
            Assert.That(_emailService, Is.Not.Null);
        }
        _emailService.Reset();
        
        var eventToPublish = new EmailRegistrationEvent(acc.Id, acc.Email);
        await _channel.Writer.WriteAsync(eventToPublish);

        Task completedTask = await Task.WhenAny(_emailService.EmailSentSignal.Task, Task.Delay(5000));
        Assert.That(completedTask, Is.EqualTo(_emailService.EmailSentSignal.Task));

        using (Assert.EnterMultipleScope())
        {       
            Assert.That(_emailService.SentEmails, Has.Count.EqualTo(1));
            var sent = _emailService.SentEmails.First();
            Assert.That(sent.To, Is.EqualTo(acc.Email));
            Assert.That(sent.HtmlContent, Does.Contain("Your account activation code is"));
            var acc2 = _db.FindByID<EmailConfirmation>(acc.Id);
            Assert.That(acc2, Is.Not.Null);
            Assert.That(acc2?.Code.Length, Is.EqualTo(6));            
        }
    }

    [Test]
    public async Task Worker_ProcessPasswordRecovery_ShouldSaveTokenAndSendEmail()
    {
        var email = "recovery@test.com";
        var pwd = "dummy";
        var acc = _db.Add(new Account { Email = email, PasswordHash = pwd });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(acc, Is.Not.Null);
            Assert.That(_emailService, Is.Not.Null);
        }
        _emailService.Reset();        
        
        var eventToPublish = new PasswordRecoveryEvent(acc.Id, acc.Email);
        await _channel.Writer.WriteAsync(eventToPublish);

        var completedTask = await Task.WhenAny(_emailService.EmailSentSignal.Task, Task.Delay(3000));
        Assert.That(completedTask, Is.EqualTo(_emailService.EmailSentSignal.Task));
        
        using (Assert.EnterMultipleScope())
        {                    
            var sent = _emailService.SentEmails.First();
            Assert.That(sent.To, Is.EqualTo(email));
            Assert.That(sent.Subject, Does.Contain("Password recovery"));
            Assert.That(sent.HtmlContent, Does.Contain("reset-password?token="));
            
            var savedReset = _db.FindByID<PasswordReset>(acc.Id);
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