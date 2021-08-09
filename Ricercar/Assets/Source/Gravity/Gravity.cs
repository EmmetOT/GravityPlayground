using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

namespace GravityPlayground.GravityStuff
{
    [RequireComponent(typeof(GravityParticleSystem))]
    public class Gravity : Singleton<Gravity>
    {
        private static class Properties
        {
            public static int ComplexSamples_StructuredBuffer = Shader.PropertyToID("_ComplexSamples");
            public static int Data_StructuredBuffer = Shader.PropertyToID("_GravityData");
            public static int GravityData_Count = Shader.PropertyToID("_GravityDataCount");
            public static int GravitationalConstant_Float = Shader.PropertyToID("_GravitationalConstant");
            public static int MaxForce_Float = Shader.PropertyToID("_MaxForce");
        }

        [SerializeField]
        [HideInInspector]
        private List<GravityBody> m_bodies = new List<GravityBody>();

        [SerializeField]
        [HideInInspector]
        private GravityParticleSystem m_particleSystem;
        public static GravityParticleSystem ParticleSystem => Instance.m_particleSystem ?? (Instance.m_particleSystem = Instance.GetComponent<GravityParticleSystem>());

        [SerializeField]
        private float m_gravitationalConstant = 667.4f;
        public static float GravitationalConstant => Instance.m_gravitationalConstant;

        [SerializeField]
        [Min(0f)]
        private float m_maxForce = 100000f;
        public static float MaxForce => Instance.m_maxForce;

        private ComputeBuffer m_dataBuffer;
        private ComputeBuffer m_complexSamplesBuffer;

        private bool HasInvalidBuffer => m_dataBuffer == null || !m_dataBuffer.IsValid() || m_complexSamplesBuffer == null || !m_complexSamplesBuffer.IsValid();

        private readonly List<Vector3> m_complexSampleData = new List<Vector3>();

        [SerializeField]
        [HideInInspector]
        private List<GravityData> m_data = new List<GravityData>();
        private readonly Dictionary<int, int> m_complexBodyStartIndices = new Dictionary<int, int>();

        private bool m_isSampleDataDirty = true;
        private bool m_isDataDirty = true;

        [SerializeField]
        [HideInInspector]
        // this bool is toggled off/on whenever the Unity callbacks OnEnable/OnDisable are called.
        // note that this doesn't always give the same result as "enabled" because OnEnable/OnDisable are
        // called during recompiles etc. you can basically read this bool as "is recompiling"
        private bool m_isEnabled = false;

        private bool m_forceUpdateNextFrame = false;
        
        private void OnValidate()
        {
            Shader.SetGlobalFloat(Properties.GravitationalConstant_Float, m_gravitationalConstant);
            Shader.SetGlobalFloat(Properties.MaxForce_Float, m_maxForce);
        }

        private void Awake()
        {
            m_isSampleDataDirty = true;
            m_isDataDirty = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

#if UNITY_EDITOR
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif

            m_isEnabled = true;
            m_isSampleDataDirty = true;
            m_isDataDirty = true;
            m_forceUpdateNextFrame = true;
        }
        
        private void OnDisable()
        {
#if UNITY_EDITOR
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif

            m_isEnabled = false;

            m_dataBuffer?.Dispose();
            Debug.Log("Disposing complex samples buffer!");
            m_complexSamplesBuffer?.Dispose();
        }
        
        public static void Register(GravityBody gravityBody)
        {
            if (!Instance)
                return;

            if (!Instance.m_bodies.Contains(gravityBody))
            {
                Instance.m_bodies.Add(gravityBody);
                
                Instance.m_isSampleDataDirty |= gravityBody is ComplexGravityBody;
                Instance.m_isDataDirty = true;

                Instance.OnDirty();
            }
        }

        public static void Deregister(GravityBody gravityBody)
        {
            if (!Instance)
                return;

            if (Instance.m_bodies.Contains(gravityBody))
            {
                Instance.m_isSampleDataDirty |= gravityBody is ComplexGravityBody;
                Instance.m_isDataDirty = true;

                Instance.m_bodies.Remove(gravityBody);
                Instance.OnDirty();
            }
        }

