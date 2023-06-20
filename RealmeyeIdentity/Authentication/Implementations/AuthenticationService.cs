using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace RealmeyeIdentity.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly IPasswordService _passwordService;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IdTokenOptions _idTokenOptions;

        private readonly IDistributedCache _cache;

        public AuthenticationService(
            IPasswordService passwordService,
            ICodeGenerator codeGenerator,
            IOptions<UserDatabaseOptions> dbOptions,
            IOptions<IdTokenOptions> idTokenOptions,
            IDistributedCache cache)
        {
            MongoClient client = new(dbOptions.Value.ConnectionString);
            IMongoDatabase db = client.GetDatabase(dbOptions.Value.Database);
            _userCollection = db.GetCollection<User>(dbOptions.Value.UserCollectionName);
            _passwordService = passwordService;
            _codeGenerator = codeGenerator;
            _idTokenOptions = idTokenOptions.Value;
            _cache = cache;
        }

        public async Task<LoginResult> Login(string name, string password)
        {
            User user = await _userCollection.Find(user => user.Name == name)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return LoginErrorType.NotFound;
            }

            string hash = _passwordService.GetHash(password, user.Salt);
            if (user.Password != hash)
            {
                return LoginErrorType.IncorrectPassword;
            }

            string idToken = GetIdToken(user);
            return idToken;
        }

        public async Task<RegistrationSession> StartRegistration()
        {
            string sessionId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(128));
            string code = _codeGenerator.GenerateCode();
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(15).AddSeconds(1);
            RegistrationSession session = new(sessionId, code, expiresAt);
            await _cache.SetStringAsync(sessionId, code, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt,
            });
            return session;
        }

        public async Task<RegisterResult> Register(
            string sessionId,
            string name,
            string password,
            bool restore)
        {
            throw new NotImplementedException();
        }

        public Task<ChangePasswordResult> ChangePassword(
            string name,
            string oldPassword,
            string newPassword)
        {
            throw new NotImplementedException();
        }

        private string GetIdToken(User user)
        {
            SecurityTokenDescriptor descriptor = new()
            {
                Issuer = _idTokenOptions.Issuer,
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(_idTokenOptions.LifetimeMinutes),
                Claims = new Dictionary<string, object>
                {
                    { "uid", user.Id },
                    { "unm", user.Name },
                }
            };
            JwtSecurityTokenHandler handler = new();
            JwtSecurityToken jwt = handler.CreateJwtSecurityToken(descriptor);
            string serializedJwt = handler.WriteToken(jwt);
            return serializedJwt;
        }
    }
}
