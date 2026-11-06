namespace LoginAuth.DTO;

public sealed record TwoFactorSetup(string SecretKey, string QrCodeUri);
// {
//     public required string SecretKey
//     {
//         get;
//         set
//         {
//             if (value == field) return;
//             field = string.IsNullOrWhiteSpace(field) ? throw new ArgumentException("Must contain a value!") : value;
//         }
//     }
//     public required string QrCodeUri
//     {
//         get;
//         set => field = !string.IsNullOrWhiteSpace(field)
//                 ? value
//                 : throw new ArgumentException("Must contain a value!");
//     }
// }