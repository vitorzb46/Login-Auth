using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.ErrorMessages;
using LoginAuth.Model;
using LoginAuth.Security;
using LoginAuth.Validation;

namespace LoginAuth.Endpoints.v1;

public static class Register
{
    public static Func<RegisterRequest, DatabaseAbstract<Account>, AccountPasswordHasher, IResult> Handler()
        => GetResult;

    private static IResult GetResult(RegisterRequest request, DatabaseAbstract<Account> database, AccountPasswordHasher hasher)
    {
        if (!request.Validate())
            return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

        return !database.Add(hasher.HashPassword(request))
            ? Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid)
            : Results.Ok();
    }
}