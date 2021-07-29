using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public SettingsPathInputGroup propertyPresetPathInputGroup;
    public SettingsPathInputGroup globalPropertiesPathInputGroup;
    public TMP_Dropdown colorThemeDropdown;
    public Button submitButton;

    // Start is called before the first frame update
    void Start()
    {
        colorThemeDropdown.options
            = new List<TMP_Dropdown.OptionData>(
                Array.ConvertAll
                (EditorManager.instance.colorThemes, ct => new TMP_Dropdown.OptionData(ct.themeName)));
            
        submitButton.onClick.AddListener(
            () =>
            {
                string pppath = propertyPresetPathInputGroup.inputField.text;
                if (!string.IsNullOrWhiteSpace(pppath) && Directory.Exists(pppath))
                    PlayerPrefs.SetString("PropertyPresetPath", pppath);
                else
                {
                    ErrorMessage.instance.ShowErrorMessage
                        ("The Property Preset save path you entered is either blank or not valid. " +
                         "Try to use the file browser to get a valid path.");

                    return;
                }

                string gppath = globalPropertiesPathInputGroup.inputField.text;
                if (!string.IsNullOrWhiteSpace(gppath) && Directory.Exists(gppath))
                    PlayerPrefs.SetString("GlobalPropertyPath", gppath);
                else
                {
                    ErrorMessage.instance.ShowErrorMessage
                        ("The Global Property save path you entered is either blank or not valid. " +
                         "Try to use the file browser to get a valid path.");

                    return;
                }

                ColorTheme newColorTheme 
                    = Array.Find(EditorManager.instance.colorThemes,
                                 ct => ct.themeName.Equals
                                (colorThemeDropdown.options[colorThemeDropdown.value].text));

                PlayerPrefs.SetString("ColorTheme", newColorTheme.themeName);

                EditorManager.instance.ChangeColorTheme(newColorTheme);

                EditorManager.instance.ActiveMenu = null;
            }
        );

        colorThemeDropdown.value
            = colorThemeDropdown.options.ConvertAll(o => o.text)
            .IndexOf(EditorManager.instance.ActiveColorTheme.themeName);

        colorThemeDropdown.RefreshShownValue();
    }

    private void OnEnable()
    {
        colorThemeDropdown.value
            = colorThemeDropdown.options.ConvertAll(o => o.text)
            .IndexOf(EditorManager.instance.ActiveColorTheme.themeName);

        colorThemeDropdown.RefreshShownValue();
    }
}
