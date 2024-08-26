using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDealDamage : DealDamage
{
    public BossAI bossAI;
    protected virtual void OnEnable()
    {
        bossAI = bossAI ?? GameObject.FindWithTag("Boss").GetComponent<BossAI>();
        base.deltaDamage = bossAI.currentStage == BossAI.Stage.Fury ? 5 : 0;
    }
}
