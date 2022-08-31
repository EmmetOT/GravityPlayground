using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GravityPlayground.GravityStuff
{
    public class GravityParticleEmitter : MonoBehaviour
    {
        [SerializeField]
        private bool m_isRunning = true;

        [SerializeField]
        private Sprite[] m_sprites = new Sprite[1];

        [SerializeField]
        [Min(0f)]
        private float m_particleEmissionFrequency = 0f;

        [SerializeField]
        [Min(0)]
        private int m_minimumParticleEmission = 0;

        [SerializeField]
        [Min(0)]
        private int m_maximumParticleEmission = 0;

        [SerializeField]
        [Min(0f)]
        private float m_innerRadius = 1f;

        [SerializeField]
        [Min(0f)]
        private float m_outerRadius = 1f;

        [SerializeField]
        private float m_startAngle = 0f;

        [SerializeField]
        private float m_angleSize = 360f;

        [SerializeField]
        [Min(0f)]
        private float m_minimumParticleSpeed = 1f;

        [SerializeField]
        [Min(0f)]
        private float m_maximumParticleSpeed = 1f;

        [SerializeField]
        private int m_sortKey;

        private enum RotationType { None, FaceMovementDirection, Randomized };

        [SerializeField]
        private RotationType m_rotationType = RotationType.None;
        
        [SerializeField]
        [Range(0f, 1f)]
        private float m_bounceChance;

        [SerializeField]
        [Min(0f)]
        private float m_startRadius;

        [SerializeField]
        [Min(0f)]
        private float m_endRadius;

        [SerializeField]
        [Min(0f)]
        private float m_radiusRandomOffset;

        [SerializeField]
        private float m_minimumParticleMass;

        [SerializeField]
        private float m_maximumParticleMass;

        [SerializeField]
        private float m_minimumParticleDuration;

        [SerializeField]
        private float m_maximumParticleDuration;

        [SerializeField]
        private Gradient m_startColourGradient;

        [SerializeField]
        private Gradient m_endColourGradient;
        
        [SerializeField]
        private bool m_flipStartDirection = false;

        private int m_spriteSequenceStartIndex = -1;

        private float m_timer = 0f;

        private readonly List<GravityParticleSystem.Particle> m_particles = new List<GravityParticleSystem.Particle>();
        
        private void Start()
        {
            m_timer = -Mathf.Max(0.1f, m_particleEmissionFrequency);
        }

        private void Update()
        {
            if (!m_isRunning)
                return;

            m_timer += Time.deltaTime;

            if (m_timer >= m_particleEmissionFrequency)
            {
                m_particles.Clear();
                m_timer = 0f;
                int particlesToEmit = Random.Range(m_minimumParticleEmission, m_maximumParticleEmission);

                for (int i = 0; i < particlesToEmit; i++)
                {
                    m_particles.Add(GenerateParticle());
                }

                Gravity.ParticleSystem.SpawnParticles(m_particles);
            }
        }

        private Vector3 SelectRandomSpawnPoint()
        {
            float angle = Random.Range(m_startAngle, m_startAngle + m_angleSize);
            float distance = Random.Range(m_innerRadius, m_outerRadius);

            return transform.TransformPoint(new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * distance);
        }

        private GravityParticleSystem.Particle GenerateParticle()
        {
            if (m_spriteSequenceStartIndex == -1)
                m_spriteSequenceStartIndex = Gravity.ParticleSystem.GetSpriteSequenceStartIndex(m_sprites);

            Vector3 pos = SelectRandomSpawnPoint();
            float colEval = Random.Range(0f, 1f);

            float rotation = 0f;

            if (m_rotationType == RotationType.FaceMovementDirection)
                rotation = 1f;
            else if (m_rotationType == RotationType.Randomized)
                rotation = Random.Range(0f, -360f);

            return new GravityParticleSystem.Particle()
            {
                Position = pos,
                Velocity = Random.Range(m_minimumParticleSpeed, m_maximumParticleSpeed) * (pos - transform.position).normalized * (m_flipStartDirection ? -1f : 1f),
                SpriteIndex = m_spriteSequenceStartIndex,
                SpriteCount = m_sprites.Length,
                SortKey = m_sortKey * 10000,
                RotateToFaceMovementDirection = rotation,
                BounceChance = m_bounceChance,
                Mass = Random.Range(m_minimumParticleMass, m_maximumParticleMass),
                StartColour = m_startColourGradient.Evaluate(colEval),
                EndColour = m_endColourGradient.Evaluate(colEval),
                StartRadius = m_startRadius,
                EndRadius = m_endRadius,
                RadiusRandomOffset = Random.Range(-m_radiusRandomOffset, m_radiusRandomOffset),
                Duration = Random.Range(m_minimumParticleDuration, m_maximumParticleDuration)
            };
        }

        public void SetRadius(float radius) => m_outerRadius = radius;
        
    }
}