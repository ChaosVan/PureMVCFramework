using PureMVCFramework.Patterns;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace PureMVCFramework.UI
{
    public class UIWindow : UIComponent
    {
        public enum WindowMode
        {
            Single = 0,     // 只存在一个
            SingleInStack,  // 只存在一个，并且进栈
            Multiple,       // 可以存在多个
        }

        [System.Serializable]
        public struct UIWindowParams
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

        public struct WorldParam
        {
            public Transform root;
            public Vector3 position;
            public Quaternion rotation;
            /// <summary>
            /// 创建成功时发的消息
            /// </summary>
            public string callbackNotification;
            public object userdata;
        }

        public UIWindowParams config;
        public WorldParam worldParam;

        public bool IsLoading { get; internal set; }
        public bool IsOpen { get; private set; }
        public bool IsFocus
        {
            get
            {
                return UIManager.Instance.CurrentFocusWindow == this;
            }
            internal set
            {
                OnFocus(value);
            }
        }
        public Canvas Canvas { get; private set; }

        internal bool ForceClosed { get; set; }

        protected override void OnDelete()
        {
            IsOpen = false;
            IsFocus = false;

            config = default;
            worldParam = default;

            base.OnDelete();
        }

        protected virtual void OnFocus(bool tf)
        {

        }

        private void SetCanvas(GameObject gameObject)
        {
            Canvas = gameObject.GetComponent<Canvas>();
            if (Canvas != null)
            {
                switch (Canvas.renderMode)
                {
                    case RenderMode.ScreenSpaceOverlay:
                        if (config.layer == UILayer.Top)
                        {
                            Canvas.sortingOrder = config.orderInLayer;
                        }
                        else
                        {
                            Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                            Canvas.worldCamera = UIManager.Instance.GetCamera(config.cameraIndex);
                            Canvas.sortingLayerName = config.layer.ToString();
                            Canvas.sortingOrder = config.orderInLayer;

                            UIManager.Instance.RegisterWindowCamera(this, config.cameraIndex);
                        }
                        break;
                    case RenderMode.ScreenSpaceCamera:
                        if (config.layer == UILayer.Top)
                        {
                            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                            Canvas.sortingOrder = config.orderInLayer;
                        }
                        else
                        {
                            Canvas.worldCamera = UIManager.Instance.GetCamera(config.cameraIndex);
                            Canvas.sortingLayerName = config.layer.ToString();
                            Canvas.sortingOrder = config.orderInLayer;

                            UIManager.Instance.RegisterWindowCamera(this, config.cameraIndex);
                        }
                        break;
                    case RenderMode.WorldSpace:
                        Canvas.worldCamera = Camera.main;
                        Canvas.sortingLayerName = config.layer.ToString();
                        Canvas.sortingOrder = config.orderInLayer;
                        break;
                    default:
                        break;
                }
            }
        }

        private void ResetCanvas()
        {
            if (Canvas != null)
            {
                UIManager.Instance.UnregistWindowCamera(this, config.cameraIndex);
                Canvas.worldCamera = null;
                Canvas.sortingLayerName = UILayer.Default.ToString();
                Canvas.sortingOrder = 0;
                Canvas = null;
            }
        }

        internal void Open(GameObject gameObject, object userdata)
        {
            Assert.IsNotNull(gameObject, config.prefabPath);
            Assert.IsFalse(IsOpen);

            IsLoading = false;

            if (ForceClosed)
            {
                gameObject.Recycle();
                ForceClosed = false;
                return;
            }

            IsOpen = true;

            // Set Canvas Layer
            SetCanvas(gameObject);

            if (userdata is WorldParam param)
            {
                if (param.root != null)
                {
                    gameObject.transform.SetParent(param.root, false);
                }
                else
                {
                    gameObject.transform.position = param.position;
                    gameObject.transform.rotation = param.rotation;
                }

                worldParam = param;
                userdata = param.userdata;

                if (!string.IsNullOrEmpty(config.mediatorName))
                {
                    var mediator = Facade.RetrieveMediator(config.mediatorName);
                    if (mediator == null)
                        SendNotification(RegistMediatorCommand.Name, new Dictionary<string, UIWindow>(), config.mediatorName);
                }
            }

            // window是否需要进栈
            if (config.windowMode == WindowMode.SingleInStack)
                UIManager.Instance.PushIntoStack(this);
            else
                IsFocus = true;

            try
            {
                OnCreate(gameObject, userdata);
                ApplySafeArea(Screen.safeArea);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }

            transform.SetAsLastSibling();

            if (!string.IsNullOrEmpty(config.mediatorName))
            {
                if (config.windowMode != WindowMode.Multiple)
                {
                    SendNotification(RegistMediatorCommand.Name, this, config.mediatorName);
                }
                else
                {
                    if (!string.IsNullOrEmpty(worldParam.callbackNotification))
                        SendNotification(worldParam.callbackNotification, this);
                }
            }
        }

        internal void Close()
        {
            if (IsLoading)
            {
                ForceClosed = true;
                return;
            }

            if (!string.IsNullOrEmpty(config.mediatorName))
            {
                if (config.windowMode != WindowMode.Multiple)
                {
                    SendNotification(RemoveMediatorCommand.Name, this, config.mediatorName);

                }
            }

            // Reset Canvas Layer
            ResetCanvas();

            if (gameObject != null)
                gameObject.Recycle();

            try
            {
                OnDelete();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        protected virtual void ApplySafeArea(Rect area)
        {

        }
    }
}
