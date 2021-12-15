using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using VehicleBase.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace VehicleBase.Camera
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        public static ThirdPersonCamera instance;

        [SerializeField] public GameObject CameraEmuObject;
        [SerializeField] public GameObject ElevAxis;

        public Rigidbody Target;

        private CinemachineVirtualCamera cam;

        bool following;

        float targetRotX;
        float targetRotY;

        public float speed = 100;

        public float MaxUpper = 80;
        public float MaxRootRot = 60;

        public float MaxLower = -80;
        public float MinRootRot = -70;

        public float SensX = 3;
        public float SensY = 3;

        public float CameraMoveSpeed;

        [SerializeField] Vector3 Offset;

        public AnimationCurve OrbitBlendCurve;

        public LayerMask ClippingMask;

        private Vector3 InitialPos;
        private Vector3 SpeedRef;
        private RaycastHit CameraRayHit;

        private float EdgeApproach;
        private float ApproachSample;

        private bool menuDisable;

        private void Start()
        {
            InitialPos = CameraEmuObject.transform.localPosition;

            following = true;

            if (instance != null && instance != this)
            {
                Debug.LogWarning("Duplicate PlayerCamera orbits detected, destroying");
                Destroy(gameObject);
            }
            else
            {
                instance = this;
            }

            cam = transform.GetChild(0).GetChild(0).GetComponent<CinemachineVirtualCamera>();

            //Cursor.lockState = CursorLockMode.Locked;

        }

        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.G))
             //   following = !following;

             if (menuDisable)
                 return;

             RotateCamera();

             MoveCamera();

            /*
            if (PlayerInstance.ADS)
            {
                ExtCam.m_Lens.FieldOfView = Mathf.Lerp(ExtCam.m_Lens.FieldOfView, PlayerInstance.PCharacter.Gun1.ZoomFOV, 0.1f);
            }
            else
            {
                ExtCam.m_Lens.FieldOfView = Mathf.Lerp(ExtCam.m_Lens.FieldOfView, 60, 0.1f);
            }
            */
        }

        public static void ToggleActive(bool active)
        {
            if (active)
                CinemachineBrain.SoloCamera = instance.cam;
            else
                CinemachineBrain.SoloCamera = null;
        }
        #region camera motion
        void RotateCamera()
        {
            float X = Input.GetAxis("Mouse X") * SensX * Time.deltaTime;
            float Y = Input.GetAxis("Mouse Y") * SensY * Time.deltaTime;

            EdgeApproach = Mathf.Abs(targetRotY / MaxUpper);
            ApproachSample = OrbitBlendCurve.Evaluate(EdgeApproach);

            float VerticalDamp = ((targetRotY - 3 < MaxUpper && Y < 0) || (targetRotY + 3 > MaxLower && Y < 0))
                ? targetRotY - Y * SensY * (1 - EdgeApproach)
                : targetRotY - Y * SensY;

            targetRotY = Mathf.Clamp(targetRotY - Y, MaxLower, MaxUpper);

            targetRotX += X;

            transform.eulerAngles = new Vector3(0, targetRotX, 0);
            ElevAxis.transform.localEulerAngles = new Vector3(Mathf.Clamp(targetRotY, MinRootRot, MaxRootRot), 0, 0);

            CameraEmuObject.transform.localEulerAngles =
                new Vector3(-(ElevAxis.transform.localEulerAngles.x - targetRotY), 0, 0);
        }

        void MoveCamera()
        {
            if (following)
            {
                if (Target != null)
                    transform.position = Target.transform.TransformPoint(Target.centerOfMass) + Offset;

                // then what the fuck is this checking?
                if (Physics.Raycast(
                    ElevAxis.transform.TransformPoint(InitialPos) - CameraEmuObject.transform.forward * InitialPos.z,
                    -CameraEmuObject.transform.forward, out CameraRayHit, -InitialPos.z, ClippingMask))
                {
                    CameraEmuObject.transform.localPosition =
                        new Vector3(InitialPos.x, InitialPos.y, -CameraRayHit.distance + 1f);
                }
                else
                {
                    CameraEmuObject.transform.localPosition = InitialPos;
                }

                return;
            }

            float updown = 0;

            if (Input.GetKey(KeyCode.Space))
                updown += 1;

            if (Input.GetKey(KeyCode.LeftControl))
                updown -= 1;


            Vector3 move =
                (transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal") +
                 transform.up * (updown)) * speed * Time.deltaTime;

            transform.position += move;

            // check for camera clipping
            if (Physics.Raycast(ElevAxis.transform.TransformPoint(InitialPos), -CameraEmuObject.transform.forward,
                out CameraRayHit, -InitialPos.z, ClippingMask))
            {
                CameraEmuObject.transform.localPosition =
                    new Vector3(InitialPos.x, InitialPos.y, -CameraRayHit.distance + 1f);
            }
            else
            {
                CameraEmuObject.transform.localPosition = InitialPos;
            }
        }

        #endregion
    }
}