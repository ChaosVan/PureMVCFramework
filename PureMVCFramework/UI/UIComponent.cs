/****************************************************
    FileName:   UIComponent.cs
    Author:     ChaosVan
    CreateTime: 2020/08/13 18:12:25
    Description:
*****************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.UI
{
    public class UIComponent : Updatable
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<UIComponent> components = new List<UIComponent>();
        public UIComponent holder;

        public GameObject gameObject;
        public Transform transform;
        public RectTransform rectTransform;
        public object userdata;

        public void Destroy()
        {
            if (holder != null)
                holder.components.Remove(this);

            OnDelete();
        }

        private void AddComponent(UIComponent component, GameObject gameObject, object userdata = null)
        {
            components.Add(component);
            component.holder = this;
            component.OnCreate(gameObject, userdata);
        }

        public T AddComponent<T>(GameObject gameObject, object userdata = null) where T : UIComponent, new()
        {
            T component = ReferencePool.SpawnInstance<T>();
            AddComponent(component, gameObject, userdata);
            return component;
        }

        public UIComponent AddComponent(Type type, GameObject gameObject, object userdata = null)
        {
            UIComponent component = (UIComponent)ReferencePool.SpawnInstance(type);
            AddComponent(component, gameObject, userdata);
            return component;
        }

        public UIComponent AddComponent(string type, GameObject gameObject, object userdata = null)
        {
            UIComponent component = (UIComponent)ReferencePool.SpawnInstance(type);
            AddComponent(component, gameObject, userdata);
            return component;
        }

        protected virtual void OnCreate(GameObject gameObject, object userdata)
        {
            this.gameObject = gameObject;
            this.userdata = userdata;

            transform = gameObject.transform;
            rectTransform = transform.GetComponent<RectTransform>();
        }

        protected virtual void OnDelete()
        {
            gameObject = null;
            userdata = null;
            transform = null;
            rectTransform = null;

            foreach (var component in components)
            {
                component.OnDelete();
            }

            components.Clear();
            holder = null;

            EnableUpdate(false);

            ReferencePool.RecycleInstance(this);
        }
    }
}
