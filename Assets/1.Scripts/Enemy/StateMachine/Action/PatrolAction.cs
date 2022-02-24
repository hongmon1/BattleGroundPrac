using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Waypoint 돌면서 정찰
/// </summary>
/// 
[CreateAssetMenu(menuName = "PluggableAI/Actions/Patrol")]
public class PatrolAction : Action
{
    public override void OnReadyAction(StateController controller)
    {
        //타게팅 해제
        controller.enemyAnimation.AbortPendingAim();
        controller.enemyAnimation.anim.SetBool(FC.AnimatorKey.Crouch, false);
        controller.personalTarget = Vector3.positiveInfinity;
        controller.CoverSpot = Vector3.positiveInfinity;
    }

    private void Patrol(StateController controller)
    {
        if(controller.patrolWaypoints.Count == 0)
        {
            return;
        }

        controller.focusSight = false;
        controller.nav.speed = controller.generalStats.patrolSpeed; //조금 느리게

        //멈춰있으면
        if(controller.nav.remainingDistance <= controller.nav.stoppingDistance && !controller.nav.pathPending)
        {
            controller.variables.patrolTimer += Time.deltaTime;
            //타이머 초과하면 새 웨이포인트 인덱스 업데이트
            if (controller.variables.patrolTimer >= controller.generalStats.patrolWaitTime)
            {
                controller.wayPointIndex = (controller.wayPointIndex + 1) % controller.patrolWaypoints.Count;
                controller.variables.patrolTimer = 0;
            }
        }

        //새 웨이포인트로 이동
        try
        {
            controller.nav.destination = controller.patrolWaypoints[controller.wayPointIndex].position;
        }
        catch (UnassignedReferenceException)
        {
            Debug.LogWarning("웨이포인트가 없어요 세팅해주세요.", controller.gameObject);
            controller.patrolWaypoints = new List<Transform>
            {
                controller.transform //임의로 하나 넣어줌
            };
            controller.nav.destination = controller.transform.position;
        }
    }

    public override void Act(StateController controller)
    {
        Patrol(controller);
    }
}
