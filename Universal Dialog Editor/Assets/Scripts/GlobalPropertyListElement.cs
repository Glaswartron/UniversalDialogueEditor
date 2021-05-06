using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class GlobalPropertyListElement : PropertyListElement
{
    public override void Init(DialogComponent dialogComponent = null)
    {
        if (dialogComponent != null)
            Debug.LogWarning("GlobalPropertyListElement.Init is called with dialogComponent != null");

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

                string localKey = id;

                // Standard case: Property exists but ID is being changed
                if (EditorManager.globalProperties.ContainsKey(localKey))
                {
                    var oldProperty = EditorManager.globalProperties[localKey];
                    EditorManager.globalProperties.Remove(localKey);
                    EditorManager.globalProperties[input] = oldProperty;
                }
                // Special case: Happens only when the ID is being edited for the first time
                else
                {
                    EditorManager.globalProperties[input] = new UDSProperty(value, type);
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
                    string localKey = id;

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            var val = TypeDescriptor.GetConverter(type).ConvertFromString(input);

                            SetGlobalProperty(localKey, val);
                        }
                        else
                        {
                            var defaultValue = Activator.CreateInstance(type);

                            SetGlobalProperty(localKey, defaultValue);

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
                    string localKey = id;

                    SetGlobalProperty(localKey, state);
                }
            );
        }
    }

    private void SetGlobalProperty(string key, object newValue)
    {
        var oldProperty = EditorManager.globalProperties[key];

        EditorManager.globalProperties[key] = new UDSProperty(newValue, oldProperty.type);
    }

}
