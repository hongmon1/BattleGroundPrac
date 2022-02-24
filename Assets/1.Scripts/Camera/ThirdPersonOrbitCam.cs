using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 카메라로 부터 - 위치 오프셋 벡터 : 충동 처리용으로 사용 - 피봇 오프셋 벡터 : 시선 이동에 사용
/// 충돌 체크 : 이중 충돌 체크 기능(캐릭터로부터 카메라, 카메라로부터 캐릭터 사이)
/// 사격 시 반동(리코이)을 위한 기능
/// FOV(시야각) 변경 기능
/// </summary>
/// 
[RequireComponent(typeof(Camera))]
public class ThirdPersonOrbitCam : MonoBehaviour
{
    public Transform player; //player transform
    public Vector3 pivotOffset = new Vector3(0.1f, 0.8f, -0.3f);//플레이어 어깨 위치 쯤
    public Vector3 camOffset = new Vector3(0.4f, 0.5f, -2.0f); //충돌 처리에 사용

    public float smooth = 10f; //카메라 반응속도
    public float horizontalAimingSpeed = 6.0f; //조준 수평 회전 속도
    public float verticalAimingSpeed = 6.0f;//조준 수직 회전 속도

    //카메라 회전 최대/최소 각도
    public float maxVerticalAngle = 30.0f; //카메라 수직 최대 각도
    public float minVerticalAngle = -60.0f; //카메라 수직 최소 각도

    public float recoilAngleBounce = 5.0f; //사격 반동 바운스값

    //카메라는 커서의 이동에 따라 값 변경, 마우스 좌우 : y, 마우스 앞뒤 : x
    private float angleH = 0.0f; //마우스 이동에 따른 카메라 수평이동 수치
    private float angleV = 0.0f; //마우스 이동에 따른 카메라 수직이동 수치

    //카메라 트랜스폼 캐싱
    private Transform cameraTransform; //transform으로 쓰면 쓸 때마다 불러오기 때문에 캐싱을 위해

    private Camera myCamera; //FOV 위해

    private Vector3 relCameraPos;//플레이어로부터 카메라까지의 벡터
    private float relCameraPosMag; //플레이어로부터 카메라까지의 거리

    //카메라 움직임을 부드럽게할 떄 쓰는 오프셋
    private Vector3 smoothPivotOffest;//카메라 피봇용 보간용 벡터
    private Vector3 smoothCamOffset; //카메라 위치용 보간용 벡터
    private Vector3 targetPivotOffset; //카메라 피봇용 보간용 벡터
    private Vector3 targetCamOffset; //카메라 위치용 보간용 벡터

    //FOV
    private float defaultFOV;//기본 시야값
    private float targetFOV;//타겟 시야값

    private float targetMaxVerticalAngle;//카메라 수직 최대 각도(반동때문에)
    private float recoilAngle = 0f;//사격 반동 각도

    /// <summary>
    /// angleH가져옴
    /// </summary>
    public float GetH
    {
        get
        {
            return angleH;
        }
    }

    private void Awake()
    {
        //캐싱
        cameraTransform = transform;
        myCamera = cameraTransform.GetComponent<Camera>();

        //카메라 기본 포지션 세팅
        cameraTransform.position = player.position + Quaternion.identity * pivotOffset + Quaternion.identity * camOffset;//회전하지 않은 오프셋 값들
        cameraTransform.rotation = Quaternion.identity;

        //카메라-플레이어간 상대 벡터, 충돌체크에 사용하기 위해
        relCameraPos = cameraTransform.position - player.position;
        relCameraPosMag = relCameraPos.magnitude - 0.5f; //플레이어를 빼고 충돌체크 하기 위해 0.5f 뺌

        //기본 세팅
        smoothPivotOffest = pivotOffset;
        smoothCamOffset = camOffset;
        defaultFOV = myCamera.fieldOfView;
        angleH = player.eulerAngles.y; //초기 플레이어 y 앵글 값 (모든 게임이 0,0,0에서 시작하진 않아서), 마우스와 동기화 위해

        ResetTargetOffset();
        ResetFOV();
        ResetMaxVerticalAngle();

    }

    #region Reset
    public void ResetTargetOffset()
    {
        targetPivotOffset = pivotOffset;
        targetCamOffset = camOffset;
    }

    public void ResetFOV()
    {
        this.targetFOV = defaultFOV;
    }

    public void ResetMaxVerticalAngle()
    {
        targetMaxVerticalAngle = maxVerticalAngle;
    }
    #endregion

    //각도를 주면 튕김
    public void BounceVertical(float degree)
    {
        recoilAngle = degree;
    }

    public void SetTargetOffset(Vector3 newPivotOffset, Vector3 newCamOffset)
    {
        targetPivotOffset = newPivotOffset;
        targetCamOffset = newCamOffset;
    }

