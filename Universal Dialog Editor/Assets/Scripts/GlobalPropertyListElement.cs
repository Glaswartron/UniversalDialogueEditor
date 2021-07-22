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

        // Type text
        if (typeText != null) {
            if (type == typeof(string))
                typeText.SetText("String");
            else if (type == typeof(int))
                typeText.SetText("Int");
            else if (type == typeof(bool))
                typeText.SetText("Bool");
            else if (type == typeof(float))
                typeText.SetText("Float");
        }

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
    }
}
