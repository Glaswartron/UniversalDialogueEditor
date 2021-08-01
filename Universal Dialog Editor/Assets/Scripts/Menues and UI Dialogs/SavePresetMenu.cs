using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavePresetMenu : MonoBehaviour
{
    [Header("Main UI")]
    public TMP_InputField idInputField;
    public Button saveButton;
    public Button closeButton;

    private void Start()
    {
        closeButton.onClick.AddListener(
            () =>
            {
                EditorManager.instance.ActiveMenu = null;
            }
        );
    }

    public void Init
        (Dictionary<string, UDSProperty> properties, List<string> keys, PropertyPreset.PropertyPresetType type)
    {
        saveButton.onClick.RemoveAllListeners();

        saveButton.onClick.AddListener(
            () =>
            {
                bool successful = CreateAndSavePreset(properties, keys, type);

                if (successful)
                {
                    ErrorMessage.instance.ShowErrorMessage("Preset saved", true);
                    EditorManager.instance.ActiveMenu = null;
                }
            }
        );

        string typeStr = "";
        switch (type)
        {
            case PropertyPreset.PropertyPresetType.DIALOG_PART:
                typeStr = "Dialogue Part";
                break;
            case PropertyPreset.PropertyPresetType.ANSWER:
                typeStr = "Answer";
                break;
        }

        saveButton.GetComponentInChildren<TMP_Text>().SetText("Save " + typeStr + " Preset");
    }

    private bool CreateAndSavePreset
        (Dictionary<string, UDSProperty> _properties, List<string> keys, PropertyPreset.PropertyPresetType type)
    {
        if (string.IsNullOrWhiteSpace(idInputField.text))
        {
            ErrorMessage.instance.ShowErrorMessage("You have to enter a name/id for the Property Preset");
            return false;
        }

        foreach (char c in EditorManager.invalidCharacters)
        {
            if (idInputField.text.Contains(c.ToString()))
                ErrorMessage.instance.ShowErrorMessage("Name contains the invalid character " + c);
        }

        PropertyPreset preset = new PropertyPreset()
        {
            id = idInputField.text,
            properties = _properties,
            orderedKeyList = keys,
            propertyPresetType = type
        };

        if (FileHandler.ExistsPropertyPreset(preset))
        {
            bool done = false;
            AreYouSureDialog.instance.Open(
                "A Property Preset with this ID already exists. Do you want to override it?",
                "Yes",
                "No",
                onYes: () => { done = Save(preset); },
                onNo: () => { done = false; }
            );

            return done;
        } 
        else
        {
            return Save(preset);
        }
    }

    private bool Save(PropertyPreset preset)
    {
        return FileHandler.SavePropertyPreset(preset);
    }
}
