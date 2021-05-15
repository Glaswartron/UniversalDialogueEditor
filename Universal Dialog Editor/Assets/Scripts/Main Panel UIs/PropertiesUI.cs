using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct PropertyPreset
{
    public string id;
    public List<Dictionary<string, UDSProperty>> properties;
    public PropertyPresetType propertyPresetType;

    [Serializable]
    public enum PropertyPresetType
    {
        DIALOG_PART, ANSWER
    }
}

public class PropertiesUI : MonoBehaviour, ISubUI
{
    // If the dialogComponent is null => Global properties
    private DialogComponent dialogComponent;

    [Header("Main UI")]
    public Transform scrollViewContent;
    public Button addPropertyButton;
    public AddPropertyDropdown addPropertyDropdown;
    public Button loadPresetButton; 
    public Button savePresetButton;

    [Header("Optional")]
    public TMP_InputField searchBar; // Optional
    public Button closeButton; // Optional

    [Header("Prefabs")]
    public GameObject stringProperty;
    public GameObject intProperty;
    public GameObject floatProperty;
    public GameObject boolProperty;

    protected List<PropertyListElement> listElements;

    public void Start()
    {
        addPropertyButton.onClick.AddListener(
            () =>
            {
                addPropertyDropdown.transform.position 
                    = (Vector2)Input.mousePosition 
                        + EditorManager.instance.menuOffsetFromMouse;

                addPropertyDropdown.gameObject.SetActive(true);
            }
        );

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                () =>
                {
                    FileHandler.SaveGlobalProperties(); // !

                    EditorManager.instance.ActiveMenu = null;
                }
            );
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            if (!IsMouseOverAddPropertyDropdown())
                addPropertyDropdown.gameObject.SetActive(false);
    }

    /// <summary>
    /// Sets up the PropertiesUI and populates the
    /// connected ScrollView based on the properties 
    /// of the given dialogComponent OR the Global
    /// Properties (if dialogComponent is null). In
    /// that case, use GlobalPropertiesUI
    /// </summary>
    /// <param name="dialogComponent">The dialogComponent this PropertiesUI 
    /// is responsible for. Null if it is for Global Properties</param>
    public virtual void Init(DialogComponent dialogComponent)
    {
        listElements = new List<PropertyListElement>();

        this.dialogComponent = dialogComponent;

        InitAddPropertyDropdown();

        /* Instantiate a fitting list/scroll view element for all properties
         * and add listeners to the various UI elements within it. */
        foreach (string key in dialogComponent.GetPropertyKeys())
        {
            UDSProperty property = dialogComponent.GetProperty(key);

            CreateListElement(key, property);
        }
    }

    protected void CreateListElement(string id, UDSProperty property)
    {
        GameObject prefab = null;

        // Determine correct list element based on type of value
        Type type = property.type;
        if (type == typeof(string))
            prefab = stringProperty;
        else if (type == typeof(int))
            prefab = intProperty;
        else if (type == typeof(float))
            prefab = floatProperty;
        else if (type == typeof(bool))
            prefab = boolProperty;
        else Debug.LogError("Property type proplems");

        GameObject listElementGO = Instantiate(prefab, scrollViewContent);

        InitListElement(listElementGO, id, property);
    }

    protected virtual void InitListElement(GameObject listElementGO,
        string id, UDSProperty property)
    {
        PropertyListElement listElement = listElementGO.GetComponent<PropertyListElement>();

        listElements.Add(listElement);
        
        listElement.id = id;
        listElement.idInputField.SetTextWithoutNotify(id);

        listElement.type = property.type; // Important

        // Takes care of all UI elements except for the Delete Button
        listElement.Init(dialogComponent);

        // Delete Button
        listElement.deleteButton.onClick.AddListener(
            () =>
            {
                string localKey = listElement.id;

                EditorManager.globalProperties.Remove(localKey);

                listElements.Remove(listElement);

                Destroy(listElement.gameObject);
            }
        );
    }

    public void InitAddPropertyDropdown()
    {
        // Important since the Dropdown is being reused
        addPropertyDropdown.stringPropertyButton.onClick.RemoveAllListeners();
        addPropertyDropdown.intPropertyButton.onClick.RemoveAllListeners();
        addPropertyDropdown.boolPropertyButton.onClick.RemoveAllListeners();
        addPropertyDropdown.floatPropertyButton.onClick.RemoveAllListeners();

        addPropertyDropdown.stringPropertyButton.onClick.AddListener(
            () =>
            {
                GameObject newListElement = Instantiate(stringProperty, scrollViewContent);
                listElements.Add(newListElement.GetComponent<PropertyListElement>());
                InitListElement(newListElement, "", new UDSProperty("", typeof(string)));
                addPropertyDropdown.gameObject.SetActive(false);
            }
        );

        addPropertyDropdown.intPropertyButton.onClick.AddListener(
            () =>
            {
                GameObject newListElement = Instantiate(intProperty, scrollViewContent);
                listElements.Add(newListElement.GetComponent<PropertyListElement>());
                InitListElement(newListElement, "", new UDSProperty(0, typeof(int)));
                addPropertyDropdown.gameObject.SetActive(false);
            }
        );

        addPropertyDropdown.boolPropertyButton.onClick.AddListener(
            () =>
            {    
                GameObject newListElement = Instantiate(boolProperty, scrollViewContent);
                listElements.Add(newListElement.GetComponent<PropertyListElement>());
                InitListElement(newListElement, "", new UDSProperty(false, typeof(bool)));
                addPropertyDropdown.gameObject.SetActive(false);
            }
        );

        addPropertyDropdown.floatPropertyButton.onClick.AddListener(
            () =>
            {
                GameObject newListElement = Instantiate(floatProperty, scrollViewContent);
                listElements.Add(newListElement.GetComponent<PropertyListElement>());
                InitListElement(newListElement, "", new UDSProperty(0, typeof(float)));
                addPropertyDropdown.gameObject.SetActive(false);
            }
        );
    }

    protected void ClearScrollView()
    {
        /* Must go backwards to prevent InvalidOperationException
         * (similar to ConcurrentModificationException in Java) */
        for (int i = listElements.Count - 1; i >= 0; i--)
        {
            GameObject go = listElements[i].gameObject;
            Destroy(go);
        }
    }

    protected void OnDisable()
    {
        ClearScrollView();

        listElements.Clear();
    }

    private bool IsMouseOverAddPropertyDropdown()
    {
        return addPropertyDropdown.gameObject.activeSelf
            && Utility.IsMouseOverUI(addPropertyDropdown.GetComponent<RectTransform>());
    }
}
