using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// data의 기본 클래스입니다
/// 공통적인 데이터를 가지고 있음(현재는 이름만)
/// 데이터의 갯수와 이름의 목록 리스트를 얻을 수 있음
/// </summary>
public class BaseData : ScriptableObject
{
    //data 디렉토리 위치
    public const string dataDirectory = "/9.ResourcesData/Resources/Data/";

    public string[] names = null;

    //데이터 클래스 XML 사용할 것임
    public BaseData(){ }
    
    /// <summary>
    /// 데이터 개수 반환
    /// </summary>
    public int GetDataCount()
    {
        int retValue = 0;

        if(this.names != null)
        {
            retValue = this.names.Length;
        }
        return retValue;
    }

    /// <summary>
    /// 툴에 출력하기 위한 이름 목록 만들어줌
    /// </summary>
    public string[] GetNameList(bool showID, string filterWord="")
    {
        string[] retList = new string[0];

        if(this.names == null) return retList;

        retList = new string[this.names.Length];

        for(int i = 0; i < this.names.Length; i++)
        {
            //필터가 있으면
            if (filterWord != "")
            {
                //필터에 안걸리는 값 스킵
                if (names[i].ToLower().Contains(filterWord.ToLower()) == false) continue;

            }

            //인덱스 번호 보여줄건지
            if (showID) retList[i] = i.ToString() + " : " + this.names[i];
            else retList[i] = this.names[i];
        }

        return retList;
    }

    public virtual int AddData(string newname)
    {
        return GetDataCount();
    }

    public virtual void RemoveData(int index)
    {
    }

    public virtual void CopyData(int index)
    {
    }
}
