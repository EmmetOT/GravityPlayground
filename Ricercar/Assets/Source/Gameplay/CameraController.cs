using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using NaughtyAttributes;
using GravityPlayground.GravityStuff;

namespace GravityPlayground
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class CameraController : Singleton<CameraController>
    {
        [SerializeField]
        //[ShowIf("IsFollowingTransform")]
        private Transform m_followTransform;
        
        [SerializeField]
        //[MinValue(0f)]
        private float m_followSpeed = 1f;
        
        [SerializeField]
        [HideInInspector]
        private Camera m_camera;

        public static Camera Camera => Instance.m_camera ?? (Instance.m_camera = Instance.GetComponent<Camera>());
        
        private Transform m_transform;

        [SerializeField]
        private bool m_rotateWithGravity = false;

        [SerializeField]
        //[MinValue(0f)]
        //[ShowIf("m_rotateWithGravity")]
        private float m_rotationSpeed = 1f;

        [SerializeField]
        private bool m_runInEditMode = false;

        private void Reset()
        {
            m_camera = GetComponent<Camera>();
        }

        private void Awake()
        {
            m_transform = transform;
        }

        private void LateUpdate()
        {
            if (!m_followTransform)
                return;

            if (!Application.isPlaying && !m_runInEditMode)
                return;

            Vector3 pos = m_followTransform.position;
            Vector2 gravity = Gravity.GetTotalGravityForce(pos);
            
            Vector3 currentPos = m_transform.position;
            Vector3 targetPos = pos.SetZ(currentPos.z);

            m_transform.position = Vector3.Lerp(currentPos, targetPos, m_followSpeed * Time.smoothDeltaTime);

            if (m_rotateWithGravity)
            {
                if (gravity.IsZero())
                    gravity = Vector2.down;

                Camera.transform.rotation = Quaternion.Slerp(Camera.transform.rotation, Quaternion.LookRotation(Vector3.forward, -gravity.normalized), m_rotationSpeed * Time.deltaTime);
            }
        }
    }

}