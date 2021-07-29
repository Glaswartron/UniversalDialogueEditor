using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPathInputGroup : MonoBehaviour
{
    public string correspondingPlayerPref;

    [Header("Main UI")]
    public TMP_InputField inputField;
    public Button fileBrowserButton;
    public Button revertButton;

    private void Start()
    {
        fileBrowserButton.onClick.AddListener(
            () =>
            {
                SimpleFileBrowser.FileBrowser.ShowSaveDialog(
                    onSuccess: (paths) => { inputField.text = paths[0]; },
                    onCancel: () => { },
                    folderMode: true,
                    title: "Select path");
            }
        );

        revertButton.onClick.AddListener(
            () =>
            {
                PlayerPrefs.SetString(correspondingPlayerPref, Application.persistentDataPath);
                inputField.text = Application.persistentDataPath;
            }
        );
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        LoadStoredPath();
    }

    private void LoadStoredPath()
    {
        string value = PlayerPrefs.GetString(correspondingPlayerPref, Application.persistentDataPath);
        inputField.text = value;
    }
}
