using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GravityPlayground.GravityStuff
{
    [RequireComponent(typeof(Rigidbody2D))]
    [ExecuteInEditMode]
    public class GravityForceApplier : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private Rigidbody2D m_rigidbody;
        public Rigidbody2D Rigidbody => m_rigidbody ? m_rigidbody : m_rigidbody = GetComponent<Rigidbody2D>();

        private void OnEnable() => Init();
        private void Awake() => Init();

        //private void OnDisable()
        //{
        //    if (m_self)
        //        m_self.OnMassChanged -= OnMassChanged;
        //}

        [SerializeField]
        private bool m_applyGravity = true;

        //[SerializeField]
        //private GravityBody m_self = null;

        [SerializeField]
        private bool m_showGizmo = false;

        [SerializeField]
        private Vector2 m_initialVelocity = Vector2.zero;

        public Vector2 CurrentGravity { get; private set; } = Vector2.zero;
        public Vector2 CurrentNormalizedGravity { get; private set; } = Vector2.zero;

        [SerializeField]
        private GravityPathPredictor m_predictor;

        [SerializeField]
        private bool m_hasNegativeMass = false;

        //public int GUID => m_self ? m_self.GUID : -1;
        //public float Mass => m_self ? m_self.Mass : Rigidbody.mass;

        public float Mass => Rigidbody.mass * (m_hasNegativeMass ? -1 : 1);

        public void SetMass(float mass)
        {
            m_hasNegativeMass = mass < 0f;

            m_rigidbody.mass = Mathf.Abs(mass);

            if (m_predictor)
                m_predictor.Simulate(m_rigidbody.position, m_rigidbody.velocity, Mass/*, GUID*/);
        }

        private void OnValidate()
        {
            Init();
        }

        private void Init()
        {
            //if (m_self)
            //{
            //    m_self.OnMassChanged -= OnMassChanged;
            //    m_self.OnMassChanged += OnMassChanged;
            //}

            if (m_predictor || (m_predictor = GetComponent<GravityPathPredictor>()))
            {
                m_predictor.SetUpdateMode(GravityPathPredictor.UpdateMode.Manual);
                m_predictor.SetMass(Mass);
            }

            m_rigidbody = GetComponent<Rigidbody2D>();
            m_rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (m_rigidbody.bodyType != RigidbodyType2D.Static)
                m_rigidbody.velocity = m_initialVelocity;

            //if (!m_self)
            //    m_self = GetComponent<GravityBody>();

            //if (m_self)
            //{
            //    m_rigidbody.mass = Mathf.Abs(m_self.Mass);
            //    m_rigidbody.centerOfMass = transform.InverseTransformPoint(m_self.CentreOfGravity);
            //}

            if (m_predictor)
                m_predictor.Simulate(m_rigidbody.position, Application.isPlaying ? m_rigidbody.velocity : m_initialVelocity, Mass/*, GUID*/);
        }

        private void Update()
        {
            if (transform.hasChanged && m_predictor)
                m_predictor.Simulate(m_rigidbody.position, Application.isPlaying ? m_rigidbody.velocity : m_initialVelocity, Mass/*, GUID*/);
        }

        private void FixedUpdate()
        {
            if (!m_applyGravity)
                return;

            if (Application.isPlaying)
            {
                CurrentGravity = Gravity.GetTotalGravityForce(m_rigidbody.position/*, GUID*/);
                CurrentNormalizedGravity = CurrentGravity.normalized;

                //if (m_self)
                //    m_rigidbody.AddForceAtPosition(CurrentGravity, m_self.CentreOfGravity);
                //else
                    m_rigidbody.AddForce(CurrentGravity);

                if (m_predictor)
                    m_predictor.Simulate(m_rigidbody.position, m_rigidbody.velocity, Mass/*, GUID*/);
            }
        }
    }
}

