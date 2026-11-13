using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Endpoints.Delegates;
using LoginAuth.ErrorMessages;
using LoginAuth.Extensions.Validation;
using LoginAuth.Model;
using LoginAuth.Security;
using System.Diagnostics;
using System.Threading.Channels;

namespace LoginAuth.Endpoints.v1;

public static class Register
{
    public static RegisterDelegate Handler => GetResult;

    private static IResult GetResult(RegisterRequest request, DatabaseAbstract database, AccountPasswordHasher hasher, Channel<EmailRegistrationEvent> channel)
    {
        if (!request.Validate())
            return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

        if (database.Add(hasher.HashPassword(request)) is not { } account)
            return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);
        
        channel.Writer.TryWrite(new EmailRegistrationEvent(account.Id, account.Email));
        return Results.Ok();
    }
}