using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엄폐물로 복귀
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Actions/Return To Cover")]
public class ReturnToCoverAction : Action
{
    public override void OnReadyAction(StateController controller)
    {
        //의미없는 위치가 아니라면
        if(!Equals(controller.CoverSpot, Vector3.positiveInfinity))
        {
            controller.nav.destination = controller.CoverSpot;
            controller.nav.speed = controller.generalStats.chaseSpeed;
            //거리가 조금 있으면
            if (Vector3.Distance(controller.CoverSpot, controller.transform.position) > 0.5f)
            {
                controller.enemyAnimation.AbortPendingAim(); //사격 중지
            }
        }
        else
        {
            controller.nav.destination = controller.transform.position; //제자리에 서있기
        }
    }

    public override void Act(StateController controller)
    {
        //현재 Coverspot 위치가 아닐때
        if(!Equals(controller.CoverSpot, controller.transform.position))
        {
            controller.focusSight = false;
        }
    }
}
