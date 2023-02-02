using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace PureMVCFramework
{
    public interface IResourceProvider
    {
        void Initialize();
        void ReleaseAsset(Object asset);
        void ReleaseAssets<TObject>(IList<TObject> assets) where TObject : Object;
        void LoadAssetAsync<TObject>(object key, System.Action<TObject, object> callback = null, object userdata = null) where TObject : Object;
        void LoadAssetsAsync<TObject>(object key, System.Action<IList<TObject>> callbackAll, System.Action<TObject> callbackEach) where TObject : Object;
        void LoadSceneAsync(object key, System.Action<SceneInstance, object> callback, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, object userdata = null);
        void UnloadSceneAsync(string sceneName, System.Action<string> callback);
    }

    public interface IResourcesUpdateProvider
    {
        void Initialize(string url, string plist, string md5, System.Action<bool, string> callback);
        void CheckForUpdates(object key, System.Action<object, long> callback);
        void StartDownload(object key, System.Action<object, float> percentComplete, System.Action<object> downloadComplete, System.Action<object, string> downloadFailed, int retry, int times);
    }

    public class ResourceManager : SingletonBehaviour<ResourceManager>
    {
        private readonly List<string> SpriteAtlasRequest = new List<string>();

        public bool IsSpriteAtlasRequesting => SpriteAtlasRequest.Count > 0;

        public IResourceProvider Provider { get; private set; }
        public IResourcesUpdateProvider Updater { get; private set; }

        public void Initialize(IResourceProvider p)
        {
            Provider = p;
            p.Initialize();
        }

        public void Initialize(IResourceProvider p, IResourcesUpdateProvider updater, string url, string plist, string md5 = null)
        {
            Provider = p;

            Updater = updater;
            Updater.Initialize(url, plist, md5, (tf, err) =>
            {
                if (tf)
                    Provider.Initialize();
            });
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            SpriteAtlasManager.atlasRequested += OnAtlasRequested;
            SpriteAtlasManager.atlasRegistered += OnAtlasRegistered;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        protected override void OnDelete()
        {
            SpriteAtlasManager.atlasRequested -= OnAtlasRequested;
            SpriteAtlasManager.atlasRegistered -= OnAtlasRegistered;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            base.OnDelete();
        }

        private void OnAtlasRequested(string atlasName, System.Action<SpriteAtlas> action)
        {
            Debug.LogWarningFormat("OnAtlasRequested: {0}", atlasName);
            SpriteAtlasRequest.Add(atlasName);
            LoadSpriteAtlas(atlasName, action, 10);
        }

        private void OnAtlasRegistered(SpriteAtlas sa)
        {
            SpriteAtlasRequest.Remove(sa.name);
            Debug.LogWarningFormat("OnAtlasRegistered: {0}", sa.name);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log(scene.name + " Loaded");
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log(scene.name + " Unloaded");
        }

        /// <summary>
        /// 加载图集的方法，holdingTime大于0时，图集会在指定时间后释放一次引用。
        /// 只在图集的IncludeInBuild设置为False时使用
        /// </summary>
        /// <param name="atlasName"></param>
        /// <param name="callback"></param>
        /// <param name="holdingTime"></param>
        public void LoadSpriteAtlas(string atlasName, System.Action<SpriteAtlas> callback, float holdingTime = 0)
        {
            LoadAssetAsync<SpriteAtlas>(atlasName, (atlas, _) =>
            {
                if (atlas)
                {
                    callback?.Invoke(atlas);

                    if (holdingTime > 0)
                    {
                        StartCoroutine(YieldReleaseAsset(atlas, holdingTime));
                    }
                }
            });
        }

        IEnumerator YieldReleaseAsset(Object asset, float delay)
        {
            yield return new WaitForSeconds(delay);

            ReleaseAsset(asset);
        }

        public void ReleaseAsset(Object asset)
        {
            Provider.ReleaseAsset(asset);
        }

        public void ReleaseAssets<TObject>(IList<TObject> assets) where TObject : Object
        {
            Provider.ReleaseAssets(assets);
        }

        public void LoadAssetAsync<TObject>(string key, System.Action<TObject, object> callback = null, object userdata = null) where TObject : Object
        {
            Provider.LoadAssetAsync(key, callback, userdata);
        }

        public void LoadAssetsAsync<TObject>(string key, System.Action<IList<TObject>> callbackAll, System.Action<TObject> callbackEach = null) where TObject : Object
        {
            Provider.LoadAssetsAsync(key, callbackAll, callbackEach);
        }

        public void LoadSceneAsync(string key, System.Action<SceneInstance, object> callback, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, object userdata = null)
        {
            Provider.LoadSceneAsync(key, callback, loadMode, activateOnLoad, userdata);
        }

        public void UnloadSceneAsync(Scene scene, System.Action<string> callback)
        {
            Provider.UnloadSceneAsync(scene.name, callback);
        }

        public void UnloadSceneAsync(string key, System.Action<string> callback)
        {
            Provider.UnloadSceneAsync(key, callback);
        }

        public void CheckForUpdates(object key, System.Action<object, long> callback)
        {
            if (Updater != null)
                Updater.CheckForUpdates(key, callback);
        }

        public void StartDownload(object key, System.Action<object, float> percentComplete, System.Action<object> downloadComplete, System.Action<object, string> downloadFailed, int retry, int times = 0)
        {
            if (Updater != null)
                Updater.StartDownload(key, percentComplete, downloadComplete, downloadFailed, retry, times);
        }
    }
}
