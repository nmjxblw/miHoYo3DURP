using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using Random = UnityEngine.Random;
using UnityEngine.Events;
public class BossAI : MonoBehaviour
{
    [Header("Gizmos设定")]
    public float gizmoRadius = 0.5f;
    #region 设置目标
    [Header("锁敌相关")]
    public GameObject chaseTarget;
    public Transform visionFrom;
    public Vector3 agentDestination;
    public float distanceToAgentDestination;
    public float distanceToChaseTarget;
    public Vector3 agentMoveDirection;
    public float inputMagnitude = 1f;
    public bool isTargetInVision;
    #endregion
    #region 组件声明
    [Header("组件设置")]
    private NavMeshAgent agent;
    private Animator animator;
    private Health health;
    private Rigidbody rb;
    #endregion
    #region 内置参数
    [Header("参数设置")]
    public Vector3 rbVelocity;
    public Vector3 localAnimVelocity;
    public Vector3 steeringTarget;
    public float horizontalMovingSpeed = 0f;
    public float meleeAttackDistance = 6f;
    public float rangeAttackDistance = 15f;
    [Header("决策值")]
    public int randomFactor;
    [Header("能量相关")]
    [SerializeField] private int _fullEnergy = 100;
    public int fullEnergy { get { return _fullEnergy; } }
    public int currentEnergy = 100;
    [Range(0, 100)] public int energyRecoverPreSecond = 10;
    public int normalRunConsumePreSecond = 5;
    public int fastRunConsumePreSecond = 10;
    public int deltaEnergy = 0;
    private Coroutine energyChangeCoroutine;
    [Header("遁藏相关")]
    public GameObject bunkerParent;
    public float burstRecoverEnergyTime = 3f;
    public float burstRecoverEnergyTimer = 0f;
    public int burstRecoverEnergyAmount = 25;
    public float hideOffset = 2.0f;
    [Header("受伤相关")]
    public bool isHurt;
    public bool unstoppable = false;
    public bool invincible = false;
    public DealDamage damageSource;
    [Header("死亡相关")]
    public bool isDead;
    [Header("眩晕相关")]
    public bool isStun;
    public float stunTime = 2f;
    public float stunTimeRemaining;
    [Header("闪避设置")]
    public bool isEvade;
    public bool playerAttackInRange = false;
    public Vector3 checkRegion = new Vector3(0f, 0f, 1f);
    public float checkRegionRadius = 1f;
    public LayerMask playerAttackLayer = 1 << 6;
    [Header("攻击相关")]
    public bool isAttack = false;
    public bool atk1ComboPlayable = true;
    public bool atk2ComboPlayable = true;
    public bool hasMadeDecision = false;
    [Header("技能相关")]
    public float skillCoolDown = 10f;
    public float skillCoolDownRemaining = 0f;
    public bool skillActivable = true;
    public bool isSkill;
    public Transform aimTargetTransform;
    public Transform leftPistolMuzzle;
    public Transform rightPistolMuzzle;
    public GameObject bossBullet;
    [Header("终极技能相关")]
    public float ultCoolDown = 60f;
    public float ultCoolDownRemaining = 0f;
    public int normalUltConsume = 40;
    public int furyUltConsume = 30;
    public int ultEnergyConsume => currentStage == Stage.Fury ? furyUltConsume : normalUltConsume;
    public bool ultActivable = true;
    public bool isUlt;
    #endregion
    #region 其他属性
    [Header("其他属性")]
    #endregion
    [Header("动画相关")]
    #region 动画哈希
    private int animHorizontalSpeedHash;
    private int animInputMagnitudeHash;
    private int animVelocityYHash;
    private int animJumpFixedHash;
    private int animJumpHash;
    private int animEvadeFixedHash;
    private int animEvadeHash;
    private int animIsAtkHash;
    private int animIsGroundedHash;
    private int animHurtHash;
    private int animLightHitHash;
    private int animHeavyHitHash;
    private int animDeadHash;
    private int animStunHash;
    private int animAimHash;
    private int animSixShootHash;
    private int animEmptyHash;
    private int animAtk1Hash;
    private int animAtk2Hash;
    private int animUltHash;
    #endregion
    #region 动画层引索
    private int animBasicLayerIndex;
    private int animAttackLayerIndex;
    private int animHurtLayerIndex;
    private int animEvadeLayerIndex;
    private int animJumpLayerIndex;
    #endregion
    #region 状态机逻辑
    public enum Model
    {
        IDLE = 0,
        Decision = 1,
        Chase = 2,
        Attack = 3,
        Evade = 4,
        Skill = 5,
        Ult = 6,
        Hide = 7,
        Hurt = 8,
        Stun = 9,
        Dead = 10,
    }
    [Header("状态机相关")]
    public Model currentModel = Model.IDLE;
    public UnityEvent OnBattleStartEvent;
    public enum Stage
    {
        Normal = 0,
        Fury = 1
    }
    [Header("战斗阶段")]
    public Stage currentStage = Stage.Normal;
    public float furyThreshold = 1.0f / 3.0f;
    public UnityEvent StageChangeEvent;
    public class State
    {
        protected BossAI ai;
        public State(BossAI ai) { this.ai = ai; }
        public virtual void OnEnter() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnUpdate() { }
        public virtual void PreAnimatorMove() { }
        public virtual void PostAnimatorMove() { }
        public virtual void OnExit() { }
    }
    public class IDLE : State
    {
        public IDLE(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.deltaEnergy = ai.energyRecoverPreSecond;
            if (ai.energyChangeCoroutine == null)
            {
                ai.energyChangeCoroutine = ai.StartCoroutine(ai.EnergyChangePreSecond());
            }
            ai.inputMagnitude = 0f;
            ai.agent.SetDestination(ai.transform.position);
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove() { }
        public override void PostAnimatorMove() { }
        public override void OnUpdate() { }
        public override void OnExit()
        {
            ai.deltaEnergy = 0;
            ai.inputMagnitude = 1f;
            ai.OnBattleStartEvent?.Invoke();
        }
    }
    public class Decision : State
    {
        public Decision(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.agent.isStopped = false;
            ai.randomFactor = Random.Range(0, 100);
            ai.inputMagnitude = ai.randomFactor >= 50 ? 1f : 0.5f;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
            float targetInputMagnitude = ai.randomFactor >= 50 ? 1f : 0.5f;
            targetInputMagnitude = ai.currentStage == Stage.Fury ? 2f * targetInputMagnitude : 1f * targetInputMagnitude;
            ai.inputMagnitude = Mathf.Lerp(ai.inputMagnitude, targetInputMagnitude, Time.deltaTime * 5f);
        }
        public override void PostAnimatorMove()
        {
            if (ai.inputMagnitude >= 2f) { ai.deltaEnergy = -ai.fastRunConsumePreSecond; }
            else if (ai.inputMagnitude >= 1f) { ai.deltaEnergy = -ai.normalRunConsumePreSecond; }
            else { ai.deltaEnergy = ai.energyRecoverPreSecond; }
        }
        public override void OnUpdate()
        {
            //闪避
            if (ai.currentEnergy >= 20 && ai.playerAttackInRange && ai.randomFactor >= 50)
            {
                ai.SwitchState(Model.Evade);
            }
            //当攻击距离超过远程距离，且能量大于50，
            //进入追击模式
            if (ai.distanceToChaseTarget > ai.rangeAttackDistance * 0.9f && ai.currentEnergy >= 50)
            {
                ai.SwitchState(Model.Chase);
            }
            //当能量小于10，
            //进入遁藏模式
            if (ai.currentEnergy < 10)
            {
                ai.SwitchState(Model.Hide);
            }
            if (ai.distanceToChaseTarget > ai.rangeAttackDistance)
            {
                return;
            }
            //当攻击距离小于近战距离，
            //1.当前模式为普通模式，能量大于80，且随机因子大于80
            //2.当前模式为狂暴模式，且能量大于50
            //进入终极技能模式
            if (ai.isTargetInVision && ai.ultActivable && ai.distanceToChaseTarget <= ai.meleeAttackDistance * 0.8f &&
                 ((ai.currentEnergy >= ai.normalUltConsume && ai.randomFactor >= 80) ||
                 (ai.currentEnergy >= ai.furyUltConsume && ai.currentStage == Stage.Fury)))
            {
                ai.SwitchState(Model.Ult);
            }
            //当目标在视线范围内，攻击距离小于远程距离，且当前能量值大于10
            //1.随机因子大于40
            //2.随机因子大于30，当前阶段为狂暴模式
            //进入技能模式
            if (ai.isTargetInVision && ai.distanceToChaseTarget <= ai.rangeAttackDistance && ai.skillActivable && ai.currentEnergy >= 20 &&
             (ai.randomFactor >= 40 || (ai.randomFactor >= 30 && ai.currentStage == Stage.Fury)))
            {
                ai.SwitchState(Model.Skill);
            }
            //当攻击距离小于近战距离，且当前能量值大于20，随机因子大于20
            //进入攻击模式
            if (ai.isTargetInVision && ai.distanceToChaseTarget <= ai.meleeAttackDistance && ai.currentEnergy >= 20)
            {
                ai.SwitchState(Model.Attack);
            }
        }
        public override void OnExit()
        {
            ai.agent.isStopped = true;
            ai.deltaEnergy = 0;
            ai.inputMagnitude = 1f;
        }
    }
    public class Chase : State
    {
        public Chase(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.deltaEnergy = ai.currentStage == Stage.Fury ? ai.fastRunConsumePreSecond : ai.normalRunConsumePreSecond;
            ai.deltaEnergy *= -1;
            ai.agent.isStopped = false;
            ai.inputMagnitude = ai.currentStage == Stage.Fury ? 2f : 1f;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if ((ai.distanceToChaseTarget <= ai.meleeAttackDistance * 1.1f && ai.isTargetInVision) || ai.currentEnergy <= 10)
            {
                ai.SwitchState(Model.Decision);
            }
        }
        public override void OnExit()
        {
            ai.agent.isStopped = true;
            ai.deltaEnergy = 0;
            ai.inputMagnitude = 1f;
        }
    }
    public class Attack : State
    {
        public Attack(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.hasMadeDecision = false;
            ai.deltaEnergy = 0;
            ai.inputMagnitude = 1;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if ((ai.distanceToChaseTarget > ai.meleeAttackDistance || (!ai.atk1ComboPlayable && !ai.atk2ComboPlayable) || !ai.isTargetInVision) && ai.animator.GetCurrentAnimatorStateInfo(ai.animAttackLayerIndex).normalizedTime >= 0.9f)
            {
                ai.SwitchState(Model.Decision);
            }
            if (!ai.hasMadeDecision)
            {
                if (ai.atk2ComboPlayable)
                {
                    ai.randomFactor = Random.Range(0, 100);
                    if (ai.randomFactor >= 50)
                    {
                        ai.hasMadeDecision = true;
                        ai.animator.SetTrigger(ai.animAtk2Hash);
                    }
                    else if (ai.atk1ComboPlayable)
                    {
                        ai.hasMadeDecision = true;
                        ai.animator.SetTrigger(ai.animAtk1Hash);
                    }
                }
                else if (ai.atk1ComboPlayable)
                {
                    ai.hasMadeDecision = true;
                    ai.animator.SetTrigger(ai.animAtk1Hash);
                }
            }
        }
        public override void OnExit()
        {
            if (ai.currentModel == Model.Hurt) return;
            ai.animator.CrossFade(ai.animEmptyHash, 0.01f, ai.animAttackLayerIndex);
        }
    }
    public class Evade : State
    {
        public Evade(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.animator.CrossFade(ai.animEmptyHash, 0.01f, ai.animAttackLayerIndex);
            ai.invincible = true;
            ai.isEvade = true;
            ai.deltaEnergy = 0;
            ai.inputMagnitude = 0;
            ai.EnergyChange(-20);
            ai.animator.CrossFade(ai.animEvadeFixedHash, 0.01f, ai.animEvadeLayerIndex);
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove() { }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (!ai.isEvade) ai.SwitchState(Model.Decision);
        }
        public override void OnExit()
        {
            ai.invincible = false;
            ai.isEvade = false;
        }
    }
    public class Skill : State
    {
        public float targetNotInVisionTimer = 0f;
        public int shootCount = 0;
        public Skill(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            targetNotInVisionTimer = 0;
            ai.skillActivable = false;
            ai.StartCoroutine(ai.SkillCoolDownCoroutine());
            ai.isSkill = true;
            ai.deltaEnergy = 0;
            ai.EnergyChange(-20);
            ai.animator.CrossFade(ai.animSixShootHash, 0.01f, ai.animAttackLayerIndex);
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (!ai.isTargetInVision)
            {
                targetNotInVisionTimer = Mathf.Clamp(targetNotInVisionTimer + Time.deltaTime, 0, 1f);
            }
            else
            {
                targetNotInVisionTimer = 0;
            }
            if (!ai.isSkill || targetNotInVisionTimer >= 1f)
            {
                ai.SwitchState(Model.Decision);
            }
        }
        public override void OnExit()
        {
            ai.inputMagnitude = 0;
            ai.animator.CrossFade(ai.animEmptyHash, 0.01f, ai.animAttackLayerIndex);
        }
    }
    public class Ult : State
    {
        public Ult(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.deltaEnergy = 0;
            ai.inputMagnitude = 0;
            ai.isUlt = true;
            ai.EnergyChange(ai.ultEnergyConsume);
            ai.animator.CrossFade(ai.animUltHash, 0.01f, ai.animAttackLayerIndex);
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (!ai.isUlt)
            {
                ai.SwitchState(Model.Decision);
            }
        }
        public override void OnExit()
        {
            if (ai.currentModel == Model.Hurt) return;
            ai.animator.CrossFade(ai.animEmptyHash, 0.01f, ai.animAttackLayerIndex);
        }
    }

