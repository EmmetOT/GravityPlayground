using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityPlayground.GravityStuff
{
    [RequireComponent(typeof(TransformChangeDetector))]
    [ExecuteInEditMode]
    public abstract class GravityBody : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private TransformChangeDetector m_transformChangeDetector;

        [SerializeField]
        private float m_mass = 0f;
        public float Mass => m_mass;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_bounciness = 0f;
        public float Bounciness => m_bounciness;

        [SerializeField]
        //[ColorUsage(showAlpha: false)]
        private Color m_colour;
        public Color Colour => m_colour;

        public virtual bool IsValid => true;

        public bool IsDirty { get; private set; } = false;

        public int GUID => gameObject.GetInstanceID();

        protected virtual void OnValidate()
        {
            SubscribeToOnTransformChanged();

            OnMassChanged?.Invoke(m_mass);
            SetDirty(true);
        }

        protected void OnEnable()
        {
            SubscribeToOnTransformChanged();
            Gravity.Register(this);
        }

        protected void OnDisable()
        {
            Gravity.Deregister(this);
        }

        private void SubscribeToOnTransformChanged()
        {
            m_transformChangeDetector = gameObject.GetOrAddComponent<TransformChangeDetector>();

            m_transformChangeDetector.OnTransformChange -= OnTransformChanged;
            m_transformChangeDetector.OnTransformChange += OnTransformChanged;
        }

        private void OnTransformChanged() => SetDirty(true);

        public void SetDirty(bool isDirty)
        {
            IsDirty = isDirty;
        }

        public void SetMass(float mass)
        {
            m_mass = mass;

            OnMassChanged?.Invoke(m_mass);

            SetDirty(true);
        }

        public Vector2 GetCurrentGravity() => Gravity.GetTotalGravityForce(transform.position, GUID);
        public abstract Vector2 GetGravityForce(Vector2 point);
        public abstract float GetSignedDistance(Vector2 point);
        public abstract GravityData GetGPUData();
        public abstract Vector2 CentreOfGravity { get; }

        public System.Action<float> OnMassChanged;
    }
}