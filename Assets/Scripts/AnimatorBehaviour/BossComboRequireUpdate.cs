using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class BossComboRequireUpdate : StateMachineBehaviour
{
    public BossAI ai;
    public bool hasBranch = false;
    public ComboRequire[] comboRequireList = new ComboRequire[2];
    [Serializable]
    public class ComboRequire
    {
        public int energyRequire = 0;
    }
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ai = animator.GetComponent<BossAI>();
        ai.hasMadeDecision = false;
        int currentEnergy = ai.currentEnergy;
        ai.atk1ComboPlayable = currentEnergy >= comboRequireList[0].energyRequire;
        ai.atk2ComboPlayable = false;
        if (hasBranch)
            ai.atk2ComboPlayable = currentEnergy >= comboRequireList[1].energyRequire;
    }


    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ai.atk1ComboPlayable = true;
        ai.atk2ComboPlayable = true;
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