    public class Hide : State
    {
        public Hide(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.burstRecoverEnergyTimer = ai.burstRecoverEnergyTime;
        }
        public override void OnFixedUpdate()
        {
        }
        public override void PreAnimatorMove()
        {
            ai.agent.SetDestination(FindNearestBunkerPosition());
            ai.inputMagnitude = ai.currentEnergy <= 20 || ai.distanceToAgentDestination <= 1f ? 0.5f : 1f;
        }
        public override void PostAnimatorMove()
        {
            ai.deltaEnergy = ai.inputMagnitude <= 0.5f ? 10 : -1 * ai.normalRunConsumePreSecond;
        }
        public override void OnUpdate()
        {
            ai.burstRecoverEnergyTimer = Mathf.Clamp(ai.burstRecoverEnergyTimer - Time.deltaTime, 0, ai.burstRecoverEnergyTime);
            if (ai.burstRecoverEnergyTimer <= 0f)
            {
                ai.burstRecoverEnergyTimer = ai.burstRecoverEnergyTime;
                ai.EnergyChange(ai.burstRecoverEnergyAmount);
            }
            if (ai.currentEnergy >= 75 || (ai.currentEnergy >= 50 && ai.currentStage == Stage.Fury))
            {
                if (ai.isTargetInVision && ai.skillActivable)
                {
                    ai.SwitchState(Model.Skill);
                    return;
                }
                ai.SwitchState(Model.Decision);
            }
        }
        public override void OnExit()
        {
            ai.deltaEnergy = 0;
        }
        public virtual Vector3 FindNearestBunkerPosition()
        {
            Vector3 targetPos = new Vector3();
            float minDistance = float.MaxValue;
            Vector3 simplePos = new Vector3();
            foreach (Transform bunker in ai.bunkerParent.transform)
            {
                simplePos = new Vector3(bunker.position.x, 0, bunker.position.z);
                float tempDistance = Vector3.Distance(simplePos, ai.transform.position);
                if (tempDistance < minDistance)
                {
                    minDistance = tempDistance;
                    targetPos = simplePos;
                }
            }
            Vector3 dir = (targetPos - new Vector3(ai.chaseTarget.transform.position.x, 0, ai.chaseTarget.transform.position.z)).normalized;
            targetPos += dir * ai.hideOffset;
            return targetPos;
        }

    }
    public class Hurt : State
    {
        public Hurt(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.isHurt = true;
            ai.deltaEnergy = 0;
            ai.invincible = true;
            ai.EnergyChange(-10);
            ai.health.HandleHealthChange(ai.damageSource.totalDamage);
            if (ai.currentStage == Stage.Normal && ai.health.currentHealthPercent <= ai.furyThreshold)
            {
                ai.currentStage = Stage.Fury;
                ai.StageChangeEvent?.Invoke();
            }
            if (!ai.unstoppable)
            {
                ai.animator.CrossFade(ai.animEmptyHash, 0.01f, ai.animAttackLayerIndex);
                Vector3 directionToDamageSource = ai.damageSource.transform.position - ai.transform.position;

                Quaternion rotationToDamageSource = Quaternion.LookRotation(directionToDamageSource);

                ai.transform.rotation = Quaternion.Euler(0, rotationToDamageSource.eulerAngles.y, 0);
            }
            else
            {
                return;
            }
            if (ai.damageSource.isHeavyAttack)
            {
                GameManager.FrameFrozen(0.25f);
                ai.animator.CrossFade(ai.animHeavyHitHash, 0f, ai.animHurtLayerIndex);
            }
            else
            {
                GameManager.FrameFrozen(0.1f);
                ai.animator.CrossFade(ai.animLightHitHash, 0f, ai.animHurtLayerIndex);
            }
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (!ai.invincible && !ai.isHurt)
            {
                if (ai.currentEnergy <= 0)
                {
                    ai.SwitchState(Model.Stun);
                    return;
                }
                ai.SwitchState(Model.Decision);
            }
            if (ai.unstoppable && ai.animator.GetCurrentAnimatorStateInfo(ai.animAttackLayerIndex).normalizedTime >= 0.9f)
            {
                ai.SwitchState(Model.Decision);
            }
        }
        public override void OnExit()
        {
            ai.invincible = false;
            ai.isHurt = false;
        }
    }
    public class Stun : State
    {
        public Stun(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.stunTimeRemaining = ai.stunTime;
            ai.isStun = true;
            ai.deltaEnergy = 25;
            ai.animator.SetBool(ai.animStunHash, true);
            ai.agent.isStopped = true;
            ai.inputMagnitude = 0;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            ai.stunTimeRemaining = Mathf.Clamp(ai.stunTimeRemaining - Time.deltaTime, 0, ai.stunTime);
            if (ai.stunTimeRemaining <= 0)
            {
                ai.SwitchState(Model.Decision);
            }
        }
        public override void OnExit()
        {
            ai.animator.SetBool(ai.animStunHash, false);
            ai.deltaEnergy = 0;
            ai.agent.isStopped = false;
            ai.isStun = false;
        }
    }
    public class Dead : State
    {
        public Dead(BossAI ai) : base(ai) { }
        public override void OnEnter()
        {
            ai.isDead = true;
            ai.inputMagnitude = 0;
            ai.deltaEnergy = 0;
            ai.animator.SetBool(ai.animDeadHash, true);
            ai.agent.isStopped = true;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate() { }
        public override void OnExit() { }
    }
    public State currentState;
    public List<State> states;
    #endregion
    private void Awake()
    {
        chaseTarget = chaseTarget == null ? GameObject.FindGameObjectWithTag("Player") : chaseTarget;
        visionFrom = visionFrom == null ? transform.Find("Bip001/LookAtTarget") : visionFrom;
        bunkerParent = bunkerParent == null ? GameObject.FindGameObjectWithTag("Bunker") : bunkerParent;
        bossBullet = bossBullet == null ? Resources.Load<GameObject>("Prefabs/敌人子弹") : bossBullet;
        leftPistolMuzzle = leftPistolMuzzle == null ? transform.Find("Bip001/Bip001 Prop1/Weapon_L 1/Muzzle") : leftPistolMuzzle;
        rightPistolMuzzle = rightPistolMuzzle == null ? transform.Find("Bip001/Bip001 Prop2/Weapon_R 1/Muzzle") : rightPistolMuzzle;
        aimTargetTransform = aimTargetTransform == null ? chaseTarget.transform.Find("AimTarget") : aimTargetTransform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        SetAnimatorHash();
        SetAnimatorLayerIndex();
    }
    private void OnEnable()
    {
        health.onDamageTakenEvent.AddListener(OnTakeDamage);
        health.onDeathEvent.AddListener(HandleDeadEvent);
    }
    public void Start()
    {
        currentEnergy = fullEnergy;
        agent.SetDestination(transform.position);
        states = new List<State>(){
        new IDLE(this),
        new Decision(this),
        new Chase(this),
        new Attack(this),
        new Evade(this),
        new Skill(this),
        new Ult(this),
        new Hide(this),
        new Hurt(this),
        new Stun(this),
        new Dead(this),
    };
        SwitchState(currentModel);
    }

