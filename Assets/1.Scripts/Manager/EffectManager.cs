using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//SingletonMonobehaviour는 강좌에서 만들어놓은 코드
public class EffectManager : SingletonMonobehaviour<EffectManager>
{

    private Transform effectRoot = null;

    // Start is called before the first frame update
    void Start()
    {
        if(effectRoot == null)
        {
            effectRoot = new GameObject("EffectRoot").transform;
            effectRoot.SetParent(transform);
        }    
        //이펙트가 이펙트매니저밑으로 붙도록
        // 풀링할 때 이펙트루트 밑으로
    }

    public GameObject EffectOneShot(int index, Vector3 position)
    {
        EffectClip clip = DataManager.EffectData().GetClip(index); //preloading하고 한걸 돌려줌
        GameObject effectInstance = clip.Instantiate(position); //원하는 위치에 클립 인스턴스
        effectInstance.SetActive(true);
        return effectInstance;
    }
}
