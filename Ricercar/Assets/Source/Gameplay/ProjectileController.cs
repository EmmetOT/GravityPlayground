using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityPlayground.Character
{
    public class ProjectileController : Singleton<ProjectileController>
    {
        [SerializeField]
        private Projectile m_projectilePrefab;

        private Pool<Projectile> m_pool;

        private void Start()
        {
            if (Application.isPlaying) // weird that i need this. possible beta bug?
                m_pool = new Pool<Projectile>(m_projectilePrefab, Instance.transform, 5);
        }

        public static Projectile SpawnProjectile(ProjectileData data, Vector2 position, Vector2 velocity)
        {
            Projectile projectile = Instance.m_pool.GetNew();
            projectile.Init(data, position, velocity);

            return projectile;
        }

        public static void DestroyProjectile(Projectile projectile)
        {
            if (!Instance.m_pool.Owns(projectile))
                return;

            Instance.m_pool.ReturnToPool(projectile);
        }
    }
}