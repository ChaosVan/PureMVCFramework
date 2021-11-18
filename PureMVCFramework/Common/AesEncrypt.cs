using System.Text;
using System.Security.Cryptography;

namespace PureMVCFramework
{
    public class AesEncrypt
    {
        public const int BIT_SIZE = 16;

        /// <summary>
        /// 16 byte (128 bit)
        /// </summary>
        private byte[] keyArray = null;

        public AesEncrypt(string key)
        {
            keyArray = Encoding.UTF8.GetBytes(key);
        }

        public AesEncrypt(byte[] key)
        {
            keyArray = key;
        }

        public byte[] Encrypt(byte[] toEncryptArray)
        {
            var rijndael = new RijndaelManaged
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros,
            };
            ICryptoTransform cTransform = rijndael.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return resultArray;
        }

        public int Encrypt(byte[] toEncryptArray, int length, byte[] buffer)
        {
            var rijndael = new RijndaelManaged
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros,
            };
            ICryptoTransform cTransform = rijndael.CreateEncryptor();
            return cTransform.TransformBlock(toEncryptArray, 0, length, buffer, 0);
        }

        public byte[] Decrypt(byte[] toEncryptArray)
        {
            var rijndael = new RijndaelManaged
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros,
            };
            ICryptoTransform cTransform = rijndael.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return resultArray;
        }

        public int Decrypt(byte[] toEncryptArray, int len, byte[] buffer)
        {
            var rijndael = new RijndaelManaged
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros,
            };
            ICryptoTransform cTransform = rijndael.CreateDecryptor();
            return cTransform.TransformBlock(toEncryptArray, 0, len, buffer, 0);
        }
    }
}