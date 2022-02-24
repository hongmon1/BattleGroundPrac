using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Transition 
{
    public Decision decision;
    public State trueState; //decision이 트루일 때 다음 스테이트
    public State falseState;//decision이 false일 떄 다음 스테이트
}
