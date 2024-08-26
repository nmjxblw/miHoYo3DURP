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
public class BossAI : MonoBehaviour
{
    [Header("启动设定")]
    public bool activable = true;
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
    public float currentStateTargetInputMagnitude = 1f;
    public bool isTargetInVision;
    public const float walkInputMagnitude = 0.5f;
    public const float runInputMagnitude = 1f;
    public const float sprintInputMagnitude = 2f;
    #endregion
    #region 关键部位
    [Header("部位标记")]
    public Transform headTopTransform;
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
    public float onGroundMovingSpeed = 0f;
    public float meleeAttackDistance = 6f;
    public float rangeAttackDistance = 15f;
    [Header("决策相关")]
    public int randomFactor;
    [Range(0f, 10f)] public float normalDecisionDuration = 5f;
    [Range(0f, 10f)] public float furyDecisionDuration = 2f;
    public float decisionTimer = 0f;
    [Header("精力相关")]
    [SerializeField] private float _fullEnergy = 100f;
    public float fullEnergy { get { return _fullEnergy; } }
    public float currentEnergy = 100f;
    [Range(0f, 100f)] public float energyRecoverPreSecond = 0f;
    public float deltaEnergy = 0f;
    public TextMeshProUGUI energyText;
    private Coroutine energyChangeCoroutine;
    [Header("遁藏相关")]
    public GameObject bunkerParent;
    public Vector3 nearestBunkerPosition = Vector3.zero;
    public bool isCloseToNearestBunker = false;
    [Range(0f, 20f)] public float closeToBunkerDistance = 5f;
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
    public enum Model
    {
        Default,
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
        Patrol = 11,
    }
    [Header("状态机相关")]
    public Model currentModel = Model.IDLE;
    public TextMeshProUGUI currentModelText;
    public UnityEvent OnBattleStartEvent;
    public enum Stage
    {
        Normal = 0,
        Fury = 1
    }
    [Header("战斗阶段")]
    public Stage currentStage = Stage.Normal;
    [Range(0f, 1f)] public float furyThreshold = 1.0f / 3.0f;
    public UnityEvent StageChangeEvent;
    public class State
    {
        protected BossAI ai;
        protected Model lastModel;
        public bool quitState = false;
        public State(BossAI ai) { this.ai = ai; }
        public virtual void OnEnter(Model lastModel) { quitState = false; this.lastModel = lastModel; }
        public virtual void OnFixedUpdate() { if (quitState) return; }
        public virtual void OnUpdate() { if (quitState) return; }
        public virtual void PreAnimatorMove() { if (quitState) return; }
        public virtual void PostAnimatorMove() { if (quitState) return; }
        public virtual void OnExit() { quitState = true; }
    }
    #region 待机状态
    public class IDLE : State
    {
        public IDLE(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;
            ai.deltaEnergy = ai.energyRecoverPreSecond;
            if (ai.energyChangeCoroutine == null)
            {
                ai.energyChangeCoroutine = ai.StartCoroutine(ai.EnergyChangePreSecond());
            }
            ai.inputMagnitude = 0f;
            ai.currentStateTargetInputMagnitude = ai.inputMagnitude;
            ai.agent.SetDestination(ai.transform.position);
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove() { }
        public override void PostAnimatorMove() { }
        public override void OnUpdate() { }
        public override void OnExit()
        {
            ai.inputMagnitude = 1f;
            ai.OnBattleStartEvent?.Invoke();
            quitState = true;
        }
    }
    #endregion
    #region 决策状态
    public class Decision : State
    {
        public Decision(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;

            ai.agent.isStopped = false;
            ai.randomFactor = Random.Range(0, 100);
            ai.decisionTimer = ai.currentStage == Stage.Fury ? ai.furyDecisionDuration : ai.normalDecisionDuration;
            ai.decisionTimer *= 1 + Random.Range(-0.1f, 0.1f);
            ai.decisionTimer = (lastModel == Model.Attack || lastModel == Model.Skill || lastModel == Model.Ult) ? ai.decisionTimer : 0f;
        }
        public override void OnFixedUpdate()
        {
            if (quitState) return;
            ai.decisionTimer -= Time.fixedDeltaTime;
        }
        public override void PreAnimatorMove()
        {
            //判断玩家是否在近战范围内
            // 如果在近战范围内，则将目标地点设置为当前位置
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
            //根据随机因子决定目标输入值
            // 当随机因子小于50，则步行，否则切换为奔跑模式
            ai.currentStateTargetInputMagnitude = ai.randomFactor >= 50 ? BossAI.runInputMagnitude : BossAI.walkInputMagnitude;
            // 根据当前状态决定输入倍率
            // 如果为愤怒模式，则移动速度提升
            // 普通模式，当目标大于0.9倍远程距离时，切换为奔跑。
            // 当距离小于0.8倍近战距离时，不移动
            if (ai.currentStage == Stage.Fury)
            {
                if (ai.currentStateTargetInputMagnitude == BossAI.runInputMagnitude)
                {
                    ai.currentStateTargetInputMagnitude = BossAI.sprintInputMagnitude;
                }
                else
                {
                    ai.currentStateTargetInputMagnitude = BossAI.runInputMagnitude;
                }
            }
            else if (ai.distanceToChaseTarget >= ai.rangeAttackDistance * 0.9f)
            {
                ai.currentStateTargetInputMagnitude = BossAI.runInputMagnitude;
            }
            if (ai.distanceToChaseTarget < ai.meleeAttackDistance * 0.8f)
            {
                ai.currentStateTargetInputMagnitude = 0f;
            }
        }
        public override void PostAnimatorMove()
        {
            if (quitState) return;
        }
        public override void OnUpdate()
        {
            if (quitState) return;
            //闪避
            if (ai.playerAttackInRange && ai.randomFactor >= 50)
            {
                ai.SwitchState(Model.Evade);
                return;
            }
            //当攻击距离超过远程距离
            //进入追击模式
            if (ai.distanceToChaseTarget > ai.rangeAttackDistance)
            {
                ai.SwitchState(Model.Chase);
                return;
            }
            //当能量小于30
            //进入遁藏模式
            if (ai.currentEnergy <= 10f)
            {
                ai.SwitchState(Model.Hide);
                return;
            }
            if (ai.decisionTimer > 0f)
            {
                return;
            }
            //当攻击距离小于近战距离，
            //1.当前模式为普通模式，且随机因子大于80
            //2.当前模式为狂暴模式
            //进入终极技能模式
            if (ai.isTargetInVision && ai.ultActivable && ai.distanceToChaseTarget <= ai.meleeAttackDistance * 0.8f &&
                 (ai.randomFactor >= 80 || ai.currentStage == Stage.Fury))
            {
                ai.SwitchState(Model.Ult);
                return;
            }
            //当目标在视线范围内，攻击距离小于远程距离
            //1.随机因子大于40
            //2.随机因子大于30，当前阶段为狂暴模式
            //进入技能模式
            if (ai.isTargetInVision && ai.distanceToChaseTarget <= ai.rangeAttackDistance && ai.skillActivable &&
             (ai.randomFactor >= 40 || (ai.randomFactor >= 30 && ai.currentStage == Stage.Fury) || lastModel == Model.Hide))
            {
                ai.SwitchState(Model.Skill);
                return;
            }

            //当攻击距离小于近战距离
            //进入攻击模式
            if (ai.isTargetInVision && ai.distanceToChaseTarget <= ai.meleeAttackDistance)
            {
                ai.SwitchState(Model.Attack);
                return;
            }
        }
        public override void OnExit()
        {
            ai.agent.isStopped = true;
            quitState = true;
        }
    }
    #endregion
    #region 追逐状态
    public class Chase : State
    {
        public Chase(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;

        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            if (quitState) return;
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
            ai.currentStateTargetInputMagnitude = ai.currentStage == Stage.Fury ? BossAI.sprintInputMagnitude : BossAI.runInputMagnitude;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (quitState) return;
            if ((ai.distanceToChaseTarget <= ai.meleeAttackDistance * 1.2f && ai.isTargetInVision) || ai.currentEnergy <= 10)
            {
                ai.SwitchState(Model.Decision, Model.Chase);
            }
        }
        public override void OnExit()
        {

            quitState = true;
        }
    }
    #endregion
    #region 攻击状态
    public class Attack : State
    {
        bool hasAttacked = false;
        public Attack(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;
            hasAttacked = false;

            ai.hasMadeDecision = false;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
            ai.currentStateTargetInputMagnitude = ai.currentStage == Stage.Fury ? BossAI.sprintInputMagnitude : BossAI.runInputMagnitude;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (quitState) return;
            if ((ai.distanceToChaseTarget > ai.meleeAttackDistance || (!ai.atk1ComboPlayable && !ai.atk2ComboPlayable) || !ai.isTargetInVision) && ai.animator.GetCurrentAnimatorStateInfo(ai.animAttackLayerIndex).normalizedTime >= 0.9f)
            {
                ai.SwitchState(Model.Decision, Model.Attack);
            }
            if (!ai.hasMadeDecision)
            {
                ai.randomFactor = Random.Range(0, 100);
                if (hasAttacked && (ai.randomFactor <= 30 || (ai.randomFactor <= 10 && ai.currentStage == Stage.Fury)))
                {
                    ai.hasMadeDecision = true;
                    return;
                }
                if (ai.atk2ComboPlayable)
                {
                    if (ai.randomFactor >= 50)
                    {
                        ai.hasMadeDecision = true;
                        hasAttacked = true;
                        ai.animator.SetTrigger(ai.animAtk2Hash);
                        return;
                    }
                }
                if (ai.atk1ComboPlayable)
                {
                    ai.hasMadeDecision = true;
                    hasAttacked = true;
                    ai.animator.SetTrigger(ai.animAtk1Hash);
                }
            }
            else if (
                ai.animator.GetCurrentAnimatorStateInfo(ai.animAttackLayerIndex).normalizedTime >= 0.9f
            )
            {
                ai.SwitchState(Model.Decision, Model.Attack);
            }
        }
        public override void OnExit()
        {
            quitState = true;

            if (ai.unstoppable) { return; }
            ai.animator.Play(ai.animEmptyHash, ai.animAttackLayerIndex);
        }
    }
    #endregion
    #region 闪避状态
    public class Evade : State
    {
        public Evade(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;
            ai.animator.Play(ai.animEmptyHash, ai.animAttackLayerIndex);
            ai.invincible = true;
            ai.isEvade = true;
            ai.inputMagnitude = 0f;
            ai.animator.Play(ai.animEvadeFixedHash, ai.animEvadeLayerIndex);
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove() { ai.currentStateTargetInputMagnitude = 0f; }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (quitState) return;
            if (!ai.isEvade) ai.SwitchState(Model.Decision, Model.Evade);
        }
        public override void OnExit()
        {

            ai.invincible = false;
            ai.isEvade = false;
            quitState = true;
        }
    }
    #endregion
    #region 技能状态
    public class Skill : State
    {
        public float targetNotInVisionTimer = 0f;
        public int shootCount = 0;
        public Skill(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;
            targetNotInVisionTimer = 0;
            ai.StartCoroutine(ai.SkillCoolDownCoroutine());
            ai.isSkill = true;
            ai.animator.Play(ai.animSixShootHash, ai.animAttackLayerIndex);
        }
        public override void OnFixedUpdate()
        {
            if (quitState) return;
            if (!ai.isTargetInVision)
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
            if (quitState) return;
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
            ai.currentStateTargetInputMagnitude = BossAI.runInputMagnitude;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (quitState) return;
            if (!ai.isSkill || targetNotInVisionTimer >= 1f)
            {
                ai.SwitchState(Model.Decision, Model.Skill);
            }
        }
        public override void OnExit()
        {

            ai.inputMagnitude = 0;
            ai.animator.Play(ai.animEmptyHash, ai.animAttackLayerIndex);
            quitState = true;
        }
    }
    #endregion
    #region 终极技能状态
    public class Ult : State
    {
        public Ult(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;
            ai.inputMagnitude = 0f;
            ai.currentStateTargetInputMagnitude = 0f;
            ai.isUlt = true;
            ai.animator.Play(ai.animUltHash, ai.animAttackLayerIndex);
            ai.StartCoroutine(ai.UltCoolDownCoroutine());
        }
        public override void OnFixedUpdate()
        {
        }
        public override void PreAnimatorMove()
        {
            if (quitState) return;
            ai.agent.SetDestination(ai.chaseTarget.transform.position);
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (quitState) return;
            if (!ai.isUlt)
            {
                ai.SwitchState(Model.Decision, Model.Ult);
            }
        }
        public override void OnExit()
        {

            quitState = true;
            if (ai.unstoppable) { return; }
            ai.animator.Play(ai.animEmptyHash, ai.animAttackLayerIndex);

        }
    }
    #endregion
    #region 躲藏状态
    public class Hide : State
    {
        bool hided = false;
        public Hide(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;

            hided = false;
            ai.hideTimer = ai.hideDuration;
        }
        public override void OnFixedUpdate()
        {
            if (quitState) return;
            if (ai.distanceToAgentDestination <= 1f)
                ai.hideTimer -= Time.fixedDeltaTime;
            if (ai.hideTimer <= 0f)
            {
                ai.hideTimer = ai.hideDuration;
                ai.EnergyChange(ai.burstRecoverEnergyAmount);
            }
        }
        public override void PreAnimatorMove()
        {
            if (quitState) return;
            if (!hided) hided = ai.distanceToAgentDestination <= 1f;
            //寻找最近的掩体
            ai.agent.SetDestination(ai.currentEnergy >= ai.fullEnergy * 4f / 5f && hided ? ai.chaseTarget.transform.position : ai.nearestBunkerPosition);
            // 如果掩体离得很近，则使用步行，否则奔跑接近掩体
            ai.currentStateTargetInputMagnitude = ai.distanceToAgentDestination <= 1f ? BossAI.walkInputMagnitude : BossAI.sprintInputMagnitude;
        }
        public override void PostAnimatorMove()
        {
            if (quitState) return;
        }
        public override void OnUpdate()
        {
            if (quitState) return;
            if ((ai.currentEnergy >= ai.fullEnergy * 4f / 5f && ai.isTargetInVision && ai.skillActivable) || ai.currentEnergy == ai.fullEnergy)
            {
                ai.SwitchState(Model.Decision, Model.Hide);
            }
        }
        public override void OnExit()
        {

            quitState = true;
        }
    }
    #endregion
    #region 受伤状态
    public class Hurt : State
    {
        public float hurtTimer = 3f;
        public Hurt(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;

            ai.currentStateTargetInputMagnitude = 0f;
            hurtTimer = 3f;
            ai.isHurt = true;
            ai.invincible = true;
            ai.EnergyChange(-ai.hurtEnergyConsume);
            ai.health.HandleHealthChange(ai.damageSource.totalDamage);
            DamageText damageText = UIPoolManager.Release("DamageText").GetComponent<DamageText>();
            damageText.followTransform = ai.headTopTransform;
            damageText.tmp.text = $"{ai.damageSource.totalDamage}";
            if (ai.currentStage == Stage.Normal && ai.health.currentHealthPercent <= ai.furyThreshold)
            {
                ai.currentStage = Stage.Fury;
                ai.StageChangeEvent?.Invoke();
            }
            if (!ai.unstoppable)
            {
                ai.animator.Play(ai.animEmptyHash, ai.animAttackLayerIndex);
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
                GameManager.FrameFrozen(0.1f);
                ai.animator.Play(ai.animHeavyHitHash, ai.animHurtLayerIndex);
                CameraManager.Instance.impulseSource.GenerateImpulse(new Vector3(0f, -0.1f, 0f));
            }
            else
            {
                ai.animator.Play(ai.animLightHitHash, ai.animHurtLayerIndex);
            }
        }

