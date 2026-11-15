using System.Threading.Channels;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Interfaces;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult PasswordRecoveryDelegate(
    RecoverPasswordRequest request,
    DatabaseAbstract db,
    Channel<IEmailEvent> channel);