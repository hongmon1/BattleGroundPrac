using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 숨을 수 있는 엄폐물이 없다면 가만히 서있지만 새로운 엄폐물이 있고 엄폐물보다 가깝다면 엄폐물을 변경
/// 총알 장전도 해준다.
/// </summary>
/// 
[CreateAssetMenu(menuName = "PluggableAI/Actions/Find Cover")]
public class FindCoverAction : Action
{
    public override void OnReadyAction(StateController controller)
    {
        controller.focusSight = false;
        controller.enemyAnimation.AbortPendingAim();
        controller.enemyAnimation.anim.SetBool(FC.AnimatorKey.Crouch, false);

        ArrayList nextCoverData = controller.coverLookUp.GetBestCoverSpot(controller);
        Vector3 potentialCover = (Vector3)nextCoverData[1];
        //의미없는 값이면(없으면)
        if (Vector3.Equals(potentialCover, Vector3.positiveInfinity))
        {
            controller.nav.destination = controller.transform.position;//가만히 있음
            return;
        }
        //현재 엄폐물보다 좋은 위치의 엄폐물이 있으면
        //타겟과 더 가까운 엄폐물
        //그리고 제일 좋은 엄폐물(제일 가까이 있는)
        else if((controller.personalTarget - potentialCover).sqrMagnitude < (controller.personalTarget - controller.CoverSpot).sqrMagnitude 
            && !controller.IsNearOtherSpot(potentialCover, controller.nearRadius))
        {
            controller.coverHash = (int)nextCoverData[0];
            controller.CoverSpot = potentialCover;
        }
        controller.nav.destination = controller.CoverSpot;
        controller.nav.speed = controller.generalStats.evadeSpeed;

        controller.variables.currentShots = controller.variables.shotsInRounds;//재장전
    }

    public override void Act(StateController controller)
    {
        //딱히 여기서 할 거 없음
    }
}
