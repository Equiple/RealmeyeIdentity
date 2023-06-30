namespace RealmeyeIdentity.Authentication
{
    public interface IRealmeyeService
    {
        Task<ValidateNameResult> ValidateCode(string name, string code);

        Task<ValidateNameResult> ValidateNameChange(string oldName, string newName);
    }
}
