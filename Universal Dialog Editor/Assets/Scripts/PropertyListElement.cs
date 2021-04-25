using System;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertyListElement : MonoBehaviour, ISubUI
{
    [Header("Main UI")]
    public TMP_InputField idInputField;
    public Button deleteButton;

    [Header("Input Fields")]
    public TMP_InputField stringIntFloatInputField;
    public Toggle boolToggle;

    [HideInInspector]
    public Type type;

    [HideInInspector]
    public string id;
    [HideInInspector]
    public object value;

    private DialogComponent dialogComponent;

    public void Init(DialogComponent dialogComponent)
    {
        this.dialogComponent = dialogComponent;

        // ID Input Field
        idInputField.onValueChanged.AddListener(
            (input) =>
            {
                // Invalid input
                if (string.IsNullOrWhiteSpace(input))
                {
                    idInputField.SetTextWithoutNotify(id);
                    return;
                }

                // ID already taken
                if (dialogComponent.HasProperty(input))
                {
                    idInputField.SetTextWithoutNotify(id);
                    return;
                }

                DialogComponent localDC = dialogComponent;

                var oldProperty = localDC.GetProperty(id);
                localDC.UpdateProperty
                    (id, input, oldProperty.value, oldProperty.type);

                id = input;
            }
        );

        // String/Int/Float => InputField, Bool => Toggle
        if (type != typeof(bool))
        {
            // Input Field
            stringIntFloatInputField.onValueChanged.AddListener(
                (input) =>
                {
                    DialogComponent localDC = dialogComponent;
                    string localKey = id;
                    (object value, Type type) localValue = localDC.GetProperty(localKey);

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            var val = TypeDescriptor.GetConverter(localValue.type).ConvertFromString(input);
                            localDC.SetProperty(localKey, val, localValue.type);
                        }
                        else
                        {
                            var defaultValue = Activator.CreateInstance(localValue.type);
                            localDC.SetProperty(localKey, defaultValue, localValue.type);

                            stringIntFloatInputField.SetTextWithoutNotify(defaultValue.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError
                        ("Couldn't convert " + input + " to " +
                        localValue.type + " -- " + e.Message);
                    }
                }
            );
        }
        else
        {
            // Toggle
            boolToggle.onValueChanged.AddListener(
                (state) =>
                {
                    DialogComponent localDC = dialogComponent;
                    string localKey = id;

                    localDC.SetProperty(localKey, state);
                }
            );
        }
    }
}
