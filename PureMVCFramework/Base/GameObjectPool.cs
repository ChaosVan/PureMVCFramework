using UnityEngine;
using System.Collections.Generic;
using System.Collections;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework
{
    public sealed class GameObjectPool : SingletonBehaviour<GameObjectPool>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeOnDisableDomainReload()
        {
            applicationIsQuitting = false;
            tempList.Clear();
        }

        public enum StartupPoolMode { Awake, Start, CallManually };

        [System.Serializable]
        public class StartupPool
        {
            public int size;
            public GameObject prefab;
        }

        static List<GameObject> tempList = new List<GameObject>();

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.Foldout)]
#endif
        private readonly Dictionary<GameObject, List<GameObject>> pooledObjects = new Dictionary<GameObject, List<GameObject>>(); // prefab, List<obj>
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.OneLine)]
#endif
        private readonly Dictionary<GameObject, GameObject> spawnedObjects = new Dictionary<GameObject, GameObject>(); // obj, prefab
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.OneLine)]
#endif
        private readonly Dictionary<GameObject, float> pooledTimeStamps = new Dictionary<GameObject, float>();

        public StartupPoolMode startupPoolMode = StartupPoolMode.CallManually;
        public StartupPool[] startupPools;

        bool startupPoolsCreated;

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo")]
#endif
        public float ExpiredTime { get; set; } = 60;


        void Awake()
        {
            if (startupPoolMode == StartupPoolMode.Awake)
                CreateStartupPools();
        }

        void Start()
        {
            if (startupPoolMode == StartupPoolMode.Start)
                CreateStartupPools();

            TimerManager.Instance.AddRealtimeRepeatTask(ExpiredTime, 1, -1, CheckExpiredObject);
        }

        bool CheckExpiredObject()
        {
            foreach (var p in pooledTimeStamps)
            {
                if (p.Key == null || Time.realtimeSinceStartup - p.Value > ExpiredTime)
                {
                    tempList.Add(p.Key);
                }
            }
            foreach (var key in tempList)
            {
                pooledTimeStamps.Remove(key);
                if (key != null)
                    Destroy(key);
            }
            tempList.Clear();

            foreach (var p in pooledObjects)
            {
                if (p.Key == null)
                {
                    tempList.Add(p.Key);
                }
            }
            foreach (var key in tempList)
            {
                pooledObjects.Remove(key);
            }
            tempList.Clear();

            return !isActiveAndEnabled;
        }

        private static void RecordTimeStamp(GameObject obj)
        {
            Instance.pooledTimeStamps[obj] = Time.realtimeSinceStartup;
        }

        private static void RemoveFromTimeStamp(GameObject obj)
        {
            if (Instance.pooledTimeStamps.ContainsKey(obj))
                Instance.pooledTimeStamps.Remove(obj);
        }

        public static void CreateStartupPools()
        {
            if (!Instance.startupPoolsCreated)
            {
                Instance.startupPoolsCreated = true;
                var pools = Instance.startupPools;
                if (pools != null && pools.Length > 0)
                    for (int i = 0; i < pools.Length; ++i)
                        CreatePool(pools[i].prefab, pools[i].size);
            }
        }

        public static void CreatePool<T>(T prefab, int initialPoolSize) where T : Component
        {
            CreatePool(prefab.gameObject, initialPoolSize);
        }
        public static void CreatePool(GameObject prefab, int initialPoolSize)
        {
            if (prefab != null && !Instance.pooledObjects.ContainsKey(prefab))
            {
                var list = new List<GameObject>();
                Instance.pooledObjects[prefab] = list;

                if (initialPoolSize > 0)
                {
                    bool active = prefab.activeSelf;
                    prefab.SetActive(false);
                    Transform parent = Instance.transform;
                    while (list.Count < initialPoolSize)
                    {
                        var obj = Instantiate(prefab);
                        obj.name = obj.name.Replace("(Clone)", "(Spawn)"); // mark object as "Spawn"
                        obj.transform.SetParent(parent, false);
                        list.Add(obj);
                        RecordTimeStamp(obj);
                    }
                    prefab.SetActive(active);
                }
            }
        }

        public static T Spawn<T>(T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
        {
            return Spawn(prefab.gameObject, parent, position, rotation).GetComponent<T>();
        }
        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            return Spawn(prefab.gameObject, null, position, rotation).GetComponent<T>();
        }
        public static T Spawn<T>(T prefab, Transform parent, Vector3 position) where T : Component
        {
            return Spawn(prefab.gameObject, parent, position, Quaternion.identity).GetComponent<T>();
        }
        public static T Spawn<T>(T prefab, Vector3 position) where T : Component
        {
            return Spawn(prefab.gameObject, null, position, Quaternion.identity).GetComponent<T>();
        }
        public static T Spawn<T>(T prefab, Transform parent) where T : Component
        {
            return Spawn(prefab.gameObject, parent, Vector3.zero, Quaternion.identity).GetComponent<T>();
        }
        public static T Spawn<T>(T prefab) where T : Component
        {
            return Spawn(prefab.gameObject, null, Vector3.zero, Quaternion.identity).GetComponent<T>();
        }
        public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            Transform trans;
            GameObject obj;
            if (Instance.pooledObjects.TryGetValue(prefab, out List<GameObject> list))
            {
                obj = null;
                if (list.Count > 0)
                {
                    // the object in list maybe null
                    while (obj == null && list.Count > 0)
                    {
                        obj = list[0];
                        list.RemoveAt(0);
                    }
                    if (obj != null)
                    {
                        trans = obj.transform;
                        trans.SetParent(parent, false);
                        trans.localPosition = position;
                        trans.localRotation = rotation;
                        obj.SetActive(true);
                        Instance.spawnedObjects.Add(obj, prefab);
                        RemoveFromTimeStamp(obj);
                        return obj;
                    }
                }
                obj = Instantiate(prefab);
                obj.name = obj.name.Replace("(Clone)", "(Spawn)");  // mark object as "Spawn"

                trans = obj.transform;
                trans.SetParent(parent, false);
                trans.localPosition = position;
                trans.localRotation = rotation;
                obj.SetActive(true);
                Instance.spawnedObjects.Add(obj, prefab);

                DontDestroyOnLoad(obj);
                return obj;
            }
            else
            {
                obj = Instantiate(prefab);
                trans = obj.GetComponent<Transform>();
                trans.SetParent(parent, false);
                trans.localPosition = position;
                trans.localRotation = rotation;
                obj.SetActive(true);
                return obj;
            }
        }
        public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position)
        {
            return Spawn(prefab, parent, position, Quaternion.identity);
        }
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Spawn(prefab, null, position, rotation);
        }
        public static GameObject Spawn(GameObject prefab, Transform parent)
        {
            return Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
        }
        public static GameObject Spawn(GameObject prefab, Vector3 position)
        {
            return Spawn(prefab, null, position, Quaternion.identity);
        }
        public static GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, null, Vector3.zero, Quaternion.identity);
        }

        public static void Recycle<T>(T obj) where T : Component
        {
            Recycle(obj.gameObject);
        }
        public static void Recycle(GameObject obj)
        {
            if (applicationIsQuitting)
                return;

            if (Instance != null && Instance.spawnedObjects.TryGetValue(obj, out GameObject prefab))
            {
                if (obj != null)
                    Recycle(obj, prefab);
                else
                    Instance.spawnedObjects.Remove(obj);
            }
            else
            {
                Debug.LogWarningFormat("{0} has been recycled or not in pooled, it will be destroyed", obj);
                Destroy(obj);
            }
        }
        static void Recycle(GameObject obj, GameObject prefab)
        {
            Instance.spawnedObjects.Remove(obj);
            if (Instance.pooledObjects.TryGetValue(prefab, out List<GameObject> list))
            {
                list.Add(obj);
                obj.transform.SetParent(Instance.transform, false);
                obj.SetActive(false);
                RecordTimeStamp(obj);
            }
            else
            {
                Debug.LogWarningFormat("{0} has been recycled or not in pooled, it will be destroyed", obj);
                Destroy(obj);
            }
        }

        public static void RecycleAll<T>(T prefab) where T : Component
        {
            RecycleAll(prefab.gameObject);
        }
        public static void RecycleAll(GameObject prefab)
        {
            if (applicationIsQuitting)
                return;

            if (Instance != null)
            {
                foreach (var item in Instance.spawnedObjects)
                {
                    if (item.Value == prefab)
                        tempList.Add(item.Key);
                }
                for (int i = 0; i < tempList.Count; ++i)
                {
                    Recycle(tempList[i]);
                }

                tempList.Clear();
            }
        }

        public static bool IsSpawned(GameObject obj)
        {
            return Instance.spawnedObjects.ContainsKey(obj);
        }

        public static int CountPooled<T>(T prefab) where T : Component
        {
            return CountPooled(prefab.gameObject);
        }
        public static int CountPooled(GameObject prefab)
        {
            return Instance.pooledObjects.TryGetValue(prefab, out List<GameObject> list) ? list.Count : 0;
        }

        public static int CountSpawned<T>(T prefab) where T : Component
        {
            return CountSpawned(prefab.gameObject);
        }
        public static int CountSpawned(GameObject prefab)
        {
            int count = 0;
            foreach (var instancePrefab in Instance.spawnedObjects.Values)
                if (prefab == instancePrefab)
                    ++count;
            return count;
        }

        public static int CountAllPooled()
        {
            int count = 0;
            foreach (var list in Instance.pooledObjects.Values)
                count += list.Count;
            return count;
        }

        public static List<GameObject> GetPooled(GameObject prefab, List<GameObject> list, bool appendList)
        {
            if (list == null)
                list = new List<GameObject>();
            if (!appendList)
                list.Clear();
            if (Instance.pooledObjects.TryGetValue(prefab, out List<GameObject> pooled))
            {
                list.AddRange(pooled);
            }

            return list;
        }
        public static List<T> GetPooled<T>(T prefab, List<T> list, bool appendList) where T : Component
        {
            if (list == null)
                list = new List<T>();
            if (!appendList)
                list.Clear();
            if (Instance.pooledObjects.TryGetValue(prefab.gameObject, out List<GameObject> pooled))
            {
                for (int i = 0; i < pooled.Count; ++i)
                    list.Add(pooled[i].GetComponent<T>());
            }

            return list;
        }

        public static List<GameObject> GetSpawned(GameObject prefab, List<GameObject> list, bool appendList)
        {
            if (list == null)
                list = new List<GameObject>();
            if (!appendList)
                list.Clear();
            foreach (var item in Instance.spawnedObjects)
                if (item.Value == prefab)
                    list.Add(item.Key);
            return list;
        }
        public static List<T> GetSpawned<T>(T prefab, List<T> list, bool appendList) where T : Component
        {
            if (list == null)
                list = new List<T>();
            if (!appendList)
                list.Clear();
            var prefabObj = prefab.gameObject;
            foreach (var item in Instance.spawnedObjects)
                if (item.Value == prefabObj)
                    list.Add(item.Key.GetComponent<T>());
            return list;
        }

        public static void DestroyPooled(GameObject prefab)
        {
            if (applicationIsQuitting)
                return;

            if (Instance != null && Instance.pooledObjects.TryGetValue(prefab, out List<GameObject> pooled))
            {
                Instance.pooledObjects.Remove(prefab);
                for (int i = 0; i < pooled.Count; ++i)
                {
                    if (pooled[i] != null)
                        Destroy(pooled[i]);
                }
                pooled.Clear();
            }
        }
        public static void DestroyPooled<T>(T prefab) where T : Component
        {
            DestroyPooled(prefab.gameObject);
        }

        public static GameObject GetPrefab(GameObject obj)
        {
            return Instance.spawnedObjects.TryGetValue(obj, out GameObject prefab) ? prefab : null;
        }

        public static GameObject GetPrefab<T>(T obj) where T : Component
        {
            return GetPrefab(obj.gameObject);
        }

    }
}

