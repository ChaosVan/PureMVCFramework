using PureMVCFramework.Advantages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Providers
{
    [System.Serializable]
    public class SfxProvider : IAudioProvider
    {
        public struct SfxParams
        {
            public float volume;
            public int maxSourceCount;
        }

        private readonly Queue<AudioSource> sfxQueue = new Queue<AudioSource>(); // ÒôÐ§¶ÓÁÐ

        [SerializeField]
        private string providerTag;
        [SerializeField]
        private int audioSourceMax;

        public SfxProvider(string tag, SfxParams param)
        {
            providerTag = tag;
            audioSourceMax = param.maxSourceCount;

            Volume = param.volume;
        }

        [SerializeField]
        public float Volume { get; set; }

        public void Pause()
        {
            
        }

        public void Play(AudioClip clip, object userdata)
        {
            var audioSource = FetchPlayer(clip.name);
            float volumeScale = 1;

            AutoReleaseManager.Instance.RegistAutoRelease(audioSource, clip);
            if (userdata is AudioParams param)
            {
                audioSource.transform.position = param.position;
                volumeScale = param.volumeScale > 0 ? param.volumeScale : 1;
            }
            else
            {
                audioSource.transform.position = Vector3.zero;
            }

            audioSource.PlayOneShot(clip, volumeScale);

        }

        public void Release()
        {
            while (sfxQueue.Count > 0)
            {
                var source = sfxQueue.Dequeue();
                if (source != null)
                    Object.Destroy(source);
            }
        }

        public void Resume()
        {
            
        }

        public void Stop()
        {
            
        }

        private AudioSource FetchPlayer(string clipName = "")
        {
            var count = sfxQueue.Count;
            if (count > 0)
            {
                var head = sfxQueue.Peek();

                if (head == null)
                {
                    sfxQueue.Dequeue();
                    return FetchPlayer(clipName);
                }
                else
                {
                    if (count >= audioSourceMax || !head.isPlaying)
                    {
                        head = sfxQueue.Dequeue();
                        head.volume = Volume;
                        sfxQueue.Enqueue(head);
                        head.name = string.IsNullOrEmpty(clipName) ? providerTag : providerTag + ":" + clipName;
                        return head;
                    }
                }
            }

            var source = new GameObject(string.IsNullOrEmpty(clipName) ? providerTag : providerTag + ":" + clipName).AddComponent<AudioSource>();
            source.transform.SetParent(AudioManager.Instance.transform, false);
            source.playOnAwake = false;

            source.volume = Volume;
            sfxQueue.Enqueue(source);

            return source;
        }
    }
}
