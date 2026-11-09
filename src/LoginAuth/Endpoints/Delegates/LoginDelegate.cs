using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult LoginDelegate(
    LoginRequest request, 
    AccountPasswordHasher hasher, 
    DatabaseAbstract db,
    Tokens tokens);