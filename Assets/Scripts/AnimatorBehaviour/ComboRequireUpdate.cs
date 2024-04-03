using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ComboRequireUpdate : StateMachineBehaviour
{
    public bool hasBranch = false;
    public ComboRequire[] comboRequireList = new ComboRequire[2];
    [Serializable]
    public class ComboRequire
    {
        public int leftPistolRequireBullet = 0;
        public int rightPistolRequireBullet = 0;
    }
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int leftPistolBulletCount = animator.GetComponent<PlayerControl>().leftPistolBulletCount;
        int rightPistolBulletCount = animator.GetComponent<PlayerControl>().rightPistolBulletCount;
        animator.GetComponent<PlayerControl>().atk1ComboPlayable = (leftPistolBulletCount >= comboRequireList[0].leftPistolRequireBullet)
            && (rightPistolBulletCount >= comboRequireList[0].rightPistolRequireBullet);
        animator.GetComponent<PlayerControl>().atk2ComboPlayable = false;
        if (hasBranch)
            animator.GetComponent<PlayerControl>().atk2ComboPlayable = (leftPistolBulletCount >= comboRequireList[1].leftPistolRequireBullet)
                && (rightPistolBulletCount >= comboRequireList[1].rightPistolRequireBullet);
    }


    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<PlayerControl>().atk1ComboPlayable = true;
        animator.GetComponent<PlayerControl>().atk2ComboPlayable = true;
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
