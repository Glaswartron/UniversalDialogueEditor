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
        idInputField.onEndEdit.AddListener(
            (input) =>
            {
                // Invalid input or ID already taken
                if (string.IsNullOrWhiteSpace(input) || dialogComponent.HasProperty(input))
                {
                    // Go back to previous id
                    idInputField.SetTextWithoutNotify(id);
                    idInputField.caretPosition = id.Length - 1;

                    ErrorMessage.instance.ShowErrorMessage
                        ("This ID is either invalid (empty) or already taken by another property");

                    return;
                }

                DialogComponent localDC = dialogComponent;

                // Standard case: Property exists but ID is being changed
                if (localDC.HasProperty(id)) 
                {
                    var oldProperty = localDC.GetProperty(id);
                    localDC.UpdateProperty
                        (id, input, oldProperty.value, type);
                }
                // Special case: Happens only when the ID is being edited for the first time
                else
                {
                    localDC.SetProperty(input, value, type);
                }

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

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            var val = TypeDescriptor.GetConverter(type).ConvertFromString(input);
                            localDC.SetProperty(localKey, val, type);
                        }
                        else
                        {
                            var defaultValue = Activator.CreateInstance(type);
                            localDC.SetProperty(localKey, defaultValue, type);

                            stringIntFloatInputField.SetTextWithoutNotify(defaultValue.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Couldn't convert " + input + " to " + type.ToString() + " -- " + e.Message);
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
