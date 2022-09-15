using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PureMVCFramework
{
    public static class SecurityUtils
    {
        private static AesEncrypt Encrypter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void ResetEncrypter()
        {
            Encrypter = new AesEncrypt(SHA1(SystemInfo.deviceUniqueIdentifier).Substring(0, 16));
        }

        public static string SHA1(byte[] data)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] sha1_byte = sha1.ComputeHash(data);
            string sha1_str = "";
            foreach (byte b in sha1_byte)
            {
                sha1_str += System.Convert.ToString(b, 16).PadLeft(2, '0');
            }

            return sha1_str;
        }

        public static string SHA1(string content)
        {
            var bytedata = Encoding.UTF8.GetBytes(content);
            return SHA1(bytedata);
        }

        public static string FileSHA1(string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                byte[] data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
                return SHA1(data);
            }
        }

        public static string MD5(byte[] data)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] md5_byte = md5.ComputeHash(data);
            string md5_str = "";
            foreach (byte b in md5_byte)
            {
                md5_str += System.Convert.ToString(b, 16).PadLeft(2, '0');
            }

            return md5_str;
        }

        public static string MD5(string content)
        {
            var bytedata = Encoding.UTF8.GetBytes(content);
            return MD5(bytedata);
        }

        public static string FileMD5(string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                byte[] data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
                return MD5(data);
            }
        }

        public static void WriteEncryptedString(string filePath, string content)
        {
            var bytedata = Encoding.UTF8.GetBytes(content);
            var encryptData = Encrypter.Encrypt(bytedata);

            using (FileStream stream = File.Create(filePath))
            {
                stream.Write(encryptData, 0, encryptData.Length);
            }
        }

        public static string ReadDecryptedString(string filePath)
        {
            if (File.Exists(filePath))
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] encryptData = new byte[stream.Length];
                    stream.Read(encryptData, 0, encryptData.Length);

                    var bytedata = Encrypter.Decrypt(encryptData);
                    var str = Encoding.UTF8.GetString(bytedata);
                    return str;
                }
            }

            return null;
        }

        public static void WriteEncryptedData<T>(string filePath, T jsonData)
        {
            var jsonStr = JsonUtility.ToJson(jsonData);
            WriteEncryptedString(filePath, jsonStr);
        }

        public static T ReadDecryptedData<T>(string filePath)
        {
            try
            {
                var jsonStr = ReadDecryptedString(filePath);
                if (!string.IsNullOrEmpty(jsonStr))
                {
                    return JsonUtility.FromJson<T>(jsonStr);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }

            return default;
        }
    }
}
