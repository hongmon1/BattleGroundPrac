using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제네릭 비헤이비어를 상속받은 비헤이비어를 가지고 있음
/// 현재 동작, 기본 동작, 오버라이딩 동작, 잠긴 동작, 마우스 이동값
/// 땅에 서있는지 확인, GenericBehaviour를 상속받은 동작들을 업데이트 시켜준다
/// </summary>
public class BehaviourController : MonoBehaviour
{
    #region Variables
    private List<GenericBehaviour> behaviours; //동작들
    private List<GenericBehaviour> overrideBehaviours; //우선시 되는 동작(ex, 조준시 이동보다 조준 우선한다던가)

    //해시코드
    private int currentBehaviour; //현재 동작 해시코드
    private int defaultBehaviour; //기본 동작 해시코드
    private int behaviourLocked; //잠긴 동작 해시코드

    //캐싱
    public Transform playerCamera;
    private Animator myAnimator;
    private Rigidbody myRigidbody;
    private ThirdPersonOrbitCam camScript;
    private Transform myTransform;

    //속성값
    private float h;//horizontal axis
    private float v;//vertical axis
    public float turnSmoothing = 0.06f;//카메라가 돌 때 플레이어보다 더 늦게 이동함. 그 떄 카메라를 향하도록 움직일 떄 회전속도
    private bool changedFOV;//달리기 동작이 카메라 시야각이 변경되었을 때 저장되었니?
    public float sprintFOV = 100;//달리기 시야각
    private Vector3 lastDirention; //마지막 향했던 방향
    private bool sprint; //달리는 중인지?
    private int hFloat;//애니메이터 관련 가로축 값
    private int vFloat;//애니메이터 관련 세로축 값
    private int groundedBool; //애니메이터 관련 지상에 있는가
    private Vector3 colExtents;//땅에 붙어있는지 확인하기 위해(땅과의 충돌체크 위한) 콜라이더(충돌체) 확장영역

    #endregion Variables

    #region GetterSetter
    public float GetH { get => h;}
    public float GetV { get => v;}
    public ThirdPersonOrbitCam GetCamScript { get => camScript; }
    public Rigidbody GetRigidbody { get => myRigidbody; }
    public Animator GetAnimator { get => myAnimator; }
    public int GetDefaultBehaviour { get => defaultBehaviour; }

    public Vector3 GetLastDirection()
    {
        return lastDirention;

    }
    public void SetLastDirection(Vector3 direction)
    {
        lastDirention = direction;
    }
    #endregion Getter



    #region Check function
    //이동
    //마우스 축 이동값 존재 -> 이동중
    public bool IsMoving()
    {
        //return (h!=0)||(v!=0) ;//좋은 코드 아님(변화하는 부동소수점 비교 안하는게 좋음)
        //float one = 0.15f + 0.15f;
        //float two = 0.1f + 0.2f;
        //one 과 two는 다를 수 있음
        //int로 강제캐스탱해도 되긴 함

        return Mathf.Abs(h) > Mathf.Epsilon || Mathf.Abs(v) > Mathf.Epsilon;// Epsilon -> 실수가 가지는 가장 작은 값
    }

    public bool IsHorizontalMoving()
    {
        return Mathf.Abs(h) > Mathf.Epsilon;
    }

    //달릴 수 있ㄴ느지
    public bool CanSprint()
    {
        //상태 중 달리면 안되는 동작이 있는지
        foreach(GenericBehaviour behaviour in behaviours)
        {
            if (!behaviour.AllowSprint)
            {
                //Debug.Log("cansprint1");
                return false;
            }
        }

        foreach(GenericBehaviour genericbehaviour in overrideBehaviours)
        {
            if (!genericbehaviour.AllowSprint)
            {
                //Debug.Log("cansprint2");
                return false;
            }
        }
        //Debug.Log("cansprint3");
        return true;
    }

    public bool IsSprinting()
    {
        //Debug.Log(sprint + " " + IsMoving() + " " + CanSprint());
        return sprint && IsMoving() && CanSprint(); //달리는 중이고 움직이는 중이고 달릴 수 있는지 체크
    }

