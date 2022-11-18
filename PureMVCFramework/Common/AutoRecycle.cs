using System.Collections;
using UnityEngine;

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
            StartCoroutine();
        }

        private void Update()
        {
            if (m_bStarted)
                return;

            StartCoroutine();
        }

        // Use this for initialization
        protected virtual IEnumerator DelayAction()
        {
            yield return new WaitForSeconds(delay);
            gameObject.Recycle();
        }

        private void StartCoroutine()
        {
            StopCoroutine();

            m_Coroutine = StartCoroutine(DelayAction());
            m_bStarted = true;
        }

        private void StopCoroutine()
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
                m_Coroutine = null;
            }

            m_bStarted = false;
        }

        private void OnDisable()
        {
            StopCoroutine();
            if (autoDestroy)
                Destroy(this);
        }
    }
}

