using System;
using System.ComponentModel;
using System.Globalization;
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
    public ExtendedColorToggle boolToggle;

    [HideInInspector]
    public Type type;

    [HideInInspector]
    public string id;
    [HideInInspector]
    public object value;

    private DialogComponent dialogComponent;

    public virtual void Init(DialogComponent dialogComponent)
    {
        this.dialogComponent = dialogComponent;

        UDSProperty? property = null; // Nullable
        if (dialogComponent.HasProperty(id))
            property = dialogComponent.GetProperty(id);

        if (property.HasValue && property.Value.required)
            idInputField.interactable = false;

        if (!property.HasValue || !property.Value.required) // Only if not required
        {
            // ID Input Field
            idInputField.onEndEdit.AddListener(
                (input) =>
                {
                    if (dialogComponent.HasProperty(input))
                    {
                        ErrorMessage.instance.ShowErrorMessage
                            ("The Dialog Component already contains a Property with that ID");

                        // Go back to previous id
                        idInputField.SetTextWithoutNotify(id);
                        idInputField.caretPosition = id.Length - 1;

                        return;
                    }

                    // Check if input contains invalid char
                    bool containsInvalidCharacter = false;
                    char invalidChar = default;
                    foreach (char c in EditorManager.invalidCharacters)
                    {
                        if (input.Contains(c.ToString()))
                        {
                            containsInvalidCharacter = true;
                            invalidChar = c;
                        }
                    }

                    // Invalid input or ID already taken
                    if (string.IsNullOrWhiteSpace(input) || dialogComponent.HasProperty(input)
                            || containsInvalidCharacter)
                    {
                        if (dialogComponent.HasProperty(input) && !input.Equals(id))
                            return; // ID hasn't been edited

                        // Go back to previous id
                        idInputField.SetTextWithoutNotify(id);
                        idInputField.caretPosition = id.Length - 1;

                        if (!containsInvalidCharacter)
                            ErrorMessage.instance.ShowErrorMessage
                                ("This ID is either invalid (empty) or already taken by another property");
                        else
                            ErrorMessage.instance.ShowErrorMessage("Name contains the invalid character " + invalidChar);

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
        }

        // String/Int/Float => InputField, Bool => Toggle
        if (type != typeof(bool))
        {
            if (property.HasValue)
            {
                stringIntFloatInputField.SetTextWithoutNotify(
                    // Converts value.value to a string based on value.type
                    TypeDescriptor.GetConverter(property.Value.type).ConvertToString(property.Value.value)
                );
            }

            // Input Field
            stringIntFloatInputField.onValueChanged.AddListener(
                (input) =>
                {
                    DialogComponent localDC = dialogComponent;
                    string localKey = id;
                    UDSProperty? localProperty = property;

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(input) || type == typeof(string))
                        {
                            object val = null;
                            if (type != typeof(float))
                                val = TypeDescriptor.GetConverter(type).ConvertFromString(input);
                            else
                                val = float.Parse(input, CultureInfo.CurrentCulture);

                            if (!localProperty.HasValue)
                                localDC.SetProperty(localKey, val, type);
                            else
                                localDC.SetProperty(localKey, val, type, localProperty.Value.required);
                        }
                        else
                        {
                            var defaultValue = Activator.CreateInstance(type);

                            if (!localProperty.HasValue)
                                localDC.SetProperty(localKey, defaultValue, type);
                            else
                                localDC.SetProperty(localKey, defaultValue, type, localProperty.Value.required);

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
            if (property.HasValue && property.Value.value != null) // 2nd condition important
                boolToggle.SetIsOnWithoutNotify((bool)property.Value.value);

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
