using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// navMashAgent에서 남은 거리가 멈추는 중일 정도로 얼마 남지 않았거나, 경로를 검색중이 아니라면 true
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Decisions/Reached Point")]
public class ReachedPointDecision : Decision
{
    public override bool Decide(StateController controller)
    {
        //navmeshagent는 게임이 안돌아가면 오류남
        //예외처리
        if(Application.isPlaying == false)
        {
            return false;
        }

        if(controller.nav.remainingDistance <= controller.nav.stoppingDistance &&
            !controller.nav.pathPending)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
