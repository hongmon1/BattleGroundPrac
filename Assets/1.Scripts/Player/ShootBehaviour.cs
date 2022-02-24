using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사격 기능 : 사격이 가능한지 여부 체크하는 기능
/// 발사 키 입력 받아서 애니메이션 재생, 이펙트 생성, 충돌 체크 기능
/// UI 관련해서 십자선 표시 기능
/// 발사 속도 조정
/// 캐릭터 조준에 따라 상체를 IK를 이용해 조준 시점에 맞춰 회전
/// 벽이나 충돌체에 총알이 피격되었을 경우 피탄이펙트 생성
/// 인벤토리 역할, 무기를 소지하고 있는지 확인 용
/// 재장전과 무기 교체 기능 포함
/// </summary>
public class ShootBehaviour : GenericBehaviour
{
    public Texture2D aimCrossHair, shootCrossHair;//십자선
    public GameObject muzzleFlash, shot, sparks;//이펙트
    public Material bulletHole;//피탄 이펙트
    public int MaxBulletHoles = 50; //피탄 최대 개수(부하 줄이기 위해 설정)
    public float shootErrorRate = 0.01f;//오발
    public float shootRateFactor = 1.0f;//높일수록 빨라짐

    public float armsRotation = 8f;//조준시 팔 돌아감

    public LayerMask shotMask = ~(FC.TagAndLayer.LayerMasking.IgnoreRayCast | FC.TagAndLayer.LayerMasking.IgnoreShot | FC.TagAndLayer.LayerMasking.CoverInvisible | FC.TagAndLayer.LayerMasking.Player);//피격 될 대상 마스크
    public LayerMask organicMask= FC.TagAndLayer.LayerMasking.Player | FC.TagAndLayer.LayerMasking.Enemy;//생명체인지 확인(생명체에 피탄이펙트는 이상하니까)

    public Vector3 leftArmShortAim = new Vector3(-4.0f, 0.0f, 2.0f);//짧은 총, 피스톨 같은 총 들었을 때 조준 시 왼팔 위치 보정

    private int activeWeapon = 0;//0이 아니면 활성화된 무기가 있다

    //Animator value
    private int weaponType;
    private int changeWeaponTrigger;
    private int shootingTrigger;
    private int aimBool, blockedAimBool, reloadBool;

    private List<InteractiveWeapon> weapons;//소지하고 있는 무기들
    private bool isAiming, isAimBlocked;

    //이펙트들 위치 찾기 위해
    private Transform gunMuzzle;
    private float distToHand; //손부터 목까지 거리

    private Vector3 castRelativeOrigin;

    private Dictionary<InteractiveWeapon.WeaponType, int> slotMap; //인벤토리 슬롯에 매핑되도록

    //IK르 ㄹ위해
    private Transform hips, spine, chest, rightHand, leftArm;
    private Vector3 initialRootRotation;
    private Vector3 initialHipsRotation;
    private Vector3 initialSpineRotation;
    private Vector3 initialChestRotation;

    private float shotInterval, originalShotInterval = 0.5f;//총알 사이 시간 간격
    private List<GameObject> bulletHoles;
    private int bulletHoleSlot = 0;
    private int burstShotCount = 0;
    private AimBehaviour aimBehaviour;
    private Texture2D originalCrossHair;
    private bool isShooting = false;
    private bool isChangingWeapon = false;
    private bool isShotAlive = false;

