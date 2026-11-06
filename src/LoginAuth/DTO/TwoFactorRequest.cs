namespace LoginAuth.DTO;

public partial class TwoFactors
{
    public sealed record TwoFactorRequest
    {
        public required string Key
        {
            get => field;
            set => field = !string.IsNullOrWhiteSpace(field) 
                    ? value 
                    : throw new ArgumentException("Must contain a value!");
        }
    }
}