using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GravityPlayground.GravityStuff
{
    public class LineGravityBody : GravityBody
    {
        [SerializeField]
        [Min(0f)]
        private float m_width = 0f;

        [SerializeField]
        private Vector2 m_a;
        public Vector2 A => transform.TransformPoint(m_a);

        [SerializeField]
        private Vector2 m_b;
        public Vector2 B => transform.TransformPoint(m_b);

        public override Vector2 CentreOfGravity => (A + B) * 0.5f;
        
        /// <summary>
        /// Set the position of the line caps in local space.
        /// </summary>
        public void SetLineEnds(Vector2 a, Vector2 b)
        {
            m_a = a;
            m_b = b;
        }

        public override GravityData GetGPUData()
        {
            GravityData data = new GravityData()
            {
                Transform = transform.localToWorldMatrix,
                InverseTransform = transform.worldToLocalMatrix,
                Colour = Colour,
                Type = 1,
                Mass = Mass,
                Bounciness = Bounciness,
                Data_1 = new Vector4(m_a.x, m_a.y, m_width, 0f),
                Data_2 = new Vector4(m_b.x, m_b.y, 0f, 0f)
            };
            
            return data;
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = Mass < 0f ? Color.red : Color.blue;
            Handles.DrawAAPolyLine(0.5f, A, B);
        }
#endif
        
        public override float GetSignedDistance(Vector2 point)
        {
            Vector2 a = A;
            Vector2 b = B;

            Vector2 pa = point - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            Vector2 q = pa - h * ba;
            float d = q.magnitude;
            
            return d - m_width;
        }

        public override Vector2 GetGravityForce(Vector2 point)
        {
            // adding a tiny number prevents NaN
            Vector2 difference = (Vector2)Utils.ProjectPointOnLineSegment(A, B, point) - point + Mathf.Epsilon * Vector2.one;
            Vector2 directionToLineSegment = difference.normalized;
            difference = directionToLineSegment * (difference.magnitude - m_width);
            
            float forceMagnitude = (Gravity.GravitationalConstant * Mass) / difference.sqrMagnitude;
            return (difference.normalized * forceMagnitude);
        }
    }
}