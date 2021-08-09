using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityPlayground.GravityStuff
{
    [System.Serializable]
    public struct GravityParticleConfig
    {
        [SerializeField]
        private Sprite[] m_sprites;
        
        [SerializeField]
        [Min(0f)]
        private float m_minimumParticleSpeed;

        [SerializeField]
        [Min(0f)]
        private float m_maximumParticleSpeed;

        [SerializeField]
        private int m_sortKey;

        private enum RotationType { None, FaceMovementDirection, Randomized };

        [SerializeField]
        private RotationType m_rotationType;

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
        
        public GravityParticleSystem.Particle GenerateParticle(Vector2 pos, Vector2 direction, ref int spriteSequenceStartIndex)
        {
            if (spriteSequenceStartIndex < 0)
                spriteSequenceStartIndex = Gravity.ParticleSystem.GetSpriteSequenceStartIndex(m_sprites);

            float colEval = Random.Range(0f, 1f);

            float rotation = 0f;

            if (m_rotationType == RotationType.FaceMovementDirection)
                rotation = 1f;
            else if (m_rotationType == RotationType.Randomized)
                rotation = Random.Range(0f, -360f);

            return new GravityParticleSystem.Particle()
            {
                Position = pos,
                Velocity = Random.Range(m_minimumParticleSpeed, m_maximumParticleSpeed) * direction,
                SpriteIndex = spriteSequenceStartIndex,
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
    }
}