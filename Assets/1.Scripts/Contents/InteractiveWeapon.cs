using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 충돌체를 생성해 무기를 주을 수 있도록 한다
/// 루팅헀으면 충돌체는 제거
/// 무기를 다시 버릴수도 있어야 하며, 충돌체를 다시 붙여준다.
/// 관련해서 UI도 컨트롤할 수 있어야 하고
/// ShoootBehaviour에 주은 무기를 넣어주게 된다.
/// </summary>
public class InteractiveWeapon : MonoBehaviour
{
    public string label_weaponName;//무기이름

    public SoundList shotSound, reloadSound, pickSound, dropSound, noBulletSound;

    public Sprite weaponSprite;
    public Vector3 rightHandPosition;//플레이어 오른손에 보정 위치
    public Vector3 relativeRotation; //무기는 고유의 오프셋을 가지고 있음. 플레이어 맞춘 보정을 위한 회전값

    public float bulletDamage = 10f;
    public float recoilAngle;//반동

    public enum WeaponType
    {
        NONE,
        SHORT,
        LONG,
    }
    public enum WeaponMode
    {
        SEMI,
        BURST,
        AUTO,
    }

    public WeaponType weaponType = WeaponType.NONE;
    public WeaponMode weaponMode = WeaponMode.SEMI;

    public int burstSize = 1;//버스트 모드일 때 얼마나 나가는지

    public int currentMagCapacity;//현채 탄창 양
    public int totalBullets;//소지하고 있는 전체 총알 량

    private int fullMag, maxBullets;//재장전시 꽉 채우는 탄의 양, 한번에 채울 수 있는 최대 총알량
    private GameObject player, gameController;
    private ShootBehaviour playerInventory;//인벤토리 역할 (실무에서는 이렇게 잘 안함)

    private BoxCollider weaponCollider;
    private SphereCollider interactiveRadius;
    private Rigidbody weaponRigidbody;
    private bool pickable; //주울 수 있는 총인가

    //UI
    public GameObject screenHUD;
    public WeaponUIManager weaponHUD;
    private Transform pickHUD;
    public Text pickupHUD_Label;

    [SerializeField]
    private Transform muzzleTransform;//총구

    private void Awake()
    {
        gameObject.name = this.label_weaponName;
        gameObject.layer = LayerMask.NameToLayer(FC.TagAndLayer.LayerName.IgnoreRayCast); //총에다 총을 쏠 수 없으니 충돌체크 피하기 위해

        //트랜스폼 하위 오브젝트들도 똑같이 무시(충돌체크 피하기)
        foreach (Transform tr in transform)
        {
            tr.gameObject.layer = LayerMask.NameToLayer(FC.TagAndLayer.LayerName.IgnoreRayCast);
        }

        player = GameObject.FindGameObjectWithTag(FC.TagAndLayer.TagName.Player);
        playerInventory = player.GetComponent<ShootBehaviour>();

        //씬에 있는 gameController
        gameController = GameObject.FindGameObjectWithTag(FC.TagAndLayer.TagName.GameController);

        //혹시 빠졌을까봐 예외처리
        if(weaponHUD == null)
        {
            if (screenHUD == null)
            {
                screenHUD = GameObject.Find("ScreenHUD");
            }
            weaponHUD = screenHUD.GetComponent<WeaponUIManager>();
        }
        if(pickHUD == null)
        {
            pickHUD = gameController.transform.Find("PickupHUD");
        }

        //인터랙션을 위한 충돌체 설정
        weaponCollider = transform.GetChild(0).gameObject.AddComponent<BoxCollider>();//자식에 붙이는 이유 : 자기 자신은 구형 콜라이더 붙일 것이기 때문
        CreateInteractiveRadius(weaponCollider.center);//자기 자신에 구형 콜라이더, 이 구 안에 들어가면 인터랙션 발생
        weaponRigidbody = gameObject.AddComponent<Rigidbody>();

        //예외처리
        if(this.weaponType == WeaponType.NONE)
        {
            this.weaponType = WeaponType.SHORT;
        }

        fullMag = currentMagCapacity;
        maxBullets = totalBullets;
        pickHUD.gameObject.SetActive(false); //무기를 안들고있으면 꺼줌
        
        if(muzzleTransform == null)
        {
            muzzleTransform = transform.Find("muzzle");
        }
    }

