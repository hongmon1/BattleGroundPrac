using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 생명력 관리 기반 클래스스
/// </summary>
public class HealthBase : MonoBehaviour
{
    public class DamageInfo
    {
        public Vector3 location, direction; //데미지 입은 위치, 방향(이펙트 위해
        public float damage;
        public Collider bodyPart;//맞았을 때 특정 파트인지 구분
        public GameObject origin; //피격 이펙트에 쓰임

        public DamageInfo(Vector3 location, Vector3 direction, float damage, Collider bodyPart = null, GameObject origin = null)
        {
            this.location = location;
            this.direction = direction;
            this.damage = damage;
            this.bodyPart = bodyPart;
            this.origin = origin;
        }

    }

    [HideInInspector]public bool IsDead;
    protected Animator myAnimator;

    public virtual void TakeDamage(Vector3 location, Vector3 direction, float damage, Collider bodyPart = null, GameObject origin = null) { }

    //피격 콜백
    public void HitCallBack(DamageInfo damageInfo)
    {
        this.TakeDamage(damageInfo.location, damageInfo.direction, damageInfo.damage, damageInfo.bodyPart, damageInfo.origin);
    }
}
