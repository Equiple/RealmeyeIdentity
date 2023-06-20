namespace RealmeyeIdentity.Authentication.Implementations
{
    public class RealmeyeService : IRealmeyeService
    {
        public Task<bool> ValidateCode(string name, string code)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateNameChange(string oldName, string newName)
        {
            throw new NotImplementedException();
        }
    }
}