    //땅에 있니
    public bool IsGrounded()
    {
        Ray ray = new Ray(myTransform.position + Vector3.up * 2 * colExtents.x, Vector3.down); //플레이어 높이만큼 바닥으로 쏴서 걸리는게 있으면 바닥에 서있음
        return Physics.SphereCast(ray, colExtents.x, colExtents.x + 0.2f);
    }

    #endregion Check funtion

    #region Unity function

    private void Awake()
    {
        //캐싱하기

        behaviours = new List<GenericBehaviour>();
        overrideBehaviours = new List<GenericBehaviour>();
        myAnimator = GetComponent<Animator>();
        hFloat = Animator.StringToHash(FC.AnimatorKey.Horizontal);
        vFloat = Animator.StringToHash(FC.AnimatorKey.Vertical);
        camScript = playerCamera.GetComponent<ThirdPersonOrbitCam>();
        myRigidbody = GetComponent<Rigidbody>();
        myTransform = transform;
        //ground?
        groundedBool = Animator.StringToHash(FC.AnimatorKey.Grounded);
        colExtents = GetComponent<Collider>().bounds.extents;
    }

    private void Update()
    {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        myAnimator.SetFloat(hFloat, h, 0.1f, Time.deltaTime); //demp
        myAnimator.SetFloat(vFloat, v, 0.1f, Time.deltaTime);

        sprint = Input.GetButton(ButtonName.Sprint);

        //달리는 중이면 시야각 바꿈
        if (IsSprinting())
        {
            //Debug.Log("Sprint");
            changedFOV = true;
            camScript.SetFOV(sprintFOV);
        }
        //달리기 끝나면
        else if (changedFOV)
        {
            camScript.ResetFOV();
            changedFOV = false;
        }

        myAnimator.SetBool(groundedBool, IsGrounded());

    }

    private void FixedUpdate()
    {
        bool isAnyBehaviourActive = false;

        //잠긴 동작이 있거나 overrideBehaviour가 없으면
        if(behaviourLocked >0 || overrideBehaviours.Count == 0)
        {
            foreach(GenericBehaviour behaviour in behaviours)
            {
                
                if(behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviourCode)
                {
                    isAnyBehaviourActive = true;
                    behaviour.LocalFixedUpdate(); //기본 동작 업데이트
                }
            }
        }
        //잠긴 동작 없고 overrideBehavior 있으면
        else
        {
            foreach(GenericBehaviour behaviour in overrideBehaviours)
            {
                behaviour.LocalFixedUpdate();
            }
        }

        if(!isAnyBehaviourActive && overrideBehaviours.Count == 0)
        {
            //땅에 붙여두기
            myRigidbody.useGravity = true;
            Repositioning();
        }
    }
    //update 돌고 난 다음
    //카메라 이동을 여기서 처리 많이 함(플레이어 이동 후)
    private void LateUpdate()
    {
        if (behaviourLocked > 0 || overrideBehaviours.Count == 0)
        {
            foreach(GenericBehaviour behaviour in behaviours)
            {
                if(behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviourCode)
                {
                    behaviour.LocalLateUpdate();
                }
            }
        }
        else
        {
            foreach (GenericBehaviour behaviour in overrideBehaviours)
            {
                behaviour.LocalLateUpdate();
            }
        }
    }

    #endregion Unity function

    #region Behaviour function

    //동작 추가
    public void SubScribeBehaviour(GenericBehaviour behaviour)
    {
        behaviours.Add(behaviour);
    }

    //기본동작 변경
    public void RegisterDefaultBehavior(int behaviourCode)
    {
        defaultBehaviour = behaviourCode;
        currentBehaviour = behaviourCode;
        //코드만 바꿔주면 위에서 코드에 해당하는 것만 실행
    }

    //동작 등록
    public void RegisterBehaviour(int behaviourCode)
    {
        //지금 들고있는게 기본이야
        if(currentBehaviour == defaultBehaviour)
        {
            //이 동작으로 현재동작을 바꿔줌
            currentBehaviour = behaviourCode;
        }
    }

