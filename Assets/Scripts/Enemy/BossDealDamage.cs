using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDealDamage : DealDamage
{
    public BossAI bossAI;
    protected virtual void OnEnable()
    {
        bossAI = bossAI ?? GameObject.FindWithTag("Boss").GetComponent<BossAI>();
        bossAI.StageChangeEvent.AddListener(HandleStageChange);
    }

    public virtual void HandleStageChange()
    {
        base.deltaDamage += 5;
    }
}
