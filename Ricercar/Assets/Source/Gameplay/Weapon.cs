using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AALineDrawer;
using GravityPlayground.GravityStuff;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GravityPlayground.Character
{
    public class Weapon
    {
        public WeaponData Data { get; }

        private float m_fireRateTimer = 0f;
        private int m_particleSequenceStartIndex = -1;

        public Weapon(WeaponData weaponData)
        {
            Data = weaponData;
        }

        public void Update(float deltaTime)
        {
            m_fireRateTimer += Time.deltaTime;
        }

        public bool CanFire()
        {
            return m_fireRateTimer >= 0f;
        }

        private const float PARTICLE_SPAWN_OFFSET = 1f;

        public Projectile Fire(Vector2 position, Vector2 direction, out Vector2 velocity)
        {
            m_fireRateTimer = -Data.FireRate;

            velocity = Data.GenerateRandomFiringSpeed() * direction;

            Debug.Log("---> Spawning PROJECTILE with velocity " + velocity);

            if (Data.SpawnParticles)
            {
                int count = Data.GenerateRandomParticleEmissionCount();

                for (int i = 0; i < count; i++)
                {
                    Vector2 rotatedDir = Quaternion.Euler(0f, 0f, Data.GenerateRandomParticleEmissionAngle()) * direction;
                    Vector2 offsetPosition = position + Random.Range(0f, PARTICLE_SPAWN_OFFSET) * rotatedDir;

                    GravityParticleSystem.Particle particle = Data.ParticleConfig.GenerateParticle(offsetPosition, rotatedDir, ref m_particleSequenceStartIndex);
                    Gravity.ParticleSystem.SpawnParticle(particle);
                }
            }

            return ProjectileController.SpawnProjectile(Data.Projectile, position, velocity);
        }
    }
}
