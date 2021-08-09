using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GravityPlayground.GravityStuff
{
    public class PointGravityBody : GravityBody
    {
        [SerializeField]
        [Min(0f)]
        private float m_radius = 0f;

        [SerializeField]
        [HideInInspector]
        private CircleCollider2D m_circleCollider;
        public CircleCollider2D CircleCollider
        {
            get
            {
                if (m_circleCollider)
                    return m_circleCollider;

                m_circleCollider = GetComponent<CircleCollider2D>();

                if (m_circleCollider)
                    return m_circleCollider;

                m_circleCollider = GetComponentInChildren<CircleCollider2D>();

                return m_circleCollider;
            }
        }
        
        public float SqrRadius => m_radius * m_radius;
        public float SurfaceGravity => m_radius == 0f ? Mathf.Infinity : ((Gravity.GravitationalConstant * Mass) / (m_radius * m_radius));

        public override Vector2 CentreOfGravity => transform.position;

        public override Vector2 GetGravityForce(Vector2 point)
        {
            // adding a tiny number prevents NaN
            Vector2 difference = ((Vector2)transform.position - point) + Mathf.Epsilon * Vector2.one;
            float sqrMagnitude = difference.sqrMagnitude;

            float forceMagnitude;

            if (m_radius > 0f && sqrMagnitude < SqrRadius)
                forceMagnitude = Mathf.Lerp(0f, SurfaceGravity, Mathf.Sqrt(sqrMagnitude) / m_radius);
            else
                forceMagnitude = (Gravity.GravitationalConstant * Mass) / difference.sqrMagnitude;

            return (difference.normalized * forceMagnitude);
        }

        public override float GetSignedDistance(Vector2 point) => Vector2.Distance(point, transform.position) - m_radius;

        public override GravityData GetGPUData()
        {
            return new GravityData()
            {
                Transform = transform.localToWorldMatrix,
                InverseTransform = transform.worldToLocalMatrix,
                Colour = Colour,
                Type = 0,
                Mass = Mass,
                Bounciness = Bounciness,
                Data_1 = new Vector4(m_radius, SurfaceGravity, SqrRadius)
            };
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (CircleCollider)
                CircleCollider.radius = m_radius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = Mass < 0f ? Color.red : Color.blue;
            Handles.DrawWireDisc(transform.position, Vector3.forward, m_radius);
        }
#endif


    }
}