using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Extensions
{
    public static class UnityExtensions
    {
        // 说明：lua侧判Object为空全部使用这个函数
        public static bool IsNull(this UnityEngine.Object o)
        {
            return o == null;
        }

        public static void SetLayer(this GameObject go, int layer)
        {
            foreach (Transform tran in go.transform)
            {
                SetLayer(tran.gameObject, layer);
            }

            go.layer = layer;
        }

        public static void Active(this GameObject go)
        {
            go.SetActive(true);
        }

        public static void Deactive(this GameObject go)
        {
            go.SetActive(false);
        }

        public static T GetOrAddComponent<T>(this GameObject go, bool set_enable = false) where T : Component
        {
            T result = go.GetComponent<T>();
            if (result == null)
                result = go.AddComponent<T>();

            var bcomp = result as Behaviour;
            if (set_enable && bcomp != null)
                bcomp.enabled = set_enable;

            return result;
        }

        public static Component GetOrAddComponent(this GameObject go, Type type, bool set_enable = false)
        {
            Component result = go.GetComponent(type);
            if (result == null)
                result = go.AddComponent(type);

            var bcomp = result as Behaviour;
            if (set_enable && bcomp != null)
                bcomp.enabled = set_enable;

            return result;
        }

        public static T GetOrAddComponent<T>(this Component comp, bool set_enable = false) where T : Component
        {
            T result = comp.GetComponent<T>();
            if (result == null)
                result = comp.gameObject.AddComponent<T>();

            var bcomp = result as Behaviour;
            if (set_enable && bcomp != null)
                bcomp.enabled = set_enable;

            return result;
        }

        public static Component GetOrAddComponent(this Component comp, Type type, bool set_enable = false)
        {
            Component result = comp.GetComponent(type);
            if (result == null)
                result = comp.gameObject.AddComponent(type);

            var bcomp = result as Behaviour;
            if (set_enable && bcomp != null)
                bcomp.enabled = set_enable;

            return result;
        }
    }

    public static class TransformExtentions
    {
        public static Transform GetOrAddTransform(this Transform parent, string childName, Vector3 position, Vector3 roll)
        {
            Transform t = GetOrAddTransform(parent, childName);
            if (t != null)
            {
                t.position = position;
                t.rotation = Quaternion.Euler(roll);
            }

            return t;
        }

        public static Transform GetOrAddTransform(this Transform parent, string childName, Vector3 position, Quaternion rotation)
        {
            Transform t = GetOrAddTransform(parent, childName);
            if (t != null)
            {
                t.position = position;
                t.rotation = rotation;
            }

            return t;
        }

        public static Transform GetOrAddTransform(this Transform parent, string childName)
        {
            Transform t = parent.Find(childName);
            if (t == null)
            {
                if (childName.Contains("/"))
                {
                    int index = childName.IndexOf('/');
                    parent = parent.GetOrAddTransform(childName.Substring(0, index++));
                    childName = childName.Substring(index);
                    t = parent.GetOrAddTransform(childName);
                }
                else
                {
                    t = new GameObject(childName).transform;
                    t.SetParent(parent, false);
                }
            }


            return t;
        }
    }
}
