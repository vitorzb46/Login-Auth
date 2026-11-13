using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Endpoints.Delegates;
using LoginAuth.ErrorMessages;
using LoginAuth.Model;

namespace LoginAuth.Endpoints.v1;

public static class Email
{
    public static EmailConfirmationDelegate Handler => GetResult;

    private static IResult GetResult(ConfirmEmailRequest request, DatabaseAbstract db)
    {
        var user = Account.Restore(db, request.Email);
        if (user is null)
            return Results.BadRequest(AccountErrorMessage.EmailOrPasswordInvalid);
        
        var confirmation = db.FindByID<EmailConfirmation>(user.Id);
        if (confirmation is null || confirmation.Code != request.Code)
            return Results.BadRequest(EmailErrorMessage.InvalidCode);

        if (DateTime.UtcNow > confirmation.ExpiresAt)
            return Results.BadRequest(EmailErrorMessage.ExpiredCode);

        user.EmailConfirmed = 1;
        if (!db.Update(user)) return Results.InternalServerError(EmailErrorMessage.InternalServerError);
        if (!db.Remove<EmailConfirmation>(user.Id)) return Results.InternalServerError(EmailErrorMessage.InternalServerError);

        return Results.Ok(new { message = EmailErrorMessage.Success });
    }
}