        public static Vector2 GetTotalGravityForce(Vector2 point, int guid = -1)
        {
            Vector2 sum = Vector2.zero;

            for (int i = 0; i < Instance.m_bodies.Count; i++)
            {
                GravityBody body = Instance.m_bodies[i];

                if (body.enabled && (guid == -1 || body.GUID != guid))
                    sum += body.GetGravityForce(point);
            }

            return Vector2.ClampMagnitude(sum, Instance.m_maxForce);
        }

        public static float GetSignedDistance(Vector2 point, int guid = -1)
        {
            float min = 1000000f;

            for (int i = 0; i < Instance.m_bodies.Count; i++)
            {
                GravityBody body = Instance.m_bodies[i];

                if (body.enabled && (guid == -1 || body.GUID != guid))
                    min = Mathf.Min(min, body.GetSignedDistance(point));
            }

            return min;
        }

        public static Vector2 GetDirectionToSurface(Vector2 point, int guid = -1)
        {
            const float epsilon = 0.1f;

            return -(new Vector2(
                GetSignedDistance(point + Vector2.right * epsilon, guid) - GetSignedDistance(point - Vector2.right * epsilon, guid),
                GetSignedDistance(point + Vector2.up * epsilon, guid) - GetSignedDistance(point - Vector2.up * epsilon, guid)))
                .normalized;
        }

        public static Vector2 GetClosestSurfacePoint(Vector2 point, int guid = -1)
        {
            float signedDistance = GetSignedDistance(point, guid);
            Vector2 directionToSurface = GetDirectionToSurface(point, guid);

            return point + directionToSurface * Mathf.Abs(signedDistance);
        }
        
        public static Vector3 GetTotalGravityForceAndSignedDistance(Vector2 point, int guid = -1)
        {
            Vector2 gravitySum = new Vector3(0f, 0f);
            float minSignedDistance = 1000000f;

            for (int i = 0; i < Instance.m_bodies.Count; i++)
            {
                GravityBody body = Instance.m_bodies[i];

                if (body.enabled && (guid == -1 || body.GUID != guid))
                {
                    gravitySum += body.GetGravityForce(point);
                    minSignedDistance = Mathf.Min(minSignedDistance, body.GetSignedDistance(point));
                }
            }

            // clamp force magnitude before returning
            gravitySum = Vector2.ClampMagnitude(gravitySum, Instance.m_maxForce);

            return new Vector3(gravitySum.x, gravitySum.y, minSignedDistance);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < Instance.m_bodies.Count; i++)
            {
                m_isDataDirty |= Instance.m_bodies[i].IsDirty;
                Instance.m_bodies[i].SetDirty(false);
            }

            if (HasInvalidBuffer || m_forceUpdateNextFrame || m_isDataDirty || m_isSampleDataDirty)
                OnDirty();

            m_forceUpdateNextFrame = false;
        }

