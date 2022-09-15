using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PureMVCFramework.Editor
{
    public static class InternalEditorUtils
    {
        private static List<string> GetDefineSymbols(this BuildTargetGroup group)
        {
            return new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';'));
        }

        public static void SetScriptingDefineSymbolEnabled(string symbol, bool enable)
        {
            List<string> defines = EditorUserBuildSettings.selectedBuildTargetGroup.GetDefineSymbols();
            if (enable)
            {
                if (!defines.Contains(symbol))
                {
                    defines.Add(symbol);
                }
            }
            else
            {
                while (defines.Contains(symbol))
                {
                    defines.Remove(symbol);
                }
            }

            string definesString = string.Join(";", defines.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, definesString);
        }

        public static void OpenFoloder(string path)
        {
            path = path.Replace("/", "\\");
            var p = System.Diagnostics.Process.Start("explorer.exe", path);
            p.Close();
        }

        [UnityEditor.MenuItem("Tools/PureMVCFramework/Quick Open/PersistentDataPath", false, 100)]
        public static void OpenPersistentDataPath()
        {
            OpenFoloder(Application.persistentDataPath);
        }

        [UnityEditor.MenuItem("Tools/PureMVCFramework/Quick Open/TemporaryCachePath", false, 101)]
        public static void OpenTemporaryCachePath()
        {
            OpenFoloder(Application.temporaryCachePath);
        }

        [UnityEditor.MenuItem("Tools/PureMVCFramework/Quick Open/StreamingAssetsPath", false, 102)]
        public static void OpenStreamingAssetsPath()
        {
            OpenFoloder(Application.streamingAssetsPath);
        }

        [UnityEditor.MenuItem("Tools/PureMVCFramework/Quick Open/AssetDataPath", false, 103)]
        public static void OpenDataPath()
        {
            OpenFoloder(Application.dataPath);
        }

        [UnityEditor.MenuItem("Tools/PureMVCFramework/Quick Open/UnityDataPath", false, 104)]
        public static void OpenUnityContentsPath()
        {
            OpenFoloder(EditorApplication.applicationContentsPath);
        }

        [UnityEditor.MenuItem("Tools/PureMVCFramework/Quick Open/ConsoleLog #%L", false, 115)]
        public static void OpenConsoleLogPath()
        {
            OpenFoloder(Application.consoleLogPath);
        }

        [UnityEditor.MenuItem("Tools/PureMVCFramework/Common/Delete PlayerPrefs #%D", false, 0)]
        public static void PlayerPrefsDeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }

        [MenuItem("Tools/PureMVCFramework/Common/Find Duplicate Files", false, 1)]
        public static void FindDuplicateFile()
        {
            string[] files = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories);
            string dataPath = Application.dataPath.Replace("Assets", "");
            Dictionary<string, string> dic = new Dictionary<string, string>();
            int p = 0;

            FileStream fs = new FileStream(Application.dataPath + "/FindDuplicateFiles.log", FileMode.Create, FileAccess.Write);
            StreamWriter writer = new StreamWriter(fs);

            foreach (var file in files)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Finding duplicate files", file, p++ * 1f / files.Length))
                {
                    break;
                }

                if (Path.GetFileName(file).StartsWith(".", System.StringComparison.Ordinal) || Path.GetExtension(file) == ".meta")
                    continue;

                string path = dataPath + file;
                string hashcode = SecurityUtils.FileMD5(path);

                if (!string.IsNullOrEmpty(hashcode))
                {
                    if (dic.ContainsKey(hashcode))
                    {
                        string log = string.Format("{0} duplicate with {1}", file.PadRight(84, ' '), dic[hashcode]);
                        Debug.Log(log);
                        writer.WriteLine(log);
                    }
                    else
                    {
                        dic.Add(hashcode, file);
                    }
                }
            }

            writer.Close();
            fs.Close();

            EditorUtility.ClearProgressBar();
        }
    }
}
