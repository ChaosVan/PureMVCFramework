#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Advantages
{
    public class AutoReleaseManager : SingletonBehaviour<AutoReleaseManager>
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.OneLine)]
#endif
        /// <summary>
        /// key is holder Object of the asset, such as GameObject, Image...
        /// value is the source asset
        /// </summary>
        private readonly Dictionary<Object, Object> m_AutoReleaseObjects = new Dictionary<Object, Object>();    // <holder, asset>
        private readonly List<Object> m_Unloaded = new List<Object>();


        protected override void OnInitialized()
        {
            base.OnInitialized();

            updateMode = UpdateMode.LATE_UPDATE;
        }

        protected override void OnDelete()
        {
            m_AutoReleaseObjects.Clear();

            base.OnDelete();
        }

        protected override void OnUpdate(float delta)
        {
            if (m_AutoReleaseObjects.Count > 0)
            {
                Dictionary<Object, Object>.Enumerator e = m_AutoReleaseObjects.GetEnumerator();
                while (e.MoveNext())
                {
                    if (e.Current.Key == null)
                    {
                        ResourceManager.Instance.ReleaseAsset(e.Current.Value);
                        m_Unloaded.Add(e.Current.Key);
                    }
                }

                if (m_Unloaded.Count > 0)
                {
                    for (int i = 0; i < m_Unloaded.Count; ++i)
                    {
                        m_AutoReleaseObjects.Remove(m_Unloaded[i]);
                    }
                    m_Unloaded.Clear();
                }
            }
        }

        public void RegistAutoRelease(Object obj, Object asset)
        {
            if (m_AutoReleaseObjects.TryGetValue(obj, out var cache))
                ResourceManager.Instance.ReleaseAsset(cache);

            m_AutoReleaseObjects[obj] = asset;
        }

        public void LoadGameObjectAsync(string assetPath, System.Action<GameObject, object> callback = null, object userdata = null)
        {
            void OnLoaded(GameObject prefab, object data)
            {
                if (prefab != null)
                {
                    prefab.CreatePool();
                    GameObject go = prefab.Spawn();
                    RegistAutoRelease(go, prefab);
                    callback?.Invoke(go, data);
                }
                else
                {
                    callback?.Invoke(null, data);
                }
            }

            ResourceManager.Instance.LoadAssetAsync<GameObject>(assetPath, OnLoaded, userdata);
        }

        public void LoadGameObjectAsync(string assetPath, Transform parent, System.Action<GameObject, object> callback = null, object userdata = null)
        {
            void OnLoaded(GameObject prefab, object data)
            {
                if (prefab != null)
                {
                    prefab.CreatePool();
                    GameObject go = prefab.Spawn(parent);
                    RegistAutoRelease(go, prefab);
                    callback?.Invoke(go, data);
                }
                else
                {
                    callback?.Invoke(null, data);
                }
            }

            ResourceManager.Instance.LoadAssetAsync<GameObject>(assetPath, OnLoaded, userdata);
        }

        public void LoadGameObjectAsync(string assetPath, Vector3 position, Quaternion rotation, System.Action<GameObject, object> callback = null, object userdata = null)
        {
            void OnLoaded(GameObject prefab, object data)
            {
                if (prefab != null)
                {
                    prefab.CreatePool();
                    GameObject go = prefab.Spawn(position, rotation);
                    RegistAutoRelease(go, prefab);
                    callback?.Invoke(go, data);
                }
                else
                {
                    callback?.Invoke(null, data);
                }
            }

            ResourceManager.Instance.LoadAssetAsync<GameObject>(assetPath, OnLoaded, userdata);
        }
    }
}
