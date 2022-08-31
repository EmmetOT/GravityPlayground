using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GravityPlayground.GravityStuff
{
    [ExecuteInEditMode]
    public class ComplexGravityBody : GravityBody
    {
        [SerializeField]
        private GravityMap m_map = null;
        public GravityMap Map => m_map;

        public override bool IsValid => m_map != null;

        public override Vector2 CentreOfGravity => GetWorldSpaceFromNormalized(m_map.CentreOfGravity);

        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_map)
                m_cellsPerSide = m_map.TotalSize - Vector2Int.one;
        }

        private void Awake()
        {
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x, transform.localScale.x);

            if (m_map)
                m_cellsPerSide = m_map.TotalSize - Vector2Int.one;
        }

        [SerializeField]
        private bool m_showGizmo = false;

        [SerializeField]
        [HideInInspector]
        private Vector2Int m_cellsPerSide;
        
        private void Update()
        {
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x, transform.localScale.x);
        }

        private Vector2 GetWorldSpaceFromNormalized(Vector2 normalized)
        {
            normalized = new Vector2(Mathf.Lerp(-0.5f, 0.5f, normalized.x), Mathf.Lerp(-0.5f, 0.5f, normalized.y));
            return transform.TransformPoint(normalized);
        }

        private Vector2 Clamp(Vector2 point, float bounds = 0.5f)
        {
            Vector2 transformed = transform.InverseTransformPoint(point);
            Vector2 clamped = new Vector2(Mathf.Clamp(transformed.x, -bounds, bounds), Mathf.Clamp(transformed.y, -bounds, bounds));
            return transform.TransformPoint(clamped);
        }

        /// <summary>
        /// Given a point anywhere in 2D space, return true if the point is within the 'baked' area of this complex body, as well as the gravitational force to the body and the signed distance.
        /// </summary>
        private bool GetForceAndSignedDistanceAtClosestPointInBounds(Vector2 point, out Vector2 force, out float signedDistance)
        {
            // transform the point into the gravity body's local space
            point = transform.InverseTransformPoint(point);
            
            // get the point clamped inside the complex region
            // normalize the point to the range [0, 1] where (-0.5, -0.5) is the bottom left corner and (0.5, 0.5) is the top right corner
            Vector2 normalizedPoint = new Vector2
            (
                Mathf.InverseLerp(-0.5f, 0.5f, Mathf.Clamp(point.x, -0.5f, 0.5f)),
                Mathf.InverseLerp(-0.5f, 0.5f, Mathf.Clamp(point.y, -0.5f, 0.5f))
            );
            
            Vector2Int coords = new Vector2Int(Mathf.Min(m_cellsPerSide.x - 1, Mathf.FloorToInt(normalizedPoint.x * m_cellsPerSide.x)), Mathf.Min(m_cellsPerSide.y - 1, Mathf.FloorToInt(normalizedPoint.y * m_cellsPerSide.y)));
            Vector2 fracs = new Vector2((normalizedPoint.x * m_cellsPerSide.x) % 1f, (normalizedPoint.y * m_cellsPerSide.y) % 1f);

            Vector3 sampleA = m_map.GetForceAndSignedDistance(coords);
            Vector3 sampleB = m_map.GetForceAndSignedDistance(coords + Vector2Int.right);
            Vector3 sampleC = m_map.GetForceAndSignedDistance(coords + Vector2Int.up);
            Vector3 sampleD = m_map.GetForceAndSignedDistance(coords + Vector2Int.right + Vector2Int.up);

            Vector3 interpolated = Utils.BilinearInterpolate(fracs, sampleA, sampleB, sampleC, sampleD);

            force = transform.TransformDirection(new Vector2(interpolated.x, interpolated.y));
            signedDistance = interpolated.z * transform.localScale.x;

            return point.x >= -0.5f && point.x <= 0.5f && point.y >= -0.5f && point.y <= 0.5f;
        }
        
        /// <summary>
        /// Get the vector representing the distance and direction from any point in 2D space to the closest point on the surface of this body.
        /// </summary>
        public Vector2 GetVector(Vector2 point)
        {
            // we need to sample the gradient slightly inside the baked area,
            // as there has to be distance information all around the sample point to get a good result
            const float smallBounds = 0.48f;
            
            Vector2 vecToBounds = Clamp(point) - point;

            Vector2 directionInBakedArea;
            
            const float e = 0.01f;

            Vector2 clampedP = Clamp(point, smallBounds);

            Vector2 xy = new Vector2(e, -e);
            Vector2 yy = new Vector2(-e, -e);
            Vector2 yx = new Vector2(-e, e);
            Vector2 xx = new Vector2(e, e);

            GetForceAndSignedDistanceAtClosestPointInBounds(clampedP + xy, out _, out float xyDist);
            GetForceAndSignedDistanceAtClosestPointInBounds(clampedP + yy, out _, out float yyDist);
            GetForceAndSignedDistanceAtClosestPointInBounds(clampedP + yx, out _, out float yxDist);
            GetForceAndSignedDistanceAtClosestPointInBounds(clampedP + xx, out _, out float xxDist);

            directionInBakedArea = (
                xy * xyDist +
                yy * yyDist +
                yx * yxDist +
                xx * xxDist).normalized;

            GetForceAndSignedDistanceAtClosestPointInBounds(point, out _, out float signedDistance);

            Vector2 gradient = -directionInBakedArea;
            Vector2 vecInBounds = gradient * signedDistance;
            return vecToBounds + vecInBounds;
        }

        private Vector3 GetComplexForceAndSignedDistance(Vector2 point)
        {
            if (GetForceAndSignedDistanceAtClosestPointInBounds(point, out Vector2 force, out float signedDistance))
            {
                force *= Mass / transform.localScale.x;
                return new Vector3(force.x, force.y, signedDistance);
            }
            else
            {
                Vector2 centreOfGravity = m_map.CentreOfGravity;
                centreOfGravity -= Vector2.one * 0.5f;

                // transform the point into the gravity body's local space
                Vector2 transformedPoint = transform.InverseTransformPoint(point);
                transformedPoint /= transform.localScale.x;

                // adding a tiny number prevents NaN
                Vector2 difference = (centreOfGravity - transformedPoint) + 0.00001f * Vector2.one;

                float forceMagnitude = (Gravity.GravitationalConstant * Mass) / difference.sqrMagnitude;
                
                Vector2 dir = difference.normalized * forceMagnitude;

                return new Vector3(dir.x, dir.y, GetVector(point).magnitude);
            }
        }

        public override Vector2 GetGravityForce(Vector2 point) => GetComplexForceAndSignedDistance(point);
        public override float GetSignedDistance(Vector2 point) => GetComplexForceAndSignedDistance(point).z;

        public override GravityData GetGPUData()
        {
            if (m_map == null)
                return new GravityData();
            
            Vector2 centreOfGravity = GetWorldSpaceFromNormalized(m_map.CentreOfGravity);

            return new GravityData()
            {
                Transform = transform.localToWorldMatrix,
                Colour = Colour,
                InverseTransform = transform.worldToLocalMatrix,
                Type = 2,
                Mass = Mass,
                Bounciness = Bounciness,
                Data_1 = new Vector4(0f, m_map.TotalSize.x, m_map.TotalSize.y, transform.localScale.x),
                Data_2 = new Vector4(centreOfGravity.x, centreOfGravity.y, 0f, 0f)
            };
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!m_showGizmo)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(GetWorldSpaceFromNormalized(m_map.CentreOfGravity), 0.08f);
            
            Vector2 bl = new Vector2(-0.5f, -0.5f);
            Vector2 br = new Vector2(0.5f, -0.5f);
            Vector2 tr = new Vector2(0.5f, 0.5f);
            Vector2 tl = new Vector2(-0.5f, 0.5f);

            Handles.matrix = transform.localToWorldMatrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Handles.color = Color.white;
            Handles.DrawAAPolyLine(bl, br, tr, tl, bl);
        }
#endif
    }
}