using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엄폐물에 숨어있는동안 뭐할지
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Actions/Take Cover")]
public class TakeCoverAction : Action
{
    private readonly int coverMin = 2;
    private readonly int coverMax = 5;

    public override void OnReadyAction(StateController controller)
    {
        controller.variables.feelAlert = false;
        controller.variables.waitInCoverTime = 0f;
        //엄폐물 있음
        if(!Equals(controller.CoverSpot, Vector3.positiveInfinity))
        {
            //랜덤시간동안 엄폐하기
            controller.enemyAnimation.anim.SetBool(FC.AnimatorKey.Crouch, true);
            controller.variables.coverTime = Random.Range(coverMin, coverMax);
        }
        //없음
        else
        {
            controller.variables.coverTime = 0.1f;
        }
    }

    //캐릭터 바라보게 회전
    private void Rotating(StateController controller)
    {
        //너무 작은 값이면 lookrotation에서 오류남
        Vector3 dirToVector = controller.personalTarget - controller.transform.position;
        if(dirToVector.sqrMagnitude < 0.001f || dirToVector.sqrMagnitude > 1000000.0f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(dirToVector);
        if(Quaternion.Angle(controller.transform.rotation, targetRotation) > 5f)
        {
            controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    public override void Act(StateController controller)
    {
        if (!controller.reloading)
        {
            //대기시간 누적
            controller.variables.waitInCoverTime += Time.deltaTime;
        }
        controller.variables.blindEngageTimer += Time.deltaTime;
        if (controller.enemyAnimation.anim.GetBool(FC.AnimatorKey.Crouch))
        {
            Rotating(controller);
        }
    }
}
