using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class CameraManager : MonoBehaviour
{
    [Header("测试")]
    public bool testOn = false;
    /// <summary>
    /// A public Transform variable used for testing purposes.
    /// </summary>
    public Transform testTransform;
    public GameObject testUI;
    public Vector3 testViewportPoint = new Vector3();
    public Quaternion testLookRotation = new Quaternion();
    public Quaternion cameraRotationPlusLookRotation = new Quaternion();
    public float testYaw = 0f;
    public float cameraRotationPlusLookRotationYaw = 0f;
    public float testPitch = 0f;
    public float cameraRotationPlusLookRotationPitch = 0f;

    [Header("索敌模式")]
    [SerializeField]
    private bool _traceMode = false;

    public bool TraceMode
    {
        get { return _traceMode; }
        set
        {
            if (value)
            {
                UIManager.Instance.UI["TraceUI"].SetActive(true);
                CameraTrace();
            }
            else
            {
                UIManager.Instance.UI["TraceUI"].SetActive(false);
            }
            _traceMode = value;
        }
    }

    [Range(0f, 50f)]
    public float detectRange = 35f;
    public LayerMask enemyLayer = 1 << 9;
    public LayerMask obstacleLayer = 1 << 10;
    public List<Collider> traceColliders = new List<Collider>();
    public List<Transform> traceTransformList = new List<Transform>();
    public Transform currentTraceTarget = null;
    public Vector3 currentTraceTargetViewportPoint = Vector3.zero;

    [Range(0f, 90f)]
    public float yawRange = 30f;
    public float fixYaw = 0f;

    [Range(0f, 90f)]
    public float pitchRange = 15f;

    public float fixPitch = 0f;

    [Header("相机抖动")]
    public CinemachineVirtualCamera cinemachineVirtualCamera;
    public CinemachineImpulseSource impulseSource;
    [Header("相机旋转")]
    public bool freezeCamera = false;
    public InputControl InputControl => InputManager.Instance.inputControl;

    public GameObject cinemachineTarget;
    public float TopClamp = 70f;
    public float BottomClamp = -30f;

    [Tooltip(
        "Additional degress to override the camera. Useful for fine tuning camera position when locked"
    )]
    public float CameraAngleOverride = 0.0f;

    // cinemachine
    [SerializeField]
    private float _cinemachineTargetYaw;

    [SerializeField]
    private float _cinemachineTargetPitch;

    private Vector2 Look => InputControl.Gameplay.Look.ReadValue<Vector2>();
    public float deltaTimeMultiplier;
    [Header("黑白闪")]
    public GameObject impactFrameParticle;
    private Coroutine smoothRotateCoroutine = null;
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
    }

    public void Start()
    {
        impulseSource = impulseSource != null ? impulseSource : Camera.main.gameObject.GetComponent<CinemachineImpulseSource>();
    }

    public void FreezeCamera()
    {
        freezeCamera = true;
    }

    public void UnfreezeCamera()
    {
        freezeCamera = false;
    }

    private void LateUpdate()
    {
        //相机冻结
        if (freezeCamera)
            return;
        //相机非冻结
        CameraRotation();
    }

    private void CameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (Look.sqrMagnitude >= 0.001f)
        {
            if (smoothRotateCoroutine != null)
            {
                StopCoroutine(smoothRotateCoroutine);
            }
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
            _cinemachineTargetYaw += Look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch -= Look.y * deltaTimeMultiplier;
        }
        if (TraceMode)
        {
            if (
                currentTraceTargetViewportPoint.x >= 1f
                || currentTraceTargetViewportPoint.x <= 0f
                || currentTraceTargetViewportPoint.y >= 1f
                || currentTraceTargetViewportPoint.y <= 0f
            )
            {
                TraceMode = false;
                currentTraceTarget = null;
                return;
            }
            Vector3 direction = currentTraceTarget.position - Camera.main.transform.position;
            Quaternion targetRotation =
                Quaternion.LookRotation(direction)
                * Quaternion.Inverse(Camera.main.transform.rotation);
            float targetYaw = ClampAngle(targetRotation.eulerAngles.y, -180f, 180f);
            float targetPitch = ClampAngle(targetRotation.eulerAngles.x, -180f, 180f);
            if (targetYaw > yawRange)
            {
                fixYaw = targetYaw - yawRange;
            }
            else if (targetYaw < -yawRange)
            {
                fixYaw = targetYaw + yawRange;
            }
            else
            {
                fixYaw = 0f;
            }
            _cinemachineTargetYaw += Mathf.Lerp(
                0,
                fixYaw,
                Time.deltaTime * 10f
            );
            if (targetPitch > pitchRange)
            {
                fixPitch = targetPitch - pitchRange;
            }
            else if (targetPitch < -pitchRange)
            {
                fixPitch = targetPitch + pitchRange;
            }
            else
            {
                fixPitch = 0f;
            }
            // _cinemachineTargetPitch +=  fixPitch;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        cinemachineTarget.transform.rotation = Quaternion.Euler(
            _cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw,
            0.0f
        );
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        while (lfAngle < -180f)
            lfAngle += 360f;
        while (lfAngle > 180f)
            lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void CameraTrace()
    {
        if (smoothRotateCoroutine != null)
        {
            StopCoroutine(smoothRotateCoroutine);
        }
        //检测是否有符合条件的目标
        //没有合适的currentTraceTarget则退出索敌模式。
        if (currentTraceTarget == null)
        {
            TraceMode = false;
            IEnumerator SmoothRotateForward()
            {
                float duration = 0.2f; // 持续时间
                float timer = 0f; // 初始化计时器

                float targetYaw = ClampAngle(
                    cinemachineTarget.transform.root.rotation.eulerAngles.y,
                    -180f,
                    180f
                );
                float targetPitch = ClampAngle(
                    cinemachineTarget.transform.root.rotation.eulerAngles.x,
                    BottomClamp,
                    TopClamp
                );

                // 记录初始的 yaw 和 pitch
                float initialYaw = _cinemachineTargetYaw;
                float initialPitch = _cinemachineTargetPitch;

                while (timer < duration)
                {
                    timer += Time.deltaTime;

                    // 计算插值比例
                    float t = timer / duration;

                    // 使用 Lerp 进行平滑过渡
                    _cinemachineTargetYaw = Mathf.Lerp(initialYaw, targetYaw, t);
                    _cinemachineTargetPitch = Mathf.Lerp(initialPitch, targetPitch, t);

                    yield return null;
                }
                _cinemachineTargetYaw = targetYaw;
                _cinemachineTargetPitch = targetPitch;
            }

            smoothRotateCoroutine = StartCoroutine(SmoothRotateForward());
        }
        else
        {
            IEnumerator SmoothRotateTarget()
            {
                float duration = 0.2f; // 持续时间
                float timer = 0f; // 初始化计时器

                Vector3 direction =
                    currentTraceTarget.position - cinemachineTarget.transform.position;
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                float targetYaw = ClampAngle(targetRotation.eulerAngles.y, -180f, 180f);
                float targetPitch = ClampAngle(targetRotation.eulerAngles.x, BottomClamp, TopClamp);

                // 记录初始的 yaw 和 pitch
                float initialYaw = _cinemachineTargetYaw;
                float initialPitch = _cinemachineTargetPitch;

                while (timer < duration)
                {
                    timer += Time.deltaTime;

                    // 计算插值比例
                    float t = timer / duration;

                    // 使用 Lerp 进行平滑过渡
                    _cinemachineTargetYaw = Mathf.Lerp(initialYaw, targetYaw, t);
                    _cinemachineTargetPitch = Mathf.Lerp(initialPitch, targetPitch, t);

                    yield return null;
                }
                _cinemachineTargetYaw = targetYaw;
                _cinemachineTargetPitch = targetPitch;
            }
            smoothRotateCoroutine = StartCoroutine(SmoothRotateTarget());
        }
    }

    public void Update()
    {
        Test();
        //每帧都更新currentTraceTarget
        if (currentTraceTarget == null)
        {
            currentTraceTarget = DetectEnemy(detectRange);
        }
        if (TraceMode)
        {
            currentTraceTargetViewportPoint = Camera.main.WorldToViewportPoint(currentTraceTarget.position);
            float scale = Mathf.Clamp(20f / Vector3.Distance(currentTraceTarget.position, Camera.main.transform.position), 0.5f, 1f);
            UIManager.Instance.UpdateTraceUIPosition(currentTraceTargetViewportPoint, scale);
        }
        // if (Input.GetKeyDown(KeyCode.T))
        // {
        //     ReleaseImpactFrameParticle();
        // }
    }

    private bool CheckTraceTargetInVision(Transform traceTargetTransform)
    {
        //检测target transform是否在view port中
        Vector3 targetViewportPoint = Camera.main.WorldToViewportPoint(
            traceTargetTransform.position
        );
        bool inViewport =
            targetViewportPoint.x >= 0
            && targetViewportPoint.x <= 1
            && targetViewportPoint.y >= 0
            && targetViewportPoint.y <= 1;
        //检测是否有遮挡物
        Vector3 direction =
            traceTargetTransform.position - cinemachineTarget.transform.position;
        RaycastHit hit;
        if (Physics.Raycast(cinemachineTarget.transform.position, direction, out hit))
        {
            Debug.DrawLine(cinemachineTarget.transform.position, hit.point, Color.red);
            return ((1 << hit.transform.gameObject.layer) & obstacleLayer) == 0 && inViewport;
        }
        return false;
    }

    /// <summary>
    /// 寻找Enemy Layer中，range范围内，名为“TraceTarget”的Transform型，没有的话就返回null
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    public Transform DetectEnemy(float range)
    {
        traceColliders = Physics
            .OverlapSphere(cinemachineTarget.transform.position, range, enemyLayer)
            .ToList();
        traceTransformList = new List<Transform>();
        if (traceColliders.Count > 0)
        {
            for (int i = 0; i < traceColliders.Count; i++)
            {
                if (
                    ((1 << traceColliders[i].gameObject.layer) & enemyLayer) != 0
                    && CheckTraceTargetInVision(traceColliders[i].transform)
                )
                    traceTransformList.Add(traceColliders[i].transform.Find("TraceTarget"));
            }
            if (traceTransformList.Count > 0)
            {
                return traceTransformList[0];
            }
        }
        TraceMode = false;
        return null;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        if (cinemachineTarget != null)
            Gizmos.DrawSphere(cinemachineTarget.transform.position, detectRange);
        if (testTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(testTransform.position, 0.5f);
        }
    }

    public void Test()
    {
        if (!testOn)
            return;
        if (testTransform == null)
            return;
        Vector3 direction = testTransform.position - Camera.main.transform.position;
        testLookRotation = Quaternion.LookRotation(direction);
        cameraRotationPlusLookRotation =
            testLookRotation * Quaternion.Inverse(Camera.main.transform.rotation);
        testYaw = ClampAngle(testLookRotation.eulerAngles.y, -180f, 180f);
        testPitch = ClampAngle(testLookRotation.eulerAngles.x, -180f, 180f);
        cameraRotationPlusLookRotationYaw = ClampAngle(
            cameraRotationPlusLookRotation.eulerAngles.y,
            -180f,
            180f
        );
        cameraRotationPlusLookRotationPitch = ClampAngle(
            cameraRotationPlusLookRotation.eulerAngles.x,
            -180f,
            180f
        );
        testViewportPoint = Camera.main.WorldToViewportPoint(testTransform.position);
        testUI.transform.position = new Vector3(
            testViewportPoint.x * Screen.width,
            testViewportPoint.y * Screen.height,
            0f
        );
    }

    public void ReleaseImpactFrameParticle()
    {
        impactFrameParticle.SetActive(true);
    }
}
