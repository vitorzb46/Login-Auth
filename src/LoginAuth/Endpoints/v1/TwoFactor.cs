using System.Security.Claims;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Endpoints.Delegates;
using LoginAuth.ErrorMessages;
using LoginAuth.Model;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.v1;

public static class TwoFactor
{
    public static TwoFactorSetupDelegate SetupHandler => GetSetup;

    public static TwoFactorConfirmDelegate ConfirmHandler => GetConfirm;

    private static IResult GetConfirm(TwoFactorRequest request,
                                      ClaimsPrincipal principal,
                                      DatabaseAbstract<Account> db,
                                      TwoFactorsAuth tfa)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email)) return Results.Unauthorized();

        Account? user = db.FindAll().FirstOrDefault(e => e.Email == email);
        if (user is null || string.IsNullOrEmpty(user.TwoFactorSecret)) 
            return Results.BadRequest(AccountErrorMessage.ConfirmBeforeSetup);

        if (!tfa.TryValidate(user.TwoFactorSecret, request.Code, out var message))
            return Results.BadRequest(AccountErrorMessage.TOTPInvalid);
        
        user.TwoFactorEnabled = 1;
        db.Update(user);

        return Results.Ok(new { message });
    }


    private static IResult GetSetup(ClaimsPrincipal principal, DatabaseAbstract<Account> db, TwoFactorsAuth tfa)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email)) return Results.Unauthorized();

        Account? user = db.FindAll().FirstOrDefault(e => e.Email == email);
        if (user is null) return Results.NotFound(AccountErrorMessage.AccountNotFound);

        string? secretKey = user.TwoFactorSecret;
        string qrCode = string.Empty;
        if (string.IsNullOrEmpty(secretKey))
        {
            TwoFactorSetup twoFactor = tfa.Generate(email);
            secretKey = twoFactor.SecretKey;
            qrCode = twoFactor.QrCodeUri;
            user.TwoFactorSecret = secretKey;
            db.Update(user);
        }

        return Results.Ok(new TwoFactorSetup(secretKey, qrCode));
    }
}