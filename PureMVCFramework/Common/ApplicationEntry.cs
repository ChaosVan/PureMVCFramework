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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeOnDisableDomainReload()
        {
#if UNITY_EDITOR
            var assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
            var types = assembly.GetTypes();

            var facade = PureMVC.Patterns.Facade.Facade.GetInstance(() => new Facade());
            foreach (var type in types)
            {
                if (type is IMediator)
                {
                    if (facade.HasMediator(type.FullName))
                        facade.RemoveMediator(type.FullName);
                    if (facade.HasMediator(type.Name))
                        facade.RemoveMediator(type.Name);
                }
                else if (type is IProxy)
                {
                    if (facade.HasProxy(type.FullName))
                        facade.RemoveProxy(type.FullName);
                    if (facade.HasProxy(type.Name))
                        facade.RemoveProxy(type.Name);
                }
                else if (type is ICommand)
                {
                    if (facade.HasCommand(type.FullName))
                        facade.RemoveCommand(type.FullName);
                    if (facade.HasCommand(type.Name))
                        facade.RemoveCommand(type.Name);
                }
            }
#endif
        }

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
