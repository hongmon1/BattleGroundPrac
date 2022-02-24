using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무기를 획득하면 획득한 무기를 UI를 통해 보여주고
/// 현재 잔탄량과 전체 소지할 수 있는 총알량을 출력
/// </summary>
public class WeaponUIManager : MonoBehaviour
{

    public Color bulletColor = Color.white;
    public Color emptyBulletColor = Color.black;

    private Color noBulletColor;//총알 없으면 투명하게 색깔 표시

    //링크 걸면 UI나 이미지 위치 바뀌면 링크 깨짐
    //Find 쓰면 위의 문제 해결, 그러나 이름을 바꾸면 못찾음
    //실무에서는 두개 섞어 씀
    //스타트에서 링크걸린게 없으면 find로 찾아줌

    [SerializeField] private Image weaponHUD;
    [SerializeField] private GameObject bulletMag;
    [SerializeField] private Text totalHulletsHUD;

    // Start is called before the first frame update
    void Start()
    {
        noBulletColor = new Color(0f, 0f, 0f, 0f);

        //유니티에서 링크 걸어두지만 꺠지는 경우 대비해 find
        if (weaponHUD == null)
        {
            weaponHUD = transform.Find("WeaponHUD/Weapon").GetComponent<Image>();
        }
        if (bulletMag == null)
        {
            bulletMag = transform.Find("WeaponHUD/Data/Mag").gameObject;
        }
        if (totalHulletsHUD == null)
        {
            totalHulletsHUD = transform.Find("WeaponHUD/Data/Label").GetComponent<Text>();
        }

        Toggle(false);
    }

    //무기 hud 껐다켰다
    //무기 들 때 켜짐
    public void Toggle(bool active)
    {
        weaponHUD.transform.parent.gameObject.SetActive(active);
    }

    //weapon hud update
    public void UpdateWeaponHUD(Sprite weaponSprite, int bulletLeft, int fullMag, int ExtraBullet)
    {
        //무기이미지 교체
        if(weaponSprite != null && weaponHUD.sprite != weaponSprite)
        {
            weaponHUD.sprite = weaponSprite;
            weaponHUD.type = Image.Type.Filled;
            weaponHUD.fillMethod = Image.FillMethod.Horizontal;
        }

        int bulletCount = 0;
        foreach(Transform bullet in bulletMag.transform)
        {
            //잔탄 보여줌
            if (bulletCount < bulletLeft)
            {
                bullet.GetComponent<Image>().color = bulletColor;
            }
            //넘치는 탄
            else if(bulletCount >= fullMag)
            {
                bullet.GetComponent<Image>().color = noBulletColor;
            }
            //사용한 탄
            else
            {
                bullet.GetComponent<Image>().color = emptyBulletColor;
            }
            bulletCount++;
        }
        totalHulletsHUD.text = bulletLeft + "/" + ExtraBullet;
    }
}
