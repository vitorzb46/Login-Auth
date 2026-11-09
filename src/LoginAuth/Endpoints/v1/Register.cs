using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Endpoints.Delegates;
using LoginAuth.ErrorMessages;
using LoginAuth.Extensions.Validation;
using LoginAuth.Model;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.v1;

public static class Register
{
    public static RegisterDelegate Handler => GetResult;

    private static IResult GetResult(RegisterRequest request, DatabaseAbstract database, AccountPasswordHasher hasher)
    {
        if (!request.Validate())
            return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

        return !database.Add(hasher.HashPassword(request))
            ? Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid)
            : Results.Ok();
    }
}