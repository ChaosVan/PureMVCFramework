using PureMVCFramework.Advantages;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace PureMVCFramework.Extensions
{
    public static class TextureExtensions
    {
        public static void LoadTexture(this RawImage image, string texturePath, Action<RawImage, object> onSuccess = null, Action<RawImage, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(RawImage holder, Texture2D tex, object data)
            {
                holder.texture = tex;
                onSuccess?.Invoke(holder, data);
            }

            image.InternalLoadTexture(texturePath, OnSuccess, onFailure, userdata);
        }

        public static void LoadTextureFromUrl(this RawImage image, string url, Action<RawImage, object> onSuccess = null, Action<RawImage, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(RawImage holder, Texture2D tex, object data)
            {
                holder.texture = tex;
                onSuccess?.Invoke(holder, data);
            }

            image.InternalLoadTextureFromUrl(url, OnSuccess, onFailure, userdata);
        }

        public static void LoadTexture(this MeshRenderer mesh, string texturePath, Action<MeshRenderer, object> onSuccess = null, Action<MeshRenderer, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(MeshRenderer holder, Texture2D tex, object data)
            {
                holder.material.mainTexture = tex;
                onSuccess?.Invoke(holder, data);
            }

            mesh.InternalLoadTexture(texturePath, OnSuccess, onFailure, userdata);
        }

        public static void LoadTextureFromUrl(this MeshRenderer mesh, string url, Action<MeshRenderer, object> onSuccess = null, Action<MeshRenderer, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(MeshRenderer holder, Texture2D tex, object data)
            {
                holder.material.mainTexture = tex;
                onSuccess?.Invoke(holder, data);
            }

            mesh.InternalLoadTextureFromUrl(url, OnSuccess, onFailure, userdata);
        }

        private static void InternalLoadTextureFromUrl<T>(this T holder, string url, Action<T, Texture2D, object> onSuccess, Action<T, object> onFailure, object userdata) where T : UnityEngine.Object
        {
            WebRequestManager.Instance.LoadTexture(url, (req, data) =>
            {
                if (req.isDone)
                {
#if UNITY_2020_1_OR_NEWER
                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        onFailure?.Invoke(holder, data);
                    }
#else
                    if (req.isHttpError || req.isNetworkError)
                    {
                        onFailure?.Invoke(holder, data);
                    }
#endif
                    else
                    {
                        Texture2D texture = DownloadHandlerTexture.GetContent(req);
                        if (texture != null)
                        {
                            onSuccess?.Invoke(holder, texture, data);
                        }
                        else
                        {
                            onFailure?.Invoke(holder, data);
                        }
                    }
                }
            }, 0, 10, userdata);
        }

        private static void InternalLoadTexture<T>(this T holder, string texturePath, Action<T, Texture2D, object> onSuccess, Action<T, object> onFailure, object userdata) where T : UnityEngine.Object
        {
            ResourceManager.Instance.LoadAssetAsync<Texture2D>(texturePath, (texture, data) =>
            {
                if (holder == null)
                {
                    ResourceManager.Instance.ReleaseAsset(texture);
                    return;
                }

                if (texture != null)
                {
                    AutoReleaseManager.Instance.RegistAutoRelease(holder, texture);
                    onSuccess?.Invoke(holder, texture, data);
                }
                else
                {
                    onFailure?.Invoke(holder, data);
                }
            }, userdata);
        }
    }
}
