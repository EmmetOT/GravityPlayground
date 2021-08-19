using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityPlayground
{
    /// <summary>
    /// Use this instead of using 'transform.hasChanged' directly if you have multiple components on the same gameobject which need to 
    /// do something on transform change.
    /// </summary>
    [ExecuteInEditMode]
    public class TransformChangeDetector : MonoBehaviour
    {
        public event System.Action OnTransformChange;

        [SerializeField]
        private bool m_executeInEditMode = true;

        public void LateUpdate()
        {
            if (!m_executeInEditMode && !Application.isPlaying)
                return;

            if (transform.hasChanged)
            {
                OnTransformChange?.Invoke();
                transform.hasChanged = false;
            }
        }
    }
}