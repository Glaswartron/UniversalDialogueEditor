using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class PropertiesUI : MonoBehaviour
{
    private DialogComponent dialogComponent;

    [Header("Main UI")]
    public Transform scrollViewContent;
    public Button addPropertyButton;
    public Button loadPresetButton;
    public Button savePresetButton;

    [Header("Prefabs")]
    public GameObject stringProperty;
    public GameObject intProperty;
    public GameObject floatProperty;
    public GameObject boolProperty;

    private List<GameObject> listElements;

    /// <summary>
    /// Sets up the PropertiesUI and populates the
    /// connected ScrollView based on the properties 
    /// of the given dialogComponent
    /// </summary>
    /// <param name="dialogComponent">The dialogComponent this PropertiesUI 
    /// is responsible for</param>
    public void Init(DialogComponent dialogComponent)
    {
        listElements = new List<GameObject>();

        this.dialogComponent = dialogComponent;

        /* Instantiate a fitting list/scroll view element for all properties
         * and add listeners to the various UI elements within it */
        foreach (string key in dialogComponent.GetPropertyKeys())
        {
            (object value, Type type) value = dialogComponent.GetProperty(key);
            GameObject prefab = null;

            // Determine correct list element based on type of value
            if (value.type == typeof(string))
                prefab = stringProperty;
            else if (value.type == typeof(int))
                prefab = intProperty;
            else if (value.type == typeof(float))
                prefab = floatProperty;
            else if (value.type == typeof(bool))
                prefab = boolProperty;
            else Debug.LogError("Property type proplems");

            GameObject listElementGO = Instantiate(prefab, scrollViewContent);

            listElements.Add(listElementGO);

            PropertyListElement listElement 
                = listElementGO.GetComponent<PropertyListElement>();

            listElement.idInputField.SetTextWithoutNotify(key);

            listElement.stringIntFloatInputField.SetTextWithoutNotify(
                    // Converts value.value to a string based on value.type
                    TypeDescriptor.GetConverter(value.type).ConvertToString(value.value)
                );

            // ID Input Field
            listElement.idInputField.onValueChanged.AddListener(
                (input) =>
                {
                    if (string.IsNullOrWhiteSpace(input))
                        return;

                    DialogComponent localDC = dialogComponent;

                    localDC.id = input;
                }
            );

            // Either the stringIntFloatInputField or the boolToggle is there
            if (listElement.stringIntFloatInputField != null)
            {
                // Input Field
                listElement.stringIntFloatInputField.onValueChanged.AddListener(
                    (input) =>
                    {
                        DialogComponent localDC = dialogComponent;
                        string localKey = key;
                        (object value, Type type) localValue = value;

                        try
                        {

                            if (!string.IsNullOrWhiteSpace(input))
                            {
                                var val = TypeDescriptor.GetConverter(localValue.type).ConvertFromString(input);
                                localDC.SetProperty(localKey, val, value.type);
                            } else {
                                var defaultValue = Activator.CreateInstance(localValue.type);
                                localDC.SetProperty(localKey, defaultValue, value.type);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError
                            ("Couldn't convert " + input + " to " +
                            value.type.ToString() + " -- " + e.Message);
                        }
                    }
                );
            } 
            else
            {
                // Toggle
                listElement.boolToggle.onValueChanged.AddListener(
                    (state) =>
                    {
                        DialogComponent localDC = dialogComponent;
                        string localKey = key;

                        localDC.SetProperty(localKey, state);
                    }
                );
            }

            // Delete Button
            listElement.deleteButton.onClick.AddListener(
                () =>
                {
                    DialogComponent localDC = dialogComponent;
                    string localKey = key;

                    localDC.DeleteProperty(localKey); // !

                    listElements.Remove(listElement.gameObject);
                    Destroy(listElement.gameObject);
                }
            );
        }
    }

    private void OnDisable()
    {
        /* Must go backwards to prevent InvalidOperationException
         * (similar to ConcurrentModificationException in Java) */
        for (int i = listElements.Count - 1; i >= 0; i--)
        {
            GameObject go = listElements[i];
            Destroy(go);
        }

        listElements.Clear();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
