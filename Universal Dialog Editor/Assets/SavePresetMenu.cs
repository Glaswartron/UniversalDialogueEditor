using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavePresetMenu : MonoBehaviour
{
    public static SavePresetMenu instance;

    [Header("Main UI")]
    public TMP_InputField idInputField;
    public Button saveButton;
    public Button closeButton;

    private void Start()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        closeButton.onClick.AddListener(
            () =>
            {
                EditorManager.instance.ActiveMenu = null;
            }
        );
    }

    public void Init
        (Dictionary<string, UDSProperty> properties, PropertyPreset.PropertyPresetType type)
    {
        saveButton.onClick.RemoveAllListeners();

        saveButton.onClick.AddListener(
            () => 
            {
                CreateAndSavePreset(properties, type); 
            }
        );
    }

    private void CreateAndSavePreset
        (Dictionary<string, UDSProperty> _properties, PropertyPreset.PropertyPresetType type)
    {
        if (string.IsNullOrWhiteSpace(idInputField.text))
        {
            ErrorMessage.instance.ShowErrorMessage("You have to enter a name/id for the Property Preset");
            return;
        }

        PropertyPreset preset = new PropertyPreset()
        {
            id = idInputField.text,
            properties = _properties,
            propertyPresetType = type
        };

        FileHandler.SavePropertyPreset(preset);
    }
}
