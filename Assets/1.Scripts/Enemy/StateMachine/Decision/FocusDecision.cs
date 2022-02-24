using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 타게팅 디시젼
/// 거리체크 디시젼
/// 인지 타입에 따라 특정 거리로부터 가깝진 않지만 시야는 막히지 않았지만 위험요소를 감지했거나
/// 너무 가까운 거리에 타겟(플레이어)이 있는 지 판단
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Decisions/Focus")]
public class FocusDecision : Decision
{
    //3가지 타입

    public enum Sense
    {
        NEAR, //완전 근처
        PERCEPTION, //인지 범위
        VIEW, //볼 수 있는 거리
    }

    [Tooltip("어떤 크기로 위험요소 감지를 하겠습니까?")]
    public Sense sense;
    [Tooltip("현재 엄폐물을 해제 할까요?")]
    public bool invalidateCoverSpot;

    private float radius;//sense에 따른 범위

    public override void OnEnableDecision(StateController controller)
    {
        switch (sense)
        {
            case Sense.NEAR:
                radius = controller.nearRadius;
                break;
            case Sense.PERCEPTION:
                radius = controller.perceptionRadius;
                break;
            case Sense.VIEW:
                radius = controller.viewRadius;
                break;
            default:
                Debug.Log("타입 범위를 넘어감");
                radius = controller.nearRadius;
                break;
        }
    }

    private bool MyHandleTargets(StateController controller, bool hasTarget, Collider[] targetsInHearRadius)
    {
        //타겟이 존재하고 시야가 막히지 않았으면
        if(hasTarget && !controller.BlockedSight())
        {
            //엄폐물이 없음
            if (invalidateCoverSpot)
            {
                //엄폐물 날려버리기
                controller.CoverSpot = Vector3.positiveInfinity;
            }
            controller.targetInSight = true;
            controller.personalTarget = controller.aimTarget.position;
            return true; //이 디시젼을 갖고있는 것 중에 true인 state로 변하겠다 -> 타겟이 있는 스테이트로 가겠다
        }
        return false;
    }

    public override bool Decide(StateController controller)
    {
        //완전 가깝지 않은 상태에서 시야가 막히지 않은 상태로 경고를 느꼈거나
        //타겟이 있음(범위 안에)
        return (sense != Sense.NEAR && controller.variables.feelAlert && !controller.BlockedSight())
            || Decision.CheckTargetInRadius(controller, radius, MyHandleTargets);
    }
}
