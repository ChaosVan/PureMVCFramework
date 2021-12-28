using PureMVC.Interfaces;
using PureMVC.Patterns.Facade;
using PureMVCFramework.Advantages;
using PureMVCFramework.Patterns;
using PureMVCFramework.Providers;
using UnityEngine;

namespace PureMVCFramework
{
    public class ApplicationEntry : NotifierBehaviour
    {
        private void Awake()
        {
            Debug.unityLogger.logEnabled = Debug.isDebugBuild;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            OnLaunch();
        }

        protected virtual void OnLaunch()
        {
            var provider = new AddressableProvider();
            provider.InitializedCallback = OnStart;
            ResourceManager.Instance.Initialize(provider);

            ReferencePool.Instance.LoadTypes("PureMVCFramework");
            ReferencePool.Instance.LoadTypes("Entity");
            ReferencePool.Instance.LoadTypes("Assembly-CSharp");
        }

        private void OnApplicationQuit()
        {
            OnStop();
        }

        protected virtual void InitializeCommand() { }
        protected virtual void InitializeProxy() { }
        protected virtual void InitializeMediator() { }
        protected virtual void InitializeSystem() { }

        public virtual void OnStart()
        {
            Debug.Log("OnStart");

            InitializeProxy();
            InitializeCommand();
            InitializeMediator();
            InitializeSystem();
        }

        protected virtual void OnStop() { }
    }
}
