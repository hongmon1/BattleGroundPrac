using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟이 멀리 있고 엄폐물에서 최소 한 타임정도는 공격을 기다린 후에 다음 장애물로 이동할지 판단
/// 똑똑할지 말지도 랜덤하게 작동
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Decisions/Advance Cover")]
public class AdvanceCoverDecision : Decision
{
    public int waitRounds = 1;// 다음 장애물까지 얼마나 고민할거냐

    //타겟이 가까우면 다른 장애물로 갈 필요 없음. 
    //다른 decision 가짐
    [Header("Extra Decision")]
    [Tooltip("플레이어가 가까이 있는지 판단")]
    public FocusDecision targetNear;

    public override void OnEnableDecision(StateController controller)
    {
        controller.variables.waitRounds += 1;

        //다음 장애물 얻을지 말지 랜덤하게 판단
        controller.variables.advanceCoverDecision = Random.Range(0f, 1f) < controller.classStats.ChangeCoverChance / 100f; //똑똑함을 확률로

    }

    public override bool Decide(StateController controller)
    {
        if (controller.variables.waitRounds <= waitRounds)
        {
            return false;
        }

        controller.variables.waitRounds = 0;
        return controller.variables.advanceCoverDecision && !targetNear.Decide(controller);

    }

}
