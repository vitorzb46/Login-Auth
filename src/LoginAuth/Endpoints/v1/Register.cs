using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Endpoints.Delegates;
using LoginAuth.ErrorMessages;
using LoginAuth.Extensions.Validation;
using LoginAuth.Model;
using LoginAuth.Security;
using System.Threading.Channels;

namespace LoginAuth.Endpoints.v1;

public static class Register
{
    public static RegisterDelegate Handler => GetResult;

    private static IResult GetResult(RegisterRequest request, DatabaseAbstract database, AccountPasswordHasher hasher, Channel<EmailRegistrationEvent> channel)
    {
        if (!request.Validate())
            return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

        if (!database.Add(hasher.HashPassword(request)))
            Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

        var x = database.FindAll<Account>().FirstOrDefault(e => e.Email == request.Email); // get the id registered on the db
        channel.Writer.TryWrite(new EmailRegistrationEvent(x.Id.ToString(), x.Email));

        return Results.Ok();

        //return !database.Add(hasher.HashPassword(request))
        //    ? Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid)
        //    : Results.Ok();
    }
}