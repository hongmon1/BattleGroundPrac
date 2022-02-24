using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 총 4단계에 걸쳐 사격
/// 1. 조준 중이고 조준 유효 각도 안에 타겟이 있거나 가깝다면
/// 2. 발사 간격 딜레이충분히 되었다면 애니메이션을 재생
/// 3. 충돌 검출을 하는데 약간의 사격시 충격파도 더해주게 된다 (오발률 만듬)
/// 4. 총구 이펙트 및 총알 이펙트를 생성
/// </summary>
/// 

[CreateAssetMenu(menuName = "PluggableAI/Actions/Attack")]
public class AttackAction : Action
{
    private readonly float startShootDelay = 0.2f;
    private readonly float aimAngleGap = 30f; //사격 각도

    public override void OnReadyAction(StateController controller)
    {
        controller.variables.shotsInRounds = Random.Range(controller.maximuBurst/2, controller.maximuBurst);//이번 사격에 쏠 수 있는 총알 개수
        controller.variables.currentShots = 0;
        controller.variables.startShootTimer = 0f;
        controller.enemyAnimation.anim.ResetTrigger(FC.AnimatorKey.Shooting);
        controller.enemyAnimation.anim.SetBool(FC.AnimatorKey.Crouch, false); //엄폐중이면 일어남
        controller.variables.waitInCoverTime = 0;
        controller.enemyAnimation.ActivatePendingAim();//조준 대기. 이제 시야에만 들어오면 조준 가능
    }

    //organic : 생명체인지 아닌지
    //4단계
    private void DoShot(StateController controller, Vector3 direction, Vector3 hitPoint, 
        Vector3 hitNormal = default, bool organic = false, Transform target = null)
    {
        //4번 단계 - 총구 이펙트
        GameObject muzzleFlash = EffectManager.Instance.EffectOneShot((int)EffectList.flash, Vector3.zero);
        muzzleFlash.transform.SetParent(controller.enemyAnimation.gunMuzzle);
        muzzleFlash.transform.localPosition = Vector3.zero;
        muzzleFlash.transform.localEulerAngles = Vector3.left * 90f;
        DestroyDelayed destroyDelayed = muzzleFlash.AddComponent<DestroyDelayed>();
        destroyDelayed.DelayTime = 0.5f; //auto destroy

        //4번 단계 - 총알
        GameObject shotTracer = EffectManager.Instance.EffectOneShot((int)EffectList.tracer, Vector3.zero);
        shotTracer.transform.SetParent(controller.enemyAnimation.gunMuzzle);
        //총구 방향으로 총알 이펙트
        Vector3 origin = controller.enemyAnimation.gunMuzzle.position;
        shotTracer.transform.position = origin;
        shotTracer.transform.rotation = Quaternion.LookRotation(direction);
        
        //organic 아니면 피탄 이펙트 만듬
        if(target && !organic)
        {
            GameObject bulletHole = EffectManager.Instance.EffectOneShot((int)EffectList.bulletHole, hitPoint + 0.01f * hitNormal);
            bulletHole.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitNormal);

            GameObject InstantSpark = EffectManager.Instance.EffectOneShot((int)EffectList.sparks, hitPoint); //스파크 이펙트
        }
        else if(target && organic) //플레이어 맞춘 경우
        {
            HealthBase targetHealth = target.GetComponent<HealthBase>(); //player Health
            if (targetHealth)
            {
                targetHealth.TakeDamage(hitPoint, direction, controller.classStats.BulletDamage, 
                    target.GetComponent<Collider>(), controller.gameObject);
            }
        }

        SoundManager.Instance.PlayShotSound(controller.classID, controller.enemyAnimation.gunMuzzle.position, 2f);
    }

    //3단계
    private void CastShot(StateController controller)
    {
        //충격파 줘서 오발률을 만듬
        //오른쪽, 위쪽으로
        Vector3 imprecision = Random.Range(-controller.classStats.ShotErrorRate, controller.classStats.ShotErrorRate) * controller.transform.right;
        imprecision += Random.Range(-controller.classStats.ShotErrorRate, controller.classStats.ShotErrorRate) * controller.transform.up;

        Vector3 shotDirection = controller.personalTarget - controller.enemyAnimation.gunMuzzle.position;
        shotDirection = shotDirection.normalized + imprecision; //약간 빗나가게

        Ray ray = new Ray(controller.enemyAnimation.gunMuzzle.position, shotDirection);//총구에서 약간 빗나간 확률을 적용한 ray가 나감
        if(Physics.Raycast(ray, out RaycastHit hit, controller.viewRadius, controller.generalStats.shotMask.value))
        {
            //부딪힌 것이 존재

            bool isOrganic = ((1 << hit.transform.root.gameObject.layer) & controller.generalStats.targetMask) != 0; //0이 아니면 organic 임
            DoShot(controller, ray.direction, hit.point, hit.normal, isOrganic, hit.transform);
        }
        else
        {
            DoShot(controller, ray.direction, ray.origin + (ray.direction * 500f)); //맞춘게 없으니 허공에 발사
        }
    }

    private bool CanShoot(StateController controller)
    {
        float distance = (controller.personalTarget - controller.enemyAnimation.gunMuzzle.position).sqrMagnitude;// 타겟과 총구사이 거리
        //사격거리 괜찮고 조준 중이고 각도 안이면
        if (controller.Aiming &&(controller.enemyAnimation.currentAimingAngleGap<aimAngleGap || distance <= 5.0f))
        {
            if (controller.variables.startShootTimer >= startShootDelay)
            {
                return true;
            }
            else
            {
                controller.variables.startShootTimer += Time.deltaTime;
            }
        }
        return false;
    }

    //2번째 단계
    private void Shoot(StateController controller)
    {
        //시간이 0이 되면 발사
        if(Time.timeScale > 0 && controller.variables.shotTimer == 0f)
        {
            controller.enemyAnimation.anim.SetTrigger(FC.AnimatorKey.Shooting);
            CastShot(controller);
        }
        //타이머가 지났으면(애니메이션 쏘는 시간 필요)
        else if(controller.variables.shotTimer >= (0.1f + 2f * Time.deltaTime))
        {
            controller.bullets = Mathf.Max(--controller.bullets, 0);
            controller.variables.currentShots++;
            controller.variables.shotTimer = 0;
            return;
        }
        controller.variables.shotTimer += controller.classStats.ShotRateFactor * Time.deltaTime; //ShotRateFactor가 높을수록 사격 빨리 함
    }

    public override void Act(StateController controller)
    {
        controller.focusSight = true;
        if (CanShoot(controller))
        {
            Shoot(controller);
        }
        controller.variables.blindEngageTimer += Time.deltaTime;
    }

}
