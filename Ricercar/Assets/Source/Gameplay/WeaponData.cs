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
    [CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObjects/WeaponData", order = 1)]
    public class WeaponData : ScriptableObject
    {
        [field: SerializeField]
        public bool SpawnProjectiles { get; private set; } = false;

        [field: SerializeField]
        [field: Min(0f)]
        public float FireRate { get; private set; } = 1f;

        [field: SerializeField]
        [field: Min(0f)]
        public float MinimumFiringSpeed { get; private set; } = 1f;

        [field: SerializeField]
        [field: Min(0f)]
        public float MaximumFiringSpeed { get; private set; } = 1f;
        
        [field: SerializeField]
        [field: Min(0f)]
        public float KnockbackForceScalar { get; private set; } = 0f;
        
        public float AverageSpeed => (MinimumFiringSpeed + MaximumFiringSpeed) * 0.5f;

        public float GenerateRandomFiringSpeed() => Random.Range(MinimumFiringSpeed, MaximumFiringSpeed);

        [field: SerializeField]
        public bool HoldToFire { get; private set; } = false;

        public float AverageProjectileMass
        {
            get
            {
                if (Projectile)
                    return Projectile.AverageMass;

                if (SpawnParticles)
                    return ParticleConfig.AverageMass;

                return 0f;
            }
        }

        [field: SerializeField]
        public ProjectileData Projectile { get; private set; } = null;

        [field: SerializeField]
        public bool SpawnParticles { get; private set; } = false;
        
        [field: SerializeField]
        [field: Min(0)]
        public int MinimumParticleEmission { get; private set; } = 0;

        [field: SerializeField]
        [field: Min(0)]
        public int MaximumParticleEmission { get; private set; } = 0;

        public int GenerateRandomParticleEmissionCount() => Random.Range(MinimumParticleEmission, MaximumParticleEmission);

        [field: SerializeField]
        [field: Min(0f)]
        public int ParticleEmissionAngleSize { get; private set; } = 0;

        public float GenerateRandomParticleEmissionAngle() => Random.Range(-ParticleEmissionAngleSize * 0.5f, ParticleEmissionAngleSize * 0.5f);

        [field: SerializeField]
        public GravityParticleConfig ParticleConfig { get; private set; }
    }
}
