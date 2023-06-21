namespace RealmeyeIdentity.Authentication
{
    public interface IPasswordService
    {
        byte[] GenerateSalt();

        byte[] GetHash(byte[] password, byte[] salt);
    }
}
