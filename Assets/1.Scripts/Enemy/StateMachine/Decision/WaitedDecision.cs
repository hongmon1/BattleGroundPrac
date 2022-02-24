using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 랜덤하게 정해진 시간만큼 기다렸는가? 
/// </summary>
[CreateAssetMenu(menuName = "PluggableAI/Decisions/Waited")]
public class WaitedDecision : Decision
{
    public float maxTimeToWait;
    private float timeToWait; //기다린 시간
    private float startTime; //기다리기 시작 시간

    public override void OnEnableDecision(StateController controller)
    {
        timeToWait = Random.Range(0, maxTimeToWait);
        startTime = Time.time;
    }

    public override bool Decide(StateController controller)
    {
        return (Time.time - startTime) >= timeToWait; //크면 충분히 기다림, 작으면 기다릴 시간 남음
    }
}
