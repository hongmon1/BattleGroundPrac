using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 이펙트 프리팹과 경로와 타입등의 속성 데이터를 가지고 있게 되며
/// 프리팹 사전로딩(풀로딩) 기능을 갖고 있고 - 풀링을 위한 기능이기도 함
/// 이펙트 인스턴스 기능도 갖고 있으며 - 풀링과 연계해서 사용하기도 함
/// </summary>
public class EffectClip
{
    //클립을 복사할것이기 때문
    //구별하기 위해(디버깅에 용이)
    //추후 속성은 같지만 다른 이펙트 클립이 있을 수 있어서 분별용
    public int realId = 0;

    public EffectType effectType = EffectType.NORMAL;
    public GameObject effectPrefab = null;
    //폴더까지의 경로
    public string effectPath = string.Empty;
    //리소스 파일 이름
    public string effectName = string.Empty;
    //path+name
    public string effectFullPath = string.Empty;

    public EffectClip() { }

    //사전로딩
    public void PreLoad()
    {
        this.effectFullPath = effectPath + effectName;

        //경로 데이터가 있으며 사전로딩이 아직 안된 경우
        if(this.effectFullPath != string.Empty && this.effectPrefab == null)
        {
            this.effectPrefab = ResourceManager.Load(effectFullPath) as GameObject;
        }
    }

    //메모리
    public void ReleaseEffect()
    {
        if(this.effectPrefab != null)
        {
            this.effectPrefab = null;
        }
        //가비지컬렉터에서 참조하지 않는군! 하고 해제해줌
    }

    //인스턴스
    /// <summary>
    /// 원하는 위치에 내가 원하는 이펙트를 인스턴스 한다.
    /// </summary>
    public GameObject Instantiate(Vector3 Pos)
    {
        if(this.effectPrefab == null)
        {
            //데이터 읽어오기
            this.PreLoad();
        }

        //읽어온 데이터로 인스턴스
        if (this.effectPrefab != null)
        {
            GameObject effect = GameObject.Instantiate(effectPrefab, Pos, Quaternion.identity);
            return effect;
        }
        return null;
    }
}