    //초기화
    private void Start()
    {
        weaponType = Animator.StringToHash(FC.AnimatorKey.Weapon);
        aimBool = Animator.StringToHash(FC.AnimatorKey.Aim);
        blockedAimBool = Animator.StringToHash(FC.AnimatorKey.BlockedAim);
        changeWeaponTrigger = Animator.StringToHash(FC.AnimatorKey.ChangeWeapon);
        shootingTrigger = Animator.StringToHash(FC.AnimatorKey.Shooting);
        reloadBool = Animator.StringToHash(FC.AnimatorKey.Reload);
        weapons = new List<InteractiveWeapon>(new InteractiveWeapon[3]);
        aimBehaviour = GetComponent<AimBehaviour>();
        bulletHoles = new List<GameObject>();

        muzzleFlash.SetActive(false);
        shot.SetActive(false);
        sparks.SetActive(false);

        //Inventory
        slotMap = new Dictionary<InteractiveWeapon.WeaponType, int>
        {
            {InteractiveWeapon.WeaponType.SHORT,1 },
            {InteractiveWeapon.WeaponType.LONG,2 }
        };

        Transform neck = this.behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Neck);
        //neck이 없는 모델도 있음
        if (!neck)
        {
            neck = this.behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Head).parent;
        }

        hips = this.behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Hips);
        spine= this.behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Spine);
        chest = this.behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Chest);
        rightHand = this.behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        leftArm = this.behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);

        initialRootRotation = (hips.parent == transform) ? Vector3.zero : hips.parent.localEulerAngles;
        initialHipsRotation = hips.localEulerAngles;
        initialSpineRotation = spine.localEulerAngles;
        initialChestRotation = chest.localEulerAngles;

        originalCrossHair = aimBehaviour.crossHair;
        shotInterval = originalShotInterval;
        castRelativeOrigin = neck.position - transform.position;
        distToHand = (rightHand.position - neck.position).magnitude * 1.5f;
    }
    
    //발사 비주얼 담당
    private void DrawShoot(GameObject weapon, Vector3 destination, Vector3 targetNormal, Transform parent, bool placeSparks = true, bool placeBulletHole = true)
    {
        Vector3 origin = gunMuzzle.position - gunMuzzle.right * 0.5f; //살짝 오른쪽으로

        //머즐 활성화
        muzzleFlash.SetActive(true);
        muzzleFlash.transform.SetParent(gunMuzzle);
        muzzleFlash.transform.localPosition = Vector3.zero;
        muzzleFlash.transform.localEulerAngles = Vector3.back * 90f;//뒤쪽을 바라보게

        //총구 발사 이펙트
        GameObject instantShot = EffectManager.Instance.EffectOneShot((int)EffectList.tracer, origin);
        instantShot.SetActive(true);
        instantShot.transform.rotation = Quaternion.LookRotation(destination - origin);
        instantShot.transform.parent = shot.transform.parent; //child로 붙여서 하이라키 안복잡하게

        if (placeSparks)
        {
            //피탄 이펙트
            GameObject instantSparks = EffectManager.Instance.EffectOneShot((int)EffectList.sparks, destination);
            instantSparks.SetActive(true);
            instantSparks.transform.parent = sparks.transform.parent;
        }

        if (placeBulletHole)
        {
            //피탄 이펙트
            Quaternion hitRotation = Quaternion.FromToRotation(Vector3.back, targetNormal);
            GameObject bullet = null;
            if(bulletHoles.Count < MaxBulletHoles)
            {
                bullet = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bullet.GetComponent<MeshRenderer>().material = bulletHole;
                bullet.GetComponent<Collider>().enabled = false;
                bullet.transform.localScale = Vector3.one * 0.07f;
                bullet.name = "BulletHole";
                bulletHoles.Add(bullet);
            }
            //이미 개수 꽉 찼으면 재활용
            else
            {
                bullet = bulletHoles[bulletHoleSlot];
                bulletHoleSlot++;
                bulletHoleSlot %= MaxBulletHoles;
            }

            bullet.transform.position = destination + 0.01f * targetNormal; //살짝 뜨게
            bullet.transform.rotation = hitRotation;
            bullet.transform.SetParent(parent);
        }
    }

    private void ShootWeapon(int weapon,bool firstShot = true)
    {
        //총을 쏠 수 없는 상황일 때 or 총 쏘는 상황이 아닐 떄
        if(!isAiming || isAimBlocked || behaviourController.GetAnimator.GetBool(reloadBool) || !weapons[weapon].Shoot(firstShot))
        {
            return;
        }

        else
        {
            this.burstShotCount++;
            behaviourController.GetAnimator.SetTrigger(shootingTrigger);//총쏘는 애니메이션
            aimBehaviour.crossHair = shootCrossHair;//십자선교체
            behaviourController.GetCamScript.BounceVertical(weapons[weapon].recoilAngle);//반동

            Vector3 imprecision = Random.Range(-shootErrorRate, shootErrorRate) * behaviourController.playerCamera.forward;

            Ray ray = new Ray(behaviourController.playerCamera.position, behaviourController.playerCamera.forward + imprecision); //총 발사 살짝 흔드리게
            RaycastHit hit = default(RaycastHit);
            if(Physics.Raycast(ray,out hit, 500f, shotMask))
            {
                if (hit.collider.transform != transform)
                {
                    bool isOrganic = (organicMask == (organicMask | (1 << hit.transform.root.gameObject.layer)));//생명체인지 확인
                    DrawShoot(weapons[weapon].gameObject, hit.point, hit.normal, hit.collider.transform, !isOrganic, !isOrganic);

                    if (hit.collider)
                    {
                        hit.collider.SendMessage("HitCallBack", new HealthBase.DamageInfo(hit.point, ray.direction, weapons[weapon].bulletDamage, hit.collider), SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
            //총 쐈는데 맞은게 없음
            else
            {
                Vector3 destination = (ray.direction * 500f) - ray.origin;
                DrawShoot(weapons[weapon].gameObject, destination, Vector3.up, null, false, false);//허공에 쏨
            }

            SoundManager.Instance.PlayOneShotEffect((int)weapons[weapon].shotSound, gunMuzzle.position, 5f);
            GameObject gameController = GameObject.FindGameObjectWithTag(FC.TagAndLayer.TagName.GameController);
            gameController.SendMessage("RootAlertNearBy", ray.origin, SendMessageOptions.DontRequireReceiver);
            shotInterval = originalShotInterval;
            isShotAlive = true;
            //interval로 다음 총에 대한 제어
        }
    }

    public void EndReloadWeapon()
    {
        behaviourController.GetAnimator.SetBool(reloadBool, false);
        weapons[activeWeapon].EndReload();
    }

    private void SetWeaponCrossHair(bool armed)
    {
        if (armed)
        {
            aimBehaviour.crossHair = aimCrossHair;
        }
        else
        {
            aimBehaviour.crossHair = originalCrossHair;
        }
    }

    private void ShotPrograss()
    {
        if (shotInterval > 0.2f)
        {
            shotInterval -= shootRateFactor * Time.deltaTime;//팩터 클수록 빠르게 총 쏠 수 있음
            if(shotInterval <= 0.4f) 
            {
                SetWeaponCrossHair(activeWeapon > 0);
                muzzleFlash.SetActive(false);
                if (activeWeapon > 0)
                {
                    behaviourController.GetCamScript.BounceVertical(-weapons[activeWeapon].recoilAngle * 0.1f);//총구 반동 줄여줌

                    
                    if(shotInterval <= (0.4f -2f * Time.deltaTime))
                    {
                        //총 모델에 따라
                        if(weapons[activeWeapon].weaponMode == InteractiveWeapon.WeaponMode.AUTO && 
                            Input.GetAxisRaw(ButtonName.Shoot) != 0)
                        {
                            ShootWeapon(activeWeapon, false);
                        }
                        //샷건
                        else if(weapons[activeWeapon].weaponMode == InteractiveWeapon.WeaponMode.BURST &&
                            burstShotCount < weapons[activeWeapon].burstSize)
                        {
                            ShootWeapon(activeWeapon, false);
                        }
                        else if(weapons[activeWeapon].weaponMode != InteractiveWeapon.WeaponMode.BURST)
                        {
                            burstShotCount = 0;
                        }
                    }
                }
            }
        }
        //총알 발사 후 시간이 좀 지남
        else
        {
            isShotAlive = false;
            behaviourController.GetCamScript.BounceVertical(0);
            burstShotCount = 0;
;        }
    }

    private void ChangeWeapon(int oldWeapon, int newWeapon)
    {
        //획득한 무기 또 획득하면 버리고 재장전
        //빈슬롯 있으면 장착

        //무기가 있음
        //좋은 방법은 아님. Enum으로 하는 게 좋음
        if(oldWeapon > 0)
        {
            weapons[oldWeapon].gameObject.SetActive(false);
            gunMuzzle = null;
            weapons[oldWeapon].Toggle(false);
        }

        while(weapons[newWeapon]==null && newWeapon > 0)
        {
            newWeapon = (newWeapon + 1) % weapons.Count; //빈 슬롯 찾기
            
        }

        //새로운 무기 주움
        if (newWeapon > 0)
        {
            weapons[newWeapon].gameObject.SetActive(true);
            gunMuzzle = weapons[newWeapon].transform.Find("muzzle");
            weapons[newWeapon].Toggle(true);
        }
        activeWeapon = newWeapon;
        if(oldWeapon != newWeapon)
        {
            behaviourController.GetAnimator.SetTrigger(changeWeaponTrigger);//무기 교환 애니메이션
            behaviourController.GetAnimator.SetInteger(weaponType, weapons[newWeapon] ? (int)weapons[newWeapon].weaponType : 0);
        }
        //십자선 변경
        SetWeaponCrossHair(newWeapon > 0);
    }

    private void Update()
    {
        //Debug.Log("activeWeapon : " + activeWeapon);
        //총 쏘고, 재장전하고, 버리고...를 키 입력을 받아서 함수 호출

        float shootTrigger = Mathf.Abs(Input.GetAxisRaw(ButtonName.Shoot));

        //사격 중
        if(shootTrigger>Mathf.Epsilon && !isShooting && activeWeapon >0 && burstShotCount == 0)
        {
            isShooting = true;
            ShootWeapon(activeWeapon);
        }

        //총쏘는 중인데 트리거가 0이면 -> 총 쏘는거 끝남
        else if(isShooting && shootTrigger < Mathf.Epsilon)
        {
            isShooting = false;
        }

        //재장전
        else if(Input.GetButtonUp(ButtonName.Reload)&& activeWeapon > 0)
        {
            if (weapons[activeWeapon].StartReload())
            {
                SoundManager.Instance.PlayOneShotEffect((int)weapons[activeWeapon].reloadSound, gunMuzzle.position, 0.5f);
                behaviourController.GetAnimator.SetBool(reloadBool, true);
            }
        }

        //무기 떨어뜨림
        else if (Input.GetButtonDown(ButtonName.Drop) && activeWeapon > 0)
        {
            EndReloadWeapon();
            int weaponToDrop = activeWeapon;
            ChangeWeapon(activeWeapon, 0);//빈 무기랑 바꿈
            weapons[weaponToDrop].Drop();
            weapons[weaponToDrop] = null;
        }

        //무기 교체중이냐
        else
        {
            if(Mathf.Abs(Input.GetAxisRaw(ButtonName.Change))>Mathf.Epsilon && !isChangingWeapon)
            {
                isChangingWeapon = true;
                int nextWeapon = activeWeapon + 1;
                ChangeWeapon(activeWeapon, nextWeapon % weapons.Count);
            }
            else if(Mathf.Abs(Input.GetAxisRaw(ButtonName.Change)) < Mathf.Epsilon)
            {
                isChangingWeapon = false;
            }
        }

        //총알이 살아있으면
        if (isShotAlive)
        {
            ShotPrograss();
        }
        isAiming = behaviourController.GetAnimator.GetBool(aimBool);
    }



    /// <summary>
    /// 인벤토리 역할을 할 함수
    /// </summary>
    /// <param name="weapon"></param>
    public void AddWeapon(InteractiveWeapon newWeapon)
    {
        newWeapon.gameObject.transform.SetParent(rightHand);
        newWeapon.transform.localPosition = newWeapon.rightHandPosition;
        newWeapon.transform.localRotation = Quaternion.Euler(newWeapon.relativeRotation);

        if (weapons[slotMap[newWeapon.weaponType]])
        {
            //같은 무기면
            if(weapons[slotMap[newWeapon.weaponType]].label_weaponName == newWeapon.label_weaponName)
            {
                weapons[slotMap[newWeapon.weaponType]].ResetBullet();
                ChangeWeapon(activeWeapon, slotMap[newWeapon.weaponType]);
                Destroy(newWeapon.gameObject);//같은 무기니까 하나는 파괴
                return;
            }
            //다른 무기면
            else
            {
                //가지고 있는거 떨궈라
            }
        }
        weapons[slotMap[newWeapon.weaponType]] = newWeapon;
        ChangeWeapon(activeWeapon, slotMap[newWeapon.weaponType]);
    }

    private bool CheckforBlockedAim()
    {
        //구 범위, 손을 중심으로 막혀있는지 확인
        isAimBlocked = Physics.SphereCast(transform.position + castRelativeOrigin, 0.1f, 
            behaviourController.GetCamScript.transform.forward, out RaycastHit hit, distToHand - 0.1f);
        isAimBlocked = isAimBlocked && hit.collider.transform != transform;

        behaviourController.GetAnimator.SetBool(blockedAimBool, isAimBlocked);

        Debug.DrawRay(transform.position + castRelativeOrigin, 
            behaviourController.GetCamScript.transform.forward * distToHand, isAimBlocked
            ? Color.red : Color.cyan);
        return isAimBlocked;

    }

    //IK함수 이용해 조준하고 있으면 상체 기울여줌
    public void OnAnimatorIK(int layerIndex)
    {
        if(isAiming && activeWeapon > 0)
        {
            if (CheckforBlockedAim())
            {
                return;
            }
            Quaternion targetRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            targetRot *= Quaternion.Euler(initialRootRotation);
            targetRot *= Quaternion.Euler(initialHipsRotation);
            targetRot *= Quaternion.Euler(initialSpineRotation);
            behaviourController.GetAnimator.SetBoneLocalRotation(HumanBodyBones.Spine, Quaternion.Inverse(hips.rotation) * targetRot);

            float xcamRot = Quaternion.LookRotation(behaviourController.playerCamera.forward).eulerAngles.x;
            targetRot = Quaternion.AngleAxis(xcamRot + armsRotation, this.transform.right);
            if(weapons[activeWeapon]&&weapons[activeWeapon].weaponType == InteractiveWeapon.WeaponType.LONG)
            {
                //긴 총이면 좀 더 보정

                targetRot *= Quaternion.AngleAxis(9f, transform.right);
                targetRot *= Quaternion.AngleAxis(20f, transform.up);
           
            }

            targetRot *= spine.rotation;
            targetRot *= Quaternion.Euler(initialChestRotation);
            behaviourController.GetAnimator.SetBoneLocalRotation(HumanBodyBones.Chest, Quaternion.Inverse(spine.rotation) * targetRot);
        }
    }

    private void LateUpdate()
    {

        //작은총이면 추가 보정
        if(isAiming && weapons[activeWeapon] && weapons[activeWeapon].weaponType == InteractiveWeapon.WeaponType.SHORT )
        {
            leftArm.localEulerAngles = leftArm.localEulerAngles + leftArmShortAim;
        }
    }
}
