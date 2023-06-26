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
        private const string TokenSessionsId = "TokenSession";

        private readonly IMongoCollection<User> _userCollection;

        private readonly IPasswordService _passwordService;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IRealmeyeService _realmeyeService;

        private readonly AuthenticationOptions _options;

        private readonly IDistributedCache _cache;

        public AuthenticationService(
            IPasswordService passwordService,
            ICodeGenerator codeGenerator,
            IRealmeyeService realmeyeService,
            IOptions<UserDatabaseOptions> dbOptions,
            IOptions<AuthenticationOptions> options,
            IDistributedCache cache)
        {
            MongoClient client = new(dbOptions.Value.ConnectionString);
            IMongoDatabase db = client.GetDatabase(dbOptions.Value.Database);
            _userCollection = db.GetCollection<User>(dbOptions.Value.UserCollectionName);

            _passwordService = passwordService;
            _codeGenerator = codeGenerator;
            _realmeyeService = realmeyeService;

            _options = options.Value;

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

            string authCode = await CreateAuthCode(user.Id);
            return authCode;
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
                RandomNumberGenerator.GetBytes(_options.RegistrationSessionIdLengthBytes));
            string code = _codeGenerator.GenerateCode();
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow
                .AddMinutes(_options.RegistrationSessionLifetimeMinutes).AddSeconds(1);
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

            await _cache.RemoveAsync(RegistrationSessionId(sessionId));

            byte[] salt = _passwordService.GenerateSalt();
            byte[] hash = _passwordService.GetHash(Encoding.UTF8.GetBytes(password), salt);
            if (user == null)
            {
                user = new()
                {
                    Name = name,
                    Password = Convert.ToBase64String(hash),
                    Salt = Convert.ToBase64String(salt),
                };
                await _userCollection.InsertOneAsync(user);
            }
            else
            {
                user.Password = Convert.ToBase64String(hash);
                user.Salt = Convert.ToBase64String(salt);
                await _userCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
            }

            string authCode = await CreateAuthCode(user.Id);
            return authCode;
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

        public async Task<string?> CreateIdTokenAsync(string authCode)
        {
            string sessionId = TokenSessionId(authCode);
            string userId = await _cache.GetStringAsync(sessionId);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            User? user = await _userCollection.Find(user => user.Id == userId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return null;
            }

            await _cache.RemoveAsync(sessionId);

            SecurityTokenDescriptor descriptor = new()
            {
                Issuer = _options.IdTokenIssuer,
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(_options.IdTokenLifetimeMinutes),
                Claims = new Dictionary<string, object>
                {
                    { "uid", user.Id },
                    { "unm", user.Name },
                },
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Convert.FromBase64String(_options.IdTokenSecretKey)),
                    SecurityAlgorithms.HmacSha256)
            };
            JwtSecurityTokenHandler handler = new();
            JwtSecurityToken jwt = handler.CreateJwtSecurityToken(descriptor);
            string serializedJwt = handler.WriteToken(jwt);
            return serializedJwt;
        }

        private async Task<string> CreateAuthCode(string userId)
        {
            string authCode = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(_options.AuthCodeLengthBytes));
            await _cache.SetStringAsync(
                TokenSessionId(authCode),
                userId,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow
                        .AddMinutes(_options.AuthCodeLifetimeMinutes),
                });
            return authCode;
        }

        private static string RegistrationSessionId(string sessionId)
        {
            return $"{RegistrationSessionsId}_{sessionId}";
        }

        private static string TokenSessionId(string authCode)
        {
            return $"{TokenSessionsId}_{authCode}";
        }
    }
}
