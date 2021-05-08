using System;
using System.ComponentModel;
using System.Globalization;
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
                if (string.IsNullOrWhiteSpace(input) || EditorManager.globalProperties.ContainsKey(input))
                {
                    if (EditorManager.globalProperties.ContainsKey(input) && !input.Equals(id))
                        return; // ID hasn't been edited

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
                            object val = null;
                            if (type != typeof(float))
                                val = TypeDescriptor.GetConverter(type).ConvertFromString(input);
                            else
                                val = float.Parse(input, CultureInfo.CurrentCulture);

                            SetGlobalProperty(localKey, val, type);
                        }
                        else
                        {
                            var defaultValue = Activator.CreateInstance(type);

                            SetGlobalProperty(localKey, defaultValue, type);

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

                    SetGlobalProperty(localKey, state, typeof(bool));
                }
            );
        }
    }

    private void SetGlobalProperty(string key, object newValue, Type type)
    {
        if (!EditorManager.globalProperties.ContainsKey(key))
            EditorManager.globalProperties.Add(key, new UDSProperty(newValue, type));
        else
            EditorManager.globalProperties[key] = new UDSProperty(newValue, type);
    }

}
