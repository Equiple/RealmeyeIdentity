namespace RealmeyeIdentity.Authentication
{
    public interface IAuthenticationService
    {
        Task<LoginResult> Login(string name, string password);

        Task<RegisterResult> Register(string name, string password, string code);
    }
}
