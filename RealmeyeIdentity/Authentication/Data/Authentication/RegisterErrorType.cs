namespace RealmeyeIdentity.Authentication
{
    public enum RegisterErrorType
    {
        IncorrectCode = 0,
        SessionExpired = 1,
        AlreadyExists = 2,
        RestoreNotFound = 3,
    }
}
