using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityPlayground.GravityStuff
{
    [RequireComponent(typeof(GravityParticleEmitter))]
    public class GravityParticleEmitterRadiusSetter : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private GravityParticleEmitter m_emitter;
        
        [SerializeField]
        private float m_minRadius = 0f;

        [SerializeField]
        private float m_maxRadius = 0f;

        [SerializeField]
        [Min(0.01f)]
        private float m_period = 0f;

        [SerializeField]
        private bool m_isRunning = true;

        private float m_timeElapsed = 0f;
        
        private void Reset()
        {
            m_emitter = GetComponent<GravityParticleEmitter>();
            m_timeElapsed = 0f;
        }

        private void OnValidate()
        {
            m_emitter = GetComponent<GravityParticleEmitter>();

            m_minRadius = Mathf.Clamp(m_minRadius, 0f, m_maxRadius);
            m_maxRadius = Mathf.Max(m_maxRadius, m_minRadius);
        }

        private void Update()
        {
            if (!m_isRunning)
                return;

            m_timeElapsed += Time.smoothDeltaTime;

            float t = (Mathf.Sin(m_timeElapsed * Mathf.PI * 2f / m_period) + 1f) * 0.5f;
            m_emitter.SetRadius(Mathf.Lerp(m_minRadius, m_maxRadius, t));
        }
    }
}
