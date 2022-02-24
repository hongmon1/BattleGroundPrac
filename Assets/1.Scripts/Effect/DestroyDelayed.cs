using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특정시간 대기하고 삭제
/// </summary>
public class DestroyDelayed : MonoBehaviour
{
    public float DelayTime = 0.5f;

    private void Start()
    {
        Destroy(gameObject, DelayTime);
    }
}
