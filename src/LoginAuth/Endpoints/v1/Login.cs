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
    public static LoginDelegate Handler => GetResult;
    public static TwoFactorDelegate MfaHandler => GetMfaResult;

    private static IResult GetMfaResult(TwoFactorRequest request,
                                        DatabaseAbstract db,
                                        TwoFactorsAuth tfa,
                                        Tokens tokens)
    {
        var user = Account.Restore(db, request.Email);
        if (user is not { TwoFactorEnabled: 1})
            return Results.BadRequest(AccountErrorMessage.TwoFactorNotEnabled);

        if (!tfa.TryValidate(user.TwoFactorSecret, request.Code, out _))
            return Results.BadRequest(AccountErrorMessage.TOTPInvalid);

        TokenAuth? token = tokens.PersistNewRefreshToken(user, db);
        return token is null ? Results.InternalServerError() : Results.Ok(token);
    }
    
    private static IResult GetResult(LoginRequest request,
                                     AccountPasswordHasher hasher,
                                     DatabaseAbstract db,
                                     Tokens tokens)
    {
        var user = Account.Restore(db, request.Email);
        if (user is null)
            return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);

        if (!hasher.VerifyPassword(request, user.PasswordHash))
            return Results.Unauthorized();

        if (user.TwoFactorEnabled == 1)
            return Results.Accepted(value: new { RequiresTwoFactor = true, user.Email });

        TokenAuth? token = tokens.PersistNewRefreshToken(user, db);
        return token is null ? Results.InternalServerError() : Results.Ok(token);
    }
}
