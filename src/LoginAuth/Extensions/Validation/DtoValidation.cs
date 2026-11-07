using LoginAuth.DTO;

namespace LoginAuth.Extensions.Validation;

static class DtoValidation
{
    public static bool Validate(this RegisterRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password)) return false;
        if (dto.Email.Length > 254) return false;
        if (dto.Password is { Length: < 8 or > 64 }) return false;
        
        return true;
    }
}