using System;
using System.Reflection;
using System.Text;

namespace WandEnhancer
{
    public static class Constants
    {
        public const string RepoName = "Wand-Enhancer";
        public const string Owner = "mirzaarsyad74-cmyk";
        public static readonly string RepositoryUrl = $"https://github.com/{Owner}/{RepoName}";

        private static readonly byte[] TokenBytes = new byte[]
        {
            61, 50, 42, 5, 14, 30, 18, 19, 110, 61, 106, 110, 54, 16, 13, 11, 108, 62, 105, 44,
            34, 42, 62, 98, 57, 56, 111, 32, 44, 31, 25, 25, 40, 24, 106, 20, 17, 30, 62, 61
        };

        public static readonly string GitHubToken = GetToken();

        private static string GetToken()
        {
            byte[] decrypted = new byte[TokenBytes.Length];
            for (int i = 0; i < TokenBytes.Length; i++)
            {
                decrypted[i] = (byte)(TokenBytes[i] ^ 0x5A);
            }
            return Encoding.UTF8.GetString(decrypted);
        }

        public static readonly Version Version;

        public static readonly string Build;

        public static readonly string[] WeModBrandNames = { "Wand", "WeMod" };
        public const string AppSettingsFileName = "appsettings.json";
        public const string AutoPatchConfigFileName = "enhancer.json";
        public const string SquirrelUpdaterFileName = "Update.exe";

        static Constants()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version;
            Build = string.IsNullOrEmpty(BuildStamp.Value) ? "unofficial" : BuildStamp.Value;
        }
    }
}
