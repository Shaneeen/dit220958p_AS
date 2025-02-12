using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace dit220958p_AS.Services
{
    public class EncryptionHelper
    {
        private readonly byte[] _encryptionKey;

        public EncryptionHelper(IConfiguration configuration)
        {
            string hexKey = configuration["EncryptionSettings:Key"];  // Get the hex key from configuration
            _encryptionKey = ConvertHexStringToByteArray(hexKey);      // Convert hex string to byte array
        }

        // Convert Hexadecimal String to Byte Array
        private byte[] ConvertHexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Invalid length of hexadecimal key.");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public (string EncryptedText, string IV) Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return (null, null);

            using (Aes aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.GenerateIV();
                var ivString = Convert.ToBase64String(aes.IV);

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    sw.Flush();          // Ensure all data is written to the CryptoStream
                    cs.FlushFinalBlock(); // Finalize the encryption process

                    // Move return inside the 'using' block for 'ms'
                    return (Convert.ToBase64String(ms.ToArray()), ivString);
                }
            }
        }



        public string Decrypt(string cipherText, string iv)
        {
            if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(iv))
                return null;

            using (Aes aes = Aes.Create())
            {
                aes.Key = _encryptionKey;               // Use the same key for decryption
                aes.IV = Convert.FromBase64String(iv);  // Use the provided IV

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
