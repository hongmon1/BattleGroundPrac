using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 더블체크를 하는데 근처에 장애물이나 엄폐물이 가깝게 있는지 체크 한번
/// 타겟 목표까지 장애물이나 엄페물이 있는지 체크 한번  
/// 만약 충돌 검출된 충돌체가 플레이어라면 막힌게 없다는 뜻
/// Raycast로 확인 
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Decisions/Clear Shot")]
public class ClearShotDecision : Decision
{
    [Header("Extra Decision")]
    public FocusDecision targetNear; //너무 가까운지 판단

    //지금 클리어 샷인지 판단
    private bool HaveClearShot(StateController controller)
    {
        Vector3 shotOrigin = controller.transform.position + 
            Vector3.up * (controller.generalStats.aboveCoverHeight + controller.nav.radius);//총구 위치 보정
        Vector3 shotDirection = controller.personalTarget - shotOrigin;

        //주변에 엄폐물이 있으면 안맞을테니까
        bool blockedShot = Physics.SphereCast(shotOrigin, controller.nav.radius, shotDirection, 
            out RaycastHit hit, controller.nearRadius, controller.generalStats.coverMask | controller.generalStats.obstacleMask);

        //주변에 장애물 없으면
        if (!blockedShot)
        {
            //직선거리에 장애물 있는가
            blockedShot = Physics.Raycast(shotOrigin, shotDirection, out hit, shotDirection.magnitude, 
                controller.generalStats.coverMask | controller.generalStats.obstacleMask);
            if (blockedShot)
            {
                blockedShot = !(hit.transform.root == controller.aimTarget.root); //막힌게 플레이어임 -> 중간에 장애물없음
            }
        }
        return !blockedShot;
    }

    public override bool Decide(StateController controller)
    {
        //너무 가까우면 clear하게 쏠 수 있으니까
        return targetNear.Decide(controller) || HaveClearShot(controller);
    }
}
