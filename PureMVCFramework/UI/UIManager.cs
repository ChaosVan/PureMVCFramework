using PureMVCFramework.Advantages;
using PureMVCFramework.Patterns;
using static PureMVCFramework.UI.UIWindow;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.UI
{
    public enum UILayer
    {
        Background = -1,
        Default = 0,
        SceneLayer,
        Info,
        Top,
    }

    public class UIManager : SingletonBehaviour<UIManager>
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.Foldout)]
#endif
        private readonly Dictionary<UILayer, List<UIWindow>> m_ActiveWindows = new Dictionary<UILayer, List<UIWindow>>();

        // mode为single的windows缓存，确保只打开一个
        private readonly Dictionary<string, UIWindow> m_SingleWindows = new Dictionary<string, UIWindow>();
        // UI栈，支持UI的后开先关功能
        private readonly Stack<UIWindow> m_UIStack = new Stack<UIWindow>();

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo")]
#endif
        public UIWindow CurrentFocusWindow { get; private set; }
        public EventSystem EventSystem { get; private set; }

        private List<Camera> cameras = new List<Camera>();
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.Foldout)]
#endif
        private Dictionary<Camera, List<UIWindow>> cameraBinders = new Dictionary<Camera, List<UIWindow>>();

        public int StackSize
        {
            get
            {
                return m_UIStack.Count;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            EventSystem = FindObjectOfType<EventSystem>();

            CreateCustomCamera("UI Camera", 1 << LayerMask.NameToLayer("UI"));

            Facade.RegisterCommand(RegistMediatorCommand.Name, () => new RegistMediatorCommand());
            Facade.RegisterCommand(RemoveMediatorCommand.Name, () => new RemoveMediatorCommand());
        }

        protected override void OnDelete()
        {
            Facade.RemoveCommand(RegistMediatorCommand.Name);
            Facade.RemoveCommand(RemoveMediatorCommand.Name);

            base.OnDelete();
        }

        public Camera CreateCustomCamera(string name, int cullingMask, bool enabled = false, int depth = 1)
        {
            var camera = new GameObject(name).AddComponent<Camera>();
            camera.transform.SetParent(transform, false);
            camera.clearFlags = CameraClearFlags.Depth;
            camera.cullingMask = cullingMask;
            camera.depth = depth;

            camera.enabled = enabled;

            cameras.Add(camera);

            return camera;
        }

        public Camera GetCamera(int index)
        {
            Assert.IsTrue(index >= 0);
            if (index < cameras.Count)
                return cameras[index];

            return cameras[cameras.Count - 1];
        }

        public void RegisterWindowCamera(UIWindow window, int index)
        {
            var camera = GetCamera(index);
            if (camera != null)
            {
                if (!cameraBinders.TryGetValue(camera, out var list))
                {
                    list = new List<UIWindow>();
                    cameraBinders.Add(camera, list);
                    camera.enabled = true;
                }

                list.Add(window);
            }
        }

        public void UnregistWindowCamera(UIWindow window, int index)
        {
            var camera = GetCamera(index);
            if (camera != null)
            {
                if (cameraBinders.TryGetValue(camera, out var list))
                {
                    list.Remove(window);

                    if (list.Count == 0)
                    {
                        cameraBinders.Remove(camera);
                        camera.enabled = false;
                    }
                }
            }
        }

        private void UpdateCurrentFocusWindow()
        {
            while (CurrentFocusWindow != null && !CurrentFocusWindow.IsOpen)
            {
                m_UIStack.Pop();
                if (m_UIStack.Count > 0)
                    CurrentFocusWindow = m_UIStack.Peek();
                else
                    CurrentFocusWindow = null;
            }

            if (CurrentFocusWindow != null)
                CurrentFocusWindow.IsFocus = true;
        }

        internal void PushIntoStack(UIWindow window)
        {
            m_UIStack.Push(window);

            // 当前window失去焦点，新的window获得焦点
            if (CurrentFocusWindow != null)
                CurrentFocusWindow.IsFocus = false;
            CurrentFocusWindow = window;
            CurrentFocusWindow.IsFocus = true;
        }

        public UIWindow GetWindow(string windowName)
        {
            foreach (var windows in m_ActiveWindows.Values)
            {
                foreach (var window in windows)
                {
                    if (window.config.name == windowName)
                        return window;
                }
            }

            return null;
        }

        protected UIWindow InternalOpenWindow(UIWindowParams param)
        {
            Assert.IsNotNull(param);
            Assert.IsFalse(string.IsNullOrEmpty(param.name));

            // 多开的window或者单开window的缓存里找不到，都需要创建window
            if (param.windowMode == WindowMode.Multiple || !m_SingleWindows.TryGetValue(param.name, out UIWindow window))
            {
                if (string.IsNullOrEmpty(param.windowClass))
                {
                    window = ReferencePool.SpawnInstance<UIWindow>();
                }
                else
                {
                    window = ReferencePool.SpawnInstance(param.windowClass) as UIWindow;
                }

                // 避免重复打开
                if (param.windowMode != WindowMode.Multiple)
                    m_SingleWindows[param.name] = window;
            }

            Assert.IsNotNull(window, param.name + " open failed!");

            if (window.IsLoading || window.IsOpen)
                return window;

            // 覆盖新的config
            window.config = param;

            // 创建Layer
            if (!m_ActiveWindows.ContainsKey(param.layer))
                m_ActiveWindows[param.layer] = new List<UIWindow>();

            m_ActiveWindows[param.layer].Add(window);


            return window;
        }

        public UIWindow OpenWindow(string json, object userdata = null)
        {
            try
            {
                UIWindowParams param = JsonUtility.FromJson<UIWindowParams>(json);
                return OpenWindow(param, userdata);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }

            return null;
        }

        public UIWindow OpenWindow(UIWindowParams param, object userdata = null)
        {
            return OpenWindow(param, null, userdata);
        }

        public UIWindow OpenWindow(UIWindowParams param, System.Action<UIWindow, object> callback, object userdata = null)
        {
            var window = InternalOpenWindow(param);

            if (!window.IsLoading && !window.IsOpen)
            {
                window.IsLoading = true;
                // 加载Prefab
                AutoReleaseManager.Instance.LoadGameObjectAsync(param.prefabPath, transform, (obj, data) =>
                {
                    if (window.Init(obj, data))
                    {
                        callback?.Invoke(window, userdata);
                        delayOpen.Enqueue(window);
                    }
                }, userdata);
            }

            return window;
        }

        //private struct DelayCallback
        //{
        //    public UIWindow window;
        //    public System.Action<UIWindow, object> callback;
        //    public object userdata;
        //}

        private Queue<UIWindow> delayOpen = new Queue<UIWindow>();

        protected override void OnUpdate(float delta)
        {
            if (!ResourceManager.Instance.IsSpriteAtlasRequesting)
            {
                while(delayOpen.Count > 0)
                {
                    delayOpen.Dequeue().Open();
                }
            }
        }

        public void CloseWindow(UIWindow window)
        {
            Assert.IsNotNull(window);

            if (m_ActiveWindows.TryGetValue(window.config.layer, out List<UIWindow> windows) && windows != null)
            {
                windows.Remove(window);
            }

            if (window.config.windowMode != WindowMode.Multiple)
                m_SingleWindows.Remove(window.config.name);

            window.Close();
            UpdateCurrentFocusWindow();
        }

        public void CloseWindow(UIWindowParams param)
        {
            if (m_ActiveWindows.TryGetValue(param.layer, out List<UIWindow> windows) && windows != null)
            {
                var window = windows.Find(x => x.config.name == param.name);
                if (window != null)
                    CloseWindow(window);
            }
        }

        public bool CloseWindowFromStack()
        {
            if (CurrentFocusWindow != null)
                CloseWindow(CurrentFocusWindow);
            else
                return false;

            return true;
        }

        public void CloseWindowByLayer(UILayer layer)
        {
            if (m_ActiveWindows.TryGetValue(layer, out List<UIWindow> windows) && windows != null)
            {
                foreach (var window in windows)
                {
                    if (window.config.windowMode != WindowMode.Multiple)
                        m_SingleWindows.Remove(window.config.name);

                    window.Close();
                }

                m_ActiveWindows[layer].Clear();
                UpdateCurrentFocusWindow();
            }
        }

        public void CloseWindowExceptLayer(UILayer layer)
        {
            foreach (var pair in m_ActiveWindows)
            {
                if (pair.Key == layer)
                    continue;

                foreach (var window in pair.Value)
                {
                    if (window.config.windowMode != WindowMode.Multiple)
                        m_SingleWindows.Remove(window.config.name);

                    window.Close();
                }

                m_ActiveWindows[pair.Key].Clear();
            }

            UpdateCurrentFocusWindow();
        }

        public void CloseAllWindows()
        {
            foreach (var windows in m_ActiveWindows.Values)
            {
                foreach (var window in windows)
                {
                    if (window.config.windowMode != WindowMode.Multiple)
                        m_SingleWindows.Remove(window.config.name);

                    window.Close();
                }
                windows.Clear();
            }

            m_ActiveWindows.Clear();
            m_UIStack.Clear();
            CurrentFocusWindow = null;
        }
    }
}
