using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using Random = UnityEngine.Random;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine.Rendering.PostProcessing;
/// <summary>
/// Boss 的AI处理部分
/// </summary>
/// <remarks>包括状态、决策以及动画管理</remarks>
public class BossAI : MonoBehaviour
{
    /// <summary>
    /// 启用AI标识符
    /// </summary>
    [Header("启动设定")]
    [DisplayName("启用AI标识符")]
    public bool activable = true;
    /// <summary>
    /// Gizmos范围设置
    /// </summary>
    [Header("Gizmos设定")]
    public float gizmoRadius = 0.5f;
    #region 设置目标
    /// <summary>
    /// 索敌目标
    /// </summary>
    /// <remarks>比如玩家</remarks>
    [Header("锁敌相关")]
    public GameObject chaseTarget;
    /// <summary>
    /// AI的视角源
    /// </summary>
    public Transform visionFrom;
    /// <summary>
    /// AI代理的终点
    /// </summary>
    /// <remarks>Vector3表示</remarks>
    public Vector3 agentDestination;
    /// <summary>
    /// AI代理到终点坐标的直线距离
    /// </summary>
    public float distanceToAgentDestination;
    /// <summary>
    /// AI代理到锁敌目标的直线距离
    /// </summary>
    public float distanceToChaseTarget;
    /// <summary>
    /// AI代理的移动方向
    /// </summary>
    public Vector3 agentMoveDirection;
    /// <summary>
    /// 控制器输入的大小
    /// </summary>
    public float inputMagnitude = 1f;
    /// <summary>
    /// 当前状态的控制器输入的最终大小
    /// </summary>
    public float currentStateTargetInputMagnitude = 1f;
    /// <summary>
    /// 索敌目标是否在视线范围内标识符
    /// </summary>
    public bool isTargetInVision;
    /// <summary>
    /// AI行走时控制器输入的大小
    /// </summary>
    public const float walkInputMagnitude = 0.5f;
    /// <summary>
    /// AI奔跑时控制器输入的大小
    /// </summary>
    public const float runInputMagnitude = 1f;
    /// <summary>
    /// AI冲刺时控制器输入的大小
    /// </summary>
    public const float sprintInputMagnitude = 2f;
    #endregion
    #region 关键部位
    /// <summary>
    /// 头部所在的Transform
    /// </summary>
    [Header("部位标记")]
    public Transform headTopTransform;
    #endregion
    #region 组件声明
    /// <summary>
    /// NavMesh代理
    /// </summary>
    [Header("组件设置")]
    private NavMeshAgent agent;
    /// <summary>
    /// 动画控制器
    /// </summary>
    private Animator animator;
    /// <summary>
    /// 血量组件
    /// </summary>
    private Health health;
    /// <summary>
    /// 刚体组件
    /// </summary>
    private Rigidbody rb;
    #endregion
    #region 内置参数
    /// <summary>
    /// 刚体速度
    /// </summary>
    [Header("参数设置")]
    public Vector3 rbVelocity;
    /// <summary>
    /// 动画速度
    /// </summary>
    public Vector3 localAnimVelocity;
    /// <summary>
    /// AI代理的当前目标朝向
    /// </summary>
    public Vector3 steeringTarget;
    /// <summary>
    /// 代理在地面上的速度
    /// </summary>
    public float onGroundMovingSpeed = 0f;
    /// <summary>
    /// 触发近战攻击的距离
    /// </summary>
    public float meleeAttackDistance = 6f;
    /// <summary>
    /// 触发远程攻击的距离
    /// </summary>
    public float rangeAttackDistance = 15f;
    /// <summary>
    /// 决策因子
    /// </summary>
    [Header("决策相关")]
    public int randomFactor;
    /// <summary>
    /// 非愤怒状态下的决策时间间隔
    /// </summary>
    [Range(0f, 10f)] public float normalDecisionInterval = 5f;
    /// <summary>
    /// 愤怒状态下的决策时间间隔
    /// </summary>
    [Range(0f, 10f)] public float furyDecisionInterval = 2f;
    /// <summary>
    /// 决策时间计时器
    /// </summary>
    public float decisionTimer = 0f;
    /// <summary>
    /// 最大精力值
    /// </summary>
    [Header("精力相关")]
    [SerializeField] private float _fullEnergy = 100f;
    /// <summary>
    /// 最大精力值（只读）
    /// </summary>
    public float FullEnergy { get { return _fullEnergy; } }
    /// <summary>
    /// 当前精力值
    /// </summary>
    public float currentEnergy = 100f;
    /// <summary>
    /// 精力每秒回复量
    /// </summary>
    [Range(0f, 100f)] public float energyRecoverPreSecond = 0f;
    /// <summary>
    /// 能量变化量
    /// </summary>
    public float deltaEnergy = 0f;
    /// <summary>
    /// 精力变化文本
    /// </summary>
    public TextMeshProUGUI energyText;
    /// <summary>
    /// 精力变化协程
    /// </summary>
    private Coroutine energyChangeCoroutine;
    /// <summary>
    /// 掩体组件
    /// </summary>
    [Header("遁藏相关")]
    public GameObject bunkerParent;
    /// <summary>
    /// 最近的掩体位置
    /// </summary>
    public Vector3 nearestBunkerPosition = Vector3.zero;
    /// <summary>
    /// 是否已经到达最近的掩体
    /// </summary>
    public bool isCloseToNearestBunker = false;
    /// <summary>
    /// 靠近掩体的距离阈值
    /// </summary>
    [Range(0f, 20f)] public float closeToBunkerDistance = 5f;
    /// <summary>
    /// 躲藏时间
    /// </summary>
    [Range(0f, 3f)] public float hideDuration = 1f;
    public float hideTimer = 0f;
    public float burstRecoverEnergyAmount = 25f;
    public float hideOffset = 2.0f;
    [Header("受伤相关")]
    public bool isHurt;
    [Range(0f, 100f)] public float hurtEnergyConsume = 10f;
    public bool unstoppable = false;
    public bool invincible = false;
    public DealDamage damageSource;
    [Header("死亡相关")]
    public bool isDead;
    [Header("眩晕相关")]
    public bool isStun;
    [Range(0f, 30f)] public float stunTime = 5f;
    public float stunTimeRemaining;
    [Range(0f, 100f)] public float stunEnergyRecoverPreSecond = 20f;
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
    public float normalQuitStateDelayDuration = 2f;
    public float furyQuitStateDelayDuration = 0.5f;
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
    public bool ultActivable = true;
    public bool isUlt;
    #endregion
    #region 巡逻范围设定
    [Header("巡逻参数设置")]
    [Range(0f, 50f)] public float patrolRange = 5f;
    #endregion
    #region 其他属性
    [Header("其他属性")]
    #endregion
    [Header("动画相关")]
    #region 动画哈希
    private int animOnGroundSpeedHash;
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
    /// <summary>
    /// 状态模式枚举
    /// </summary>
    public enum Model
    {
        /// <summary>
        /// 默认状态
        /// </summary>
        Default = 0,
        /// <summary>
        /// 待机状态
        /// </summary>
        IDLE = 1,
        /// <summary>
        /// 决策状态
        /// </summary>
        Decision = 2,
        /// <summary>
        /// 追逐状态
        /// </summary>
        Chase = 3,
        /// <summary>
        /// 攻击状态
        /// </summary>
        Attack = 4,
        /// <summary>
        /// 躲避状态
        /// </summary>
        Evade = 5,
        /// <summary>
        /// 技能状态
        /// </summary>
        Skill = 6,
        /// <summary>
        /// 终极技能状态
        /// </summary>
        Ult = 7,
        /// <summary>
        /// 躲藏状态
        /// </summary>
        Hide = 8,
        /// <summary>
        /// 受伤状态
        /// </summary>
        Hurt = 9,
        /// <summary>
        /// 昏迷状态
        /// </summary>
        Stun = 10,
        /// <summary>
        /// 死亡状态
        /// </summary>
        Dead = 11,
        /// <summary>
        /// 巡逻状态
        /// </summary>
        Patrol = 12,
    }
    /// <summary>
    /// 当前状态
    /// </summary>
    [Header("状态机相关")]
    public Model currentModel = Model.IDLE;
    /// <summary>
    /// 当前状态提示文本
    /// </summary>
    public TextMeshProUGUI currentModelText;
    /// <summary>
    /// 战斗开始事件
    /// </summary>
    public UnityEvent OnBattleStartEvent;
    /// <summary>
    /// 战斗阶段枚举
    /// </summary>
    public enum BattleStage
    {
        /// <summary>
        /// 第一阶段
        /// </summary>
        FirstStage = 0,
        /// <summary>
        /// 第二阶段
        /// </summary>
        SecondStage = 1
    }
    /// <summary>
    /// 当前战斗阶段
    /// </summary>
    [Header("战斗阶段")]
    public BattleStage currentStage = BattleStage.FirstStage;
    /// <summary>
    /// 战斗阶段切换阈值
    /// </summary>
    [Range(0f, 1f)] public float furyThreshold = 1.0f / 3.0f;
    /// <summary>
    /// 当状态切换时触发的事件
    /// </summary>
    public UnityEvent StageChangeEvent;
    /// <summary>
    /// 状态模板
    /// </summary>
    public class StateTemplate
    {
        /// <summary>
        /// BossAI类实例化
        /// </summary>
        protected BossAI AI { get; set; }
        /// <summary>
        /// 上一个状态模式
        /// </summary>
        protected Model LastModel { get; set; }
        /// <summary>
        /// 是否推出当前状态
        /// </summary>
        public bool QuitState { get; set; } = false;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="AI"></param>
        public StateTemplate(BossAI AI) { this.AI = AI; }
        /// <summary>
        /// 进入当前状态时逻辑
        /// </summary>
        /// <param name="lastModel"></param>
        public virtual void OnEnter(Model lastModel) { QuitState = false; this.LastModel = lastModel; }
        /// <summary>
        /// 固定时间逻辑更新
        /// </summary>
        public virtual void OnFixedUpdate() { if (QuitState) return; }
        /// <summary>
        /// 每帧逻辑更新
        /// </summary>
        public virtual void OnUpdate() { if (QuitState) return; }
        /// <summary>
        /// AnimatorMove前逻辑
        /// </summary>
        public virtual void PreAnimatorMove() { if (QuitState) return; }
        /// <summary>
        /// AnimatorMove后逻辑
        /// </summary>
        public virtual void PostAnimatorMove() { if (QuitState) return; }
        /// <summary>
        /// 退出当前状态时逻辑
        /// </summary>
        public virtual void OnExit() { QuitState = true; }
    }
    #region 待机状态
    /// <summary>
    /// 待机状态类
    /// </summary>
    public class IDLE : StateTemplate
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ai"></param>
        public IDLE(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;
            AI.deltaEnergy = AI.energyRecoverPreSecond;
            if (AI.energyChangeCoroutine == null)
            {
                AI.energyChangeCoroutine = AI.StartCoroutine(AI.EnergyChangePreSecond());
            }
            AI.inputMagnitude = 0f;
            AI.currentStateTargetInputMagnitude = AI.inputMagnitude;
            AI.agent.SetDestination(AI.transform.position);
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove() { }
        public override void PostAnimatorMove() { }
        public override void OnUpdate() { }
        public override void OnExit()
        {
            AI.inputMagnitude = 1f;
            AI.OnBattleStartEvent?.Invoke();
            QuitState = true;
        }
    }
    #endregion
    #region 决策状态
    /// <summary>
    /// 决策状态类
    /// </summary>
    public class Decision : StateTemplate
    {
        public Decision(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;

            AI.agent.isStopped = false;
            AI.randomFactor = Random.Range(0, 100);
            AI.decisionTimer = AI.currentStage == BattleStage.SecondStage ? AI.furyDecisionInterval : AI.normalDecisionInterval;
            AI.decisionTimer *= 1 + Random.Range(-0.1f, 0.1f);
            AI.decisionTimer = (lastModel == Model.Attack || lastModel == Model.Skill || lastModel == Model.Ult) ? AI.decisionTimer : 0f;
        }
        public override void OnFixedUpdate()
        {
            if (QuitState) return;
            AI.decisionTimer -= Time.fixedDeltaTime;
        }
        public override void PreAnimatorMove()
        {
            //判断玩家是否在近战范围内
            // 如果在近战范围内，则将目标地点设置为当前位置
            AI.agent.SetDestination(AI.chaseTarget.transform.position);
            //根据随机因子决定目标输入值
            // 当随机因子小于50，则步行，否则切换为奔跑模式
            AI.currentStateTargetInputMagnitude = AI.randomFactor >= 50 ? BossAI.runInputMagnitude : BossAI.walkInputMagnitude;
            // 根据当前状态决定输入倍率
            // 如果为愤怒模式，则移动速度提升
            // 普通模式，当目标大于0.9倍远程距离时，切换为奔跑。
            // 当距离小于0.8倍近战距离时，不移动
            if (AI.currentStage == BattleStage.SecondStage)
            {
                if (AI.currentStateTargetInputMagnitude == BossAI.runInputMagnitude)
                {
                    AI.currentStateTargetInputMagnitude = BossAI.sprintInputMagnitude;
                }
                else
                {
                    AI.currentStateTargetInputMagnitude = BossAI.runInputMagnitude;
                }
            }
            else if (AI.distanceToChaseTarget >= AI.rangeAttackDistance * 0.9f)
            {
                AI.currentStateTargetInputMagnitude = BossAI.runInputMagnitude;
            }
            if (AI.distanceToChaseTarget < AI.meleeAttackDistance * 0.8f)
            {
                AI.currentStateTargetInputMagnitude = 0f;
            }
        }
        public override void PostAnimatorMove()
        {
            if (QuitState) return;
        }
        public override void OnUpdate()
        {
            if (QuitState) return;
            //闪避
            if (AI.playerAttackInRange && AI.randomFactor >= 50)
            {
                AI.SwitchState(Model.Evade);
                return;
            }
            //当攻击距离超过远程距离
            //进入追击模式
            if (AI.distanceToChaseTarget > AI.rangeAttackDistance)
            {
                AI.SwitchState(Model.Chase);
                return;
            }
            //当能量小于30
            //进入遁藏模式
            if (AI.currentEnergy <= 10f)
            {
                AI.SwitchState(Model.Hide);
                return;
            }
            if (AI.decisionTimer > 0f)
            {
                return;
            }
            //当攻击距离小于近战距离，
            //1.当前模式为普通模式，且随机因子大于80
            //2.当前模式为狂暴模式
            //进入终极技能模式
            if (AI.isTargetInVision && AI.ultActivable && AI.distanceToChaseTarget <= AI.meleeAttackDistance * 0.8f &&
                 (AI.randomFactor >= 80 || AI.currentStage == BattleStage.SecondStage))
            {
                AI.SwitchState(Model.Ult);
                return;
            }
            //当目标在视线范围内，攻击距离小于远程距离
            //1.随机因子大于40
            //2.随机因子大于30，当前阶段为狂暴模式
            //进入技能模式
            if (AI.isTargetInVision && AI.distanceToChaseTarget <= AI.rangeAttackDistance && AI.skillActivable &&
             (AI.randomFactor >= 40 || (AI.randomFactor >= 30 && AI.currentStage == BattleStage.SecondStage) || LastModel == Model.Hide))
            {
                AI.SwitchState(Model.Skill);
                return;
            }

            //当攻击距离小于近战距离
            //进入攻击模式
            if (AI.isTargetInVision && AI.distanceToChaseTarget <= AI.meleeAttackDistance)
            {
                AI.SwitchState(Model.Attack);
                return;
            }
        }
        public override void OnExit()
        {
            AI.agent.isStopped = true;
            QuitState = true;
        }
    }
    #endregion
    #region 追逐状态
    public class Chase : StateTemplate
    {
        public Chase(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;

        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            if (QuitState) return;
            AI.agent.SetDestination(AI.chaseTarget.transform.position);
            AI.currentStateTargetInputMagnitude = AI.currentStage == BattleStage.SecondStage ? BossAI.sprintInputMagnitude : BossAI.runInputMagnitude;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (QuitState) return;
            if ((AI.distanceToChaseTarget <= AI.meleeAttackDistance * 1.2f && AI.isTargetInVision) || AI.currentEnergy <= 10)
            {
                AI.SwitchState(Model.Decision, Model.Chase);
            }
        }
        public override void OnExit()
        {

            QuitState = true;
        }
    }
    #endregion
    #region 攻击状态
    public class Attack : StateTemplate
    {
        bool hasAttacked = false;
        public Attack(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;
            hasAttacked = false;

            AI.hasMadeDecision = false;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            AI.agent.SetDestination(AI.chaseTarget.transform.position);
            AI.currentStateTargetInputMagnitude = AI.currentStage == BattleStage.SecondStage ? BossAI.sprintInputMagnitude : BossAI.runInputMagnitude;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (QuitState) return;
            if ((AI.distanceToChaseTarget > AI.meleeAttackDistance || (!AI.atk1ComboPlayable && !AI.atk2ComboPlayable) || !AI.isTargetInVision) && AI.animator.GetCurrentAnimatorStateInfo(AI.animAttackLayerIndex).normalizedTime >= 0.9f)
            {
                AI.SwitchState(Model.Decision, Model.Attack);
            }
            if (!AI.hasMadeDecision)
            {
                AI.randomFactor = Random.Range(0, 100);
                if (hasAttacked && (AI.randomFactor <= 30 || (AI.randomFactor <= 10 && AI.currentStage == BattleStage.SecondStage)))
                {
                    AI.hasMadeDecision = true;
                    return;
                }
                if (AI.atk2ComboPlayable)
                {
                    if (AI.randomFactor >= 50)
                    {
                        AI.hasMadeDecision = true;
                        hasAttacked = true;
                        AI.animator.SetTrigger(AI.animAtk2Hash);
                        return;
                    }
                }
                if (AI.atk1ComboPlayable)
                {
                    AI.hasMadeDecision = true;
                    hasAttacked = true;
                    AI.animator.SetTrigger(AI.animAtk1Hash);
                }
            }
            else if (
                AI.animator.GetCurrentAnimatorStateInfo(AI.animAttackLayerIndex).normalizedTime >= 0.9f
            )
            {
                AI.SwitchState(Model.Decision, Model.Attack);
            }
        }
        public override void OnExit()
        {
            QuitState = true;

            if (AI.unstoppable) { return; }
            AI.animator.Play(AI.animEmptyHash, AI.animAttackLayerIndex);
        }
    }
    #endregion
    #region 闪避状态
    public class Evade : StateTemplate
    {
        public Evade(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;
            AI.animator.Play(AI.animEmptyHash, AI.animAttackLayerIndex);
            AI.invincible = true;
            AI.isEvade = true;
            AI.inputMagnitude = 0f;
            AI.animator.Play(AI.animEvadeFixedHash, AI.animEvadeLayerIndex);
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove() { AI.currentStateTargetInputMagnitude = 0f; }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (QuitState) return;
            if (!AI.isEvade) AI.SwitchState(Model.Decision, Model.Evade);
        }
        public override void OnExit()
        {

            AI.invincible = false;
            AI.isEvade = false;
            QuitState = true;
        }
    }
    #endregion
    #region 技能状态
    public class Skill : StateTemplate
    {
        public float targetNotInVisionTimer = 0f;
        public int shootCount = 0;
        public Skill(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;
            targetNotInVisionTimer = 0;
            AI.StartCoroutine(AI.SkillCoolDownCoroutine());
            AI.isSkill = true;
            AI.animator.Play(AI.animSixShootHash, AI.animAttackLayerIndex);
        }
        public override void OnFixedUpdate()
        {
            if (QuitState) return;
            if (!AI.isTargetInVision)
            {
                targetNotInVisionTimer = Mathf.Clamp(targetNotInVisionTimer + Time.fixedDeltaTime, 0, 1f);
            }
            else
            {
                targetNotInVisionTimer = 0;
            }
        }
        public override void PreAnimatorMove()
        {
            if (QuitState) return;
            AI.agent.SetDestination(AI.chaseTarget.transform.position);
            AI.currentStateTargetInputMagnitude = BossAI.runInputMagnitude;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (QuitState) return;
            if (!AI.isSkill || targetNotInVisionTimer >= 1f)
            {
                AI.SwitchState(Model.Decision, Model.Skill);
            }
        }
        public override void OnExit()
        {

            AI.inputMagnitude = 0;
            AI.animator.Play(AI.animEmptyHash, AI.animAttackLayerIndex);
            QuitState = true;
        }
    }
    #endregion
    #region 终极技能状态
    public class Ult : StateTemplate
    {
        public Ult(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;
            AI.inputMagnitude = 0f;
            AI.currentStateTargetInputMagnitude = 0f;
            AI.isUlt = true;
            AI.animator.Play(AI.animUltHash, AI.animAttackLayerIndex);
            AI.StartCoroutine(AI.UltCoolDownCoroutine());
        }
        public override void OnFixedUpdate()
        {
        }
        public override void PreAnimatorMove()
        {
            if (QuitState) return;
            AI.agent.SetDestination(AI.chaseTarget.transform.position);
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (QuitState) return;
            if (!AI.isUlt)
            {
                AI.SwitchState(Model.Decision, Model.Ult);
            }
        }
        public override void OnExit()
        {

            QuitState = true;
            if (AI.unstoppable) { return; }
            AI.animator.Play(AI.animEmptyHash, AI.animAttackLayerIndex);

        }
    }
    #endregion
    #region 躲藏状态
    public class Hide : StateTemplate
    {
        bool hided = false;
        public Hide(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;

            hided = false;
            AI.hideTimer = AI.hideDuration;
        }
        public override void OnFixedUpdate()
        {
            if (QuitState) return;
            if (AI.distanceToAgentDestination <= 1f)
                AI.hideTimer -= Time.fixedDeltaTime;
            if (AI.hideTimer <= 0f)
            {
                AI.hideTimer = AI.hideDuration;
                AI.EnergyChange(AI.burstRecoverEnergyAmount);
            }
        }
        public override void PreAnimatorMove()
        {
            if (QuitState) return;
            if (!hided) hided = AI.distanceToAgentDestination <= 1f;
            //寻找最近的掩体
            AI.agent.SetDestination(AI.currentEnergy >= AI.FullEnergy * 4f / 5f && hided ? AI.chaseTarget.transform.position : AI.nearestBunkerPosition);
            // 如果掩体离得很近，则使用步行，否则奔跑接近掩体
            AI.currentStateTargetInputMagnitude = AI.distanceToAgentDestination <= 1f ? BossAI.walkInputMagnitude : BossAI.sprintInputMagnitude;
        }
        public override void PostAnimatorMove()
        {
            if (QuitState) return;
        }
        public override void OnUpdate()
        {
            if (QuitState) return;
            if ((AI.currentEnergy >= AI.FullEnergy * 4f / 5f && AI.isTargetInVision && AI.skillActivable) || AI.currentEnergy == AI.FullEnergy)
            {
                AI.SwitchState(Model.Decision, Model.Hide);
            }
        }
        public override void OnExit()
        {

            QuitState = true;
        }
    }
    #endregion
    #region 受伤状态
    public class Hurt : StateTemplate
    {
        public float hurtTimer = 3f;
        public Hurt(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;

            AI.currentStateTargetInputMagnitude = 0f;
            hurtTimer = 3f;
            AI.isHurt = true;
            AI.invincible = true;
            AI.EnergyChange(-AI.hurtEnergyConsume);
            AI.health.HandleHealthChange(AI.damageSource.totalDamage);
            DamageText damageText = UIPoolManager.Release("DamageText").GetComponent<DamageText>();
            damageText.followTransform = AI.headTopTransform;
            damageText.tmp.text = $"{AI.damageSource.totalDamage}";
            if (AI.currentStage == BattleStage.FirstStage && AI.health.currentHealthPercent <= AI.furyThreshold)
            {
                AI.currentStage = BattleStage.SecondStage;
                AI.StageChangeEvent?.Invoke();
            }
            if (!AI.unstoppable)
            {
                AI.animator.Play(AI.animEmptyHash, AI.animAttackLayerIndex);
                Vector3 directionToDamageSource = AI.damageSource.transform.position - AI.transform.position;

                Quaternion rotationToDamageSource = Quaternion.LookRotation(directionToDamageSource);

                AI.transform.rotation = Quaternion.Euler(0, rotationToDamageSource.eulerAngles.y, 0);
            }
            else
            {
                return;
            }
            if (AI.damageSource.isHeavyAttack)
            {
                GameManager.FrameFrozen(0.1f);
                AI.animator.Play(AI.animHeavyHitHash, AI.animHurtLayerIndex);
                CameraManager.Instance.impulseSource.GenerateImpulse(new Vector3(0f, -0.1f, 0f));
            }
            else
            {
                AI.animator.Play(AI.animLightHitHash, AI.animHurtLayerIndex);
            }
        }

