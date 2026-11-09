using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult RegisterDelegate(
    RegisterRequest request,
    DatabaseAbstract database,
    AccountPasswordHasher hasher);
