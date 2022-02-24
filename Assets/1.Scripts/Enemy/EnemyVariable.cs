using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class EnemyVariable : MonoBehaviour
{
    // feel shot decision
    // cover decision
    //repeat decision
    //patrol decsion
    //attack


    public bool feelAlert; //위험 감지?
    public bool hearAlert; //위험 들음?
    public bool advanceCoverDecision;//더 좋은 엄폐물 찾았나?
    public int waitRounds;//대기 타임(행동)
    public bool repeatShot;//반복공격?
    public float waitInCoverTime;//엄폐물에 얼마나 대기?
    public float coverTime;//이번 교전에 얼마나 cover중이냐
    public float patrolTimer;//순찰시간
    public float shotTimer;//총 쏘는 딜레이 타임
    public float startShootTimer;
    public float currentShots;//현재 발사한 총알 개수
    public float shotsInRounds;//교전에서 얼마나 총알 썼는가
    public float blindEngageTimer;//플레이어가 시야에서 사라져도 얼마나 인지하고 있을지(플레이어 찾기)

}
