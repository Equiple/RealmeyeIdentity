using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace RealmeyeIdentity.Authentication
{
    public class PasswordService : IPasswordService
    {
        private readonly PasswordOptions _options;
        private readonly byte[] _pepper;

        public PasswordService(IOptions<PasswordOptions> options)
        {
            _options = options.Value;
            _pepper = Convert.FromBase64String(_options.Pepper);
        }

        public byte[] GenerateSalt()
        {
            byte[] salt = RandomNumberGenerator.GetBytes(_options.SaltLengthBytes);
            return salt;
        }

        public byte[] GetHash(byte[] password, byte[] salt)
        {
            Argon2id argon2 = new(password)
            {
                Salt = salt,
                KnownSecret = _pepper,
                MemorySize = _options.HashMemoryKbytes,
                Iterations = _options.HashIterations,
                DegreeOfParallelism = _options.HashParallelismDegree
            };
            byte[] hash = argon2.GetBytes(_options.HashLengthBytes);
            return hash;
        }
    }
}
