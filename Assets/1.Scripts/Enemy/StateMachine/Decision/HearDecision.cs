using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// alertCheck를 통해 경고를 들었거나(총소리ㅏㄱ 들렸거나)
/// 특정거리에서 시야가 막혀있어도 특정위치에서 타겟의 위치치가 여러번 인지되었을 경우
/// 어들었는지 판단
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Decisions/Hear")]
public class HearDecision : Decision
{
    private Vector4 lastPos, currentPos;

    public override void OnEnableDecision(StateController controller)
    {
        lastPos = currentPos = Vector3.positiveInfinity;//의미없는 값으로 초기화

        //currentPos : 마지막으로 타겟의 위치를 인지한 위치
    }

    private bool MyHandleTargets(StateController controller, bool hasTarget, Collider[] targetInHearRadius)
    {
        if (hasTarget)
        {
            currentPos = targetInHearRadius[0].transform.position;

            if(!Equals(lastPos, Vector3.positiveInfinity))
            {
                if(!Equals(lastPos, currentPos))
                {
                    controller.personalTarget = currentPos;
                    return true; //뭔가 들렸다고 인지
                }
            }
            lastPos = currentPos;
        }
        return false;
    }

    public override bool Decide(StateController controller)
    {
        if (controller.variables.hearAlert)
        {
            controller.variables.hearAlert = false;
            return true;
        }
        else
        {
            return CheckTargetInRadius(controller, controller.perceptionRadius, MyHandleTargets);
        }
    }
}
