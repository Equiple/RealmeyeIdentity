using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;

namespace RealmeyeIdentity.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly IPasswordService _passwordService;
        private readonly IdTokenOptions _idTokenOptions;

        public AuthenticationService(
            IOptions<UserDatabaseOptions> dbOptions,
            IOptions<IdTokenOptions> idTokenOptions,
            IPasswordService passwordService)
        {
            MongoClient client = new(dbOptions.Value.ConnectionString);
            IMongoDatabase db = client.GetDatabase(dbOptions.Value.Database);
            _userCollection = db.GetCollection<User>(dbOptions.Value.UserCollectionName);
            _passwordService = passwordService;
            _idTokenOptions = idTokenOptions.Value;
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
                return LoginErrorType.InvalidPassword;
            }

            string idToken = GetIdToken(user);
            return idToken;
        }

        public async Task<RegisterResult> Register(string name, string password, string code)
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
