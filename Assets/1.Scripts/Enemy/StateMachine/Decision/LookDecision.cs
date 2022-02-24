using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보이는가?(시야 안에)
/// 타겟이 시야가 막히지 않은 상태에서 타겟이 시야각(1/2) 사이에 있는지 판정
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Decisions/Look")]
public class LookDecision : Decision
{
    private bool MyHandleTargets(StateController controller, bool hasTarget, Collider[] targetsInRadius)
    {
        if (hasTarget)
        {
            Vector3 target = targetsInRadius[0].transform.position;//플레이어 위치
            Vector3 dirToTarget = target - controller.transform.position;
            bool inFOVCondition = (Vector3.Angle(controller.transform.forward, dirToTarget) < controller.viewAngle / 2); //FOV 안에 들어오냐

            if(inFOVCondition && !controller.BlockedSight())
            {
                controller.targetInSight = true;
                controller.personalTarget = controller.aimTarget.position;
                return true;
            }
        }

        return false;
    }

    public override bool Decide(StateController controller)
    {
        //결정전에 일단 꺼둠
        controller.targetInSight = false;

        return CheckTargetInRadius(controller, controller.viewRadius, MyHandleTargets);
    }
}
