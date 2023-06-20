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
        private readonly IRealmeyeService _realmeyeService;

        private readonly RegistrationSessionOptions _registrationSessionOptions;
        private readonly IdTokenOptions _idTokenOptions;

        private readonly IDistributedCache _cache;

        public AuthenticationService(
            IPasswordService passwordService,
            ICodeGenerator codeGenerator,
            IRealmeyeService realmeyeService,
            IOptions<UserDatabaseOptions> dbOptions,
            IOptions<RegistrationSessionOptions> registrationSessionOptions,
            IOptions<IdTokenOptions> idTokenOptions,
            IDistributedCache cache)
        {
            MongoClient client = new(dbOptions.Value.ConnectionString);
            IMongoDatabase db = client.GetDatabase(dbOptions.Value.Database);
            _userCollection = db.GetCollection<User>(dbOptions.Value.UserCollectionName);

            _passwordService = passwordService;
            _codeGenerator = codeGenerator;
            _realmeyeService = realmeyeService;

            _registrationSessionOptions = registrationSessionOptions.Value;
            _idTokenOptions = idTokenOptions.Value;

            _cache = cache;
        }

        public async Task<LoginResult> Login(string name, string password)
        {
            User? user = await _userCollection.Find(user => user.Name == name)
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

        public async Task<RegistrationSession?> GetRegistrationSession(string sessionId)
        {
            byte[] serializedSession = await _cache.GetAsync(sessionId);
            if (serializedSession == null)
            {
                return null;
            }
            RegistrationSession session = RegistrationSession.Deserialize(serializedSession);
            return session;
        }

        public async Task<RegistrationSession> StartRegistration()
        {
            string sessionId = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(_registrationSessionOptions.IdLengthBytes));
            string code = _codeGenerator.GenerateCode();
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow
                .AddMinutes(_registrationSessionOptions.LifetimeMinutes).AddSeconds(1);
            RegistrationSession session = new(sessionId, code, expiresAt);
            byte[] serializedSession = session.Serialize();
            await _cache.SetAsync(sessionId, serializedSession, new DistributedCacheEntryOptions
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
            RegistrationSession? session = await GetRegistrationSession(sessionId);

            if (session == null)
            {
                return RegisterErrorType.SessionExpired;
            }

            User? user = await _userCollection.Find(user => user.Name == name)
                .FirstOrDefaultAsync();

            if (restore && user == null)
            {
                return RegisterErrorType.RestoreNotFound;
            }

            if (!restore && user != null)
            {
                return RegisterErrorType.AlreadyExists;
            }

            bool codeValid = await _realmeyeService.ValidateCode(name, session.Code);
            if (!codeValid)
            {
                return RegisterErrorType.IncorrectCode;
            }

            if (user == null) // restore == false
            {
                string salt = _passwordService.GenerateSalt();
                user = new()
                {
                    Name = name,
                    Password = _passwordService.GetHash(password, salt),
                    Salt = salt,
                };
                await _userCollection.InsertOneAsync(user);
            }

            string idToken = GetIdToken(user);
            return idToken;
        }

        public async Task<ChangePasswordResult> ChangePassword(
            string name,
            string oldPassword,
            string newPassword)
        {
            User? user = await _userCollection.Find(user => user.Name == name)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return ChangePasswordErrorType.NotFound;
            }

            string hash = _passwordService.GetHash(oldPassword, user.Salt);
            if (user.Password != hash)
            {
                return ChangePasswordErrorType.IncorrectPassword;
            }

            string salt = _passwordService.GenerateSalt();
            user.Password = _passwordService.GetHash(newPassword, salt);
            user.Salt = salt;
            await _userCollection.ReplaceOneAsync(u => u.Id == user.Id, user);

            return new ChangePasswordResult.Ok();
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
