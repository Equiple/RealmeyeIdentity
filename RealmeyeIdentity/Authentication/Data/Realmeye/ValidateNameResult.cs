namespace RealmeyeIdentity.Authentication
{
    public abstract record class ValidateNameResult
    {
        public record class Ok(string ExactName) : ValidateNameResult;

        public record class Error : ValidateNameResult;

        public static implicit operator ValidateNameResult(string exactName)
        {
            return new Ok(exactName);
        }
    }
}
