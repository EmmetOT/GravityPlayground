using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityPlayground.GravityStuff
{
    [ExecuteInEditMode]
    public abstract class GravityBody : MonoBehaviour
    {
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
        
        public bool IsDirty { get; private set; } = false;

        public int GUID => gameObject.GetInstanceID();
        
        protected virtual void OnValidate()
        {
            OnMassChanged?.Invoke(m_mass);
            SetDirty(true);
        }

        protected void OnEnable()
        {
            Gravity.Register(this);
        }

        protected void OnDisable()
        {
            Gravity.Deregister(this);
        }

        protected virtual void Update()
        {
            if (transform.hasChanged)
                SetDirty(true);
        }

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