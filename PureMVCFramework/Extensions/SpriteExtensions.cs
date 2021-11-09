using PureMVCFramework.Advantages;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace PureMVCFramework.Extensions
{
    public static class SpriteExtensions
    {
        public static void LoadSprite(this Image image, string spritePath, Action<Image, object> onSuccess = null, Action<Image, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(Image holder, Sprite sprite, object data)
            {
                holder.sprite = sprite;
                onSuccess?.Invoke(holder, data);
            }

            image.InternalLoadSprite(spritePath, OnSuccess, onFailure, userdata);
        }

        public static void LoadSpriteFromAtlas(this Image image, string atlasName, string spriteName, Action<Image, object> onSuccess = null, Action<Image, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(Image holder, Sprite sprite, object data)
            {
                holder.sprite = sprite;
                onSuccess?.Invoke(holder, data);
            }

            image.InternalLoadSpriteFromAtlas(atlasName, spriteName, OnSuccess, onFailure, userdata);
        }

        public static void LoadSprite(this SpriteRenderer renderer, string spritePath, Action<SpriteRenderer, object> onSuccess = null, Action<SpriteRenderer, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(SpriteRenderer holder, Sprite sprite, object data)
            {
                holder.sprite = sprite;
                onSuccess?.Invoke(holder, data);
            }

            renderer.InternalLoadSprite(spritePath, OnSuccess, onFailure, userdata);
        }

        public static void LoadSpriteFromAtlas(this SpriteRenderer renderer, string atlasName, string spriteName, Action<SpriteRenderer, object> onSuccess = null, Action<SpriteRenderer, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(SpriteRenderer holder, Sprite sprite, object data)
            {
                holder.sprite = sprite;
                onSuccess?.Invoke(holder, data);
            }

            renderer.InternalLoadSpriteFromAtlas(atlasName, spriteName, OnSuccess, onFailure, userdata);
        }

        public static void LoadSprite(this RawImage image, string spritePath, Action<RawImage, object> onSuccess = null, Action<RawImage, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(RawImage holder, Sprite sprite, object data)
            {
                holder.texture = sprite.texture;
                holder.uvRect = new Rect(0, 0, 1, 1);
                onSuccess?.Invoke(holder, data);
            }

            image.InternalLoadSprite(spritePath, OnSuccess, onFailure, userdata);
        }

        public static void LoadSprite(this MeshRenderer renderer, string spritePath, Action<MeshRenderer, object> onSuccess = null, Action<MeshRenderer, object> onFailure = null, object userdata = null)
        {
            void OnSuccess(MeshRenderer holder, Sprite sprite, object data)
            {
                holder.material.mainTexture = sprite.texture;
                onSuccess?.Invoke(holder, data);
            }

            renderer.InternalLoadSprite(spritePath, OnSuccess, onFailure, userdata);
        }

        private static void InternalLoadSprite<T>(this T holder, string spritePath, Action<T, Sprite, object> onSuccess, Action<T, object> onFailure, object userdata) where T : UnityEngine.Object
        {
            ResourceManager.Instance.LoadAssetAsync<Sprite>(spritePath, (sprite, data) =>
            {
                if (holder == null)
                {
                    ResourceManager.Instance.ReleaseAsset(sprite);
                    return;
                }

                if (sprite != null)
                {
                    AutoReleaseManager.Instance.RegistAutoRelease(holder, sprite);
                    onSuccess?.Invoke(holder, sprite, data);
                }
                else
                {
                    onFailure?.Invoke(holder, data);
                }
            }, userdata);
        }

        private static void InternalLoadSpriteFromAtlas<T>(this T holder, string atlasName, string spriteName, Action<T, Sprite, object> onSuccess, Action<T, object> onFailure, object userdata) where T : UnityEngine.Object
        {
            ResourceManager.Instance.LoadAssetAsync<SpriteAtlas>(atlasName, (atlas, data) =>
            {
                if (holder == null)
                {
                    ResourceManager.Instance.ReleaseAsset(atlas);
                    return;
                }

                if (atlas != null)
                {
                    Sprite sprite = atlas.GetSprite(spriteName);
                    if (sprite != null)
                    {
                        AutoReleaseManager.Instance.RegistAutoRelease(holder, atlas);
                        onSuccess?.Invoke(holder, sprite, data);
                    }
                    else
                    {
                        ResourceManager.Instance.ReleaseAsset(atlas);
                        onFailure?.Invoke(holder, data);
                    }
                }
                else
                {
                    onFailure?.Invoke(holder, data);
                }

            }, userdata);
        }
    }
}
