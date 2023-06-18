namespace RealmeyeIdentity.Authentication
{
    public abstract record class RegisterResult
    {
        public record class Ok(string IdToken) : RegisterResult;

        public record class Error(RegisterErrorType Type) : RegisterResult;

        public static implicit operator RegisterResult(string idToken)
        {
            return new Ok(idToken);
        }

        public static implicit operator RegisterResult(RegisterErrorType errorType)
        {
            return new Error(errorType);
        }
    }
}
