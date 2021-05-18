using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalPropertiesUI : PropertiesUI
{
    private void OnEnable()
    {
        Init();
    }

    public override void Init(DialogComponent dialogComponent = null)
    {
        if (dialogComponent != null)
            Debug.LogWarning("GlobalPropertiesUI.Init is called with dialogComponent != null");

        listElements = new List<PropertyListElement>();

        InitAddPropertyDropdown();

        Dictionary<string, UDSProperty> properties = EditorManager.globalProperties;

        InitListElements(properties.Keys);

        searchBar.onValueChanged.AddListener(
            (input) =>
            {
                if (!string.IsNullOrWhiteSpace(input))
                {
                    List<string> ids = SearchProperties(input);
                    InitListElements(ids);
                } else
                    InitListElements(EditorManager.globalProperties.Keys);
            }
        );
    }

    protected override void InitListElement(GameObject listElementGO, string id, UDSProperty property)
    {
        GlobalPropertyListElement listElement = listElementGO.GetComponent<GlobalPropertyListElement>();

        listElements.Add(listElement);

        listElement.id = id;
        listElement.idInputField.SetTextWithoutNotify(id);

        listElement.type = property.type; // Important

        // Takes care of all UI elements except for the Delete Button
        listElement.Init();

        if (property.required)
            listElement.deleteButton.gameObject.SetActive(false);
        else
            listElement.deleteButton.gameObject.SetActive(true);

        // Delete Button
        listElement.deleteButton.onClick.AddListener(
            () =>
            {
                string localKey = id;

                EditorManager.globalProperties.Remove(localKey); // !

                listElements.Remove(listElement);

                Destroy(listElement.gameObject);
            }
        );
    }

    private List<string> SearchProperties(string query)
    {
        List<string> matchingGlobalPropertyIDs = new List<string>();

        foreach (string id in EditorManager.globalProperties.Keys)
        {
            if (id.ToLower().Contains(query.ToLower()))
                matchingGlobalPropertyIDs.Add(id);
        }

        return matchingGlobalPropertyIDs;
    }

    private void InitListElements(IEnumerable properties)
    {
        ClearScrollView();
        listElements.Clear();

        foreach (string id in properties)
        {
            CreateListElement(id, EditorManager.globalProperties[id]);
        }
    }

}
