namespace RealmeyeIdentity.Authentication
{
    public abstract record class RegisterResult
    {
        public record class Ok(string AuthCode) : RegisterResult;

        public record class Error(RegisterErrorType Type) : RegisterResult;

        public static implicit operator RegisterResult(string authCode)
        {
            return new Ok(authCode);
        }

        public static implicit operator RegisterResult(RegisterErrorType errorType)
        {
            return new Error(errorType);
        }
    }
}
