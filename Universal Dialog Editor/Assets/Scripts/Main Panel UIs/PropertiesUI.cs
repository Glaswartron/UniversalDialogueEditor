using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class PropertiesUI : MonoBehaviour, ISubUI
{
    private DialogComponent dialogComponent;

    [Header("Main UI")]
    public Transform scrollViewContent;
    public Button addPropertyButton;
    public Button loadPresetButton;
    public Button savePresetButton;
    public AddPropertyDropdown addPropertyDropdown;

    [Header("Prefabs")]
    public GameObject stringProperty;
    public GameObject intProperty;
    public GameObject floatProperty;
    public GameObject boolProperty;

    private List<PropertyListElement> listElements;

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
    }

    /// <summary>
    /// Sets up the PropertiesUI and populates the
    /// connected ScrollView based on the properties 
    /// of the given dialogComponent
    /// </summary>
    /// <param name="dialogComponent">The dialogComponent this PropertiesUI 
    /// is responsible for</param>
    public void Init(DialogComponent dialogComponent)
    {
        listElements = new List<PropertyListElement>();

        this.dialogComponent = dialogComponent;

        InitAddPropertyDropdown();

        /* Instantiate a fitting list/scroll view element for all properties
         * and add listeners to the various UI elements within it. */
        foreach (string key in dialogComponent.GetPropertyKeys())
        {
            (object value, Type type) value = dialogComponent.GetProperty(key);
            GameObject prefab = null;

            // Determine correct list element based on type of value
            Type type = value.type;
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

            PropertyListElement listElement 
                = listElementGO.GetComponent<PropertyListElement>();

            listElements.Add(listElement);

            listElement.id = key;
            listElement.idInputField.SetTextWithoutNotify(key);

            listElement.stringIntFloatInputField.SetTextWithoutNotify(
                    // Converts value.value to a string based on value.type
                    TypeDescriptor.GetConverter(value.type).ConvertToString(value.value)
                );

            InitListElement(listElement, type);
        }
    }

    public void InitListElement(PropertyListElement listElement, Type type)
    {
        listElement.type = type; // Important

        // Takes care of all UI elements except for the Delete Button
        listElement.Init(dialogComponent);

        // Delete Button
        listElement.deleteButton.onClick.AddListener(
            () =>
            {
                DialogComponent localDC = dialogComponent;
                string localKey = listElement.id;

                localDC.DeleteProperty(localKey); // !

                listElements.Remove(listElement);
                Destroy(listElement.gameObject);
            }
        );
    }

    public void InitAddPropertyDropdown()
    {
        addPropertyDropdown.stringPropertyButton.onClick.AddListener(
            () =>
            {
                GameObject newListElement = Instantiate(stringProperty, scrollViewContent);
                listElements.Add(newListElement.GetComponent<PropertyListElement>());
                InitListElement(newListElement.GetComponent<PropertyListElement>(), typeof(string));
            }
        );

        addPropertyDropdown.intPropertyButton.onClick.AddListener(
            () =>
            {
                GameObject newListElement = Instantiate(intProperty, scrollViewContent);
                listElements.Add(newListElement.GetComponent<PropertyListElement>());
                InitListElement(newListElement.GetComponent<PropertyListElement>(), typeof(int));
            }
        );

        addPropertyDropdown.boolPropertyButton.onClick.AddListener(
            () =>
            {    
                GameObject newListElement = Instantiate(boolProperty, scrollViewContent);
                listElements.Add(newListElement.GetComponent<PropertyListElement>());
                InitListElement(newListElement.GetComponent<PropertyListElement>(), typeof(bool));
            }
        );

        addPropertyDropdown.floatPropertyButton.onClick.AddListener(
            () =>
            {
                GameObject newListElement = Instantiate(floatProperty, scrollViewContent);
                listElements.Add(newListElement.GetComponent<PropertyListElement>());
                InitListElement(newListElement.GetComponent<PropertyListElement>(), typeof(float));
            }
        );
    }

    private void OnDisable()
    {
        /* Must go backwards to prevent InvalidOperationException
         * (similar to ConcurrentModificationException in Java) */
        for (int i = listElements.Count - 1; i >= 0; i--)
        {
            GameObject go = listElements[i].gameObject;
            Destroy(go);
        }

        listElements.Clear();
    }
}
