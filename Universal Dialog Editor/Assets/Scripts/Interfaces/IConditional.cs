using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UDSCondition
{
    public string globalPropertyKey;
    public string operation;
    public object compareTo;
}

public interface IConditional
{
    public void SetCondition(UDSCondition condition);
    public UDSCondition GetCondition();
}
