using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Model;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult TwoFactorDelegate(
    TwoFactorRequest request,
    DatabaseAbstract db,
    TwoFactorsAuth tfa,
    Tokens tokens);