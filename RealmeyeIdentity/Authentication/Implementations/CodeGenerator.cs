using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace RealmeyeIdentity.Authentication
{
    public class CodeGenerator : ICodeGenerator
    {
        private readonly CodeGeneratorOptions _options;

        public CodeGenerator(IOptions<CodeGeneratorOptions> options)
        {
            _options = options.Value;
        }

        public string GenerateCode()
        {
            byte[] codeBytes = RandomNumberGenerator.GetBytes(_options.CodeLengthBytes);
            string code = $"RID_{Convert.ToBase64String(codeBytes)}";
            return code;
        }
    }
}
