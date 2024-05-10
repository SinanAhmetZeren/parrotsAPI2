namespace ParrotsAPI2.Helpers
{
    public class CodeGenerator
    {
        public string GenerateCode()
        {
            Random random = new Random();
            string code = "";
            for (int i = 0; i < 6; i++)
            {
                code += random.Next(10);
            }
            return code;
        }
    }
}