    //동작 해제
    public void UnRegisterBehaviour(int behaviourCode)
    {
        if(currentBehaviour == behaviourCode)
        {
            //기본으로 돌려줌
            currentBehaviour = defaultBehaviour;
        }
    }

    //오버라이딩에 등록
    public bool OverrideWithBehaviour(GenericBehaviour behaviour)
    {
        //없으면 넣고 true 반환
        if (!overrideBehaviours.Contains(behaviour))
        {
            if(overrideBehaviours.Count == 0)
            {
                foreach(GenericBehaviour behaviour1 in behaviours)
                {
                    if(behaviour1.isActiveAndEnabled && currentBehaviour == behaviour1.GetBehaviourCode)
                    {
                        behaviour1.OnOverride();
                        break;
                    }
                }
            }

            overrideBehaviours.Add(behaviour);
            return true;
        }
        //이미 포함되어 있음
        return false;
    }

    //오버라이딩에서 동작 제거
    public bool RevokeOverridingBehaviour(GenericBehaviour behaviour)
    {
        if (overrideBehaviours.Contains(behaviour))
        {
            overrideBehaviours.Remove(behaviour);
            return true;
        }
        return false;
    }

    //오버라이딩 중이냐?
    public bool IsOverriding(GenericBehaviour behaviour = null)
    {
        if(behaviour == null)
        {
            return overrideBehaviours.Count > 0; //인자가 null이면 오버라이드 동작 존재하면 true
        }

        return overrideBehaviours.Contains(behaviour); //오버라이드 동작에 포함되면 true
    }
    
    //현재 동작이냐?
    public bool IsCurrentBehaviour(int behaviourCode)
    {
        return this.currentBehaviour == behaviourCode;
    }

    //잠겨있나?
    public bool GetTempLockStatus(int behaviourCode = 0)
    {
        return (behaviourLocked != 0 && behaviourLocked != behaviourCode);
    }

    //잠시 잠금
    public void LockTempBehaviour(int behaviourCode)
    {
        if (behaviourLocked == 0)
        {
            behaviourLocked = behaviourCode;
        }
    }

    //락 풀어줌
    public void UnLockTempBehaviour(int behaviourCode)
    {
        if(behaviourLocked == behaviourCode)
        {
            behaviourLocked = 0;
        }
    }


    #endregion Behaviour function

    //rigidbody 때문에
    //위치 보정하기
    public void Repositioning()
    {
        if (lastDirention != Vector3.zero)
        {
            //방향에 y는 고려하지 않음
            lastDirention.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(lastDirention);
            Quaternion newRotation = Quaternion.Slerp(myRigidbody.rotation, targetRotation, turnSmoothing);
            myRigidbody.MoveRotation(newRotation);

            //캐릭터가 틀어지는 경우를 대비해 위치 보정
        }
    }


}

public abstract class GenericBehaviour : MonoBehaviour
{
    protected int speedFloat; //animator farameter에서 쓸 해시
    protected BehaviourController behaviourController;
    protected int behaviourCode; //각 상속받은 타입을 해시코드로(구분을 위해)
    protected bool canSprint;//달릴수 있는지

    private void Awake()
    {
        this.behaviourController = GetComponent<BehaviourController>();
        speedFloat = Animator.StringToHash(FC.AnimatorKey.Speed);
        canSprint = true;
        //동작 타입을 해시 코드로 가지고 있다가 추후에 구별용으로 사용
        behaviourCode = this.GetType().GetHashCode();//타입에 대한 해시코드 만들어줌

    }

    public int GetBehaviourCode
    {
        get => behaviourCode;
    }

    //뛸수  있는 상태인지
    public bool AllowSprint
    {
        get => canSprint;
    }

    public virtual void LocalLateUpdate() { }

    public virtual void LocalFixedUpdate() { }

    public virtual void OnOverride() { } //특정 동작 덮어쓸 때
}