    public void SetFOV(float customFOV)
    {
        targetFOV = customFOV;
    }

    //플레이어의 높이값 필요
    //target으로부터 카메라 위치 체크
    //플레이어와 카메라 사이 충돌체크
    bool ViewingPosCheck(Vector3 checkPos, float deltaPlayerHeight)
    {
        Vector3 target = player.position + (Vector3.up * deltaPlayerHeight);
        //0.2f간격으로 원하는 지점부터 방향으로 relCameraPosMag만큼 스피어캐스트
        if(Physics.SphereCast(checkPos,0.2f,target-checkPos,out RaycastHit hit, relCameraPosMag))
        {
            //플레이어가 아니고 콜라이더가 트리거가 아닐 때
            if(hit.transform!=player && !hit.transform.GetComponent<Collider>().isTrigger)
            {
                return false;
            }
        }

        //충돌 체크되는게 없으면
        //플레이어와 카메라 사이에
        return true;
    }

    //ViewingPosCheck와 반대로 작동
    bool ReverseViewingPosCheck(Vector3 checkPos, float deltaPlayerHeight, float maxDistance)
    {
        Vector3 origin = player.position + (Vector3.up * deltaPlayerHeight);
        if(Physics.SphereCast(origin,0.2f,checkPos-origin,out RaycastHit hit, maxDistance))
        {

            //플레이어, 자기자신, 트리거가 아닐 떄
            if(hit.transform!=player && hit.transform!=transform && !hit.transform.GetComponent<Collider>().isTrigger)
            {
                return false;
            }
        }
        return true;
    }

    bool DoubleViewingPosCheck(Vector3 checkPos, float offset)
    {
        float playerFocusHeight = player.GetComponent<CapsuleCollider>().height * 0.75f;
        //하나라도 충돌하면 false return
        return ViewingPosCheck(checkPos, playerFocusHeight) && ReverseViewingPosCheck(checkPos, playerFocusHeight, offset);
    }


    private void Update()
    {
        //마우스 이동값
        angleH += Mathf.Clamp(Input.GetAxis("Mouse X"), -1f, 1f) * horizontalAimingSpeed; //이동속도도 곱해줌
        angleV += Mathf.Clamp(Input.GetAxis("Mouse Y"), -1f, 1f) * verticalAimingSpeed;

        //수직 이동 제한
        angleV = Mathf.Clamp(angleV, minVerticalAngle, targetMaxVerticalAngle);

        //수직 카메라 바운스
        angleV = Mathf.LerpAngle(angleV, angleV + recoilAngle, 10f * Time.deltaTime);

        //카메라 회전
        Quaternion camYRotation = Quaternion.Euler(0.0f, angleH, 0.0f); //y rotation 값이 나옴
        Quaternion aimRotation = Quaternion.Euler(-angleV, angleH, 0.0f); //aiming rotation, vertical은 값을 반대로 해줘야함
        cameraTransform.rotation = aimRotation;

        //set FOV
        myCamera.fieldOfView = Mathf.Lerp(myCamera.fieldOfView, targetFOV, Time.deltaTime);

        
        Vector3 baseTempPosition = player.position + camYRotation * targetPivotOffset;//기본 포지션 값
        Vector3 noCollisionOffset = targetPivotOffset;//targetCamOffset은 에임할때 바뀜(조준할 때 카메라 오프셋 값), 조준할때와 평소와 다름

        for(float zOffset = targetCamOffset.z; zOffset <= 0f; zOffset += 0.5f)
        {
            noCollisionOffset.z = zOffset;
            if(DoubleViewingPosCheck(baseTempPosition+aimRotation*noCollisionOffset, Mathf.Abs(zOffset)) || zOffset == 0f)
            {
                //너무 가까우면 처리할 필요 없음
                break;
            }
        }

        //Reposition Camera
        smoothPivotOffest = Vector3.Lerp(smoothPivotOffest, targetPivotOffset, smooth * Time.deltaTime);
        smoothCamOffset = Vector3.Lerp(smoothCamOffset, noCollisionOffset, smooth * Time.deltaTime);

        cameraTransform.position = player.position + camYRotation * smoothPivotOffest + aimRotation * smoothCamOffset;


        //recoil 되돌리기
        if (recoilAngle > 0.0f)
        {
            recoilAngle -= recoilAngleBounce * Time.deltaTime;
        }
        else if (recoilAngle < 0.0f)
        {
            recoilAngle += recoilAngleBounce * Time.deltaTime;
        }
    }

    public float GetCurrentPivotMagnitude(Vector3 finalPivotOffset)
    {
        return Mathf.Abs((finalPivotOffset - smoothPivotOffest).magnitude);
    }
}
    
