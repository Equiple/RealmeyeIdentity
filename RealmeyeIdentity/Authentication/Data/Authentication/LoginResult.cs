namespace RealmeyeIdentity.Authentication
{
    public abstract record class LoginResult
    {
        public record class Ok(string AuthCode) : LoginResult;

        public record class Error(LoginErrorType Type) : LoginResult;

        public static implicit operator LoginResult(string authCode)
        {
            return new Ok(authCode);
        }

        public static implicit operator LoginResult(LoginErrorType errorType)
        {
            return new Error(errorType);
        }
    }
}
