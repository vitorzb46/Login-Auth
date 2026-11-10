using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Security;
using System.Threading.Channels;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult RegisterDelegate(
    RegisterRequest request,
    DatabaseAbstract database,
    AccountPasswordHasher hasher,
    Channel<EmailRegistrationEvent> channel);
