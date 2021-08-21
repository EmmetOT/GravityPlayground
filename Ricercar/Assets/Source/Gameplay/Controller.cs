using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityPlayground.GravityStuff;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GravityPlayground.Character
{
    /// <summary>
    /// This is the controller for a 2D character. It is strongly based on Catlike Coding's movement
    /// tutorial, but adapted for 2D and with a variable gravity direction. The input elements have also
    /// been pulled into a separate script.
    /// </summary>
    public class Controller : MonoBehaviour
    {
        private CharacterControllerInputs m_input;

        [SerializeField]
        //[BoxGroup("Components")]
        private Rigidbody2D m_rigidbody;
        
        [SerializeField]
        //[BoxGroup("Components")]
        private GravityForceApplier m_gravityForce;

        [SerializeField]
        [HideInInspector]
        private Transform m_transform;

        [SerializeField]
        //[MinValue(0f)]
        //[BoxGroup("Controls")]
        private float m_speed = 3f;

        [SerializeField]
        //[MinValue(0f)]
        //[BoxGroup("Controls")]
        private float m_maxAcceleration = 2f;

        [SerializeField]
        //[MinValue(0f)]
        //[BoxGroup("Controls")]
        private float m_maxAirborneAcceleration;

        [SerializeField]
        [Range(0f, 90f)]
        //[BoxGroup("Controls")]
        private float m_maxGroundAngle = 25f;

        private float m_minGroundDotProduct;

        [SerializeField]
        //[MinValue(0f)]
        //[BoxGroup("Controls")]
        private float m_jumpHeight = 2f;

        [SerializeField]
        //[MinValue(0)]
        //[BoxGroup("Controls")]
        private int m_maxAirJumps = 0;

        [SerializeField]
        //[MinValue(0f)]
        //[BoxGroup("Controls")]
        private float m_maxSnapSpeed = 5f;

        [SerializeField]
        //[MinValue(0f)]
        //[BoxGroup("Controls")]
        private float m_groundProbeDistance = 1f;

        [SerializeField]
        //[BoxGroup("Controls")]
        private LayerMask m_groundProbeLayerMask;

        [SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_groundContactCount = 0;

        public bool IsGrounded => m_groundContactCount > 0;

        [SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_steepContactCount = 0;

        public bool IsTouchingSteep => m_steepContactCount > 0;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private Vector2 m_currentVelocity = Vector2.zero;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        //[SerializeField]
        private Vector2 m_desiredVelocity = Vector2.zero;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private Vector2 m_currentGroundNormal = Vector2.zero;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private Vector2 m_currentSteepNormal = Vector2.zero;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_stepsSinceLastGrounded = 0;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_stepsSinceLastJump = 0;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_jumps = 0;

        //[SerializeField]
        //[Min(0f)]
        //private float m_cameraLookSpeed = 1f;

        //[SerializeField]
        //[Min(0f)]
        //private float m_cameraLookDistance = 1f;

        private Vector2 m_movementInput = Vector2.zero;
        //private Vector2 m_aimInput = Vector3.zero;
        private bool m_jumpInput = false;

        private static readonly Quaternion CW_ROT = Quaternion.Euler(0f, 0f, -90f);
        private static readonly Quaternion ACW_ROT = Quaternion.Euler(0f, 0f, 90f);

        public Vector2 Down => Gravity.GetTotalGravityForce(m_rigidbody.position/*m_gravityForce == null ? -1 : m_gravityForce.GUID*/).normalized;
        public Vector2 Up => -Down;
        public Vector2 Right => ACW_ROT * Down;
        public Vector2 Left => CW_ROT * Down;

        #region Unity Callbacks

        private void OnValidate()
        {
            m_transform = transform;
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
        }

        private void OnEnable()
        {
            if (m_input == null)
                m_input = new CharacterControllerInputs();

            m_input.Enable();

            m_input.Player.Move.performed -= OnMoveInput;
            m_input.Player.Move.performed += OnMoveInput;

            m_input.Player.Move.canceled -= OnMoveInput;
            m_input.Player.Move.canceled += OnMoveInput;

            m_input.Player.Jump.performed -= OnJumpInput;
            m_input.Player.Jump.performed += OnJumpInput;

            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);

            m_transform = transform;
        }

        private void OnDisable()
        {
            m_input.Disable();
        }

        private void FixedUpdate()
        {
            UpdateState();

            AdjustVelocity();

            m_rigidbody.SetRotation(Quaternion.FromToRotation(Vector2.up, Up));
            m_rigidbody.angularVelocity = 0f;

            m_currentVelocity += (m_gravityForce.CurrentGravity * Time.fixedDeltaTime) / m_rigidbody.mass;
            m_rigidbody.velocity = m_currentVelocity;

            ResetState();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(3f, m_transform.position, m_transform.position + (Vector3)Right * 20f);

            Handles.color = Color.green;
            Handles.DrawAAPolyLine(3f, m_transform.position, m_transform.position + (Vector3)Up * 20f);
        }
#endif

        #endregion

        private void UpdateState()
        {
            ++m_stepsSinceLastGrounded;
            ++m_stepsSinceLastJump;

            m_currentVelocity = m_rigidbody.velocity;

            if (IsGrounded || SnapToGround() || CheckSteepContacts())
            {
                m_stepsSinceLastGrounded = 0;

                if (m_stepsSinceLastJump > 1)
                    m_jumps = 0;
            }
            else
            {
                m_currentGroundNormal = Up;
            }
        }

        private void ResetState()
        {
            m_jumpInput = false;
            m_groundContactCount = 0;
            m_steepContactCount = 0;
            m_currentGroundNormal = Vector2.zero;
            m_currentSteepNormal = Vector2.zero;
        }

        private void AdjustVelocity()
        {
            if (m_jumpInput)
                TryJump();

            // problem: while airborne, m_desiredVelocity.x goes to zero, so any horizontal movement is quickly removed
            // solution: prevent this velocity adjustment while the character is airborne and there is no input
            if (!IsGrounded && m_desiredVelocity.IsZero())
                return;

            Vector2 xAxis = ProjectDirectionOnLine(Right, m_currentGroundNormal);

            float currentX = Vector2.Dot(m_currentVelocity, xAxis);

            float maxSpeedChange = (IsGrounded ? m_maxAcceleration : m_maxAirborneAcceleration) * Time.deltaTime;

            float newX = Mathf.MoveTowards(currentX, m_desiredVelocity.x, maxSpeedChange);

            m_currentVelocity += xAxis * (newX - currentX);
        }

        /// <summary>
        /// To be called in fixed update if a jump is desired. Attempts to add upward velocity to the rigidbody, of sufficient magnitude to match the set jump height.
        /// </summary>
        private void TryJump()
        {
            m_jumpInput = false;

            Vector2 jumpDirection;

            if (IsGrounded)
            {
                jumpDirection = m_currentGroundNormal;
            }
            else if (IsTouchingSteep)
            {
                jumpDirection = m_currentSteepNormal;
                m_jumps = 0;
            }
            else if (m_maxAirJumps > 0 && m_jumps <= m_maxAirJumps)
            {
                m_jumps = Mathf.Max(1, m_jumps);

                jumpDirection = m_currentGroundNormal;
            }
            else
            {
                return;
            }

            ++m_jumps;

            m_stepsSinceLastJump = 0;

            float jumpSpeed = Mathf.Sqrt(2f * m_gravityForce.CurrentGravity.magnitude * m_jumpHeight);

            // this line introduces an upward bias to jumps, which improves wall jumping
            jumpDirection = (jumpDirection + Up).normalized;

            // how much does the calculated jump direction line up with the current velocity?
            // (does not include gravity now, which might break this)
            //float alignedSpeed = Vector3.Dot(m_currentVelocity, jumpDirection);

            //if (alignedSpeed > 0f)
            //    jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);

            // remove any down velocity
            m_currentVelocity -= Vector2.Dot(m_currentVelocity, Down) * Down;

            // add jump velocity!
            m_currentVelocity += jumpDirection * jumpSpeed;
        }

        /// <summary>
        /// Keep the player 'glued' to the ground when not jumping and below a certain speed.
        /// </summary>
        private bool SnapToGround()
        {
            if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
                return false;

            float speed = m_currentVelocity.magnitude;

            if (speed > m_maxSnapSpeed)
                return false;

            RaycastHit2D hit = Physics2D.Raycast(m_rigidbody.position, Down, m_groundProbeDistance, m_groundProbeLayerMask, -Mathf.Infinity);

            if (hit.collider == null || Vector2.Dot(Up, hit.normal) < m_minGroundDotProduct)
                return false;

            // we have just left the ground. set the velocity to snap us back down

            m_groundContactCount = 1;
            m_currentGroundNormal = hit.normal;

            float dot = Vector2.Dot(m_currentVelocity, hit.normal);

            // only do this when dot product is > 0, else it would slow us down not speed us up towards the ground
            if (dot > 0f)
                m_currentVelocity = (m_currentVelocity - m_currentGroundNormal * dot).normalized * speed;

            return true;
        }

        /// <summary>
        /// This method converts walls being touched into "virtual ground" in case th player ends up caught between walls with no ground below them.
        /// </summary>
        private bool CheckSteepContacts()
        {
            if (m_steepContactCount > 1 && Vector2.Dot(Up, m_currentSteepNormal) >= m_minGroundDotProduct)
            {
                m_groundContactCount = 1;
                m_currentGroundNormal = m_currentSteepNormal;
                return true;
            }

            return false;
        }

        #region Collision Detection

        private void OnCollisionStay2D(Collision2D collision) => EvaluateCollision(collision);
        private void OnCollisionEnter2D(Collision2D collision) => EvaluateCollision(collision);

        private void EvaluateCollision(Collision2D collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector2 normal = collision.GetContact(i).normal;
                float upDot = Vector2.Dot(Up, normal);

                if (upDot >= m_minGroundDotProduct)
                {
                    ++m_groundContactCount;
                    m_currentGroundNormal += normal;
                }
                else if (upDot > -0.01f)
                {
                    ++m_steepContactCount;
                    m_currentSteepNormal += normal;
                }
            }

            if (m_groundContactCount > 0)
                m_currentGroundNormal /= m_groundContactCount;

            if (m_steepContactCount > 0)
                m_currentSteepNormal /= m_steepContactCount;
        }

        #endregion

        #region Input Events

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            m_jumpInput = true;
        }

        private void OnMoveInput(InputAction.CallbackContext context)
        {
            m_movementInput = context.ReadValue<Vector2>();
            m_desiredVelocity = m_movementInput * m_speed;
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Given a direction vector, project it so that it's aligned with whatever
        /// tangent line the character is standing on.
        /// </summary>
        private static Vector2 ProjectDirectionOnLine(Vector2 direction, Vector2 normal) =>
            (direction - normal * Vector2.Dot(direction, normal)).normalized;

        #endregion
    }
}