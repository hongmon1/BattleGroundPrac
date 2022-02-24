using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟이 있다면 타겟까지 이동하지만, 타겟을 잃으면 가만히 서있는다.
/// </summary>
/// 

[CreateAssetMenu(menuName = "PluggableAI/Actions/Search")]
public class SearchAction : Action
{
    public override void OnReadyAction(StateController controller)
    {
        //타겟을 놓친 상태기 때문에 타겟팅 한거 모두 풀어줌
        controller.focusSight = false; //시야 풀기
        controller.enemyAnimation.AbortPendingAim(); //조준 풀기
        controller.enemyAnimation.anim.SetBool(FC.AnimatorKey.Crouch, false);//엄폐물에 숨어있었으면 풀기
        controller.CoverSpot = Vector3.positiveInfinity; //엄폐물 해제
    }

    public override void Act(StateController controller)
    {
        //타겟 못찾음
       if(Equals(controller.personalTarget, Vector3.positiveInfinity))
        {
            controller.nav.destination = controller.transform.position;
        }
       //찾음
        else
        {
            controller.nav.speed = controller.generalStats.chaseSpeed; //쫒는 속도로
            controller.nav.destination = controller.personalTarget;
        }
    }
}
