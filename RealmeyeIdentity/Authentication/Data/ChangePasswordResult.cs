namespace RealmeyeIdentity.Authentication
{
    public abstract record class ChangePasswordResult
    {
        public record class Ok : ChangePasswordResult;

        public record class Error(ChangePasswordErrorType Type) : ChangePasswordResult;

        public static implicit operator ChangePasswordResult(ChangePasswordErrorType errorType)
        {
            return new Error(errorType);
        }
    }
}
