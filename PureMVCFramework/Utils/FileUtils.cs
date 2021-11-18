using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace PureMVCFramework
{
    public static class FileUtils
    {
        public static bool ExistDirectory(string path)
        {
            return Directory.Exists(path);
        }

        public static bool ExistFile(string path)
        {
            return File.Exists(path);
        }

        public static void DeleteDirectoryIfExists(string path, bool recursive = true)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive);
        }

        public static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void CreateFileDirectoryIfNotExists(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        public static FileStream CreateFile(string path)
        {
            DeleteFileIfExists(path);
            CreateFileDirectoryIfNotExists(path);
            return File.Create(path);
        }

        public static StreamWriter CreateText(string path)
        {
            DeleteFileIfExists(path);
            CreateFileDirectoryIfNotExists(path);
            return File.CreateText(path);
        }

        public static byte[] ReadAllBytes(string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path), "Path is null");
            Assert.IsTrue(ExistFile(path), "Not Exist: " + path);

            return File.ReadAllBytes(path);
        }

        public static string ReadAllText(string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path), "Path is null");
            Assert.IsTrue(ExistFile(path), "Not Exist: " + path);

            return File.ReadAllText(path);
        }

        public static string[] ReadAllLines(string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path), "Path is null");
            Assert.IsTrue(ExistFile(path), "Not Exist: " + path);

            return File.ReadAllLines(path);
        }

        public static void WriteAllBytes(string path, byte[] bytedata, bool overwrite = true)
        {
            Assert.IsNotNull(bytedata);
            CreateFileDirectoryIfNotExists(path);

            if (overwrite && File.Exists(path))
                File.Delete(path);

            File.WriteAllBytes(path, bytedata);
        }

        public static void WriteAllText(string path, string contents, bool overwrite = true)
        {
            Assert.IsFalse(string.IsNullOrEmpty(contents));
            CreateFileDirectoryIfNotExists(path);

            if (overwrite && File.Exists(path))
                File.Delete(path);

            File.WriteAllText(path, contents);
        }

        public static void WriteAllText(string path, string contents, Encoding encoding, bool overwrite = true)
        {
            Assert.IsFalse(string.IsNullOrEmpty(contents));
            CreateFileDirectoryIfNotExists(path);

            if (overwrite && File.Exists(path))
                File.Delete(path);

            File.WriteAllText(path, contents, encoding);
        }

        public static void WriteAllLines(string path, string[] contents, Encoding encoding, bool overwrite = true)
        {
            Assert.IsNotNull(contents);
            CreateFileDirectoryIfNotExists(path);

            if (overwrite && File.Exists(path))
                File.Delete(path);

            File.WriteAllLines(path, contents, encoding);
        }

        public static void CopyFile(string srcPath, string dstPath, bool overwrite = true)
        {
            if (File.Exists(srcPath))
            {
                CreateFileDirectoryIfNotExists(dstPath);
                File.Copy(srcPath, dstPath, overwrite);
            }
            else
            {
                Debug.LogErrorFormat("File not exsits: {0}", srcPath);
            }
        }

        public static void CopyDirectory(string srcPath, string dstPath, bool overwrite = true)
        {
            if (Directory.Exists(srcPath))
            {
                if (overwrite)
                    DeleteDirectoryIfExists(dstPath);

                CreateFileDirectoryIfNotExists(dstPath);

                var files = Directory.GetFiles(srcPath);
                foreach (var file in files)
                {
                    var dstFile = file.Replace(srcPath, dstPath);
                    CopyFile(file, dstFile, true);
                }
            }
            else
            {
                Debug.LogErrorFormat("Directory not exsits: {0}", srcPath);
            }
        }

        public static long GetFileSize(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                return fs.Length;
            }
        }
    }
}
