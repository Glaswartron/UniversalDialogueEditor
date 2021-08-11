using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct PropertyPreset
{
    public string id;
    public Dictionary<string, UDSProperty> properties;
    public List<string> orderedKeyList;
    public PropertyPresetType propertyPresetType;

    [Serializable]
    public enum PropertyPresetType
    {
        DIALOG_PART, ANSWER
    }
}

public class PropertiesUI : MonoBehaviour, ISubUI
{
    // If the dialogueComponent is null => Global properties
    private DialogueComponent dialogueComponent;

    [Header("Main UI")]
    public Transform scrollViewContent;
    public Button addPropertyButton;
    public AddPropertyDropdown addPropertyDropdown;

    [Header("Presets")]
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
                    = (Vector2) Input.mousePosition + ContextMenuManager.instance.menuOffsetFromMouse;

                addPropertyDropdown.gameObject.SetActive(true);
            }
        );

        if (savePresetButton != null)
        {
            savePresetButton.onClick.AddListener(
                () =>
                {
                    EditorManager.instance.ActiveMenu
                        = EditorManager.instance.savePresetMenu.gameObject;

                    Dictionary<string, UDSProperty> properties = GetPropertiesForPreset();
                    PropertyPreset.PropertyPresetType type = GetTypeForPreset();

                    // Ordered
                    List<string> keys = listElements.ConvertAll(le => le.id);

                    EditorManager.instance.savePresetMenu.Init(properties, keys, type);
                }
            );
        }

        if (loadPresetButton != null)
        {
            loadPresetButton.onClick.AddListener(
                () =>
                {
                    EditorManager.instance.ActiveMenu
                        = EditorManager.instance.loadPresetMenu.gameObject;

                    EditorManager.instance.loadPresetMenu.Init(this, GetTypeForPreset());
                }
            );
        }

        // Close Button only there if it's the Global Properties menu
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
    /// of the given dialogueComponent OR the Global
    /// Properties (if dialogueComponent is null). In
    /// that case, use GlobalPropertiesUI
    /// </summary>
    /// <param name="dialogueComponent">The dialogueComponent this PropertiesUI 
    /// is responsible for. Null if it is for Global Properties</param>
    public virtual void Init(DialogueComponent dialogueComponent)
    {
        listElements = new List<PropertyListElement>();

        this.dialogueComponent = dialogueComponent;

        InitAddPropertyDropdown();

        /* Instantiate a fitting list/scroll view element for all properties
         * and add listeners to the various UI elements within it. */
        foreach (string key in dialogueComponent.GetPropertyKeys())
        {
            UDSProperty property = dialogueComponent.GetProperty(key);

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
        listElement.Init(dialogueComponent);

        if (property.required)
            listElement.deleteButton.gameObject.SetActive(false);
        else
        {
            listElement.deleteButton.gameObject.SetActive(true);

            // Delete Button
            listElement.deleteButton.onClick.AddListener(
                () =>
                {
                    string localKey = listElement.id;
                    PropertyListElement localListElement = listElement;

                    if (!string.IsNullOrWhiteSpace(localKey))
                        dialogueComponent.DeleteProperty(localKey); // !

                    listElements.Remove(localListElement);

                    Destroy(localListElement.gameObject);
                }
            );
        }
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

    public void LoadPropertyPreset(PropertyPreset? preset)
    {
        if (preset == null)
        {
            Debug.LogError("Preset passed to LoadPropertyPreset is null");
            return;
        }

        // Clear properties (except for the required ones)
        dialogueComponent.DeleteAllProperties();

        ClearScrollView();

        Dictionary<string, UDSProperty> properties = preset.Value.properties;

        foreach (string propertyKey in preset.Value.orderedKeyList)
        {
            UDSProperty property = properties[propertyKey];
            dialogueComponent.SetProperty(propertyKey, property.value, property.type, property.required);
        }

        Init(dialogueComponent); // Re-init
    }

    private Dictionary<string, UDSProperty> GetPropertiesForPreset()
    { 
        return dialogueComponent.GetProperties();
    }

    private PropertyPreset.PropertyPresetType GetTypeForPreset()
    {
        if (dialogueComponent is Dialogue.DialoguePart)
            return PropertyPreset.PropertyPresetType.DIALOG_PART;
        else if (dialogueComponent is Dialogue.DialoguePart.Answer)
            return PropertyPreset.PropertyPresetType.ANSWER;

        return default;
    }

    protected void ClearScrollView()
    {
        /* Must go backwards to prevent InvalidOperationException
         * (similar to ConcurrentModificationException in Java) */
        for (int i = listElements.Count - 1; i >= 0; i--)
        {
            if (listElements[i] != null && listElements[i].gameObject != null)
            {
                if (string.IsNullOrWhiteSpace(listElements[i].id))
                {
                    if (dialogueComponent != null)
                    {
                        ErrorMessage.instance.ShowErrorMessage
                            ("A Property without an ID was found and discarded on Dialogue Component " + dialogueComponent.id);
                    }
                    else
                    {
                        ErrorMessage.instance.ShowErrorMessage
                            ("A Global Property without an ID was found and discarded");
                    }
                }

                GameObject go = listElements[i].gameObject;

                Destroy(go);
            }
        }

        listElements.Clear();
    }

    protected void OnDisable()
    {
        addPropertyDropdown.gameObject.SetActive(false);

        ClearScrollView();
    }

    private bool IsMouseOverAddPropertyDropdown()
    {
        return addPropertyDropdown.gameObject.activeSelf
            && Utility.IsMouseOverUI(addPropertyDropdown.GetComponent<RectTransform>());
    }
}
