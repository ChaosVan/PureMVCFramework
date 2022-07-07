using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AssetSearcher
{
    private static class QuickSearchAssistant
    {
        [MenuItem("Assets/QuickSearchAssistant/FindPrefabByAnything", true)]
        public static bool FindPrefabByAnythingValidate()
        {
            var objects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            return objects.Length > 0;
        }

        [MenuItem("Assets/QuickSearchAssistant/FindPrefabByAnything", false, 999 * 2 + 1)]
        public static void FindPrefabByAnything()
        {
            var selects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (selects.Length > 0)
            {
                var searchs = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);
                if (FindBySelect(selects, searchs, out var result))
                {
                    ShowAssetsInHierarchy<GameObject>(result);
                }

                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Assets/QuickSearchAssistant/FindMaterialByTexture", true)]
        public static bool FindMaterialByTextureValidate()
        {
            var textures = Selection.GetFiltered<Texture>(SelectionMode.Assets);
            return textures.Length > 0;
        }

        [MenuItem("Assets/QuickSearchAssistant/FindMaterialByTexture", false, 999 * 2 + 2)]
        public static void FindMaterialByTexture()
        {
            var selects = Selection.GetFiltered<Texture>(SelectionMode.Assets);
            if (selects.Length > 0)
            {
                var searchs = Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories);
                if (FindBySelect(selects, searchs, out var result))
                {
                    ShowAssetsInHierarchy<Material>(result);
                }

                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Assets/QuickSearchAssistant/FindMaterialByShader", true)]
        public static bool FindMaterialByShaderValidate()
        {
            var selects = Selection.GetFiltered<Shader>(SelectionMode.Assets);
            return selects.Length > 0;
        }

        [MenuItem("Assets/QuickSearchAssistant/FindMaterialByShader", false, 999 * 2 + 3)]
        public static void FindMaterialByShader()
        {
            var selects = Selection.GetFiltered<Shader>(SelectionMode.Assets);
            if (selects.Length > 0)
            {
                var searchs = Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories);
                if (FindBySelect(selects, searchs, out var result))
                {
                    ShowAssetsInHierarchy<Material>(result);
                }

                EditorUtility.ClearProgressBar();
            }
        }
    }

    static void CreateAssetGameObject<T>(string path, Transform parent, bool show) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset is GameObject prefab)
        {
            GameObject clone = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            clone.transform.SetParent(parent, false);
            clone.SetActive(show);
        }
        else if (asset is Material mat)
        {
            GameObject matObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            matObj.name = "Material: " + mat.name;
            matObj.GetComponent<Renderer>().sharedMaterial = mat;
            matObj.transform.SetParent(parent, false);
            matObj.SetActive(show);
        }
    }

    static void ShowAssetsInHierarchy<T>(Dictionary<Object, List<string>> result) where T : Object
    {
        var keys = new List<Object>();
        keys.AddRange(result.Keys);
        keys.Sort((a, b) =>
        {
            return string.Compare(a.name, b.name);
        });

        foreach (var key in keys)
        {
            var list = result[key];
            list.Sort((a, b) =>
            {
                return string.Compare(a, b);
            });

            string prefix = key.GetType().ToString();
            GameObject go = new GameObject(prefix + " : " + key.name + " (" + list.Count + " results)");
            bool show = true;
            foreach (var path in list)
            {
                CreateAssetGameObject<T>(path, go.transform, show);
                show = false;
            }
        }
    }

    static bool FindBySelect(Object[] selects, string[] assets, out Dictionary<Object, List<string>> result)
    {
        float p = 0;
        int total = assets.Length;
        result = new Dictionary<Object, List<string>>();

        foreach (var asset in assets)
        {
            var dependencies = AssetDatabase.GetDependencies(asset);

            foreach (var dependency in dependencies)
            {
                foreach (var select in selects)
                {
                    var assetPath = AssetDatabase.GetAssetPath(select);
                    if (dependency == assetPath)
                    {
                        if (!result.ContainsKey(select))
                            result[select] = new List<string>();

                        result[select].Add(asset);
                    }

                }
            }

            float percent = ++p * 1f / total;
            if (EditorUtility.DisplayCancelableProgressBar(string.Format("Searching...({0}%)", (percent * 100).ToString("F2")), asset, percent))
                break;
        }

        return result.Count > 0;
    }
}
