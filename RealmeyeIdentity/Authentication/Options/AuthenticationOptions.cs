namespace RealmeyeIdentity.Authentication
{
    public class AuthenticationOptions
    {
        public int RegistrationSessionIdLengthBytes { get; set; }

        public int RegistrationSessionLifetimeMinutes { get; set; }

        public int AuthCodeLengthBytes { get; set; }

        public int AuthCodeLifetimeMinutes { get; set; }

        public string IdTokenIssuer { get; set; }

        public int IdTokenLifetimeMinutes { get; set; }

        public string IdTokenSecretKey { get; set; }
    }
}
