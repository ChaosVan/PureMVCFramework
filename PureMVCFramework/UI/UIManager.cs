using PureMVCFramework.Advantages;
using PureMVCFramework.Patterns;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace PureMVCFramework.UI
{
    public enum WindowMode
    {
        Single = 0,     // 只存在一个
        SingleInStack,  // 只存在一个，并且进栈
        Multiple,       // 可以存在多个
    }

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
        // 将要关闭的UI列表
        private readonly List<UIWindow> m_willRemove = new List<UIWindow>();

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo")]
#endif
        private UIWindow _focusWindow;

        public UIWindow CurrentFocusWindow
        {
            get { return _focusWindow; }
            set
            {
                if (_focusWindow != null)
                    _focusWindow.SetFocus(false);

                _focusWindow = value;

                if (_focusWindow != null && _focusWindow.Status == WindowStatus.Opened)
                    _focusWindow.SetFocus(true);
            }
        }
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
        }

        protected override void OnDelete()
        {
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
            while (CurrentFocusWindow != null && CurrentFocusWindow.Status != WindowStatus.Opened)
            {
                m_UIStack.Pop();
                if (m_UIStack.Count > 0)
                    CurrentFocusWindow = m_UIStack.Peek();
                else
                    CurrentFocusWindow = null;
            }
        }

        internal void PushIntoStack(UIWindow window)
        {
            m_UIStack.Push(window);
            CurrentFocusWindow = window;
        }

        public UIWindow GetWindow(string windowName)
        {
            foreach (var windows in m_ActiveWindows.Values)
            {
                foreach (var window in windows)
                {
                    if (window.Status == WindowStatus.Closed)
                        continue;

                    if (window.config != null && window.config.name == windowName)
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

                window.Status = WindowStatus.None;

                // 避免重复打开
                if (param.windowMode != WindowMode.Multiple)
                    m_SingleWindows[param.name] = window;
            }

            Assert.IsNotNull(window, param.name + " open failed!");

            if (window.Status > WindowStatus.None)
                return window;

            window.Status = WindowStatus.None;

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

            if (window.Status == WindowStatus.None)
            {
                window.Status = WindowStatus.Loading;

                // 加载Prefab
                AutoReleaseManager.Instance.LoadGameObjectAsync(param.prefabPath, transform, (obj, data) =>
                {
                    if (window.Status == WindowStatus.Closed)
                    {
                        obj.Recycle();
                        return;
                    }

                    PreOpenWindow(window, obj);

                    if (window.Init(obj, data))
                    {
                        RegistMediator(window);

                        if (window.config.windowMode == WindowMode.SingleInStack)
                            PushIntoStack(window);
                        else
                            window.SetFocus(true);

                        callback?.Invoke(window, data);
                        delayOpen.Enqueue(window);
                    }
                    else
                    {
                        callback?.Invoke(null, data);
                    }
                }, userdata);
            }

            return window;
        }

        private Queue<UIWindow> delayOpen = new Queue<UIWindow>();

        protected override void OnUpdate(float delta)
        {
            if (!ResourceManager.Instance.IsSpriteAtlasRequesting)
            {
                while (delayOpen.Count > 0)
                {
                    var window = delayOpen.Dequeue();
                    if (window.Status != WindowStatus.Closed)
                    {
                        window.Open();
                        if (window.Canvas != null)
                            window.Canvas.enabled = true;
                    }
                }
            }
        }

        public void CloseWindow(UIWindow window)
        {
            Assert.IsNotNull(window);

            if (window.Status == WindowStatus.Closed)
            {
                UpdateCurrentFocusWindow();
                return;
            }

            if (m_ActiveWindows.TryGetValue(window.config.layer, out List<UIWindow> windows) && windows != null)
            {
                windows.Remove(window);
            }

            if (window.config.windowMode != WindowMode.Multiple)
                m_SingleWindows.Remove(window.config.name);

            RemoveMediator(window);
            window.Close(out var gameObject);
            PostCloseWindow(window, gameObject);

            UpdateCurrentFocusWindow();
        }

        public void CloseWindow(UIWindowParams param)
        {
            if (m_ActiveWindows.TryGetValue(param.layer, out List<UIWindow> windows) && windows != null)
            {
                var array = windows.FindAll(x => x.config.name == param.name);
                if (array != null)
                {
                    foreach (var window in array)
                        CloseWindow(window);
                }
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
                    if (window.Status == WindowStatus.Closed)
                        continue;

                    if (window.config.windowMode != WindowMode.Multiple)
                        m_SingleWindows.Remove(window.config.name);

                    m_willRemove.Add(window);
                }

                windows.Clear();
            }

            foreach (var window in m_willRemove)
            {
                RemoveMediator(window);
                window.Close(out var gameObject);
                PostCloseWindow(window, gameObject);
            }

            m_willRemove.Clear();

            UpdateCurrentFocusWindow();
        }

        public void CloseWindowExceptLayer(UILayer layer)
        {
            foreach (var pair in m_ActiveWindows)
            {
                if (pair.Key == layer)
                    continue;

                foreach (var window in pair.Value)
                {
                    if (window.Status == WindowStatus.Closed)
                        continue;

                    if (window.config.windowMode != WindowMode.Multiple)
                        m_SingleWindows.Remove(window.config.name);

                    m_willRemove.Add(window);
                }

                pair.Value.Clear();
            }

            foreach (var window in m_willRemove)
            {
                RemoveMediator(window);
                window.Close(out var gameObject);
                PostCloseWindow(window, gameObject);
            }

            m_willRemove.Clear();

            UpdateCurrentFocusWindow();
        }

        public void CloseAllWindows()
        {
            foreach (var windows in m_ActiveWindows.Values)
            {
                foreach (var window in windows)
                {
                    if (window.Status == WindowStatus.Closed)
                        continue;

                    if (window.config.windowMode != WindowMode.Multiple)
                        m_SingleWindows.Remove(window.config.name);

                    m_willRemove.Add(window);
                }

                windows.Clear();
            }

            foreach (var window in m_willRemove)
            {
                RemoveMediator(window);
                window.Close(out var gameObject);
                PostCloseWindow(window, gameObject);
            }

            m_willRemove.Clear();

            m_ActiveWindows.Clear();
            m_UIStack.Clear();
            CurrentFocusWindow = null;
        }

        private void PreOpenWindow(UIWindow window, GameObject gameObject)
        {
            if (gameObject != null)
            {
                //var canvas = SetCanvas(window.config, gameObject);
                var canvas = SetCanvasDel == null ? SetCanvas(window.config, gameObject) : SetCanvasDel.Invoke(window.config, gameObject);
                if (canvas != null)
                {
                    window.Canvas = canvas;
                    if (window.Canvas.renderMode != RenderMode.WorldSpace)
                        RegisterWindowCamera(window, window.config.cameraIndex);

                    window.Canvas.enabled = false;
                }
            }
        }

        private void PostCloseWindow(UIWindow window, GameObject gameObject)
        {
            if (gameObject != null)
            {
                if (window.Canvas.renderMode != RenderMode.WorldSpace)
                    UnregistWindowCamera(window, window.config.cameraIndex);

                ResetCanvas(gameObject);
                if (gameObject != null)
                    gameObject.Recycle();

                window.Canvas = null;
                window.config = null;
            }
        }

        public delegate Canvas SetCanvasDelegate(UIWindowParams window, GameObject gameObject);

        public SetCanvasDelegate SetCanvasDel { get; set; }


        public Canvas SetCanvas(UIWindowParams config, GameObject gameObject)
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                switch (canvas.renderMode)
                {
                    case RenderMode.ScreenSpaceOverlay:
                        if (config.layer == UILayer.Top)
                        {
                            canvas.sortingOrder = config.orderInLayer;
                        }
                        else
                        {
                            canvas.renderMode = RenderMode.ScreenSpaceCamera;
                            canvas.worldCamera = GetCamera(config.cameraIndex);
                            canvas.sortingLayerName = config.layer.ToString();
                            canvas.sortingOrder = config.orderInLayer;
                        }
                        break;
                    case RenderMode.ScreenSpaceCamera:
                        if (config.layer == UILayer.Top)
                        {
                            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                            canvas.sortingOrder = config.orderInLayer;
                        }
                        else
                        {
                            canvas.worldCamera = GetCamera(config.cameraIndex);
                            canvas.sortingLayerName = config.layer.ToString();
                            canvas.sortingOrder = config.orderInLayer;
                        }
                        break;
                    case RenderMode.WorldSpace:
                        canvas.worldCamera = Camera.main;
                        canvas.sortingLayerName = config.layer.ToString();
                        canvas.sortingOrder = config.orderInLayer;
                        break;
                    default:
                        break;
                }
            }

            return canvas;
        }

        public void ResetCanvas(GameObject gameObject)
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.worldCamera = null;
                canvas.sortingLayerName = UILayer.Default.ToString();
                canvas.sortingOrder = 0;
            }
        }

        private void RegistMediator(UIWindow window)
        {
            if (!string.IsNullOrEmpty(window.config.mediatorName))
            {
                if (window.config.windowMode == WindowMode.Multiple)
                {
                    if (!Facade.HasMediator(window.config.mediatorName))
                        SendNotification(RegistMediatorCommand.Name, window, window.config.mediatorName);
                    else if (window.worldParam != null && !string.IsNullOrEmpty(window.worldParam.createNotification))
                        SendNotification(window.worldParam.createNotification, window);
                }
                else
                {
                    SendNotification(RegistMediatorCommand.Name, window, window.config.mediatorName);
                }
            }
        }

        private void RemoveMediator(UIWindow window)
        {
            if (!string.IsNullOrEmpty(window.config.mediatorName))
            {
                if (window.config.windowMode == WindowMode.Multiple)
                {
                    if (window.worldParam != null && !string.IsNullOrEmpty(window.worldParam.deleteNotification))
                        SendNotification(window.worldParam.deleteNotification, window);
                }
                else
                {
                    SendNotification(RemoveMediatorCommand.Name, window, window.config.mediatorName);
                }
            }
        }
    }


    [System.Serializable]
    public class UIWindowParams
    {
        public string name;
        public UILayer layer;
        public int orderInLayer;
        public int cameraIndex;
        public WindowMode windowMode;
        public string windowClass;
        public string mediatorName;
        public string prefabPath;

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
