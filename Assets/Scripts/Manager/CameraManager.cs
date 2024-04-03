using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class CameraManager : MonoBehaviour
{
    public bool targetInVision = false;
    public InputControl inputControl => InputManager.Instance.inputControl;
    public bool lockCamera = false;

    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70f;
    public float BottomClamp = -30f;
    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    // cinemachine
    [SerializeField] private float _cinemachineTargetYaw;
    [SerializeField] private float _cinemachineTargetPitch;

    private Vector2 look => inputControl.Gameplay.Look.ReadValue<Vector2>();
    public float deltaTimeMultiplier;
    public Transform targetTransform;
    public bool lookAtTarget = false;
    private static CameraManager _instance;
    public static CameraManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CameraManager>();
            }
            return _instance;
        }
    }

    public Camera mainCamera;
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this.gameObject);
        }
        if (mainCamera == null)
        {
            GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    public void LockCamera()
    {
        lockCamera = true;
    }

    public void UnlockCamera()
    {
        lockCamera = false;
    }

    private void LateUpdate()
    {
        if (!lockCamera && !lookAtTarget) { CameraRotation(); }
        if (lookAtTarget) { CameraRotateToTarget(); }
    }

    private void CameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (look.sqrMagnitude >= 0.001f)
        {
            var devices = InputSystem.devices;
            foreach (var device in devices)
            {
                if (device is Mouse)
                {
                    deltaTimeMultiplier = 0.1f;
                }
                if (device is Gamepad)
                {
                    deltaTimeMultiplier = 0.5f;
                }
            }

            _cinemachineTargetYaw += look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch -= look.y * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        while (lfAngle < -180f) lfAngle += 360f;
        while (lfAngle > 180f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void CameraRotateToTarget()
    {
        CinemachineCameraTarget.transform.LookAt(targetTransform);
        Quaternion targetRotation = CinemachineCameraTarget.transform.rotation;
        Vector3 eulerRotation = targetRotation.eulerAngles;
        _cinemachineTargetPitch = eulerRotation.x;
        _cinemachineTargetYaw = eulerRotation.y;
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
        if (look.sqrMagnitude >= 0.001f) { lookAtTarget = false; }
    }

    public void Update()
    {
        if (targetTransform != null) { CheckTargetInVision(); }

    }
    private void CheckTargetInVision()
    {
        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(targetTransform.position);
        bool inViewport = viewportPoint.x >= 0 && viewportPoint.x <= 1 && viewportPoint.y >= 0 && viewportPoint.y <= 1;
        Vector3 direction = targetTransform.position - CinemachineCameraTarget.transform.position;
        RaycastHit hit;
        if (Physics.Raycast(CinemachineCameraTarget.transform.position, direction, out hit))
        {
            Debug.DrawLine(CinemachineCameraTarget.transform.position, hit.point, Color.red);
            targetInVision = hit.transform.CompareTag("Boss") && inViewport;
        }
        if (lookAtTarget) lookAtTarget = targetInVision;
    }
}
