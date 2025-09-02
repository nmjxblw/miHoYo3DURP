using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BossHealthDisplayUI : HealthDisplayUI
{
    public TextMeshProUGUI bossName;
    public TextMeshProUGUI bossStage;
    public BossAI ai;
    public override void Awake()
    {
        base.Awake();
        bossName.text = "Boss:Shadow";
        ai = health.GetComponent<BossAI>();
        bossStage.text = "BattleStage:FirstStage";
        ai.StageChangeEvent.AddListener(UpdateBossStage);
    }

    public void UpdateBossStage()
    {
        bossStage.text = "BattleStage:SecondStage";
        bossStage.color = Color.yellow;
    }
}
