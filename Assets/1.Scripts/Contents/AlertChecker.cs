using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적이 플레이어가 총을 쏜 것을 알아차리기 위해
/// 경고를 발생시킴
/// </summary>
public class AlertChecker : MonoBehaviour
{
    [Range(0,50)]public float alertRadius;//경고 범위
    public int extraWaves = 1;//경고 웨이브 수

    public LayerMask alertMask = FC.TagAndLayer.LayerMasking.Enemy;//경고를 보낼 마스크
    private Vector3 current;
    private bool alert;

    private void Start()
    {
        {
            InvokeRepeating("PingAlert", 1, 1);//pingalert 반복
        }
    }

    private void AlertNearBy(Vector3 origin, Vector3 target, int wave = 0)
    {
        if(wave > this.extraWaves)
        {
            return;
        }
        Collider[] targetsInViewRadius = Physics.OverlapSphere(origin, alertRadius, alertMask); //범위만큼 적 마스크에 경고

        foreach(Collider obj in targetsInViewRadius)
        {
            obj.SendMessageUpwards("AlertCallback", target, SendMessageOptions.DontRequireReceiver);
            AlertNearBy(obj.transform.position, target, wave + 1);
        }
    }

    public void RootAlertNearBy(Vector3 origin)
    {
        current = origin;
        alert = true;
    }

    void PingAlert()
    {
        if (alert)
        {
            alert = false;
            AlertNearBy(current, current);
        }
    }
}
