using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조건 체크하는 클래스
/// 조건 체크를 위해 특정 위치로부터 원하는 검색 반경에 있는 충돌체 찾아서 그 안에 타겟이 있는지 확인
/// </summary>
public abstract class Decision : ScriptableObject
{
    public abstract bool Decide(StateController controller);

    //Decision 시작하기 전에 한번 초기화 해주는 함수(start와 유사한 역할)
    public virtual void OnEnableDecision(StateController controller) { }

    public delegate bool HandleTargets(StateController controller, bool hasTargets, Collider[] targetInRadius);

    //타겟 찾는거 만듬
    public static bool CheckTargetInRadius(StateController controller, float radius, HandleTargets handleTargets)
    {
        //타겟 죽은 경우는 고려 안함
        if (controller.aimTarget.root.GetComponent<HealthBase>().IsDead)
        {
            return false;
        }
        else
        {
            //컨트롤러 위치 중심으로 원하는 반경 에서 컨트롤러 타겟 마스크를 넘김, 플레이어가 있는ㄴ지 확인
            Collider[] targetsInRadius =
                Physics.OverlapSphere(controller.transform.position, radius, controller.generalStats.targetMask);
            return handleTargets(controller, targetsInRadius.Length > 0, targetsInRadius); //0보다 크면 하나라도 있는거, 충돌체 정보가 delegate 함수에 들어감
        }
    }
}
