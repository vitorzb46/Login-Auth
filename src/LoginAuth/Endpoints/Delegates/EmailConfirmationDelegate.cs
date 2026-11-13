using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;

namespace LoginAuth.Endpoints.Delegates;

public delegate IResult EmailConfirmationDelegate(ConfirmEmailRequest request, DatabaseAbstract db);