namespace RealmeyeIdentity.Authentication
{
    public record class RegistrationSession(string Id, string Code, DateTimeOffset ExpiresAt);
}
