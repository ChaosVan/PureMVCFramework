using UnityEngine;
using System.Collections;

namespace PureMVCFramework
{
    [DisallowMultipleComponent]
    public class AutoRecycle : MonoBehaviour
    {
        public bool autoDestroy;
        public float delay = 1;

        private Coroutine m_Coroutine;
        private bool m_bStarted;

        private void Start()
        {
            m_bStarted = true;
            StartCoroutine();
        }

        // Use this for initialization
        protected virtual IEnumerator DelayAction()
        {
            yield return new WaitForSecondsRealtime(delay);
            gameObject.Recycle();
        }

        private void StopCoroutine()
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
                m_Coroutine = null;
            }
        }

        private void StartCoroutine()
        {
            StopCoroutine();
            m_Coroutine = StartCoroutine(DelayAction());
        }

        private void OnEnable()
        {
            if (m_bStarted)
                StartCoroutine();
        }

        private void OnDisable()
        {
            StopCoroutine();
            if (autoDestroy)
                Destroy(this);
        }
    }
}

