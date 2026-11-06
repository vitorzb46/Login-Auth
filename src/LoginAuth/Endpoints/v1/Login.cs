using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Endpoints.Delegates;
using LoginAuth.ErrorMessages;
using LoginAuth.Model;
using LoginAuth.Security;
using static LoginAuth.Security.Tokens;

namespace LoginAuth.Endpoints.v1;

public static class Login
{
    public static LoginDelegate Handler() => GetResult;

    private static IResult GetResult(LoginRequest request,
                                     AccountPasswordHasher hasher,
                                     DatabaseAbstract<Account> accountTable,
                                     DatabaseAbstract<RefreshToken> tokenTable,
                                     Tokens tokens)
    {
        var getAccount = Account.Restore(accountTable, request.Email);
        if (getAccount is null)
            return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

        if (!hasher.VerifyPassword(request, getAccount.PasswordHash))
            return Results.Unauthorized();

        TokenAuth? token = tokens.PersistNewRefreshToken(getAccount, tokenTable);
        if (token is null) return Results.InternalServerError();

        return Results.Ok(token);
    }
}
