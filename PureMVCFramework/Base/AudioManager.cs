using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework
{
    public interface IAudioProvider
    {
        bool IsPlaying { get; }
        float Volume { get; set; }
        void Play(AudioClip clip, object userdata);
        void Pause();
        void Resume();
        void Stop();
        void Release();
    }

    public struct AudioParams
    {
        public Vector3 position;
        public float volumeScale;
    }

    public class AudioManager : SingletonBehaviour<AudioManager>
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.OneLine)]
#endif
        private readonly Dictionary<string, IAudioProvider> audioProviders = new Dictionary<string, IAudioProvider>();

        public T CreateProvider<T>(string tag, object userdata) where T : IAudioProvider
        {
            if (!audioProviders.TryGetValue(tag, out var provider))
            {
                provider = (IAudioProvider)System.Activator.CreateInstance(typeof(T), tag, userdata);
                audioProviders.Add(tag, provider);
            }

            return (T)provider;
        }

        public void RemoveProvider(string tag)
        {
            if (audioProviders.TryGetValue(tag, out var provider))
            {
                provider.Release();
                audioProviders.Remove(tag);
            }
        }

        public void SetVolume(string tag, float volume)
        {
            if (audioProviders.TryGetValue(tag, out var provider))
            {
                provider.Volume = volume;
            }
        }

        void OnApplicationPause(bool isPause)
        {
            if (isPause)
                Pause();
            else
                Resume();
        }

        public void Pause(string tag = "")
        {
            if (string.IsNullOrEmpty(tag))
            {
                foreach (var p in audioProviders.Values)
                {
                    p.Pause();
                }
            }
            else
            {
                if (audioProviders.TryGetValue(tag, out var p))
                {
                    p.Pause();
                }
            }
        }

        public void Resume(string tag = "")
        {
            if (string.IsNullOrEmpty(tag))
            {
                foreach (var p in audioProviders.Values)
                {
                    p.Resume();
                }
            }
            else
            {
                if (audioProviders.TryGetValue(tag, out var p))
                {
                    p.Resume();
                }
            }
        }

        public void Stop(string tag = "")
        {
            if (string.IsNullOrEmpty(tag))
            {
                foreach (var p in audioProviders.Values)
                {
                    p.Stop();
                }
            }
            else
            {
                if (audioProviders.TryGetValue(tag, out var p))
                {
                    p.Stop();
                }
            }
        }

        public void Play(string tag, string asset, object userdata = null)
        {
            void OnLoaded(AudioClip clip, object data)
            {
                Play(tag, clip, data);
            }

            ResourceManager.Instance.LoadAssetAsync<AudioClip>(asset, OnLoaded, userdata);
        }

        public void Play(string tag, AudioClip clip, object userdata = null)
        {
            if (clip != null && audioProviders.TryGetValue(tag, out var provider))
            {
                provider.Play(clip, userdata);
            }
        }
    }

}
