using UnityEngine;

namespace PureMVCFramework
{
    public class SingletonParent : SingletonBehaviour<SingletonParent> { }

    /// <summary>
    /// Be aware this will not prevent a non singleton constructor
    ///   such as `T myT = new T();`
    /// To prevent that, add `protected T () {}` to your singleton class.
    /// 
    /// As a note, this is made as MonoBehaviour because we need Coroutines and FixedUpdate.
    /// </summary>
    public class SingletonBehaviour<T> : NotifierBehaviour where T : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [SerializeField]
        protected bool showOdinInfo;
#endif

        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                        "' already destroyed on application quit." +
                        " Won't create again - returning null.");
                    return null;
                }

                // Double-Checked Locking
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var objs = FindObjectsOfType<T>();
                            if (objs.Length > 1)
                            {
                                Debug.LogError("[Singleton] Something went really wrong " +
                                    " - there should never be more than 1 singleton!" +
                                    " Reopenning the scene might fix it.");
                                _instance = objs[0];
                            }
                            else if (objs.Length > 0)
                            {
                                _instance = objs[0];
                            }

                            if (_instance == null)
                            {
                                GameObject singleton = new GameObject("(Singleton) " + typeof(T));
                                _instance = singleton.AddComponent<T>();

                                DontDestroyOnLoad(singleton);

                                singleton.transform.SetParent(SingletonParent.Instance.transform);
                            }
                            else
                            {
                                Debug.Log("[Singleton] Using instance already created: " +
                                    _instance.gameObject.name);
                            }
                        }
                    }
                }

                return _instance;
            }
        }

        public static bool applicationIsQuitting = false;

        public static void DestroyInstance()
        {
            if (Instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private void Awake()
        {
            OnInitialized();
        }

        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed, 
        /// it will create a buggy ghost object that will stay on the Editor scene
        /// even after stopping playing the Application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        private void OnDestroy()
        {
            applicationIsQuitting = true;

            OnDelete();
            _instance = null;
        }

        public enum UpdateMode { FIXED_UPDATE, UPDATE, LATE_UPDATE }
        public UpdateMode updateMode = UpdateMode.UPDATE;
        private void Update()
        {
            if (updateMode == UpdateMode.UPDATE) OnUpdate(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FIXED_UPDATE) OnUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (updateMode == UpdateMode.LATE_UPDATE) OnUpdate(Time.deltaTime);
        }

        protected virtual void OnUpdate(float delta)
        {

        }

        protected virtual void OnInitialized()
        {
            Debug.Log("[Singleton] An instance of " + typeof(T) +
                " is needed in the scene, so '" + gameObject +
                "' was created with DontDestroyOnLoad.");
        }

        protected virtual void OnDelete()
        {
            Debug.Log("[Singleton] An instance of " + typeof(T) + " begin to destroy.");
        }
    }
}