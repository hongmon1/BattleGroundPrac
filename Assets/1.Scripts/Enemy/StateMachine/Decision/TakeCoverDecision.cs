using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장애물로 이동할 수 있는 상황인지 아닌지 판단
/// 쏴야할 총알이 남아있거나, 엄폐물로 이동하기 전에 대기 시간이 남아 있거나
/// 근처에 숨을만한 엄폐물이 없을 경우는 실패  
/// 그외에는 그냥 엄폐물로 이동 : True  
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Decisions/Take Cover")]
public class TakeCoverDecision : Decision
{
    public override bool Decide(StateController controller)
    {
        //지금 쏴야할 총알이 남아있거나, 대기 시간이 더 필요하거나, 엄폐물 위츠를 못찾았다면 false
        if(controller.variables.currentShots<controller.variables.shotsInRounds ||
            controller.variables.waitInCoverTime>controller.variables.coverTime ||
            Equals(controller.CoverSpot, Vector3.positiveInfinity))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
