using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityPlayground.GravityStuff;

namespace GravityPlayground.Character
{
    [RequireComponent(typeof(GravityForceApplier))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField]
        private GravityForceApplier m_forceApplier;

        [SerializeField]
        private SpriteRenderer m_spriteRenderer;

        [SerializeField]
        private Rigidbody2D m_rigidbody;

        [SerializeField]
        private Collider2D m_collider;

        private float m_lifeTime;
        private float m_timer;
        private bool m_enableColliderNextPhysicsStep = false;

        public void Init(ProjectileData data, Vector3 position, Vector3 velocity)
        {
            m_spriteRenderer.enabled = data.Sprite;
            m_spriteRenderer.sprite = data.Sprite;

            m_forceApplier.SetMass(data.GenerateRandomMass());

            float size = data.GenerateRandomSize();
            
            transform.position = position;
            m_rigidbody.position = position;
            m_rigidbody.velocity = velocity;

            m_lifeTime = data.GenerateRandomLifetime();
            m_timer = m_lifeTime;

            transform.localScale = Vector3.one * size;

            m_collider.enabled = false;
            m_enableColliderNextPhysicsStep = true;
        }

        private void Update()
        {
            m_timer -= Time.deltaTime;

            if (m_timer <= 0f)
                ProjectileController.DestroyProjectile(this);
        }

        private void FixedUpdate()
        {
            if (m_enableColliderNextPhysicsStep)
            {
                m_enableColliderNextPhysicsStep = false;
                m_collider.enabled = true;
            }
        }
    }
}