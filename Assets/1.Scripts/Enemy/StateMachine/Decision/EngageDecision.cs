using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟이 보이거나 근처에 있으면 교전 대기 시간을 초기화하고
/// 반대로 보이지 않거나 멀어져 있거나하면 blindEngageTime(타깃은 인지, 사격은 못함, 찾는 시간) 만큼 기다ㄹ릴건지 판단? 
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Decisions/Engage")]
public class EngageDecision : Decision
{
    [Header("Extra Decision")]
    public LookDecision isViewing;
    public FocusDecision targetNear;

    public override bool Decide(StateController controller)
    {
        //보이거나 가까움
        if (isViewing.Decide(controller) || targetNear.Decide(controller))
        {
            //교전 대기 초기화
            controller.variables.blindEngageTimer = 0;
        }
        //교전 대기 시간을 넘김
        else if(controller.variables.blindEngageTimer >= controller.blinedEngageTime)
        {
            //시간 초기화
            controller.variables.blindEngageTimer = 0;
            return false; //전투 끝남
        }
        return true; //계속 전투
    }
}
