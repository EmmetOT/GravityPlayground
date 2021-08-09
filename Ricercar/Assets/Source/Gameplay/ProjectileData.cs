using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityPlayground.Character
{
    [CreateAssetMenu(fileName = "Projectile", menuName = "ScriptableObjects/ProjectileData", order = 1)]
    public class ProjectileData : ScriptableObject
    {
        [field: SerializeField]
        public Sprite Sprite { get; private set; } = null;
        
        [field: SerializeField]
        [field: Min(0f)]
        public float MinimumMass { get; private set; } = 1f;

        [field: SerializeField]
        [field: Min(0f)]
        public float MaximumMass { get; private set; } = 1f;

        [field: SerializeField]
        [field: Min(0f)]
        public float MinimumSize { get; private set; } = 1f;

        [field: SerializeField]
        [field: Min(0f)]
        public float MaximumSize { get; private set; } = 1f;

        [field: SerializeField]
        [field: Min(0f)]
        public float MinimumLifetime { get; private set; } = 1f;

        [field: SerializeField]
        [field: Min(0f)]
        public float MaximumLifetime { get; private set; } = 1f;

        public float AverageMass => (MinimumMass + MaximumMass) * 0.5f;

        public float GenerateRandomMass() => Random.Range(MinimumMass, MaximumMass);
        public float GenerateRandomSize() => Random.Range(MinimumSize, MaximumSize);
        public float GenerateRandomLifetime() => Random.Range(MinimumLifetime, MaximumLifetime);

    }
}
