using System.Threading.Channels;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
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
    private Channel<EmailRegistrationEvent> _channel;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiWebApplicationFactory();
        _client = _factory.CreateClient();
    }
    
    [SetUp]
    public void SetUp()
    {
        _scope = _factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<DatabaseAbstract>();
        _channel = _scope.ServiceProvider.GetRequiredService<Channel<EmailRegistrationEvent>>();
    }

    [Test]
    public async Task Worker_WhenEventIsPublished_ShouldSaveDbAndSendEmail()
    {
        var userId = "99";
        var email = "worker_test@test.com";
        var eventToPublish = new EmailRegistrationEvent(userId, email);
        var emailService = _factory.Services.GetRequiredService<IEmailService>() as FakeEmailService;
        Assert.That(emailService, Is.Not.Null, "O FakeEmailService não foi injetado corretamente na Factory.");
        
        await _channel.Writer.WriteAsync(eventToPublish);
        var completedTask = await Task.WhenAny(emailService.EmailSentSignal.Task, Task.Delay(5000));
        
        using (Assert.EnterMultipleScope())
        {
            Assert.That(completedTask, Is.EqualTo(emailService.EmailSentSignal.Task));            
            Assert.That(emailService.SentEmails.Count, Is.EqualTo(1));
            var sent = emailService.SentEmails.First();
            Assert.That(sent.To, Is.EqualTo(email));
            Assert.That(sent.HtmlContent, Does.Contain("Your account activation code is"));
            var acc = _db.FindByID<EmailConfirmation>(int.Parse(userId));
            Assert.That(acc, Is.Not.Null);
            Assert.That(acc?.Code.Length, Is.EqualTo(6));
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