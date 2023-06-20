namespace RealmeyeIdentity.Authentication
{
    public interface IAuthenticationService
    {
        Task<LoginResult> Login(string name, string password);

        Task<RegistrationSession> StartRegistration();

        Task<RegisterResult> Register(string sessionId, string name, string password, bool restore);

        Task<ChangePasswordResult> ChangePassword(string name, string oldPassword, string newPassword);
    }
}
