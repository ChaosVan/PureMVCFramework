using UnityEngine;
using UnityEngine.Assertions;

namespace PureMVCFramework.UI
{
    public enum WindowStatus
    {
        None,
        Loading,
        Inited,
        Opened,
        Closed,
    }

    public class WorldParam
    {
        public Transform root;
        public Vector3 position;
        public Quaternion rotation;

        [System.Obsolete]
        public string callbackNotification;
        /// <summary>
        /// window打开后发出通知
        /// </summary>
        public string createNotification;
        /// <summary>
        /// window关闭后发出通知
        /// </summary>
        public string deleteNotification;
        public object userdata;
    }

    public class UIWindow : UIComponent
    {
        public UIWindowParams config;
        public WorldParam worldParam;

        public WindowStatus Status { get; internal set; }

        public bool IsOpen => Status == WindowStatus.Opened;

        public Canvas Canvas { get; internal set; }

        protected virtual void OnOpen() { }

        protected virtual void OnFocus(bool tf) { }

        protected virtual void ApplySafeArea(Rect area) { }

        internal bool Init(GameObject gameObject, object userdata)
        {
            Assert.IsNotNull(gameObject, config.prefabPath);

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
            }

            Status = WindowStatus.Inited;

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

            return true;
        }

        internal void Open()
        {
            Status = WindowStatus.Opened;
            OnOpen();
        }

        internal void Close(out GameObject obj)
        {
            obj = null;
            if (Status == WindowStatus.Inited || Status == WindowStatus.Opened)
            {
                obj = gameObject;
                worldParam = null;

                try
                {
                    OnDelete();
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
            }

            Status = WindowStatus.Closed;
        }

        internal void SetFocus(bool tf)
        {
            OnFocus(tf);
        }
    }
}
