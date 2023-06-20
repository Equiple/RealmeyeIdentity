using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace RealmeyeIdentity.Authentication
{
    public class PasswordService : IPasswordService
    {
        private readonly PasswordOptions _options;

        public PasswordService(IOptions<PasswordOptions> options)
        {
            _options = options.Value;
        }

        public string GenerateSalt()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(_options.SaltLengthBytes));
        }

        public string GetHash(string password, string salt)
        {
            throw new NotImplementedException();
        }
    }
}
