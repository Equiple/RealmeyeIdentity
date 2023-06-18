namespace RealmeyeIdentity.Authentication
{
    public abstract record class LoginResult
    {
        public record class Ok(string IdToken) : LoginResult;

        public record class Error(LoginErrorType Type) : LoginResult;

        public static implicit operator LoginResult(string idToken)
        {
            return new Ok(idToken);
        }

        public static implicit operator LoginResult(LoginErrorType errorType)
        {
            return new Error(errorType);
        }
    }
}
