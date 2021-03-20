using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SimpleFileBrowser;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;

public class StartAndSelectUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField pathInputField;
    public Button fileBrowserButton;
    public Button submitButton;
    public Button newButton;
    public Button loadButton;
    public GameObject dialogsScrollViewContent;
    public ToggleGroup dialogsScrollViewToggleGroup;
    public TMP_Dropdown dialogPartPresetDropdown;
    public TMP_Dropdown answerPresetDropdown;
    public Button importPresetButton;
    public Button exportPresetButton;
    public Button helpButtonDialogs;
    public Button helpButtonPresets;

    [Header("Prefabs")]
    public GameObject selectableTextLarge;
    public GameObject newDialogInputField;

    private RectTransform loadButtonRectTransform;

    private List<string> dialogFilePaths;
    private List<ExtendedToggle> dialogSelectables;

    private bool folderLoaded = false;

    private int selectedDialogIndex;

    // Start is called before the first frame update
    void Start()
    {
        loadButtonRectTransform = loadButton.GetComponent<RectTransform>();

        dialogFilePaths = new List<string>();
        dialogSelectables = new List<ExtendedToggle>();

        fileBrowserButton.onClick.AddListener(OpenFileBrowser);
        submitButton.onClick.AddListener(LoadFolder);

        newButton.onClick.AddListener(CreateNewDialog);
    }

    private void LoadFolder()
    {
        ClearScrollView();

        string path = pathInputField.text;

        if (string.IsNullOrWhiteSpace(path))
            return;

        // Load folder content (get files as string[] of their paths)
        string[] files = Directory.GetFiles(path);

        // Make a toggle/selectable text for every...
        foreach (string file in files)
        {
            if (file.EndsWith(".udsdialog")) // ... dialog
            {
                dialogFilePaths.Add(file);

                // Dialogs are always saved in the format '.../.../nameOrID.udsdialog'
                string dialogName = file.Substring(file.LastIndexOf("\\") + 1,
                                                   file.IndexOf(".udsdialog") - file.LastIndexOf("\\") - 1);

                // That's where the magic happens
                InstantiateDialogSelectableText(dialogName);
            }
        }

        folderLoaded = true; // !
    }

    private GameObject InstantiateDialogSelectableText(string text)
    {
        GameObject toggleGO = Instantiate(selectableTextLarge,
                                          dialogsScrollViewContent.transform);

        toggleGO.GetComponentInChildren<TMP_Text>().SetText(text);

        ExtendedToggle toggle = toggleGO.GetComponent<ExtendedToggle>();
        dialogSelectables.Add(toggle); // !

        int index = dialogSelectables.Count - 1; // Important

        // Set the onValueChanged event for the toggle/selectable text
        toggle.onValueChanged.AddListener(
            (value) =>
            {
                if (value)
                {
                    // Selected => Activate loadButton + store index
                    loadButton.interactable = true;
                    selectedDialogIndex = index;
                }
                // Deselected => Deactivate loadButton
                else loadButton.interactable = false;
            }
            );

        // Set toggle/selectable text up to "deselect itself" correctly
        toggle.group = dialogsScrollViewToggleGroup;
        toggle.deselectOnUnrelatedClick = true;
        toggle.relatedUIElements = new RectTransform[] { loadButtonRectTransform };
        
        return toggleGO;
    }

    private void CreateNewDialog()
    {
        if (!folderLoaded)
        {
            ErrorMessage.instance.ShowErrorMessage
                ("Please first load a folder by entering a path " +
                 "and clicking the submit button (green arrow)!");
            return;
        }

        GameObject inputFieldGO = Instantiate(newDialogInputField,
                                              dialogsScrollViewContent.transform);

        TMP_InputField inputField = inputFieldGO.GetComponent<TMP_InputField>();

        inputField.Select();

        inputField.onDeselect.AddListener(
                (input) =>
                {
                    bool success = CreateNewDialogFile(input);

                    if (!success)
                    {
                        Destroy(inputField.gameObject);
                        return;
                    }

                    GameObject dst = InstantiateDialogSelectableText(input);

                    dialogSelectables.Add(dst.GetComponent<ExtendedToggle>());

                    Destroy(inputField.gameObject);
                }
            );
    }

    private bool CreateNewDialogFile(string nameOrID)
    {
        string folderPath = pathInputField.text;

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) {
            ErrorMessage.instance.ShowErrorMessage
                ("The folder path you entered is either blank or not valid. " +
                 "Try to use the file chooser to get a valid path.");
            return false;
        }

        string path = FileHandler.BuildDialogFilePath(nameOrID, folderPath);

        if (!File.Exists(path))
        {
            // Create a new dialog!
            Dialog dialog = new Dialog(nameOrID);

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Create);

            try
            {
                formatter.Serialize(stream, dialog);
            }
            catch (Exception e)
            {
                ErrorMessage.instance.ShowErrorMessage
                    ("Something went wrong while creating the file. Please " +
                    "check if the dialog name/id you entered contains any " +
                    "invalid characters. Also check if you/the editor has " +
                    "writing permission for the folder you selected. " +
                    "Try changing it to a different folder");

#if UNITY_EDITOR
                Debug.LogError(e.StackTrace);
#endif

                return false;
            }
            finally
            {
                stream.Flush();
                stream.Close();
            }
        }
        else
        {
            ErrorMessage.instance.ShowErrorMessage
                ("A dialog with this id/name (/path) already exists in this folder!");

            return false;
        }

        return true;
    }

    private void OpenFileBrowser()
    {
        FileBrowser.ShowLoadDialog(path => pathInputField.text = path[0], null, true);
    }

    private void ClearScrollView()
    {
        foreach (ExtendedToggle selectable in dialogSelectables)
        {
            Destroy(selectable.gameObject);
        }

        dialogSelectables.Clear();

        folderLoaded = false; // !
    }

    
}
