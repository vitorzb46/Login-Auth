using System.Threading.Channels;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Endpoints.Delegates;
using LoginAuth.ErrorMessages;
using LoginAuth.Interfaces;
using LoginAuth.Model;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.v1;

public static class Password
{
    public static PasswordRecoveryDelegate RecoverHandler => GetRecoverResult;
    
    public static PasswordResetDelegate ResetHandler => GetResetResult;

    private static IResult GetRecoverResult(RecoverPasswordRequest request, DatabaseAbstract db, Channel<IEmailEvent> channel)
    {
        var user = Account.Restore(db, request.Email);
        if (user is not null)
            channel.Writer.TryWrite(new PasswordRecoveryEvent(user.Id, user.Email));

        return Results.Ok(new { Message = "Check your e-mail for the verification code."});
    }

    private static IResult GetResetResult(ResetPasswordRequest request, DatabaseAbstract db, AccountPasswordHasher hasher)
    {
        var confirmation = db.FindAll<PasswordReset>().FirstOrDefault(t => t.Token == request.Token);
        if (confirmation is null)
            return Results.BadRequest("Invalid token!");
        
        if (confirmation.ExpiresAt < DateTime.UtcNow)
            return Results.BadRequest("Token has expired. Re-send a new e-mail!");

        var user = db.FindByID<Account>(confirmation.AccountId);
        if (user is null)
            return Results.BadRequest(AccountErrorMessage.AccountNotFound);

        var newHashedPwd = hasher.HashPassword(user.Email, request.NewPwd);
        user.PasswordHash = newHashedPwd;

        if (!db.Update(user))
            return Results.InternalServerError();

        return Results.Ok(new { Message = "New password has been changed."});
    }
}