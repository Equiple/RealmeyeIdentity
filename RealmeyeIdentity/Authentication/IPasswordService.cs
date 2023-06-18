namespace RealmeyeIdentity.Authentication
{
    public interface IPasswordService
    {
        string GetHash(string password, string salt);

        string GenerateSalt();
    }
}
