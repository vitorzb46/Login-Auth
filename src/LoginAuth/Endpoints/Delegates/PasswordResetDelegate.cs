using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult PasswordResetDelegate(
    ResetPasswordRequest request,
    DatabaseAbstract db,
    AccountPasswordHasher hasher);