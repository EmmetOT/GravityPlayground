using System.Collections.Generic;
using UnityEngine;
using AALineDrawer;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GravityPlayground.GravityStuff
{
    [RequireComponent(typeof(LineDrawer))]
    [ExecuteInEditMode]
    public class GravityPathPredictor : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private LineDrawer m_lineDrawer;

        private LineDrawer.PointData[] m_points;

        [SerializeField]
        [Min(0)]
        private int m_simulationSteps = 100;

        [SerializeField]
        [Min(0f)]
        private float m_lineStartWidth = 1f;

        [SerializeField]
        [Min(0f)]
        private float m_lineEndWidth = 1f;

        private enum ColourMode { Velocity, Time };

        [SerializeField]
        private ColourMode m_colourMode = ColourMode.Time;

        [SerializeField]
        private Color m_slowColour = Color.blue;

        [SerializeField]
        private Color m_fastColour = Color.red;

        [SerializeField]
        private Color m_startColour = Color.blue;

        [SerializeField]
        private Color m_endColour = Color.red;

        [SerializeField]
        [Min(0f)]
        private float m_slowVelocity = 0f;

        [SerializeField]
        [Min(0f)]
        private float m_fastVelocity = 10f;

        [SerializeField]
        [Min(0f)]
        private float m_timeStep = 0.1f;

        [SerializeField]
        private UpdateMode m_updateMode = UpdateMode.Always;

        public enum UpdateMode { Always, OnMove, Manual }

        [SerializeField]
        private Vector2 m_initialVelocity;

        [SerializeField]
        private float m_mass = 1f;

        [SerializeField]
        private bool m_stopOnCollision = true;

        private Vector2 m_previousPosition;

        private Vector2 m_currentVelocity;

        private Vector3[] m_positions;
        private Vector3[] m_velocities;
        private float[] m_signedDistances;

        private void Reset() => Init();
        private void OnValidate() => Init();
        private void OnEnable() => Init();

        private void OnDisable()
        {
            m_lineDrawer = GetComponent<LineDrawer>();
            m_lineDrawer.enabled = false;
        }

        private void Init()
        {
            m_lineDrawer = GetComponent<LineDrawer>();
            m_lineDrawer.enabled = true;
            m_lineDrawer.Clear();

            if (m_updateMode != UpdateMode.Manual)
                Simulate(transform.position, Application.isPlaying ? m_currentVelocity : m_initialVelocity, m_mass);
        }

        public void SetUpdateMode(UpdateMode updateMode)
        {
            m_updateMode = updateMode;
        }

        public void SetMass(float mass)
        {
            m_mass = mass;
        }

        private void Update()
        {
            if (m_updateMode == UpdateMode.Always || (m_updateMode == UpdateMode.OnMove && transform.hasChanged) || (m_updateMode == UpdateMode.Manual && !Application.isPlaying))
            {
                Simulate(transform.position, Application.isPlaying ? m_currentVelocity : m_initialVelocity, m_mass);
            }
        }

        private void FixedUpdate()
        {
            Vector2 position = transform.position;

            m_currentVelocity = position - m_previousPosition;
            m_previousPosition = position;
        }

        public void Simulate(Vector3 position, Vector3 velocity, float mass, int guid = -1)
        {
            void VerifyArray<T>(ref T[] array)
            {
                if (array == null || array.Length != m_simulationSteps)
                    array = new T[m_simulationSteps];
            }

            VerifyArray(ref m_points);
            VerifyArray(ref m_positions);
            VerifyArray(ref m_velocities);
            VerifyArray(ref m_signedDistances);

            int count = Gravity.PredictPositions(m_positions, m_velocities, m_signedDistances, position, velocity, mass, guid, m_timeStep, stopOnCollision: m_stopOnCollision);

            int steps = Mathf.Min(count, m_positions.Length);
            for (int i = 0; i < steps; i++)
            {
                float t = i / (steps - 1f);
                Vector3 pos = m_positions[i];
                Vector3 vel = m_velocities[i];
                float signedDistance = m_signedDistances[i];
                Color col;

                if (m_colourMode == ColourMode.Velocity)
                    col = Color.Lerp(m_slowColour, m_fastColour, Mathf.InverseLerp(m_slowVelocity, m_fastVelocity, signedDistance < 0f ? -1f : vel.magnitude));
                else
                    col = Color.Lerp(m_startColour, m_endColour, t);

                m_points[i] = new LineDrawer.PointData
                {
                    Position = pos,
                    Width = Mathf.Lerp(m_lineStartWidth, m_lineEndWidth, t),
                    Colour = col
                };
            }

            m_lineDrawer.SetPoints(count - 1, m_points);
        }
    }
}
