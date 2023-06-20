using System.Security.Cryptography;

namespace RealmeyeIdentity.Authentication
{
    public class CodeGenerator : ICodeGenerator
    {
        public string GenerateCode()
        {
            byte[] codeBytes = RandomNumberGenerator.GetBytes(32);
            string code = $"RID_{Convert.ToBase64String(codeBytes)}";
            return code;
        }
    }
}
