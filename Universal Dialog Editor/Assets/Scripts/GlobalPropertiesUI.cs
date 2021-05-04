using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalPropertiesUI : PropertiesUI
{
    public override void Init(DialogComponent dialogComponent)
    {
        if (dialogComponent != null)
            Debug.LogWarning("GlobalPropertiesUI.Init is called with dialogComponent != null");

        listElements = new List<PropertyListElement>();
        InitAddPropertyDropdown();


    }

}
