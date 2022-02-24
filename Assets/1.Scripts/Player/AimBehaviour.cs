using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마우스 오른쪽 버튼으로 조준, 다른 Behavior보다 상위
/// 다른 동작을 대체해서 동작하게 됨
/// 마우스 휠버튼으로 좌우 카메라 변경
/// 벽의 모서리에서 조준할 때 상체를 살짝 기울여주는 기능
/// 인벤토리 기능 일부분은 여기 있음
/// 인벤토리는 간략하게 딕셔너리로 존재하도록(다른 강의보고 배워야함)
/// </summary>
public class AimBehaviour : GenericBehaviour
{
    public Texture2D crossHair;//십자선 이미지
    public float aimTurnSmoothing = 0.15f;//카메라를 향하도록 조준할 때 회전 속도
    public Vector3 aimPivotOffset = new Vector3(0.5f, 1.2f, 0.0f); //평상시보다 가까워지게
    public Vector3 aimCamOffset = new Vector3(0.0f, 0.0f, -3.0f); //에임 카메라 위치

    private int aimBool;//애니메이터 파라미터, 조준
    private bool aim;//조준중이냐

    private int cornerBool;//애니메이터 관련, 코너
    private bool peekCorner;//플레이어가 코너 모서리에 있는지 여부

    //IK,역동작(Inverse Kinemetic)용 회전값
    private Vector3 initalRootRotation;//루트 본 로컬 회전값
    private Vector3 initalHipRotation;//
    private Vector3 initialSpineRotation;//

    private Transform myTransform;

    private void Start()
    {
        myTransform = transform;
        //setup
        aimBool = Animator.StringToHash(FC.AnimatorKey.Aim);
        cornerBool = Animator.StringToHash(FC.AnimatorKey.Corner);

        //value
        Transform hips = behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Hips); //모델링 본 얻어오기 스크립트 수정보다 애니메이션이 먼저 발동됨
        initalRootRotation = (hips.parent == transform) ? Vector3.zero : hips.parent.localEulerAngles;
        initalHipRotation = hips.localEulerAngles;
        initialSpineRotation = behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Spine).localEulerAngles;


    }

    //카메라에 따라 플레이어를 올바른 방향으로 회전
    void Rotating()
    {
        Vector3 forward = behaviourController.playerCamera.TransformDirection(Vector3.forward);
        forward.y = 0.0f;
        forward = forward.normalized; //카메라로부터 얻음

        Quaternion targetRotation = Quaternion.Euler(0f, behaviourController.GetCamScript.GetH, 0.0f); //좌우 먼저
        float minSpeed = Quaternion.Angle(transform.rotation, targetRotation) * aimTurnSmoothing;

        if (peekCorner)
        {
            //조준 중일때 플레이어 상체만 살짝 기울여 주기 위함
            myTransform.rotation = Quaternion.LookRotation(-behaviourController.GetLastDirection()); //반대방향, IK는 거꾸로
            targetRotation *= Quaternion.Euler(initalRootRotation);
            targetRotation *= Quaternion.Euler(initalHipRotation);
            targetRotation *= Quaternion.Euler(initialSpineRotation);

            Transform spine = behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Spine);
            spine.rotation = targetRotation; //상체 기울임
        
        }
        else
        {
            behaviourController.SetLastDirection(forward);
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRotation, minSpeed * Time.deltaTime);
        }
    }

   
    //조준 중일때를 관리하는 함수
    void AimMangement()
    {
        Rotating();
    }

   
    private IEnumerator ToggleAimOn()
    {
        yield return new WaitForSeconds(0.05f);

        //조준이 불가능한 상태일때에 대한 예외처리
        //
        if(behaviourController.GetTempLockStatus(this.behaviourCode) || behaviourController.IsOverriding(this))
        {
            yield return false;
        }

        else
        {
            aim = true;
            int signal = 1;
            if (peekCorner)
            {
                signal = (int)Mathf.Sign(behaviourController.GetH);
            }
            aimCamOffset.x = Mathf.Abs(aimCamOffset.x) * signal; //벽에 기댈때는 보정
            aimPivotOffset.x = Mathf.Abs(aimPivotOffset.x) * signal;

            yield return new WaitForSeconds(0.1f);

            behaviourController.GetAnimator.SetFloat(speedFloat, 0.0f);
            behaviourController.OverrideWithBehaviour(this); //에임 비헤이비어로 오버라이드
        }
    }

    private IEnumerator ToggleAimOff()
    {
        aim = false;
        yield return new WaitForSeconds(0.3f);
        behaviourController.GetCamScript.ResetTargetOffset();//카메라 원래대로 돌려줌
        behaviourController.GetCamScript.ResetMaxVerticalAngle();

        yield return new WaitForSeconds(0.1f);//카메라 돌아가는거 기다림

        behaviourController.RevokeOverridingBehaviour(this);//오버라이드 해제
    }

    public override void LocalFixedUpdate()
    {
        if (aim)
        {
            behaviourController.GetCamScript.SetTargetOffset(aimPivotOffset, aimCamOffset);//카메라 위치 세팅
        }
    }

    public override void LocalLateUpdate()
    {
        AimMangement(); //조준
    }

    //동작 입력 받음
    private void Update()
    {
        peekCorner = behaviourController.GetAnimator.GetBool(cornerBool);

        //aim 버튼을 누름
        if(Input.GetAxisRaw(ButtonName.Aim)!=0 && !aim)
        {
            StartCoroutine(ToggleAimOn());
        }
        //aim 버튼에서 땜
        else if(aim && Input.GetAxisRaw(ButtonName.Aim) == 0)
        {
            StartCoroutine(ToggleAimOff());
        }

        //조준 중일때 달리기 하지 않음
        canSprint = !aim;

        //좌우 바꿈
        if (aim && Input.GetButtonDown(ButtonName.Shoulder) && !peekCorner)
        {
            aimCamOffset.x = aimCamOffset.x * (-1);
            aimPivotOffset.x = aimPivotOffset.x * (-1);
        }

        behaviourController.GetAnimator.SetBool(aimBool, aim);
    }

    private void OnGUI()
    {
        if(crossHair != null)
        {
            float length = behaviourController.GetCamScript.GetCurrentPivotMagnitude(aimPivotOffset);
            if (length < 0.05f)
            {
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - (crossHair.width * 0.5f),
                    Screen.height * 0.5f - (crossHair.height * 0.5f), crossHair.width, crossHair.height), crossHair);
            }
        }
    }
}
