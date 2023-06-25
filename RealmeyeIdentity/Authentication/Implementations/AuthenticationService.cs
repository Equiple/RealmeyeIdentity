using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace RealmeyeIdentity.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private const string RegistrationSessionsId = "RegistrationSession";

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

            byte[] hash = _passwordService.GetHash(
                Encoding.UTF8.GetBytes(password),
                Convert.FromBase64String(user.Salt));
            string hashb64 = Convert.ToBase64String(hash);
            if (user.Password != hashb64)
            {
                return LoginErrorType.IncorrectPassword;
            }

            string idToken = GetIdToken(user);
            return idToken;
        }

        public async Task<RegistrationSession?> GetRegistrationSession(string sessionId)
        {
            byte[] serializedSession = await _cache.GetAsync(RegistrationSessionId(sessionId));
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
            await _cache.SetAsync(
                RegistrationSessionId(sessionId),
                serializedSession,
                new DistributedCacheEntryOptions
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
                byte[] salt = _passwordService.GenerateSalt();
                byte[] hash = _passwordService.GetHash(Encoding.UTF8.GetBytes(password), salt);
                user = new()
                {
                    Name = name,
                    Password = Convert.ToBase64String(hash),
                    Salt = Convert.ToBase64String(salt),
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

            byte[] oldHash = _passwordService.GetHash(
                Encoding.UTF8.GetBytes(oldPassword),
                Convert.FromBase64String(user.Salt));
            string hashb64 = Convert.ToBase64String(oldHash);
            if (user.Password != hashb64)
            {
                return ChangePasswordErrorType.IncorrectPassword;
            }

            byte[] salt = _passwordService.GenerateSalt();
            byte[] newHash = _passwordService.GetHash(Encoding.UTF8.GetBytes(newPassword), salt);
            user.Password = Convert.ToBase64String(newHash);
            user.Salt = Convert.ToBase64String(salt);
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

        private static string RegistrationSessionId(string sessionId)
        {
            return $"{RegistrationSessionsId}_{sessionId}";
        }
    }
}
