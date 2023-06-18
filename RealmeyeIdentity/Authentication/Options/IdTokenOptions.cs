namespace RealmeyeIdentity.Authentication
{
    public class IdTokenOptions
    {
        public string Issuer { get; set; }

        public int LifetimeMinutes { get; set; }
    }
}
