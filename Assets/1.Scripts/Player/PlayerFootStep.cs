using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 발자국 소리 출력 
/// </summary>
public class PlayerFootStep : MonoBehaviour
{
    public SoundList[] stepSounds;
    private Animator myAnimator;
    private int index; //어느 사운드 킬지
    private Transform leftFoot, rightFoot;
    private float dist; //거리

    //애니메이터 값들
    private int groundedBool, coverBool, aimBool, crouchFloat;

    private bool grounded;

    public enum Foot
    {
        LEFT,
        RIGHT,
    }

    private Foot step = Foot.LEFT;
    private float oldDist, maxDist = 0; //이동거리 체크용. 돌면서 땅과 내 발 위치가 바뀌고 있으면 사운드 안함, 같으면(2프레임동안) 소리 재생

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
        leftFoot = myAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = myAnimator.GetBoneTransform(HumanBodyBones.RightFoot);

        groundedBool = Animator.StringToHash(FC.AnimatorKey.Grounded);
        coverBool = Animator.StringToHash(FC.AnimatorKey.Cover);
        aimBool = Animator.StringToHash(FC.AnimatorKey.Aim);
        crouchFloat = Animator.StringToHash(FC.AnimatorKey.Crouch);
    }

    private void PlayFootStep()
    {
        //이동중
        if(oldDist < maxDist)
        {
            return;
        }
        oldDist = maxDist = 0;

        int oldIndex = index;

        //사운드 랜덤하게 재생
        while(oldIndex == index)
        {
            index = Random.Range(0, stepSounds.Length - 1);
        }
        SoundManager.Instance.PlayOneShotEffect((int)stepSounds[index], transform.position, 0.2f);
    }

    private void Update()
    {
        //땅에 붙어있지 않았다가 땅에 붙었으면
        if(!grounded && myAnimator.GetBool(groundedBool))
        {
            PlayFootStep();
        }
        grounded = myAnimator.GetBool(groundedBool);
        float factor = 0.15f;

        //움직이고 있으면
        if (grounded && myAnimator.velocity.magnitude > 1.6f)
        {
            oldDist = maxDist;
            switch (step)
            {
                case Foot.LEFT:
                    dist = leftFoot.position.y - transform.position.y; //발의 높이
                    maxDist = dist > maxDist ? dist : maxDist;
                    //땅에 닿았다고 여김
                    if (dist <= factor)
                    {
                        PlayFootStep();
                        step = Foot.RIGHT; //발바꿔줌
                    }
                    break;
                case Foot.RIGHT:
                    dist = rightFoot.position.y - transform.position.y; //발의 높이
                    maxDist = dist > maxDist ? dist : maxDist;
                    if (dist <= factor)
                    {
                        PlayFootStep();
                        step = Foot.LEFT;
                    }
                    break;
            }
        }
    }
}
