using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PureMVCFramework.Editor
{
    public static class EditorInternalUtils
    {
        private static List<string> GetDefinesList(BuildTargetGroup group)
        {
            return new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';'));
        }

        public static void SetScriptingDefineSymbolEnabled(string symbol, bool enable)
        {
            List<string> defines = GetDefinesList(EditorUserBuildSettings.selectedBuildTargetGroup);
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

        [UnityEditor.MenuItem("Tools/OpenPath/PersistentDataPath", false, 1001)]
        public static void OpenPersistentDataPath()
        {
            OpenFoloder(Application.persistentDataPath);
        }

        [UnityEditor.MenuItem("Tools/OpenPath/TemporaryCachePath", false, 1002)]
        public static void OpenTemporaryCachePath()
        {
            OpenFoloder(Application.temporaryCachePath);
        }

        [UnityEditor.MenuItem("Tools/OpenPath/StreamingAssetsPath", false, 1003)]
        public static void OpenStreamingAssetsPath()
        {
            OpenFoloder(Application.streamingAssetsPath);
        }

        [UnityEditor.MenuItem("Tools/OpenPath/AssetDataPath", false, 1004)]
        public static void OpenDataPath()
        {
            OpenFoloder(Application.dataPath);
        }

        [UnityEditor.MenuItem("Tools/OpenPath/UnityDataPath", false, 1005)]
        public static void OpenUnityContentsPath()
        {
            OpenFoloder(EditorApplication.applicationContentsPath);
        }

        [UnityEditor.MenuItem("Tools/OpenPath/ConsoleLog #%L", false, 1006)]
        public static void OpenConsoleLogPath()
        {
            OpenFoloder(Application.consoleLogPath);
        }

        [UnityEditor.MenuItem("Tools/PlayerPrefs/DeleteAll #%D", false, 1007)]
        public static void PlayerPrefsDeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}
