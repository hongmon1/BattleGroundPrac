using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.IO;

/// <summary>
/// Effect Clip 리스트와 이펙트 파일 이름과 경로를 갖고 있는 데이터 클래스
/// 파일을 읽고 쓰는 기능을 가지고 있다.
/// </summary>
public class EffectData : BaseData
{
    //리스트로 만들면 사이크가 무한으로 커질 수 있어서
    //배열로 해야 사고를 방지하기 좋음(개수가 명확하게 세팅되는게 좋아서)
    public EffectClip[] effectClips = new EffectClip[0];

    //해당 프로젝트 Resource -> Prefabs -> Effects 폴더
    public string clipPath = "Effects/";

    private string xmlFilePath = "";
    private string xmlFileName = "effectData.xml";
    private string dataPath = "Data/effectData";

    //XML 구분자
    private const string EFFECT = "effect"; //저장 키
    private const string CLIP = "clip"; //저장 키

    private EffectData() { }

    //읽어오고 저장하고, 데이터를 삭제하고, 특정 클립을 얻어오고 복사하는 기능

    //읽어오기 - Load
    public void LoadData()
    {
        Debug.Log($"xmlFilePath = {Application.dataPath}+{dataDirectory}");

        //Application.dataPaht = 유니티 어셋 폴더 경로
        //dataDirectory -> BaseData의 dataDirectory
        this.xmlFilePath = Application.dataPath + dataDirectory;

        //UnityEngine에 들어있음
        TextAsset asset = (TextAsset)ResourceManager.Load(dataPath);

        //파일이 없거나 읽었는데 아무것도 없음
        if (asset == null || asset.text == null)
        {
            this.AddData("New Effect");
            return;
        }


        using (XmlTextReader reader = new XmlTextReader(new StringReader(asset.text)))
        {
            int currentID = 0;

            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        //총 몇개의 effectclip이 있는지 확인
                        case "length":
                            int length = int.Parse(reader.ReadString());
                            this.names = new string[length];
                            this.effectClips = new EffectClip[length];
                            break;
                        case "id":
                            currentID = int.Parse(reader.ReadString());
                            this.effectClips[currentID] = new EffectClip();
                            this.effectClips[currentID].realId = currentID; //툴에서 관리하기 용이하기 위해
                            break;
                        case "name":
                            this.names[currentID] = reader.ReadString();
                            break;
                        //Enum 타입이라 Enum 클래스 파싱 함수 이용
                        case "effectType":
                            this.effectClips[currentID].effectType = (EffectType)Enum.Parse(typeof(EffectType), reader.ReadString());
                            break;
                        case "effectName":
                            this.effectClips[currentID].effectName = reader.ReadString();
                            break;
                        case "effectPath":
                            this.effectClips[currentID].effectPath = reader.ReadString();
                            break;
                    }
                }
            }
        }

    }

    //저장하기 - Save
    public void SaveData()
    {
        using (XmlTextWriter xml = new XmlTextWriter(xmlFilePath + xmlFileName, System.Text.Encoding.Unicode))
        {

            xml.WriteStartDocument();
            xml.WriteStartElement(EFFECT); //저장 키, 이 키에 대한 값을 넣을거다
            xml.WriteElementString("length", GetDataCount().ToString());

            //clip에 대한 element
            for (int i = 0; i < this.names.Length; i++)
            {
                EffectClip clip = this.effectClips[i];
                xml.WriteStartElement(CLIP);
                xml.WriteElementString("id", i.ToString());
                xml.WriteElementString("name", this.names[i]); //툴에서 이름?
                xml.WriteElementString("effectType", clip.effectType.ToString());
                xml.WriteElementString("effectPath", clip.effectPath);
                xml.WriteElementString("effectName", clip.effectName); //파일에서 이펙트 이름?
                xml.WriteEndElement();
            }

            //effect에 대한 element 종료
            xml.WriteEndElement();
            xml.WriteEndDocument();

        }
    }

    //Add
    public override int AddData(string newname)
    {
        //아무 클립데이터도 저장되어있지 않았으면
        if (this.names == null)
        {
            //클립 하나 추가
            this.names = new string[] { newname };
            this.effectClips = new EffectClip[] { new EffectClip() };
        }
        else
        {
            //ArrayHelper : 강의에서 제공해준 코드
            //특정 타입으로 리스트/배열에 추가
            //ArrayHelper - 툴에서만 사용
            this.names = ArrayHelper.Add(newname, this.names); //names에 name 추가
            this.effectClips = ArrayHelper.Add(new EffectClip(), this.effectClips);

        }

        return GetDataCount();
    }

    //제거
    public override void RemoveData(int index)
    {
        this.names = ArrayHelper.Remove(index, this.names);

        //데이터가 0개가 되면 null로 만들어줌(초기화 해주는 느낌으로)
        if (this.names.Length == 0)
        {
            this.names = null;
        }

        this.effectClips = ArrayHelper.Remove(index, this.effectClips);
    }

    //복사 
    public override void CopyData(int index)
    {
        this.names = ArrayHelper.Add(this.names[index], this.names);
        this.effectClips = ArrayHelper.Add(GetCopy(index), this.effectClips);
    }

    //clear 게임 끌때와 같은 상황에 쓸 수 있음
    public void ClearData()
    {
        foreach (EffectClip clip in this.effectClips)
        {
            clip.ReleaseEffect();
        }
        this.effectClips = null;
        this.names = null;
    }

    //특정 클립복사
    //리플렉션으로 카피 쉽게 만들수 있지만 여기서는 사용 x
    //위에 있는 CopyData에서 사용
    public EffectClip GetCopy(int index)
    {
        if (index < 0 || index >= this.effectClips.Length)
        {
            return null;
        }

        EffectClip original = this.effectClips[index];
        EffectClip clip = new EffectClip();
        clip.effectFullPath = original.effectFullPath;
        clip.effectName = original.effectName;
        clip.effectType = original.effectType;
        clip.effectPath = original.effectPath;
        clip.realId = this.effectClips.Length;
        //prefab의 경우 프리로드 될테니까 복사 안해도 됨
        return clip;
    }

    /// <summary>
    /// 원하는 인덱스를 프리로딩해서 찾아준다
    /// 특정 클립 가져오기
    /// </summary>
    public EffectClip GetClip(int index)
    {
        if (index < 0 || index >= this.effectClips.Length)
        {
            return null;
        }
        effectClips[index].PreLoad();
        return effectClips[index];
    }

    
}
