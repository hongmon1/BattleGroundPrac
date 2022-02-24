using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class EnemyAnimation : MonoBehaviour
{
    [HideInInspector] public Animator anim;
    [HideInInspector] public float currentAimingAngleGap;
    [HideInInspector] public Transform gunMuzzle;
    [HideInInspector] public float angularSpeed;

    private StateController controller;
    private NavMeshAgent nav;
    private bool pendingAim; //조준을 기다리는 시간, 심하게 IK 각도를 틀면 메시가 찢어져서 조준을 풀었다가 다시 조준하는 대기시간 필요
    private Transform hips, spine; //bone transform
    private Vector3 initialRootRotation;
    private Vector3 initialHipsRotation;
    private Vector3 initialSpineRotation;
    private Quaternion lastRotation;
    private float timeCountAim, timeCountGuard; //원하는 회전값을 갖기 위한 타임 카운트
    private readonly float turnSpeed = 25f; //스트래핑할때 npc가 얼마나의 빠르기로 움직일지

    private void Awake()
    {
        //캐싱
        controller = GetComponent<StateController>();
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        nav.updateRotation = false;//애니메이션 회전 자동으로 안하고 우리가 할게

        hips = anim.GetBoneTransform(HumanBodyBones.Hips);
        spine = anim.GetBoneTransform(HumanBodyBones.Spine);

        initialRootRotation = (hips.parent == transform) ? Vector3.zero : hips.parent.localEulerAngles;
        initialHipsRotation = hips.localEulerAngles;
        initialSpineRotation = spine.localEulerAngles;

        anim.SetTrigger(FC.AnimatorKey.ChangeWeapon);
        anim.SetInteger(FC.AnimatorKey.Weapon, (int)Enum.Parse(typeof(WeaponType), controller.classStats.WeaponType)); //총 종류 넣어줌

        
        foreach(Transform child in anim.GetBoneTransform(HumanBodyBones.RightHand))
        {
            gunMuzzle = child.Find("muzzle");
            if(gunMuzzle != null)
            {
                break;
            }
        }


        //장착 무기에 리지드 바디가 있으면 꺼줌
        foreach(Rigidbody member in GetComponentsInChildren<Rigidbody>())
        {
            member.isKinematic = true;
        }
    }

    //네비게이션 매시 매 업데이트마다 (에너미 움직일 때) setup 호출
    //애니메이터 제어
    void Setup(float speed, float angle, Vector3 strafeDirection)
    {
        angle *= Mathf.Deg2Rad;
        angularSpeed = angle / controller.generalStats.angleResponseTime; //각속도

        anim.SetFloat(FC.AnimatorKey.Speed, speed, controller.generalStats.angularSpeedDampTime, Time.deltaTime);
        anim.SetFloat(FC.AnimatorKey.AngularSpeed, angularSpeed, controller.generalStats.angularSpeedDampTime, Time.deltaTime);

        //strafing 관련
        //벡터 4원소 회전??
        anim.SetFloat(FC.AnimatorKey.Horizontal,strafeDirection.x, controller.generalStats.speedDempTime, Time.deltaTime);
        anim.SetFloat(FC.AnimatorKey.Vertical, strafeDirection.z, controller.generalStats.speedDempTime, Time.deltaTime);

    }

    //main고ㅏ 비슷한 역할
    void NavAnimSetUp()
    {
        float speed;
        float angle;
        speed = Vector3.Project(nav.desiredVelocity, transform.forward).magnitude; //

        //strafing
        if (controller.focusSight)
        {
            Vector3 dest = (controller.personalTarget - transform.position);
            dest.y = 0.0f;

            //Vector3 signedAngle
            //angle이 0보다 작으면 내가 봣을 떄 왼쪽, 크면 오른쪽
            angle = Vector3.SignedAngle(transform.forward, dest, transform.up);
            if (controller.Strafing)
            {
                dest = dest.normalized; //방향
                Quaternion targetStrafeRotation = Quaternion.LookRotation(dest); //원래는 넣기 전에 dest가 너무 작은 벡터인지 확인함
                transform.rotation = Quaternion.Lerp(transform.rotation, targetStrafeRotation, turnSpeed * Time.deltaTime);

            }
        }
        else
        {
            if(nav.desiredVelocity == Vector3.zero)
            {
                angle = 0.0f;
            }
            else
            {
                angle = Vector3.SignedAngle(transform.forward, nav.desiredVelocity, transform.up);
            }
        }

        //플레리어를 향하려할때 깜빡거리지 않도록 각도 데드존을 적용(급하게 틀때)
        if(!controller.Strafing && Mathf.Abs(angle) < controller.generalStats.angleDeadZone)
        {
            transform.LookAt(transform.position + nav.desiredVelocity);
            angle = 0f;

            //다시 조준해라
            if(pendingAim && controller.focusSight)
            {
                controller.Aiming = true;
                pendingAim = false;
            }
        }

        //Strafe direction
        Vector3 direction = nav.desiredVelocity;
        direction.y = 0.0f;
        direction = Quaternion.Inverse(transform.rotation) * direction;//특정 백터 P의 quaternion 회전은 -> P = QPQ^-1 쿼터니언은 4원수, 인버스로 3차원으로 끌어옴
        Setup(speed, angle, direction);
    }

    private void Update()
    {
        NavAnimSetUp();
    }

    private void OnAnimatorMove()
    {
        //일시정지 상태
        if (Time.timeScale > 0 && Time.deltaTime > 0)
        {
            nav.velocity = anim.deltaPosition / Time.deltaTime;
            if (!controller.Strafing)
            {

                //로테이션 보정
                transform.rotation = anim.rootRotation;
            }
        }
    }

    //조준용 보정
    private void LateUpdate()
    {
        if (controller.Aiming)
        {
            Vector3 direction = controller.personalTarget - spine.position;

            //예외처리
            if (direction.magnitude<0.01f || direction.magnitude > 1000000.0f)
            {
                return;
            }

            //spine -> 조준에 따라 IK적용
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            targetRotation *= Quaternion.Euler(initialRootRotation);
            targetRotation *= Quaternion.Euler(initialHipsRotation);
            targetRotation *= Quaternion.Euler(initialSpineRotation);

            targetRotation *= Quaternion.Euler(FC.VectorHelper.ToVector(controller.classStats.AimOffset)); //aim오프셋값 적용
            Quaternion frameRotation = Quaternion.Slerp(lastRotation, targetRotation, timeCountAim);//보간

            //일정각도 이하일때만, 이상일떄는 애니메이션 준비할 수 있게
            //각도가 60도 이하일 때만, 틀어도 메시가 찢어지지 않을때만 적용
            //엉덩이를 기준으로 척추 회전이 60도 이하인 경우는 계속 조준 가능
            if(Quaternion.Angle(frameRotation, hips.rotation) <= 60.0f)
            {
                spine.rotation = frameRotation;
                timeCountAim += Time.deltaTime;
            }
            else
            {
                //조준한 적 없고 70도이상 회전
                //비현실적인 회전 막기 위해
                if(timeCountAim == 0 && Quaternion.Angle(frameRotation, hips.rotation) > 70.0f)
                {
                    //1초 기다렸다가 에임풀고.... 메시 찢어짐 방지
                    StartCoroutine(controller.UnstuckAim(2f));
                }
                spine.rotation = lastRotation;
                timeCountAim = 0;
            }

            lastRotation = spine.rotation;
            Vector3 target = controller.personalTarget - gunMuzzle.position;
            Vector3 forward = gunMuzzle.forward;
            currentAimingAngleGap = Vector3.Angle(target, forward);

            timeCountGuard = 0;
        }

        //조준이 아닐 때
        //천천히 돌아옴
        else
        {
            lastRotation = spine.rotation;
            spine.rotation *= Quaternion.Slerp
                (Quaternion.Euler(FC.VectorHelper.ToVector(controller.classStats.AimOffset)), Quaternion.identity, timeCountGuard);
            timeCountGuard += Time.deltaTime;
            //원래 자세로 돌아옴
        }
    }

    //팬딩관련

    public void ActivatePendingAim()
    {
        pendingAim = true;
    }


    public void AbortPendingAim()
    {
        pendingAim = false;
        controller.Aiming = false;
    }
}
