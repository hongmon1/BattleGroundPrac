using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 범용 스탯
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/GeneralStats")]
public class GeneralStats : ScriptableObject
{
    [Header("General")]
    [Tooltip("NPC 정찰 속도 clear state")]
    public float patrolSpeed = 2f;
    [Tooltip("NPC 따라오는 속도 warning state")]
    public float chaseSpeed = 5f;
    [Tooltip("NPC 회피하는 속도 engage state")]
    public float evadeSpeed = 15f;
    [Tooltip("웨이포인트 대기 시간")]
    public float patrolWaitTime = 2f;

    [Header("Animation")]
    [Tooltip("장애물 레이어 마스크")]
    public LayerMask obstacleMask;//장애물 마스크
    [Tooltip("조준 시 깜빡임을 피하기 위한 최소 확정 앵글")]
    public float angleDeadZone = 5f;//특정각도 이하면 바로 사격
    [Tooltip("속도 댐핑 시간")]
    public float speedDempTime = 0.4f;
    [Tooltip("각속도 댐핑 시간")]
    public float angularSpeedDampTime = 0.2f;//각속도
    [Tooltip("각도 회전에 따른 반응 시간")]
    public float angleResponseTime = 0.2f;

    [Header("Cover")]
    [Tooltip("장애물에 숨었을 때 고려해야 할 최소 높이 값")]
    public float aboveCoverHeight = 1.5f;
    [Tooltip("장애물 레이어 마스크")]
    public LayerMask coverMask;
    [Tooltip("사격 레이어 마스크")]
    public LayerMask shotMask;
    [Tooltip("타겟 레이어 마스크")]
    public LayerMask targetMask;


}