    /// <summary>
    /// 设置动画哈希
    /// </summary> <summary>
    /// 
    /// </summary>
    private void SetAnimatorHash()
    {
        animHorizontalSpeedHash = Animator.StringToHash("水平速度");
        animInputMagnitudeHash = Animator.StringToHash("水平输入");
        animVelocityYHash = Animator.StringToHash("垂直速度");
        animJumpFixedHash = Animator.StringToHash("原地跳");
        animJumpHash = Animator.StringToHash("跳跃");
        animEvadeFixedHash = Animator.StringToHash("闪避固定");
        animEvadeHash = Animator.StringToHash("闪避向前");
        animIsAtkHash = Animator.StringToHash("正在攻击");
        animIsGroundedHash = Animator.StringToHash("在地上");
        animHurtHash = Animator.StringToHash("受伤");
        animLightHitHash = Animator.StringToHash("被轻击");
        animHeavyHitHash = Animator.StringToHash("被重击");
        animDeadHash = Animator.StringToHash("死亡");
        animStunHash = Animator.StringToHash("眩晕");
        animSixShootHash = Animator.StringToHash("六连射击");
        animAimHash = Animator.StringToHash("持枪瞄准");
        animEmptyHash = Animator.StringToHash("空");
        animAtk1Hash = Animator.StringToHash("攻击1");
        animAtk2Hash = Animator.StringToHash("攻击2");
        animUltHash = Animator.StringToHash("终极技能");
    }
    private void SetAnimatorLayerIndex()
    {
        animBasicLayerIndex = animator.GetLayerIndex("基础");
        animAttackLayerIndex = animator.GetLayerIndex("攻击");
        animHurtLayerIndex = animator.GetLayerIndex("受伤");
        animEvadeLayerIndex = animator.GetLayerIndex("闪避");
        animJumpLayerIndex = animator.GetLayerIndex("跳跃");
    }
    private void FixedUpdate()
    {
        currentState?.OnFixedUpdate();
    }
    public void OnAnimatorMove()
    {
        //设置agent目标位置
        currentState?.PreAnimatorMove();
        agentDestination = agent.destination;
        //归一化移动方向
        steeringTarget = agent.steeringTarget;
        agentMoveDirection = agent.steeringTarget - transform.position;
        agentMoveDirection = new Vector3(agentMoveDirection.x, 0, agentMoveDirection.z).normalized;
        //设置transform旋转
        if (Vector3.Distance(agent.steeringTarget, transform.position) > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agentMoveDirection);
            transform.rotation = targetRotation;
        }
        //计算目标距离
        distanceToAgentDestination = Vector3.Distance(agent.destination, transform.position);
        distanceToChaseTarget = Vector3.Distance(chaseTarget.transform.position, transform.position);
        //根据当前目标距离判断是否进入刹车范围
        //如果进入刹车范围，将输入标量设置为0
        float factor = distanceToAgentDestination <= agent.stoppingDistance ? 0f : 1f;
        float targetInputMagnitude = inputMagnitude * factor;
        inputMagnitude = Mathf.Lerp(inputMagnitude, targetInputMagnitude, Time.deltaTime * 5f);
        //更新动画机中输入标量
        animator.SetFloat(animInputMagnitudeHash, inputMagnitude);
        //获得动画机中动画的相对速度
        localAnimVelocity = transform.InverseTransformDirection(animator.velocity);
        horizontalMovingSpeed = new Vector2(animator.velocity.x, animator.velocity.z).magnitude;
        animator.SetFloat(animHorizontalSpeedHash, horizontalMovingSpeed);
        //更新刚体速度
        rb.velocity = transform.forward * localAnimVelocity.z + transform.right * localAnimVelocity.x + transform.up * rb.velocity.y;
        rbVelocity = rb.velocity;
        //设置其他逻辑
        currentState?.PostAnimatorMove();
    }

