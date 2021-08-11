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
                idInputField.text = "";
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
                CreateAndSavePreset(properties, keys, type);
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

    private void CreateAndSavePreset
        (Dictionary<string, UDSProperty> _properties, List<string> keys, PropertyPreset.PropertyPresetType type)
    {
        if (string.IsNullOrWhiteSpace(idInputField.text))
        {
            ErrorMessage.instance.ShowErrorMessage("You have to enter a name/id for the Property Preset");
            return;
        }

        foreach (char c in EditorManager.invalidCharacters)
        {
            if (idInputField.text.Contains(c.ToString()))
            {
                ErrorMessage.instance.ShowErrorMessage("Name contains the invalid character " + c);
                return;
            }
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
            AreYouSureDialog.instance.Open(
                "A Property Preset with this ID already exists. Do you want to override it?",
                "Yes",
                "No",
                onYes: () => { SaveAndLeave(preset); },
                onNo: () => { }
            );
        } 
        else
        {
            SaveAndLeave(preset);
        }
    }

    private void SaveAndLeave(PropertyPreset preset)
    {
        if (FileHandler.SavePropertyPreset(preset))
        {
            ErrorMessage.instance.ShowErrorMessage("Preset saved", true);
            idInputField.text = "";
            EditorManager.instance.ActiveMenu = null;
        }
    }
}
