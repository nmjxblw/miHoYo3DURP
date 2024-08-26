using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayerControl : MonoBehaviour
{
    [Header("Input System信息")]
    public Vector2 inputDirection;
    public float lastInputDirectionMagnitude;
    public Vector3 moveForwardDirection;
    public InputControl inputControl => InputManager.Instance.inputControl;
    public float rotationVelocity = 0f;
    public float RotationSmoothTime = 0.12f;
    [Header("玩家信息")]
    public UnityEvent OnJumpEvent;
    public LayerMask groundLayer = 1 << 3;
    public UnityEvent OnEvadeEvent;

    [Header("能量相关")]
    public float fullEnergy;
    public float currentEnergy;
    public float deltaEnergyPreSecond;
    public float energyPercentage => currentEnergy / fullEnergy;
    public float energyRecoverPreSecond = 10f;
    public float fastRunEnergyConsumePreSecond = -10f;
    public float evadeEnergyConsume = -15f;
    public float jumpEnergyConsume = -10f;
    public EnergyUI energyUI;
    [Header("死亡相关")]
    public bool isDead = false;
    private int animIsDeadHash;
    public GameObject restartPanel;
    [Header("受伤相关")]
    public bool isHurt = false;
    public Health health;
    public int hurtLayerIndex;
    private int animLightHitHash;
    private int animHeavyHitHash;
    public bool unstoppable = false;
    public bool invincible = false;
    [Header("子弹相关")]
    [SerializeField] private int _maxBullet = 3;
    private GameObject playerBullet;
    private Transform rightMuzzle;
    private Transform leftMuzzle;
    private BulletUI bulletUI;
    public int maxBullet => _maxBullet;
    public int rightPistolBulletCount;
    public int leftPistolBulletCount;
    public UnityEvent OnBulletChangedEvent;
    public Coroutine rightGunAutoReloadCoroutine;
    public float rightReloadTimer = 0;
    public Coroutine leftGunAutoReloadCoroutine;
    public float leftReloadTimer = 0;
    public float rightFullRemaining => Mathf.Min((rightPistolBulletCount * autoReloadTime + (autoReloadTime - rightReloadTimer)) / (maxBullet * autoReloadTime), 1f);
    public float leftFullRemaining => Mathf.Min((leftPistolBulletCount * autoReloadTime + (autoReloadTime - leftReloadTimer)) / (maxBullet * autoReloadTime), 1f);
    public UnityEvent OnNotEnoughBulletEvent;
    public float autoReloadTime = 5f;
    [Header("技能相关")]
    public bool isSkill = false;
    public bool isSkillActivable = true;
    public float skillRemaining => Mathf.Min(rightFullRemaining, leftFullRemaining);
    private int animSkillHash;
    [Header("连招相关")]
    public bool atk1ComboPlayable = true;
    public bool atk2ComboPlayable = true;
    private int inputAtkHash;
    private int animAtk1Hash;
    private int animAtk2Hash;
    [Header("锁敌设置")]
    [Tooltip("索敌模式中和相机共用同一个目标，非索敌模式中单独计算。")]
    public Transform traceTarget;
    public float maxTraceDistance = 35f;
    [Header("移动设定")]

    public float walkSpeed = 0.125f;
    public float runSpeed = 2.2f;
    public float fastRunSpeed = 4.8f;
    private Animator animator;
    private float horizontal;
    public Vector3 localAnimVelocity = new Vector3();
    [HideInInspector] public Rigidbody rb;
    [HideInInspector]

    public float inputDirectionMagnitude;
    private int animHorizontalInputMagnitudeHash;
    [HideInInspector] public bool isFastRun = false;
    [SerializeField]
    private float targetSpeed;
    private float currentSpeed;
    private int animSpeedHash;
    [Header("跳跃设定")]
    public bool isJump = false;
    private float vertical;
    public bool isGrounded = false;
    private int animIsGroundedHash;
    private int animVelocityYHash;
    private int animJumpFixedHash;
    private int animJumpHash;
    private int jumpLayerIndex;
    [Header("闪避设定")]
    public bool isEvade = false;
    private int animEvadeFixedHash;
    private int animEvadeHash;
    [HideInInspector] public int factor;
    [SerializeField] private int currentEvade;
    private int evadeLayerIndex;
    [Header("攻击设定")]
    public bool isAttack = false;

    private int animEmptyHash;
    private int attackLayerIndex;
    [Header("终极技能相关")]
    public float fullUltCharge = 100f;
    public float currentUltCharge = 0f;
    public float deltaUltChargePreSecond = 1f;
    public float lightHitCharge = 5f;
    public float heavyHitCharge = 10f;
    public float ultChargePercentage => currentUltCharge / fullUltCharge;
    private int animUltHash;
    public bool isUlt = false;
    public bool isUltActivable = false;
    private Coroutine secondCoroutine;
    public void Awake()
    {
        OnJumpEvent.AddListener(Jump);
        OnEvadeEvent.AddListener(Evade);
        OnBulletChangedEvent.AddListener(GetComponent<BulletUI>().UpdateUI);
        OnBulletChangedEvent.AddListener(IsSkillActivable);
        OnNotEnoughBulletEvent.AddListener(GetComponent<BulletUI>().RiseNotEnoughBulletPanel);
    }
    public void Start()
    {
        bulletUI = GetComponent<BulletUI>();
        health = GetComponent<Health>();
        health.onDamageTakenEvent.AddListener(HandleTakeDamage);
        health.onDeathEvent.AddListener(OnPlayerDied);
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        playerBullet = Resources.Load<GameObject>("Prefabs/玩家子弹");
        rightMuzzle = transform.Find("Bip001/Bip001 Prop1/Weapon_R/Muzzle");
        leftMuzzle = transform.Find("Bip001/Bip001 Prop2/Weapon_L/Muzzle");
        #region 设置InputControl事件
        inputControl.Gameplay.Jump.started += HandleJumpStarted;
        inputControl.Gameplay.Evade.started += HandleEvadeStarted;
        inputControl.Gameplay.FastRun.started += HandleFastRunStarted;
        inputControl.Gameplay.FastRun.canceled += HandleFastRunCanceled;
        inputControl.Gameplay.Attack1.started += HandleAttack1Started;
        inputControl.Gameplay.Attack1.canceled += HandleAttack1Canceled;
        inputControl.Gameplay.Attack2.started += HandleAttack2Started;
        inputControl.Gameplay.Attack2.canceled += HandleAttack2Canceled;
        inputControl.Gameplay.Skill.started += HandleSkillStarted;
        inputControl.Gameplay.Skill.canceled += HandleSkillCanceled;
        inputControl.Gameplay.Ult.started += HandleUltStarted;
        inputControl.Gameplay.Ult.canceled += HandleUltCanceled;
        inputControl.Gameplay.LockEnemy.started += HandleLockEnemyStarted;
        inputControl.Gameplay.LockEnemy.canceled += HandleLockEnemyCanceled;
        #endregion
        #region 设置弹药
        rightPistolBulletCount = maxBullet;
        leftPistolBulletCount = maxBullet;
        OnBulletChangedEvent?.Invoke();
        #endregion
        currentEnergy = fullEnergy;
        if (energyUI != null)
        {
            energyUI.currentEnergyImage.fillAmount = energyPercentage;
        }
        secondCoroutine = StartCoroutine(ChangePreSecond());
        SetAnimatorInfo();
    }
    public void SetAnimatorInfo()
    {
        animator.SetFloat("乘数因子", 1f / animator.humanScale);
        #region 设置哈希
        animAtk1Hash = Animator.StringToHash("攻击1");
        animAtk2Hash = Animator.StringToHash("攻击2");
        animSkillHash = Animator.StringToHash("六连射击");
        animSpeedHash = Animator.StringToHash("水平速度");
        animHorizontalInputMagnitudeHash = Animator.StringToHash("水平输入");
        animVelocityYHash = Animator.StringToHash("垂直速度");
        animJumpFixedHash = Animator.StringToHash("原地跳");
        animJumpHash = Animator.StringToHash("跳跃");
        animEvadeFixedHash = Animator.StringToHash("闪避固定");
        animEvadeHash = Animator.StringToHash("闪避向前");
        animIsGroundedHash = Animator.StringToHash("在地上");
        animLightHitHash = Animator.StringToHash("被轻击");
        animHeavyHitHash = Animator.StringToHash("被重击");
        animEmptyHash = Animator.StringToHash("空");
        animUltHash = Animator.StringToHash("终极技能");
        animIsDeadHash = Animator.StringToHash("死亡");
        #endregion
        #region 设置层级
        evadeLayerIndex = animator.GetLayerIndex("闪避");
        jumpLayerIndex = animator.GetLayerIndex("跳跃");
        attackLayerIndex = animator.GetLayerIndex("攻击");
        hurtLayerIndex = animator.GetLayerIndex("受伤");
        #endregion
    }
    /// <summary>
    /// 更新函数
    /// </summary> <summary>
    /// 
    /// </summary>
    private void Update()
    {
        inputDirection = inputControl.Gameplay.Move.ReadValue<Vector2>();
        isGrounded = IsGrounded();//地面检测
        RotatePlayer();
        if (inputDirection.magnitude <= 0.1f && lastInputDirectionMagnitude != 0f)
        {
            lastInputDirectionMagnitude = Mathf.Lerp(lastInputDirectionMagnitude, 0f, Time.deltaTime * 15f);
        }
        else
        {
            lastInputDirectionMagnitude = inputDirection.magnitude;
        }
        inputDirectionMagnitude = lastInputDirectionMagnitude;
    }
    #region 旋转玩家代码
    public void RotatePlayer()
    {
        if (inputDirection == Vector2.zero || isEvade || isJump || isSkill || isAttack || isUlt) return;
        float _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.y) * Mathf.Rad2Deg +
                                  Camera.main.transform.eulerAngles.y;

        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref rotationVelocity,
            RotationSmoothTime);
        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        moveForwardDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
    }
    #endregion
    #region InputSystem代码 
    public void HandleJumpStarted(InputAction.CallbackContext ctx) => OnJumpEvent?.Invoke();
    public void HandleJumpCanceled(InputAction.CallbackContext ctx) { }
    public void HandleEvadeStarted(InputAction.CallbackContext ctx) => OnEvadeEvent?.Invoke();
    public void HandleEvadeCanceled(InputAction.CallbackContext ctx) { }
    public void HandleFastRunStarted(InputAction.CallbackContext ctx) => isFastRun = !isFastRun;
    public void HandleFastRunCanceled(InputAction.CallbackContext ctx) { }
    public void HandleAttack1Started(InputAction.CallbackContext ctx)
    {
        inputAtkHash = animAtk1Hash;
    }

    public void HandleAttack1Canceled(InputAction.CallbackContext ctx)
    {
        if (atk1ComboPlayable)
        {
            HandleAttackLogic();
        }
        else if (rightPistolBulletCount <= 0 || leftPistolBulletCount <= 0)
        {
            OnNotEnoughBulletEvent?.Invoke();
        }
    }

    public void HandleAttack2Started(InputAction.CallbackContext ctx)
    {
        inputAtkHash = animAtk2Hash;
    }

    public void HandleAttack2Canceled(InputAction.CallbackContext ctx)
    {
        if (atk2ComboPlayable)
        {
            HandleAttackLogic();
        }
        else if (rightPistolBulletCount <= 0 || leftPistolBulletCount <= 0)
        {
            OnNotEnoughBulletEvent?.Invoke();
        }
    }

    public void HandleAttackLogic()
    {
        if (isJump || isEvade || isSkill || isUlt || !isGrounded) return;
        traceTarget = CameraManager.Instance.currentTraceTarget;
        if (traceTarget != null)
        {
            Vector3 direction = traceTarget.position - transform.position;
            direction.y = 0;
            transform.forward = direction;
        }
        InvokeTrigger(inputAtkHash);
    }

    public void HandleSkillStarted(InputAction.CallbackContext ctx)
    {
        IsSkillActivable();
        if (!isSkillActivable)
        {
            OnNotEnoughBulletEvent?.Invoke();
            return;
        }
        if (isAttack) return;
        inputAtkHash = animSkillHash;
        HandleAttackLogic();
    }


    public void HandleSkillCanceled(InputAction.CallbackContext ctx) { }

    public void IsSkillActivable()
    {
        isSkillActivable = (rightPistolBulletCount >= maxBullet && leftPistolBulletCount >= maxBullet);
    }
    public void HandleUltStarted(InputAction.CallbackContext ctx)
    {
        if (!isUltActivable || isJump || isEvade || isSkill || isUlt || !isGrounded || isAttack || isHurt) return;
        UltChangeDelta(-fullUltCharge);
        animator.Play(animUltHash, attackLayerIndex);
    }

    public void HandleUltCanceled(InputAction.CallbackContext ctx) { }

    public void HandleLockEnemyStarted(InputAction.CallbackContext ctx)
    {
        //索敌键被按下时先检测是否为索敌模式
        //如果是索敌模式，则取消
        CameraManager.Instance.traceMode = !CameraManager.Instance.traceMode;
    }
    public void HandleLockEnemyCanceled(InputAction.CallbackContext ctx) { }
    #endregion
    public bool IsGrounded()
    {
        isGrounded = !isJump && Physics.CheckSphere(transform.position, 0.1f, groundLayer);
        return isGrounded;
    }

    public void RightGunShotBullet()
    {
        Quaternion rotation = transform.rotation;
        if (traceTarget != null)
        {
            Vector3 direction = traceTarget.position - transform.position;
            direction.y = 0;
            transform.forward = direction;
            direction = traceTarget.position - rightMuzzle.position;
            direction.y = 0;
            rotation = Quaternion.LookRotation(direction);
        }
        PoolManager.Release(playerBullet, rightMuzzle.position, rotation);
        rightPistolBulletCount = Math.Clamp(rightPistolBulletCount - 1, 0, maxBullet);
        OnBulletChangedEvent?.Invoke();
        if (rightGunAutoReloadCoroutine != null)
            StopCoroutine(rightGunAutoReloadCoroutine);
        rightGunAutoReloadCoroutine = StartCoroutine(RightGunAutoReload());
    }

    public void LeftGunShotBullet()
    {
        Quaternion rotation = transform.rotation;
        if (traceTarget != null)
        {
            Vector3 direction = traceTarget.position - transform.position;
            direction.y = 0;
            transform.forward = direction;
            direction = traceTarget.position - leftMuzzle.position;
            direction.y = 0;
            rotation = Quaternion.LookRotation(direction);
        }
        PoolManager.Release(playerBullet, leftMuzzle.position, rotation);
        leftPistolBulletCount = Math.Clamp(leftPistolBulletCount - 1, 0, maxBullet);
        OnBulletChangedEvent?.Invoke();
        if (leftGunAutoReloadCoroutine != null)
            StopCoroutine(leftGunAutoReloadCoroutine);
        leftGunAutoReloadCoroutine = StartCoroutine(LeftGunAutoReload());
    }

    public IEnumerator RightGunAutoReload()
    {
        rightReloadTimer = autoReloadTime;
        bulletUI.UpdateRightBulletRemainingTime(rightReloadTimer / autoReloadTime);
        while (rightReloadTimer > 0f)
        {
            rightReloadTimer = Mathf.Clamp(rightReloadTimer - Time.deltaTime, 0f, autoReloadTime);
            bulletUI.UpdateRightBulletRemainingTime(rightReloadTimer / autoReloadTime);
            yield return null;
        }
        RightGunBulletReload();
    }

    public IEnumerator LeftGunAutoReload()
    {
        leftReloadTimer = autoReloadTime;
        bulletUI.UpdateLeftBulletRemainingTime(leftReloadTimer / autoReloadTime);
        while (leftReloadTimer > 0f)
        {
            leftReloadTimer = Mathf.Clamp(leftReloadTimer - Time.deltaTime, 0f, autoReloadTime);
            bulletUI.UpdateLeftBulletRemainingTime(leftReloadTimer / autoReloadTime);
            yield return null;
        }
        LeftGunBulletReload();
    }


    public void RightGunBulletReload()
    {
        rightPistolBulletCount = Math.Clamp(rightPistolBulletCount + 1, 0, maxBullet);
        OnBulletChangedEvent?.Invoke();
        if (rightGunAutoReloadCoroutine != null)
            StopCoroutine(rightGunAutoReloadCoroutine);
        bulletUI.UpdateRightBulletRemainingTime(0f);
        if (rightPistolBulletCount < maxBullet)
        {
            rightGunAutoReloadCoroutine = StartCoroutine(RightGunAutoReload());
        }
    }

    public void LeftGunBulletReload()
    {
        leftPistolBulletCount = Math.Clamp(leftPistolBulletCount + 1, 0, maxBullet);
        OnBulletChangedEvent?.Invoke();
        if (leftGunAutoReloadCoroutine != null)
            StopCoroutine(leftGunAutoReloadCoroutine);
        bulletUI.UpdateLeftBulletRemainingTime(0f);
        if (leftPistolBulletCount < maxBullet)
        {
            leftGunAutoReloadCoroutine = StartCoroutine(LeftGunAutoReload());
        }
    }

    public void EnableUnstoppable() => unstoppable = true;

    public void DisableUnstoppable()
    {
        unstoppable = false;
        invincible = false;
    }

    public void OnAnimatorMove()
    {
        animator.SetFloat(animHorizontalInputMagnitudeHash, inputDirectionMagnitude);
        animator.SetBool(animIsGroundedHash, isGrounded);
        SetHorizontalSpeed();
        horizontal = new Vector2(animator.velocity.x, animator.velocity.z).magnitude;
        localAnimVelocity = transform.InverseTransformDirection(animator.velocity);
        rb.velocity = transform.forward * localAnimVelocity.z + transform.right * localAnimVelocity.x + transform.up * rb.velocity.y;
        vertical = rb.velocity.y;
        animator.SetFloat(animVelocityYHash, vertical);
    }

    public void SetHorizontalSpeed()
    {
        if (inputDirectionMagnitude <= 0.1f)
        {
            targetSpeed = 0f;
            isFastRun = false;
        }
        else
        {
            isFastRun = currentEnergy >= 0 ? isFastRun : false;
            targetSpeed = isFastRun ? fastRunSpeed : inputDirectionMagnitude > 0.5f ? runSpeed : walkSpeed;
        }
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 20f);
        animator.SetFloat(animSpeedHash, currentSpeed);
    }

    public void Jump()
    {
        if (!isGrounded || isAttack || isSkill || isJump || isEvade || isHurt || isUlt || isDead || currentEnergy + jumpEnergyConsume < 0) return;
        EnergyChangeDelta(jumpEnergyConsume);
        if (inputDirectionMagnitude < 0.1f)
        {
            animator.CrossFade(animJumpFixedHash, 0.1f, jumpLayerIndex);
            return;
        }
        animator.CrossFade(animJumpHash, 0.1f, jumpLayerIndex);
    }

    public void Evade()
    {
        if (isEvade || isJump || isHurt || isAttack || !isGrounded || isSkill || isUlt || currentEnergy + evadeEnergyConsume < 0) return;
        EnergyChangeDelta(evadeEnergyConsume);
        if (inputDirectionMagnitude < 0.1f)
        {
            currentEvade = animEvadeFixedHash;
            animator.CrossFade(animEvadeFixedHash, 0.1f, evadeLayerIndex);
            return;
        }
        currentEvade = animEvadeHash;
        animator.CrossFade(animEvadeHash, 0.1f, evadeLayerIndex);
    }

    public void InvokeTrigger(int triggerHash)
    {
        animator.SetTrigger(triggerHash);
    }

    public void HandleTakeDamage(DealDamage damageSource)
    {
        if (invincible) return;
        if (!unstoppable)
        {
            Quaternion cameraRotation = CameraManager.Instance.cinemachineTarget.transform.rotation;
            Vector3 directionToDamageSource = damageSource.transform.position - transform.position;
            directionToDamageSource.y = 0;
            Quaternion rotationToDamageSource = Quaternion.LookRotation(directionToDamageSource);
            transform.rotation = Quaternion.Euler(0, rotationToDamageSource.eulerAngles.y, 0);
            CameraManager.Instance.cinemachineTarget.transform.rotation = cameraRotation;
            animator.Play(animEmptyHash, attackLayerIndex);
            if (damageSource.isHeavyAttack)
            {
                UltChangeDelta(heavyHitCharge);
                animator.Play(animHeavyHitHash, hurtLayerIndex);
            }
            else
            {
                UltChangeDelta(lightHitCharge);
                animator.Play(animLightHitHash, hurtLayerIndex);
            }
        }
        health.HandleHealthChange(damageSource.totalDamage);
    }

    public void OnPlayerDied()
    {
        animator.SetBool(animIsDeadHash, true);
        GameManager.FrameFrozen(3f);
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
        inputControl.asset.FindActionMap("Gameplay", false).Disable();
        Cursor.lockState = CursorLockMode.None;
        inputControl.asset.FindActionMap("UI", false).Enable();
        restartPanel.SetActive(true);
        StopAllCoroutines();
    }
    public IEnumerator ChangePreSecond()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            EnergyChangeDelta(deltaEnergyPreSecond / 10f);
            UltChangeDelta(deltaUltChargePreSecond / 10f);
        }
    }
    public float EnergyChangeDelta(float delta)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + delta, 0f, fullEnergy);
        if (energyUI != null) energyUI.currentEnergyImage.fillAmount = energyPercentage;
        isFastRun = currentEnergy <= 0f ? false : isFastRun;
        deltaEnergyPreSecond = isFastRun ? fastRunEnergyConsumePreSecond : energyRecoverPreSecond;
        return currentEnergy;
    }

    public float UltChangeDelta(float delta)
    {
        currentUltCharge = Mathf.Clamp(currentUltCharge + delta, 0f, fullUltCharge);
        isUltActivable = currentUltCharge >= fullUltCharge;
        return currentEnergy;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }

    public void CallImpactFrame(){
        CameraManager.Instance.ReleaseImpactFrameParticle();
    }
}