    public void OnCollisionEnter(Collision other)
    {
        return;
    }

    public void Update()
    {
        Vector3 offset = transform.forward * checkRegion.z + transform.right * checkRegion.x + transform.up * checkRegion.y;
        playerAttackInRange = Physics.CheckSphere(visionFrom.position + offset, checkRegionRadius, playerAttackLayer);
        RaycastHit hit;
        if (Physics.Raycast(visionFrom.position, (chaseTarget.transform.position - transform.position), out hit))
        {
            isTargetInVision = hit.transform == chaseTarget.transform;
        }
        currentState?.OnUpdate();

    }
    public void OnDisable()
    {
        return;
    }

    public void SwitchState(Model model)
    {
        currentState?.OnExit();
        currentModel = model;
        currentState = states[(int)model];
        currentState.OnEnter();
    }

    public void OnTakeDamage(DealDamage damageSource)
    {
        this.damageSource = damageSource;
        if (invincible || isDead) return;
        currentModel = Model.Hurt;
        SwitchState(currentModel);
    }
    [ContextMenu("死亡测试")]
    public void HandleDeadEvent()
    {
        StopAllCoroutines();
        currentModel = Model.Dead;
        SwitchState(currentModel);
        GameManager.FrameFrozen(3f);
    }

    public IEnumerator EnergyChangePreSecond()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            EnergyChange(deltaEnergy);
        }
    }

    public int EnergyChange(int deltaEnergy)
    {
        return currentEnergy = Math.Clamp(currentEnergy + deltaEnergy, 0, fullEnergy);
    }
    public void RightPistolShoot()
    {
        Vector3 direction = (chaseTarget.transform.position - rightPistolMuzzle.position).normalized;
        direction.y = 0;
        Quaternion rotation = Quaternion.LookRotation(direction);
        PoolManager.Release(bossBullet, rightPistolMuzzle.position, rotation);
    }

    public void LeftPistolShoot()
    {
        Vector3 direction = (chaseTarget.transform.position - leftPistolMuzzle.position).normalized;
        direction.y = 0;
        Quaternion rotation = Quaternion.LookRotation(direction);
        PoolManager.Release(bossBullet, leftPistolMuzzle.position, rotation);
    }
    public void EnableUnstopped() => unstoppable = true;
    public void DisableUnstopped()
    {
        unstoppable = false;
        invincible = false;
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(agentDestination, gizmoRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(steeringTarget, gizmoRadius);
        Gizmos.color = Color.green;
        Vector3 offset = transform.forward * checkRegion.z + transform.right * checkRegion.x + transform.up * checkRegion.y;
        Gizmos.DrawSphere(visionFrom.position + offset, checkRegionRadius);
    }
    public void Frozen() => Time.timeScale = 0f;
    public void Unfrozen() => Time.timeScale = 1f;

    public IEnumerator SkillCoolDownCoroutine()
    {
        skillCoolDownRemaining = skillCoolDown;
        while (skillCoolDownRemaining > 0)
        {
            skillCoolDownRemaining = Mathf.Clamp(skillCoolDownRemaining - Time.deltaTime, 0f, skillCoolDown);
            yield return null;
        }
        skillActivable = true;
    }

    public IEnumerator UltCoolDownCoroutine()
    {
        ultCoolDownRemaining = ultCoolDown;
        while (ultCoolDownRemaining > 0)
        {
            ultCoolDownRemaining = Mathf.Clamp(ultCoolDownRemaining - Time.deltaTime, 0f, ultCoolDown);
            yield return null;
        }
        ultActivable = true;

    }

    public void HandlePlayerDied()
    {
        SwitchState(Model.IDLE);
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
        StopAllCoroutines();
    }
}