        public override void OnFixedUpdate()
        {
            if (QuitState) return;
            hurtTimer -= Time.fixedDeltaTime;
        }
        public override void PreAnimatorMove()
        {
            if (QuitState) return;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (QuitState) return;
            if (!AI.invincible && !AI.isHurt)
            {
                if (AI.currentEnergy <= 0)
                {
                    AI.SwitchState(Model.Stun, LastModel);
                    return;
                }
                AI.SwitchState(Model.Decision, LastModel);
            }
            if ((AI.unstoppable && AI.animator.GetCurrentAnimatorStateInfo(AI.animAttackLayerIndex).normalizedTime >= 0.9f) || hurtTimer <= 0f)
            {
                AI.SwitchState(Model.Decision, LastModel);
            }
        }
        public override void OnExit()
        {

            AI.invincible = false;
            AI.isHurt = false;
            QuitState = true;
        }
    }
    #endregion
    #region 眩晕状态
    public class Stun : StateTemplate
    {
        public Stun(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;

            AI.currentStateTargetInputMagnitude = 0f;
            AI.stunTimeRemaining = AI.stunTime;
            AI.isStun = true;
            AI.EnergyChange(AI.FullEnergy);
            AI.animator.SetBool(AI.animStunHash, true);
            AI.agent.isStopped = true;
            AI.inputMagnitude = 0;
        }
        public override void OnFixedUpdate()
        {
            if (QuitState) return;
            AI.stunTimeRemaining -= Time.fixedDeltaTime;
        }
        public override void PreAnimatorMove()
        {
            if (QuitState) return;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (QuitState) return;
            if (AI.stunTimeRemaining <= 0)
            {
                AI.SwitchState(Model.Decision, Model.Stun);
            }
        }
        public override void OnExit()
        {

            AI.animator.SetBool(AI.animStunHash, false);
            AI.agent.isStopped = false;
            AI.isStun = false;
            AI.deltaEnergy = AI.energyRecoverPreSecond;
            QuitState = true;
        }
    }
    #endregion
    #region 死亡状态
    public class Dead : StateTemplate
    {
        public Dead(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;
            AI.currentStateTargetInputMagnitude = 0f;
            AI.isDead = true;
            AI.inputMagnitude = 0f;
            AI.deltaEnergy = 0f;
            AI.animator.SetBool(AI.animDeadHash, true);
            AI.agent.isStopped = true;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate() { }
        public override void OnExit() { }
    }
    #endregion
    #region 巡逻状态
    public class Patrol : StateTemplate
    {
        public Vector3 initialPatrolRegion;
        public Vector3 patrolDestination;
        public Patrol(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            QuitState = false;
            initialPatrolRegion = AI.transform.position;
            patrolDestination = initialPatrolRegion;
            AI.agent.isStopped = true;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            if (QuitState) return;
            Vector2 randomUnitCircle = Random.insideUnitCircle * AI.patrolRange;
            patrolDestination = initialPatrolRegion + new Vector3(randomUnitCircle.x, 0, randomUnitCircle.y);
            AI.currentStateTargetInputMagnitude = BossAI.walkInputMagnitude;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate() { }
        public override void OnExit() { }
    }
    #endregion 
    public StateTemplate currentState;
    public List<StateTemplate> states;
    #endregion 状态机逻辑
    private void Awake()
    {
        chaseTarget = chaseTarget == null ? GameObject.FindGameObjectWithTag("Player") : chaseTarget;
        visionFrom = visionFrom == null ? transform.Find("Bip001/LookAtTarget") : visionFrom;
        bunkerParent = bunkerParent == null ? GameObject.FindGameObjectWithTag("Bunker") : bunkerParent;
        bossBullet = bossBullet == null ? Resources.Load<GameObject>("Prefabs/敌人子弹") : bossBullet;
        leftPistolMuzzle = leftPistolMuzzle == null ? transform.Find("Bip001/Bip001 Prop1/Weapon_L 1/Muzzle") : leftPistolMuzzle;
        rightPistolMuzzle = rightPistolMuzzle == null ? transform.Find("Bip001/Bip001 Prop2/Weapon_R 1/Muzzle") : rightPistolMuzzle;
        aimTargetTransform = aimTargetTransform == null ? chaseTarget.transform.Find("AimTarget") : aimTargetTransform;
        headTopTransform = headTopTransform == null ? transform.Find("HeadTop") : headTopTransform;
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
        currentEnergy = FullEnergy;
        agent.SetDestination(transform.position);
        states = new List<StateTemplate>(){
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


    #region  动画哈希值代码
    /// <summary>
    /// 设置动画哈希
    /// </summary>     
    private void SetAnimatorHash()
    {
        animOnGroundSpeedHash = Animator.StringToHash("水平速度");
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
    #endregion 
    private void FixedUpdate()
    {
        currentState?.OnFixedUpdate();
    }
    #region 根运动处理
    public void OnAnimatorMove()
    {
        //设置agent目标位置
        currentState?.PreAnimatorMove();
        agentDestination = agent.destination;

        //更新数据
        steeringTarget = agent.steeringTarget;
        //归一化移动方向
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
        float tempTargetInputMagnitude = distanceToAgentDestination <= agent.stoppingDistance ? 0f : currentStateTargetInputMagnitude;
        inputMagnitude = Mathf.Lerp(inputMagnitude, tempTargetInputMagnitude, Time.deltaTime);
        //更新动画机中输入标量
        animator.SetFloat(animInputMagnitudeHash, inputMagnitude);
        //获得动画机中动画的相对速度
        localAnimVelocity = transform.InverseTransformDirection(animator.velocity);
        onGroundMovingSpeed = new Vector2(animator.velocity.x, animator.velocity.z).magnitude;
        animator.SetFloat(animOnGroundSpeedHash, onGroundMovingSpeed);
        //更新刚体速度
        rb.velocity = transform.forward * localAnimVelocity.z + transform.right * localAnimVelocity.x + transform.up * rb.velocity.y;
        rbVelocity = rb.velocity;
        //设置其他逻辑
        currentState?.PostAnimatorMove();
    }
    #endregion
    public void OnCollisionEnter(Collision other)
    {
        return;
    }

    public void Update()
    {
        Vector3 offset = transform.forward * checkRegion.z + transform.right * checkRegion.x + transform.up * checkRegion.y;
        playerAttackInRange = Physics.CheckSphere(visionFrom.position + offset, checkRegionRadius, playerAttackLayer);
        if (Physics.Raycast(visionFrom.position, (chaseTarget.transform.position - transform.position), out RaycastHit hit))
        {
            isTargetInVision = hit.transform.root == chaseTarget.transform.root;
        }
        FindNearestBunkerPosition();
        isCloseToNearestBunker = Vector3.Distance(transform.position, nearestBunkerPosition) <= closeToBunkerDistance;
        currentState?.OnUpdate();

    }
    public void OnDisable()
    {
        return;
    }
    /// <summary>
    /// 状态切换
    /// </summary>
    /// <param name="nextModel">目标模式</param>
    /// <param name="lastModel">上一个模式</param> <summary>
    /// 
    /// </summary>
    /// <param name="nextModel"></param>
    /// <param name="lastModel"></param>
    public void SwitchState(Model nextModel, Model lastModel = default(Model))
    {
        if (lastModel == default(Model)) lastModel = currentModel;
        currentState?.OnExit();
        currentModel = nextModel;
        currentState = states[(int)nextModel];
        currentState.OnEnter(lastModel);
    }

    public void OnTakeDamage(DealDamage damageSource)
    {
        this.damageSource = damageSource;
        if (invincible || isDead) return;
        SwitchState(Model.Hurt);
    }
    /// <summary>
    /// AI死亡事件处理
    /// </summary>
    [ContextMenu("死亡测试")]
    public void HandleDeadEvent()
    {
        StopAllCoroutines();
        SwitchState(Model.Dead);
        GameManager.FrameFrozen(3f);
    }

    public IEnumerator EnergyChangePreSecond()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            EnergyChange(deltaEnergy / 10f);
        }
    }

    public float EnergyChange(float deltaEnergy)
    {
        return currentEnergy = Mathf.Clamp(currentEnergy + deltaEnergy, 0f, FullEnergy);
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
        skillActivable = false;
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
        ultActivable = false;
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
        agent.isStopped = true;
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
        StopAllCoroutines();
    }

    public void LateUpdate()
    {
        energyText.text = $"{currentEnergy}/{FullEnergy}";
        currentModelText.text = $"{currentModel}\t{randomFactor}";
    }

    public Vector3 FindNearestBunkerPosition()
    {
        Vector3 targetPos = new Vector3();
        float minDistance = float.MaxValue;
        Vector3 simplePos = new Vector3();
        foreach (Transform bunkerTransform in bunkerParent.transform)
        {
            simplePos = new Vector3(bunkerTransform.position.x, 0f, bunkerTransform.position.z);
            float tempDistance = Vector3.Distance(simplePos, transform.position);
            if (tempDistance < minDistance)
            {
                minDistance = tempDistance;
                targetPos = simplePos;
            }
        }
        Vector3 dir = (targetPos - new Vector3(chaseTarget.transform.position.x, 0, chaseTarget.transform.position.z)).normalized;
        targetPos += dir * hideOffset;
        return nearestBunkerPosition = targetPos;
    }

    public void ResetAllTriggers()
    {
        // 获取Animator的所有参数
        AnimatorControllerParameter[] parameters = animator.parameters;

        // 遍历所有参数
        foreach (AnimatorControllerParameter parameter in parameters)
        {
            // 检查参数类型是否为Trigger
            if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                // 重置触发器
                animator.ResetTrigger(parameter.name);
            }
        }
    }
}
