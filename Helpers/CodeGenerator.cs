using System.Security.Cryptography;

namespace ParrotsAPI2.Helpers
{
    public class CodeGenerator
    {
        public string GenerateCode()
        {
            Span<byte> bytes = stackalloc byte[6];
            RandomNumberGenerator.Fill(bytes);
            var code = new char[6];
            for (int i = 0; i < 6; i++)
                code[i] = (char)('0' + bytes[i] % 10);
            return new string(code);
        }
    }
}

