using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Model;
using LoginAuth.Security;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult LoginDelegate(
    LoginRequest request, 
    AccountPasswordHasher hasher, 
    DatabaseAbstract<Account> accountDb, 
    DatabaseAbstract<RefreshToken> tokenDb, 
    Tokens tokens);