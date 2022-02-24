using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이동과 점프 동작을 담당하는 컴포넌트
/// 충돌 처리에 대한 기능도 포함
/// 기본 동작으로써 작동
/// </summary>
public class MoveBehaviour : GenericBehaviour
{ 

    #region Variables

    public float walkSpeed = 0.15f;
    public float runSpeed = 1.0f;
    public float sprintSpeed = 2.0f;
    public float speedDampTime = 0.1f;

    public float jumpHeight = 1.5f;
    public float jumpInertiaForce = 10f;//점프하면 앞으로감
    public float speed, speedSeeker;

    private int jumpBool; //애니메이터에서 쓸 해시값
    private int groundedBool;//애니메이터에서 쓸 해시값

    private bool jump;
    private bool isColliding;//충돌체크용
    private CapsuleCollider capsuleCollider;
    private Transform myTransform;

    #endregion Variables

    private void Start()
    {
        myTransform = transform;
        capsuleCollider = GetComponent<CapsuleCollider>();
        jumpBool = Animator.StringToHash(FC.AnimatorKey.Jump);
        groundedBool = Animator.StringToHash(FC.AnimatorKey.Grounded);
        behaviourController.GetAnimator.SetBool(groundedBool, true);

        //기본 동작으로 등록
        behaviourController.SubScribeBehaviour(this);
        behaviourController.RegisterDefaultBehavior(this.behaviourCode);

        speedSeeker = runSpeed;

    }

    //이동에 가장 먼저 필요한건 회전
    //회전을 가장 먼저 해야함
    //rigidbody의 경우 velocity의 y값을 없애는 부분이 꼭 들어있어야 함
    Vector3 Rotating(float horizontal, float vertical)
    {
        //카메라의 포워드 방향이 어디를 가리키는지 가져옴
        Vector3 forward = behaviourController.playerCamera.TransformDirection(Vector3.forward);

        forward.y = 0.0f;//앞을 바라봐야해서
        forward = forward.normalized;

        //두 벡터의 내적이 0이면 직교한다
        Vector3 right = new Vector3(forward.z, 0.0f, -forward.x); //뒤집기, right라서 x의 부호 반대, 직교하는 벡터
        Vector3 targetDirection = Vector3.zero;
        targetDirection = forward * vertical + right * horizontal;

        //이동 중이었고 이동할려고 하면
        if (behaviourController.IsMoving() && targetDirection != Vector3.zero)
        {
            //돌리는 과정 보간을 이용해 부드럽게
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            Quaternion newRotation = Quaternion.Slerp(behaviourController.GetRigidbody.rotation, targetRotation, behaviourController.turnSmoothing);
            behaviourController.GetRigidbody.MoveRotation(newRotation);
            behaviourController.SetLastDirection(targetDirection);
        }
        //가만히 서있거나 카메라 회전중이면 리포지셔닝
        if(!(Mathf.Abs(horizontal)>0.9f || Mathf.Abs(vertical) > 0.9f))
        {
            behaviourController.Repositioning();
        }

        return targetDirection;
    }

    //y축 방향 없애기 !!중요
    private void RemoveVerticalVelocity()
    {
        Vector3 horizontalVelocity = behaviourController.GetRigidbody.velocity;
        horizontalVelocity.y = 0.0f;
        behaviourController.GetRigidbody.velocity = horizontalVelocity;
    }

    void MovementManagement(float horizontal, float vertical)
    {
        //땅이면 중력 사용
        if (behaviourController.IsGrounded())
        {
            behaviourController.GetRigidbody.useGravity = true;
        }
        //점프 중이 아닌데 땅위에 떠있음 -> 어딘가에 껴있는거임
        else if (!behaviourController.GetAnimator.GetBool(jumpBool) && behaviourController.GetRigidbody.velocity.y > 0)
        {
            RemoveVerticalVelocity();
        }

        //회전
        Rotating(horizontal, vertical);

        //이동
        Vector2 dir = new Vector2(horizontal, vertical);
        speed = Vector2.ClampMagnitude(dir, 1f).magnitude;
        speedSeeker += Input.GetAxis("Mouse ScrollWheel");
        speedSeeker = Mathf.Clamp(speedSeeker, walkSpeed, runSpeed);
        speed *= speedSeeker;

        //달리는 중이면
        if (behaviourController.IsSprinting())
        {
            speed = sprintSpeed;
        }

        //애니메이션
        behaviourController.GetAnimator.SetFloat(speedFloat, speed, speedDampTime, Time.deltaTime);

    }

    //충돌 중(점프 전 확인)
    private void OnCollisionStay(Collision collision)
    {
        isColliding = true; //충돌중이다

        //경사면등에 부딪힘
        if (behaviourController.IsCurrentBehaviour(GetBehaviourCode) && collision.GetContact(0).normal.y <= 0.1f)
        {
            float vel = behaviourController.GetAnimator.velocity.magnitude;
            Vector3 targetMove = Vector3.ProjectOnPlane(myTransform.forward, collision.GetContact(0).normal).normalized * vel; //미끄러지게 만듬
            behaviourController.GetRigidbody.AddForce(targetMove, ForceMode.VelocityChange); //강제로 미끄러지게
        }

    }

    private void OnCollisionExit(Collision collision)
    {
        isColliding = false;
    }

    void JumpManagement()
    {
        if(jump&&!behaviourController.GetAnimator.GetBool(jumpBool)&& behaviourController.IsGrounded())
        {
            behaviourController.LockTempBehaviour(behaviourCode);//점프중엔 이동 불가
            behaviourController.GetAnimator.SetBool(jumpBool, true);
            if (behaviourController.GetAnimator.GetFloat(speedFloat) > 0.1f)
            {
                capsuleCollider.material.dynamicFriction = 0f;
                capsuleCollider.material.staticFriction = 0f;
                RemoveVerticalVelocity();
                float velocity = 2f * Mathf.Abs(Physics.gravity.y) * jumpHeight;
                velocity = Mathf.Sqrt(velocity);
                behaviourController.GetRigidbody.AddForce(Vector3.up * velocity, ForceMode.VelocityChange); //점프함
            }
        }
        else if (behaviourController.GetAnimator.GetBool(jumpBool))
        {
            //공중에 떠있는데 이동이 잠겨있음
            if(!behaviourController.IsGrounded()&&!isColliding&& behaviourController.GetTempLockStatus())
            {
                behaviourController.GetRigidbody.AddForce(myTransform.forward * jumpInertiaForce * Physics.gravity.magnitude * sprintSpeed, ForceMode.Acceleration);
                //공중에 있을 땐 앞으로 가는 힘을 줌
            }
            //땅에 떨어졌을 때
            if(behaviourController.GetRigidbody.velocity.y<0f && behaviourController.IsGrounded())
            {
                behaviourController.GetAnimator.SetBool(groundedBool, true);
                capsuleCollider.material.dynamicFriction = 0.6f;
                capsuleCollider.material.staticFriction = 0.6f;
                jump = false;
                behaviourController.GetAnimator.SetBool(jumpBool, false);
                behaviourController.UnLockTempBehaviour(this.behaviourCode);//이동할 수 있게

            }
        }
    }

    private void Update()
    {
        if (!jump && Input.GetButtonDown(ButtonName.Jump) && behaviourController.IsCurrentBehaviour(this.behaviourCode) && !behaviourController.IsOverriding())
        {
            jump = true;
        }
    }

    public override void LocalFixedUpdate()
    {
        MovementManagement(behaviourController.GetH, behaviourController.GetV);
        JumpManagement();
    }
}
