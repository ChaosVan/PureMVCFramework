using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace PureMVCFramework.Providers
{
    public class AddressableProvider : IResourceProvider
    {
        private readonly Dictionary<string, SceneInstance> m_LoadedScene = new Dictionary<string, SceneInstance>();

        public System.Action InitializedCallback;

        public AddressableProvider()
        {
            m_LoadedScene.Clear();
        }

        public void Initialize()
        {
            Addressables.InitializeAsync().Completed += InitializeAsync_Completed;
        }

        private void InitializeAsync_Completed(AsyncOperationHandle<IResourceLocator> handle)
        {
            ReleaseHandle(handle);
            InitializedCallback?.Invoke();
        }

        public void ReleaseHandle(AsyncOperationHandle handle)
        {
            Addressables.Release(handle);
        }

        public void ReleaseHandle<TObject>(AsyncOperationHandle<TObject> handle)
        {
            Addressables.Release(handle);
        }

        public void LoadAssetAsync<TObject>(IResourceLocation location, System.Action<TObject, object> callback, object userdata = null) where TObject : Object
        {
            Addressables.LoadAssetAsync<TObject>(location).Completed += handle =>
            {
                if (handle.IsDone)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        callback?.Invoke(handle.Result, userdata);
                    }
                    else
                    {
                        callback?.Invoke(null, userdata);
                    }
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="keys"></param>
        /// <param name="mergeMode"></param>
        /// <param name="callbackAll"></param>
        /// <param name="callbackEach"></param>
        /// <param name="releaseDependenciesOnFailure">
        /// Whether the Result is populated with successful objects or null is dependant on the use of the bool releaseDependenciesOnFailure parameter provided. 
        /// If you pass true into the parameter, the Result property is populated with null if any of the requested objects fail to load. 
        /// Passing false into this parameter populates the Result with any objects that were successfully loaded, even if some failed. 
        /// If this parameter is not specified then the default value true is used.</param>
        public void LoadAssetsAsync<TObject>(IEnumerable keys, Addressables.MergeMode mergeMode, System.Action<IList<TObject>, AsyncOperationHandle> callbackAll, System.Action<TObject> callbackEach = null, bool releaseDependenciesOnFailure = true) where TObject : Object
        {
            Addressables.LoadAssetsAsync(keys, callbackEach, mergeMode, releaseDependenciesOnFailure).Completed += handle =>
            {
                if (handle.IsDone)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        callbackAll?.Invoke(handle.Result, handle);
                    }
                    else
                    {
                        callbackAll?.Invoke(null, handle);
                    }
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="locations"></param>
        /// <param name="callbackAll"></param>
        /// <param name="callbackEach"></param>
        /// <param name="releaseDependenciesOnFailure">
        /// Whether the Result is populated with successful objects or null is dependant on the use of the bool releaseDependenciesOnFailure parameter provided. 
        /// If you pass true into the parameter, the Result property is populated with null if any of the requested objects fail to load. 
        /// Passing false into this parameter populates the Result with any objects that were successfully loaded, even if some failed. 
        /// If this parameter is not specified then the default value true is used.</param>
        public void LoadAssetsAsync<TObject>(IList<IResourceLocation> locations, System.Action<IList<TObject>, AsyncOperationHandle> callbackAll, System.Action<TObject> callbackEach, bool releaseDependenciesOnFailure) where TObject : Object
        {
            Addressables.LoadAssetsAsync(locations, callbackEach, releaseDependenciesOnFailure).Completed += handle =>
            {
                if (handle.IsDone)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        callbackAll?.Invoke(handle.Result, handle);
                    }
                    else
                    {
                        callbackAll?.Invoke(null, handle);
                    }
                }
            };
        }

        public void LoadResourceLocationsAsync(IEnumerable keys, Addressables.MergeMode mode, System.Action<IList<IResourceLocation>, AsyncOperationHandle> callback, System.Type type = null)
        {
            Addressables.LoadResourceLocationsAsync(keys, mode, type).Completed += handle =>
            {
                if (handle.IsDone)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        callback?.Invoke(handle.Result, handle);
                    }
                    else
                    {
                        callback?.Invoke(null, handle);
                    }
                }
            };
        }

        public void LoadResourceLocationsAsync(object key, System.Action<IList<IResourceLocation>, AsyncOperationHandle> callback, System.Type type = null)
        {
            Addressables.LoadResourceLocationsAsync(key, type).Completed += handle =>
            {
                if (handle.IsDone)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        callback?.Invoke(handle.Result, handle);
                    }
                    else
                    {
                        callback?.Invoke(null, handle);
                    }
                }
            };
        }


        #region IResourceProvider IMPL

        public void ReleaseAsset(Object asset)
        {
            Addressables.Release(asset);
        }

        public void ReleaseAssets<TObject>(IList<TObject> assets) where TObject : Object
        {
            Addressables.Release(assets);
        }

        public void LoadAssetAsync<TObject>(object key, System.Action<TObject, object> callback, object userdata = null) where TObject : Object
        {
            Addressables.LoadAssetAsync<TObject>(key).Completed += handle =>
            {
                if (handle.IsDone)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        callback?.Invoke(handle.Result, userdata);
                    }
                    else
                    {
                        callback?.Invoke(null, userdata);
                    }
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="key"></param>
        /// <param name="callbackAll"></param>
        /// <param name="callbackEach"></param>
        /// <returns></returns>
        public void LoadAssetsAsync<TObject>(object key, System.Action<IList<TObject>> callbackAll, System.Action<TObject> callbackEach) where TObject : Object
        {
            Addressables.LoadAssetsAsync(key, callbackEach, true).Completed += handle =>
            {
                if (handle.IsDone)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        callbackAll?.Invoke(handle.Result);
                    }
                    else
                    {
                        callbackAll?.Invoke(null);
                    }
                }
            };
        }

        public void LoadSceneAsync(object key, System.Action<Scene, object> callback, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, object userdata = null)
        {
            Addressables.LoadSceneAsync(key, loadMode, activateOnLoad).Completed += handle =>
            {
                if (handle.IsDone)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        if (!m_LoadedScene.TryGetValue(handle.Result.Scene.name, out var inst))
                            m_LoadedScene.Add(handle.Result.Scene.name, handle.Result);

                        if (activateOnLoad)
                            SceneManager.SetActiveScene(handle.Result.Scene);

                        callback?.Invoke(handle.Result.Scene, userdata);
                    }
                    else
                    {
                        Addressables.Release(handle);
                        callback?.Invoke(default, userdata);
                    }
                }
            };
        }

        public void UnloadSceneAsync(Scene scene, System.Action<string> callback)
        {
            if (m_LoadedScene.TryGetValue(scene.name, out var inst))
            {
                UnloadSceneAsync(inst, _ => callback?.Invoke(scene.name));
                m_LoadedScene.Remove(scene.name);
            }
        }

        public void UnloadSceneAsync(string key, System.Action<string> callback)
        {
            if (m_LoadedScene.TryGetValue(key, out var inst))
            {
                UnloadSceneAsync(inst, _ => callback?.Invoke(key));
                m_LoadedScene.Remove(key);
            }
        }
        #endregion

        public void UnloadSceneAsync(SceneInstance scene, System.Action<bool> callback)
        {
            Addressables.UnloadSceneAsync(scene).Completed += handle =>
            {
                if (handle.IsDone)
                    callback?.Invoke(handle.Status == AsyncOperationStatus.Succeeded);
            };
        }
    }
}
