using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class GlobalPropertiesUI : PropertiesUI
{
    new List<GlobalPropertyListElement> listElements;

    public override void Init(DialogComponent dialogComponent = null)
    {
        if (dialogComponent != null)
            Debug.LogWarning("GlobalPropertiesUI.Init is called with dialogComponent != null");

        listElements = new List<GlobalPropertyListElement>();
        InitAddPropertyDropdown();

        Dictionary<string, UDSProperty> properties = EditorManager.globalProperties;

        foreach (string id in properties.Keys)
        {
            CreateListElement(id, properties[id]);
        }
    }

    protected override void InitListElement(GameObject listElementGO, string id, UDSProperty property)
    {
        GlobalPropertyListElement listElement = listElementGO.GetComponent<GlobalPropertyListElement>();

        listElements.Add(listElement);

        listElement.id = id;
        listElement.idInputField.SetTextWithoutNotify(id);

        listElement.stringIntFloatInputField.SetTextWithoutNotify(
                // Converts value.value to a string based on value.type
                TypeDescriptor.GetConverter(property.type).ConvertToString(property.value)
            );

        listElement.type = property.type; // Important

        // Takes care of all UI elements except for the Delete Button
        listElement.Init();

        // Delete Button
        listElement.deleteButton.onClick.AddListener(
            () =>
            {
                string localKey = id;

                EditorManager.globalProperties.Remove(localKey);

                listElements.Remove(listElement);
                Destroy(listElement.gameObject);
            }
        );
    }

}
