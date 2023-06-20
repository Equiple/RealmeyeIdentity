namespace RealmeyeIdentity.Authentication
{
    public interface IRealmeyeService
    {
        Task<bool> ValidateCode(string name, string code);

        Task<bool> ValidateNameChange(string oldName, string newName);
    }
}
