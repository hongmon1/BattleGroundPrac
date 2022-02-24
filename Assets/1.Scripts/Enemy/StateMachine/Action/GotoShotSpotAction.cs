using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격 가능 지점으로 이동
/// 플레이어는 보이는데 유효사격거리가 아닐때
/// 유효사격거리까지 이동
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Actions/GotoShot Spot")]
public class GotoShotSpotAction : Action
{

    public override void OnReadyAction(StateController controller)
    {
        controller.focusSight = false;
        controller.nav.destination = controller.personalTarget;
        controller.nav.speed = controller.generalStats.chaseSpeed;
        controller.enemyAnimation.AbortPendingAim();
    }
    public override void Act(StateController controller)
    {
        
    }
}
