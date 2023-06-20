namespace RealmeyeIdentity.Authentication
{
    public class PasswordOptions
    {
        public int SaltLengthBytes { get; set; }

        public string Pepper { get; set; }
    }
}
