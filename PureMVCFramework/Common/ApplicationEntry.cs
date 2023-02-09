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

            Facade.RegisterCommand(RegistMediatorCommand.Name, () => new RegistMediatorCommand());
            Facade.RegisterCommand(RemoveMediatorCommand.Name, () => new RemoveMediatorCommand());
        }

        private void Start()
        {
            OnLaunch();
        }

        private void OnDestroy()
        {
            Facade.RemoveCommand(RegistMediatorCommand.Name);
            Facade.RemoveCommand(RemoveMediatorCommand.Name);
        }

        protected virtual void OnLaunch()
        {
            var provider = new AddressableProvider();
            provider.InitializedCallback = OnStart;
            ResourceManager.Instance.Initialize(provider);
        }

        private void OnApplicationQuit()
        {
            OnStop();
        }

        protected virtual void InitializeCommand() { }
        protected virtual void InitializeProxy() { }
        protected virtual void InitializeMediator() { }

        public virtual void OnStart()
        {
            InitializeProxy();
            InitializeCommand();
            InitializeMediator();
        }

        protected virtual void OnStop() { }
    }
}
