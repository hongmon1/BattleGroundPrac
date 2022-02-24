using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어의 생명력을 담당
/// 피격시 피격이펙트 표시하거나 UI업데이트 함
/// 죽었을 경우 모든 동작 스크립트 동작을 멈춘다.
/// </summary>
public class PlayerHealth : HealthBase
{
    public float health = 100f;
    public float criticalHealth = 30f;
    public Transform healthHUD;
    public SoundList deathSound;
    public SoundList hitSound;
    public GameObject hurtPrefab;//피격 프리펩
    public float decayFactor = 0.8f;//감쇠

    private float totalHealth;
    private RectTransform healthBar, placeHolderBar;
    private Text healthLabel;
    private float originalBarScale;
    private bool critical;

    private void Awake()
    {
        //캐싱
        myAnimator = GetComponent<Animator>();
        totalHealth = health;

        healthBar = healthHUD.Find("HealthBar/Bar").GetComponent<RectTransform>();
        placeHolderBar = healthHUD.Find("HealthBar/Placeholder").GetComponent<RectTransform>();
        healthLabel = healthHUD.Find("HealthBar/Label").GetComponent<Text>();

        originalBarScale = healthBar.sizeDelta.x;
        healthLabel.text = "" + (int)health;
    }

    private void Update()
    {
        //피가 닳때 천천히 줄어들게
        if(placeHolderBar.sizeDelta.x > healthBar.sizeDelta.x)
        {
            placeHolderBar.sizeDelta = Vector2.Lerp(placeHolderBar.sizeDelta, healthBar.sizeDelta, 2f * Time.deltaTime);
        }
    }

    //풀피냐?
    public bool IsFullLife()
    {
        return Mathf.Abs(health - totalHealth) < float.Epsilon;
    }

    //HP 줄어들 때 표시
    //요즘은 바인더 만들어서 특정 UI 데이터 바뀌면 리스너 통해 받아서 자동 갱신을 많이 함
    private void UpdateHealthBar()
    {
        healthLabel.text = "" + (int)health;
        float scaleFactor = health / totalHealth;
        healthBar.sizeDelta = new Vector2(scaleFactor * originalBarScale, healthBar.sizeDelta.y);
    }

    //죽음
    private void Kill()
    {
        IsDead = true;
        gameObject.layer = FC.TagAndLayer.GetLayerByName(FC.TagAndLayer.LayerName.Default);
        gameObject.tag = FC.TagAndLayer.TagName.Untagged;
        healthHUD.gameObject.SetActive(false);
        healthHUD.parent.Find("WeaponHUD").gameObject.SetActive(false);

        //애니메이션 끔
        myAnimator.SetBool(FC.AnimatorKey.Aim, false);
        myAnimator.SetBool(FC.AnimatorKey.Cover, false);
        myAnimator.SetFloat(FC.AnimatorKey.Speed, 0);

        //동작 다 꺼줌
        foreach(GenericBehaviour behaviour in GetComponentsInChildren<GenericBehaviour>())
        {
            behaviour.enabled = false;
        }

        SoundManager.Instance.PlayOneShotEffect((int)deathSound, transform.position, 5f);//죽음 사운드
    }

    public override void TakeDamage(Vector3 location, Vector3 direction, float damage, Collider bodyPart = null, GameObject origin = null)
    {
        health -= damage;
        UpdateHealthBar();

        if(health <= 0)
        {
            Kill();
        }
        //일정 체력 이하
        else if(health<=criticalHealth && !critical)
        {
            critical = true;
        }

        SoundManager.Instance.PlayOneShotEffect((int)hitSound, location, 1f);
    }
}
