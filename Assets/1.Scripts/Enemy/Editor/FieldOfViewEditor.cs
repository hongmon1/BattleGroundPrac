using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StateController))]
public class FieldOfViewEditor : Editor
{
    //각도를 백터로, 부채꼴 모양의 IMGUI를 만들기
    Vector3 DirFromAngle(Transform transform, float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }

        //각도로 방향을 만듬
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 
            0f, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    private void OnSceneGUI()
    {
        StateController fov = target as StateController;

        //로딩이 안도ㅒㅆ으면, 예외처리
        if(fov == null || fov.gameObject == null)
        {
            return;
        }

        Handles.color = Color.white;
        //perception area(circle)
        Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360, fov.perceptionRadius);
        //near
        Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360, fov.perceptionRadius * 0.5f);

        Vector3 viewAngleA = DirFromAngle(fov.transform, -fov.viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(fov.transform, fov.viewAngle / 2, false);

        Handles.DrawWireArc(fov.transform.position, Vector3.up, viewAngleA, fov.viewAngle, fov.viewRadius);
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleA * fov.viewRadius);
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleB * fov.viewRadius);

        Handles.color = Color.yellow;
        //총구에서 타겟까지
        if(fov.targetInSight && fov.personalTarget != Vector3.zero)
        {
            Handles.DrawLine(fov.enemyAnimation.gunMuzzle.position, fov.personalTarget);
        }

    }

    


}