        public override void OnFixedUpdate()
        {
            if (quitState) return;
            hurtTimer -= Time.fixedDeltaTime;
        }
        public override void PreAnimatorMove()
        {
            if (quitState) return;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (quitState) return;
            if (!ai.invincible && !ai.isHurt)
            {
                if (ai.currentEnergy <= 0)
                {
                    ai.SwitchState(Model.Stun, lastModel);
                    return;
                }
                ai.SwitchState(Model.Decision, lastModel);
            }
            if ((ai.unstoppable && ai.animator.GetCurrentAnimatorStateInfo(ai.animAttackLayerIndex).normalizedTime >= 0.9f) || hurtTimer <= 0f)
            {
                ai.SwitchState(Model.Decision, lastModel);
            }
        }
        public override void OnExit()
        {

            ai.invincible = false;
            ai.isHurt = false;
            quitState = true;
        }
    }
    #endregion
    #region 眩晕状态
    public class Stun : State
    {
        public Stun(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;

            ai.currentStateTargetInputMagnitude = 0f;
            ai.stunTimeRemaining = ai.stunTime;
            ai.isStun = true;
            ai.EnergyChange(ai.fullEnergy);
            ai.animator.SetBool(ai.animStunHash, true);
            ai.agent.isStopped = true;
            ai.inputMagnitude = 0;
        }
        public override void OnFixedUpdate()
        {
            if (quitState) return;
            ai.stunTimeRemaining -= Time.fixedDeltaTime;
        }
        public override void PreAnimatorMove()
        {
            if (quitState) return;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate()
        {
            if (quitState) return;
            if (ai.stunTimeRemaining <= 0)
            {
                ai.SwitchState(Model.Decision, Model.Stun);
            }
        }
        public override void OnExit()
        {

            ai.animator.SetBool(ai.animStunHash, false);
            ai.agent.isStopped = false;
            ai.isStun = false;
            ai.deltaEnergy = ai.energyRecoverPreSecond;
            quitState = true;
        }
    }
    #endregion
    #region 死亡状态
    public class Dead : State
    {
        public Dead(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;
            ai.currentStateTargetInputMagnitude = 0f;
            ai.isDead = true;
            ai.inputMagnitude = 0f;
            ai.deltaEnergy = 0f;
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
    #endregion
    #region 巡逻状态
    public class Patrol : State
    {
        public Vector3 initialPatrolRegion;
        public Vector3 patrolDestination;
        public Patrol(BossAI ai) : base(ai) { }
        public override void OnEnter(Model lastModel)
        {
            quitState = false;
            initialPatrolRegion = ai.transform.position;
            patrolDestination = initialPatrolRegion;
            ai.agent.isStopped = true;
        }
        public override void OnFixedUpdate() { }
        public override void PreAnimatorMove()
        {
            if (quitState) return;
            Vector2 randomUnitCircle = Random.insideUnitCircle * ai.patrolRange;
            patrolDestination = initialPatrolRegion + new Vector3(randomUnitCircle.x, 0, randomUnitCircle.y);
            ai.currentStateTargetInputMagnitude = BossAI.walkInputMagnitude;
        }
        public override void PostAnimatorMove() { }
        public override void OnUpdate() { }
        public override void OnExit() { }
    }
    #endregion 
    public State currentState;
    public List<State> states;
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
    /// 
    #region  动画哈希值代码
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
        RaycastHit hit;
        if (Physics.Raycast(visionFrom.position, (chaseTarget.transform.position - transform.position), out hit))
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
        return currentEnergy = Mathf.Clamp(currentEnergy + deltaEnergy, 0f, fullEnergy);
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
        energyText.text = $"{currentEnergy}/{fullEnergy}";
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
