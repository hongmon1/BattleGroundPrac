using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// state -> actions update -> transition(decision) check
/// state에 필요한 기능들, 애니메이션 콜백들
/// 시야 체크, 찾아놓은 엄폐물 장소 중 가장 가까운 위치 찾는 기능
/// </summary>
public class StateController : MonoBehaviour
{

    public GeneralStats generalStats;
    public ClassStats statData;
    public string classID; //PISTOL,RIFLE,AK...

    //캐싱하는 방법 찾아보기
    public ClassStats.Param classStats
    {
        //classID로 지정된 데이터를 불러올 수 있음
        get
        {
            foreach(ClassStats.Sheet sheet in statData.sheets)
            {
                foreach(ClassStats.Param parm in sheet.list)
                {
                    if (parm.ID.Equals(this.classID))
                    {
                        return parm;
                    }
                }
            }
            return null;
        }
    }

    public State currentState;
    public State remainState; //딱히 트랜지션 할 게 없을 때

    public Transform aimTarget;

    public List<Transform> patrolWaypoints;

    public int bullets; //총알개수
    [Range(0,50)]
    public float viewRadius;//시야 반경
    [Range(0,360)]
    public float viewAngle;//시야 각도
    [Range(0,25)]
    public float perceptionRadius; //인지 범위

    [HideInInspector]public float nearRadius;
    [HideInInspector] public NavMeshAgent nav; //캐싱
    [HideInInspector] public int wayPointIndex;
    [HideInInspector] public int maximuBurst = 7;//유효한 총알 개수
    [HideInInspector] public float blinedEngageTime = 30f; //일정시간동안 플레이어를 찾는 시간

    [HideInInspector] public bool targetInSight;//시야 안에 타겟?
    [HideInInspector] public bool focusSight;//포지션 포쿠스 할거냐
    [HideInInspector] public bool reloading;
    [HideInInspector] public bool hadClearShot; //before 방금까지 쏠 수 있었냐
    [HideInInspector] public bool haveClearShot; //now 지금 쏠 수 있는 상태냐
    [HideInInspector] public int coverHash = -1; //이미 누가 숨은 곳이면 다른곳에 숨게 장애물에 해싱 코드 부여

    [HideInInspector] public EnemyVariable variables;
    [HideInInspector] public Vector3 personalTarget = Vector3.zero; //각 에너미가 가지고 있는 타겟 위치

    private int magBullets; //잔탄량
    private bool aiActive;//AI 활성화?
    private static Dictionary<int, Vector3> coverSpot;//static, 숨기 가능한 위치들 목록, 해시코드로 구별
    private bool strafing;
    private bool aiming;
    private bool checkedOnLoop, blockedSight; //시야가 막혀있나, 

    [HideInInspector] public EnemyAnimation enemyAnimation;
    [HideInInspector] public CoverLookUp coverLookUp;

    public Vector3 CoverSpot
    {
        get { return coverSpot[this.GetHashCode()];  }
        set { coverSpot[this.GetHashCode()] = value; }
    }

    public void TransitionToState(State nextState, Decision decision)
    {
        if(nextState != remainState)
        {
            currentState = nextState;
        }
    }

    //strafing 중이냐
    public bool Strafing
    {
        get => strafing;
 
        set
        {
            enemyAnimation.anim.SetBool("Strafe", value);
            strafing = value;
        }
    }

    //Aiming중이냐
    public bool Aiming
    {
        get => aiming;
        set
        {
            if (aiming != value)
            {
                enemyAnimation.anim.SetBool("Aim", value);
                aiming = value;
            }
        }
    }

    //aim 푸는거
    //애니메이션 찢어지는거 방지 딜레이를 줌
    public IEnumerator UnstuckAim(float delay)
    {
        yield return new WaitForSeconds(delay * 0.5f);
        Aiming = false;
        yield return new WaitForSeconds(delay * 0.5f);
        Aiming = true;
    }

    private void Awake()
    {
        if(coverSpot == null)
        {
            coverSpot = new Dictionary<int, Vector3>();
        }
        coverSpot[this.GetHashCode()] = Vector3.positiveInfinity;//세팅되지 않음. 초기값
        nav = GetComponent<NavMeshAgent>();
        aiActive = true;
        enemyAnimation = gameObject.AddComponent<EnemyAnimation>(); //자동으로 붙이도록
        magBullets = bullets; 
        variables.shotsInRounds = maximuBurst; 

        nearRadius = perceptionRadius * 0.5f; //nearRadius 안이면 공격

        GameObject gameController = GameObject.FindGameObjectWithTag("GameController");
        coverLookUp = gameController.GetComponent<CoverLookUp>();

        if(coverLookUp == null)
        {
            coverLookUp = gameController.AddComponent<CoverLookUp>();
            coverLookUp.Setup(generalStats.coverMask);
        }

        //기본적으로 필요한거 확인용
        Debug.Assert(aimTarget.root.GetComponent<HealthBase>(), "반드시 타겟에는 생명력관련 컴포넌트를 붙여주어야 합니다");

    }

    public void Start()
    {
        currentState.OnEnableActions(this); //액션 실행 전 초기화 함수
    }

    private void Update()
    {
        checkedOnLoop = false;

        if (!aiActive)
        {
            return;
        }

        currentState.DoActions(this);
        currentState.CheckTrasitions(this);
        //현재 스테이트가 갖고있는 액션들과 decision들 돌면서 체크
    }

    private void OnDrawGizmos()
    {
        if (currentState != null)
        {
            Gizmos.color = currentState.sceneGizmoColor;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 2f);
        }
    }

    public void EndReloadWeapon()
    {
        reloading = false;
        bullets = magBullets;
    }

    //sendmessage용
    public void AlertCallback(Vector3 target)
    {
        //안죽었으면
        if (!aimTarget.root.GetComponent<HealthBase>().IsDead)
        {
            //경고를 들은게 맞음
            this.variables.hearAlert = true;
            //타겟 우ㅣ치
            this.personalTarget = target;
        }
    }

    //거리체크용, 그 위치랑 가깝냐
    public bool IsNearOtherSpot(Vector3 spot, float margin = 1f)
    {
        foreach(KeyValuePair<int,Vector3> usedSpot in coverSpot)
        {
            //거의 도착했다
            if(usedSpot.Key != gameObject.GetHashCode() && Vector3.Distance(spot, usedSpot.Value) <= margin)
            {
                return true;
            }
        }
        return false;
    }

    //시야가 막혔냐
    public bool BlockedSight()
    {

        if (!checkedOnLoop)
        {
            checkedOnLoop = true;
            Vector3 target = default;
            try
            {
                target = aimTarget.position;
            }
            catch (UnassignedReferenceException)
            {
                Debug.LogError("조준 타겟을 지정해주세요 : " + transform.name);
            }

            Vector3 castOrigin = transform.position + Vector3.up * generalStats.aboveCoverHeight; //바닥위치에서 1.5쯤 떨어진 위쪽
            Vector3 dirToTarget = target - castOrigin;

            blockedSight = Physics.Raycast(castOrigin, dirToTarget, out RaycastHit hit, dirToTarget.magnitude, generalStats.coverMask | generalStats.obstacleMask);
            //시야가 막혔는지
        }
        return blockedSight;
    }

    //Enemy가 죽으면 점유하고 있던 스팟을 해제, 다른 Enemy가 사용할 수 있게
    private void OnDestroy()
    {
        coverSpot.Remove(this.GetHashCode());
    }

}