    //콜라이더의 인터렉트 반경 정해주는 함수
    private void CreateInteractiveRadius(Vector3 center)
    {
        interactiveRadius = gameObject.AddComponent<SphereCollider>();
        interactiveRadius.center = center;
        interactiveRadius.radius = 1;
        interactiveRadius.isTrigger = true;
    }

    //구 안에 들어오면 플레이어 바라보는 UI 켜줌 
    private void TogglePickHUD(bool toggle)
    {
        //3d ui
        pickHUD.gameObject.SetActive(toggle);

        if (toggle)
        {
            pickHUD.position = this.transform.position + Vector3.up * 0.5f;
            Vector3 direction = player.GetComponent<BehaviourController>().playerCamera.forward; //카메라가 바라보는 방향
            direction.y = 0;
            pickHUD.rotation = Quaternion.LookRotation(direction);//특정 방향을 바라보게 할려면 Quaternion.LookRotation
            pickupHUD_Label.text = "Pick " + this.gameObject.name;
        }
    }

    private void UpdateHUD()
    {
        weaponHUD.UpdateWeaponHUD(weaponSprite, currentMagCapacity, fullMag, totalBullets);
    }

    //인터랙션 일어나면 호출
    public void Toggle(bool active)
    {
        //줍는 사운드 출력
        if (active)
        {
            SoundManager.Instance.PlayOneShotEffect((int)pickSound, transform.position, 0.5f);
        }
        weaponHUD.Toggle(active); //HUD 켜줌
        UpdateHUD();
    }

    private void Update()
    {
        if(this.pickable && Input.GetButtonDown(ButtonName.Pick))
        {
            //disable physics (weapon의)
            weaponRigidbody.isKinematic = true;
            weaponCollider.enabled = false;
            playerInventory.AddWeapon(this);
            Destroy(interactiveRadius);
            this.Toggle(true);
            this.pickable = false;

            TogglePickHUD(false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //총을 땅에 떨굴 때
        if(collision.collider.gameObject != player && Vector3.Distance(transform.position, player.transform.position) <= 5f)
        {
            SoundManager.Instance.PlayOneShotEffect((int)dropSound, transform.position, 0.5f);
        }
    }

    //구형 콜라이더(범위) 밖으로 나가면 상호작용 불가
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject == player)
        {
            pickable = false;
            TogglePickHUD(false);
        }
    }

    //주울 수 있는 상태
    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject==player && playerInventory && playerInventory.isActiveAndEnabled)
        {
            pickable = true;
            TogglePickHUD(true);
        }
    }

    //떨구기
    public void Drop()
    {
        gameObject.SetActive(true);
        transform.position += Vector3.up;
        weaponRigidbody.isKinematic = false;
        this.transform.parent = null;
        CreateInteractiveRadius(weaponCollider.center);
        this.weaponCollider.enabled = true;
        weaponHUD.Toggle(false);
    }


    public bool StartReload()
    {
        //탄창이 꽉 찼거나 현재 총알이 0이면
        if (currentMagCapacity == fullMag || totalBullets == 0)
        {
            return false;
        }

        //소지 총알량이 풀충전탄창 크기보다 작으면
        else if(totalBullets<fullMag - currentMagCapacity)
        {
            currentMagCapacity += totalBullets;
            totalBullets = 0;
        }
        // 그냥 재장전
        else
        {
            totalBullets -= fullMag - currentMagCapacity;
            currentMagCapacity = fullMag;
        }
        return true;
    }
       
    public void EndReload()
    {
        UpdateHUD();
    }

    public bool Shoot(bool firstShot = true)
    {
        if (currentMagCapacity > 0)
        {
            currentMagCapacity--;
            UpdateHUD();
            return true;
        }
        //총알이 없는데 쏠려하면 빈총소리
        if(firstShot && noBulletSound != SoundList.None)
        {
            SoundManager.Instance.PlayOneShotEffect((int)noBulletSound, muzzleTransform.position, 5f);
        }
        return false;
    }

    //총알량 리셋
    //무기 주을 때 쓸 수 있음
    public void ResetBullet()
    {
        currentMagCapacity = fullMag;
        totalBullets = maxBullets;
    }
}
