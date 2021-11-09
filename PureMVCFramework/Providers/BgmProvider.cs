using PureMVCFramework.Advantages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PureMVCFramework.Providers
{
    [System.Serializable]
    public class BgmProvider : Updatable, IAudioProvider
    {
        public struct BgmParams
        {
            public float volume;
            public bool playOnAwake;
            public Transform parent;
        }


        public enum State
        {
            Stop,
            Pause,
            Playing,
        }

        [SerializeField]
        private string providerTag;
        [SerializeField]
        private AudioSource audioSource;
        [SerializeField]
        private State state;
        [SerializeField]
        private Transform trackTransform;

        private float smoothDampVelocity;


        public BgmProvider(string tag, BgmParams param)
        {
            providerTag = tag;
            audioSource = new GameObject(tag).AddComponent<AudioSource>();
            audioSource.playOnAwake = param.playOnAwake;
            Volume = param.volume;

            smoothDampVelocity = 0;

            if (param.parent != null)
                audioSource.transform.SetParent(param.parent);
            else
                audioSource.transform.SetParent(AudioManager.Instance.transform, false);

            EnableUpdate(true);
        }

        [SerializeField]
        public bool IsPlaying => audioSource.isPlaying;

        [SerializeField]
        public float Volume { get; set; }

        public void Pause()
        {
            state = State.Pause;
        }

        public void Play(AudioClip clip, object userdata)
        {
            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
                audioSource.Stop();
                audioSource.volume = 0;
                AutoReleaseManager.Instance.RegistAutoRelease(audioSource, clip);
            }

            if (userdata is AudioParams param)
            {
                audioSource.transform.position = param.position;
                trackTransform = null;
            }
            else if (userdata is Transform tran)
            {
                trackTransform = tran;
                audioSource.transform.position = trackTransform.position;
            }
            else
            {
                audioSource.transform.position = Vector3.zero;
                trackTransform = null;
            }

            state = State.Playing;
            smoothDampVelocity = 0;
        }

        public void Release()
        {
            EnableUpdate(false);

            if (audioSource != null)
                Object.Destroy(audioSource);
        }

        public void Resume()
        {
            audioSource.UnPause();

            if (Volume > 0)
            {
                state = State.Playing;
                audioSource.volume = 0;
            }
            else
                state = State.Stop;

            smoothDampVelocity = 0;
        }

        public void Stop()
        {
            state = State.Stop;
            smoothDampVelocity = 0;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (audioSource == null)
            {
                AudioManager.Instance.RemoveProvider(providerTag);
                return;
            }

            switch (state)
            {
                case State.Stop:
                    if (audioSource.volume > 0)
                        audioSource.volume = Mathf.SmoothDamp(audioSource.volume, 0, ref smoothDampVelocity, 1);
                    else
                        audioSource.Stop();
                    break;
                case State.Playing:
                    if (Volume > 0)
                    {
                        if (audioSource.isPlaying)
                        {
                            if (audioSource.volume != Volume)
                                audioSource.volume = Mathf.SmoothDamp(audioSource.volume, Volume, ref smoothDampVelocity, 1);
                        }
                        else
                        {
                            if (audioSource.clip != null)
                                audioSource.Play();
                        }
                    }
                    else
                    {
                        state = State.Stop;
                    }

                    if (trackTransform != null)
                        audioSource.transform.position = trackTransform.position;
                    break;
                case State.Pause:
                    if (audioSource.volume > 0)
                        audioSource.volume = Mathf.SmoothDamp(audioSource.volume, 0, ref smoothDampVelocity, 1);
                    else
                        audioSource.Pause();
                    break;
                default:
                    Debug.LogErrorFormat("Error Audio State {0}", state);
                    break;
            }
        }
    }
}