        private void OnDirty()
        {
            if (!m_isEnabled)
                return;
            
            if (m_isSampleDataDirty)
            {
                int previousComplexCount = m_complexSampleData.Count;

                m_complexSampleData.Clear();
                m_complexBodyStartIndices.Clear();

                for (int i = 0; i < m_bodies.Count; i++)
                {
                    if (m_bodies[i] is ComplexGravityBody body && body.Map)
                    {
                        Vector3[] forces = body.Map.Forces;
                        
                        if (!m_complexBodyStartIndices.ContainsKey(body.GUID))
                        {
                            int startIndex = m_complexSampleData.Count;
                            m_complexSampleData.AddRange(forces);
                            m_complexBodyStartIndices.Add(body.GUID, startIndex);
                        }
                    }
                }

                if (m_complexSamplesBuffer == null || !m_complexSamplesBuffer.IsValid() || previousComplexCount != m_complexSampleData.Count)
                {
                    m_complexSamplesBuffer?.Dispose();
                    Debug.Log("Disposing complex samples buffer!");
                    m_complexSamplesBuffer = new ComputeBuffer(Mathf.Max(1, m_complexSampleData.Count), sizeof(float) * 3);
                }

                if (m_complexSampleData.Count > 0)
                    m_complexSamplesBuffer.SetData(m_complexSampleData);

                Debug.Log("Setting complex samples buffer!");
                Shader.SetGlobalBuffer(Properties.ComplexSamples_StructuredBuffer, m_complexSamplesBuffer);
            }

            int previousCount = m_data.Count;
            m_data.Clear();

            for (int i = 0; i < m_bodies.Count; i++)
            {
                GravityBody body = m_bodies[i];
                GravityData data = body.GetGPUData();

                // setting the index in the sample list where this data's samples start
                if (body is ComplexGravityBody)
                    data.Data_1 = data.Data_1.SetX(m_complexBodyStartIndices[body.GUID]);

                m_data.Add(data);
            }
            
            if (m_dataBuffer == null || !m_dataBuffer.IsValid() || previousCount != m_data.Count)
            {
                m_dataBuffer?.Dispose();
                m_dataBuffer = new ComputeBuffer(Mathf.Max(1, m_data.Count), GravityData.Stride);
            }

            if (m_data.Count > 0)
                m_dataBuffer.SetData(m_data);

            //Debug.Log("Sending data to buffers.");
            Shader.SetGlobalInt(Properties.GravityData_Count, m_data.Count);
            Shader.SetGlobalBuffer(Properties.Data_StructuredBuffer, m_dataBuffer);
            Shader.SetGlobalFloat(Properties.GravitationalConstant_Float, m_gravitationalConstant);
            
            m_isSampleDataDirty = false;
            m_isDataDirty = false;
        }
        
        private void OnCompilationStarted(object param)
        {
            m_isEnabled = false;

            Debug.Log("Disposing complex samples buffer!");
            m_complexSamplesBuffer?.Dispose();
            m_dataBuffer?.Dispose();
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            // this ensures "m_isEnabled" is set to false while transitioning between play modes
            m_isEnabled = stateChange == PlayModeStateChange.EnteredPlayMode || stateChange == PlayModeStateChange.EnteredEditMode;
        }
#endif

        public static void PredictPosition(Vector3 pos, Vector3 velocity, out Vector3 nextPosition, out Vector3 nextVelocity, out float signedDistance, float mass, int guid = -1, float timeStep = -1f)
        {
            if (timeStep <= 0f)
                timeStep = Time.fixedDeltaTime;

            Vector3 gravityAndSignedDistance = GetTotalGravityForceAndSignedDistance(pos, guid);
            Vector3 gravity = new Vector3(gravityAndSignedDistance.x, gravityAndSignedDistance.y, 0f);
            signedDistance = gravityAndSignedDistance.z;

            nextVelocity = velocity + (gravity * timeStep / mass);
            nextPosition = pos + nextVelocity * timeStep;
        }
        
        public static int PredictPositions(Vector3[] positions, Vector3[] velocities, float[] signedDistances, Vector3 initialPosition, Vector3 initialVelocity, float mass, int guid = -1, float timeStep = -1f, bool stopOnCollision = false)
        {
            Debug.Assert(positions.Length == velocities.Length && velocities.Length == signedDistances.Length, "Length of given arrays must be equal.");

            if (positions.Length == 0)
                return 0;

            Vector3 position = initialPosition;
            Vector3 velocity = initialVelocity;
            float signedDistance = GetSignedDistance(initialPosition, guid);
            int count = 0;

            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = position;
                velocities[i] = velocity;
                signedDistances[i] = signedDistance;
                PredictPosition(position, velocity, out position, out velocity, out signedDistance, mass, guid, timeStep);

                if (stopOnCollision && signedDistance <= 0f)
                {
                    positions[i] = GetClosestSurfacePoint(positions[i], guid);
                    return ++count;
                }

                count++;
            }

            positions[positions.Length - 1] = position;
            velocities[velocities.Length - 1] = velocity;
            signedDistances[signedDistances.Length - 1] = signedDistance;
            return count;
        }

        /// <summary>
        /// Assuming the given gravity direction represents down, and the given vector
        /// is a direction in world space, rotate it to match the gravity direction.
        /// </summary>
        public static Vector2 ConvertDirectionToGravitySpace(Vector2 gravityDirection, Vector2 vec) =>
            Quaternion.FromToRotation(Vector2.down, gravityDirection) * vec;

    }
}