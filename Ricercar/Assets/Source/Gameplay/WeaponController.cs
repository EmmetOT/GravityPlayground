using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AALineDrawer;
using GravityPlayground.GravityStuff;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GravityPlayground.Character
{
    [RequireComponent(typeof(GravityPathPredictor))]
    public class WeaponController : MonoBehaviour
    {
        private GravityPathPredictor m_trajectoryPredictor;

        [SerializeField]
        private WeaponData m_weaponData = null;

        private Weapon m_weapon;

        [SerializeField]
        [HideInInspector]
        private Transform m_transform;

        [SerializeField]
        private Rigidbody2D m_rigidbody;

        [SerializeField]
        private Transform m_cameraFocalPoint;

        [SerializeField]
        [Min(0.01f)]
        private float m_debugSpeed = 4f;

        [SerializeField]
        private float m_debugMass = 1f;

        [SerializeField]
        [Min(01f)]
        private float m_debugCameraLookDistance = 0.3f;

        [SerializeField]
        [Min(01f)]
        private float m_debugCameraLookSpeed = 1f;

        private enum State { Idle, Aiming };

        private State m_currentState = State.Idle;

        [SerializeField]
        private Vector2 m_currentAim = Vector2.zero;

        [SerializeField]
        private bool m_debugManualAim = false;

        private bool m_isFireInputDown = false;

        private CharacterControllerInputs m_input;

        private void Reset()
        {
            m_transform = transform;
            m_trajectoryPredictor = GetComponent<GravityPathPredictor>();
        }

        private void OnValidate()
        {
            m_transform = transform;
            m_trajectoryPredictor = GetComponent<GravityPathPredictor>();
        }

        private void OnEnable()
        {
            m_transform = transform;
            m_trajectoryPredictor = GetComponent<GravityPathPredictor>();

            if (m_input == null)
                m_input = new CharacterControllerInputs();

            m_input.Enable();

            m_input.Player.Fire.performed -= OnFireInputDown;
            m_input.Player.Fire.performed += OnFireInputDown;

            m_input.Player.Fire.canceled -= OnFireInputUp;
            m_input.Player.Fire.canceled += OnFireInputUp;

            m_trajectoryPredictor.SetUpdateMode(GravityPathPredictor.UpdateMode.Manual);
            m_trajectoryPredictor.enabled = false;
        }

        private void OnDisable()
        {
            m_input.Disable();
        }

        private void Awake()
        {
            if (m_weaponData)
                m_weapon = new Weapon(m_weaponData);
        }

        private void Update()
        {
            UpdateAimInput();
            
            if (m_weapon != null)
            {
                m_weapon.Update(Time.deltaTime);

                if (m_currentState == State.Aiming)
                {
                    UpdateAimLook();

                    if (m_isFireInputDown && m_weapon.Data.HoldToFire && m_weapon.CanFire())
                        FireWeapon();
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = Color.black;
            Handles.DrawAAPolyLine(3f, m_transform.position, m_transform.position + (Vector3)m_currentAim * 20f);
        }
#endif

        public void SetAim(Vector2 aim)
        {
            if (m_debugManualAim)
                aim = m_currentAim.normalized;

            m_currentAim = aim;

            if (m_weapon != null)
            {
                m_currentState = State.Aiming;

                if (!m_trajectoryPredictor.enabled)
                    m_trajectoryPredictor.enabled = true;

                m_trajectoryPredictor.Simulate(transform.position, aim * m_weapon.Data.AverageSpeed, m_weapon.Data.Projectile.AverageMass);
            }

        }

        public void CancelAim()
        {
            m_currentState = State.Idle;
            m_currentAim = Vector2.zero;
            m_trajectoryPredictor.enabled = false;
        }

        private void UpdateAimLook()
        {
            Vector3 cameraTarget = m_transform.position + (Vector3)m_currentAim * m_debugCameraLookDistance;

            m_cameraFocalPoint.position = Vector3.Lerp(m_cameraFocalPoint.position, cameraTarget, m_debugCameraLookSpeed * Time.smoothDeltaTime);
        }

        // called from update loop
        private void UpdateAimInput()
        {
            Vector2 val = m_input.Player.Aim.ReadValue<Vector2>();
            Vector2 target = CameraController.Camera.ScreenToWorldPoint(new Vector3(val.x, val.y, -CameraController.Camera.transform.position.z));

            SetAim((target - (Vector2)m_transform.position).normalized);
        }

        private void OnFireInputDown(InputAction.CallbackContext _)
        {
            m_isFireInputDown = true;

            if (m_weapon != null && m_weapon.CanFire() && !m_weapon.Data.HoldToFire)
                FireWeapon();
        }

        private void OnFireInputUp(InputAction.CallbackContext _)
        {
            m_isFireInputDown = false;
        }

        private void FireWeapon()
        {
            m_weapon.Fire(m_rigidbody.position, m_currentAim, out Vector2 velocity);

            Vector2 knockback = m_weapon.Data.KnockbackForceScalar * -velocity;
            m_rigidbody.AddForce(knockback);
        }
    }
}
