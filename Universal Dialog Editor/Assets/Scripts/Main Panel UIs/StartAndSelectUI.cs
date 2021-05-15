using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SimpleFileBrowser;
using System.IO;

public class StartAndSelectUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField pathInputField;
    public Button fileBrowserButton;
    public Button submitButton;
    public Button newButton;
    public Button loadButton;
    public Button deleteButton;
    public GameObject dialogsScrollViewContent;
    public ToggleGroup dialogsScrollViewToggleGroup;
    public TMP_Dropdown dialogPartPresetDropdown;
    public TMP_Dropdown answerPresetDropdown;
    public Button importPresetButton;
    public Button exportDialogPartPresetButton;
    public Button exportAnswerPresetButton;
    public Button helpButtonDialogs;
    public Button helpButtonPresets;

    [Header("Prefabs")]
    public GameObject selectableTextLarge;
    public GameObject newDialogInputField;

    private RectTransform loadButtonRectTransform;
    private RectTransform deleteButtonRectTransform;

    private List<string> dialogFilePaths;
    private List<ExtendedToggle> dialogSelectables;

    private bool folderLoaded = false;
    private string folderPath = "";

    private int selectedDialogIndex;

    // Start is called before the first frame update
    void Start()
    {
        loadButtonRectTransform = loadButton.GetComponent<RectTransform>();
        deleteButtonRectTransform = deleteButton.GetComponent<RectTransform>();

        dialogFilePaths = new List<string>();
        dialogSelectables = new List<ExtendedToggle>();

        fileBrowserButton.onClick.AddListener(OpenFileBrowser);

        submitButton.onClick.AddListener(
            () => {
                LoadFolder(pathInputField.text); 
            }
        );

        newButton.onClick.AddListener(CreateNewDialog);
        loadButton.onClick.AddListener(LoadSelectedDialog);

        deleteButton.onClick.AddListener(DeleteDialog);
    }

    private void OnEnable()
    {
        // Refresh (e.g. if the user returns from a dialog)
        if (folderLoaded)
            LoadFolder(folderPath);
    }

    private void OnDisable()
    {
        // Deselect selected Dialog when UI is disabled
        if (dialogSelectables.Count > 0)
            dialogSelectables[selectedDialogIndex].isOn = false;
    }

    private void LoadFolder(string path)
    {
        Clear();

        if (string.IsNullOrWhiteSpace(path))
            return;

        folderPath = path;

        pathInputField.SetTextWithoutNotify(path);

        string[] files = FileHandler.GetAllDialogPathsFromDir(path);

        /* Make a toggle/selectable text for every dialog.
         * Note that there is no deserialization here, all
         * dialogs are only stored as paths and only the
         * selected one will be deserialized */
        foreach (string file in files)
        {
            dialogFilePaths.Add(file);

            // Dialogs are always saved in the format '.../.../nameOrID.udsdialog'
            string dialogName = file.Substring(file.LastIndexOf("\\") + 1,
                                               file.IndexOf(".udsdialog") - file.LastIndexOf("\\") - 1);

            // That's where the magic happens
            InstantiateDialogSelectableText(dialogName);
        }

        folderLoaded = true; // !

        newButton.interactable = true;
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
                    // Selected => Activate loadButton, deleteButton + store index
                    loadButton.interactable = true;
                    deleteButton.interactable = true;
                    selectedDialogIndex = index;
                }
                // Deselected => Deactivate loadButton and deleteButton
                else { loadButton.interactable = false; deleteButton.interactable = false; }
            }
            );

        // Load on submit / when Enter key is pressed
        toggle.onSubmit.AddListener(
            () => LoadSelectedDialog()
        ); 

        // Set toggle/selectable text up to "deselect itself" correctly
        toggle.group = dialogsScrollViewToggleGroup;
        toggle.deselectOnUnrelatedClick = true;
        toggle.relatedUIElements = new RectTransform[] { loadButtonRectTransform,
                                                         deleteButtonRectTransform };

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
                CreateDialogFileAndSelectable(input, inputField);
            }
        );

        inputField.onSubmit.AddListener(
            (input) =>
            {
                CreateDialogFileAndSelectable(input, inputField);
            }
        );
    }

    private void LoadSelectedDialog()
    {
        string path = dialogFilePaths[selectedDialogIndex];

        Dialog dialog = FileHandler.LoadDialogFile(path);

        if (dialog == null)
            return; // Error message handled by FileHandler.LoadDialogFile

        EditorManager.instance.LoadDialog(dialog, path); // Also switches the UI
    }

    private void CreateDialogFileAndSelectable(string input, TMP_InputField inputField)
    {
        bool success;
        if (string.IsNullOrWhiteSpace(input))
            success = false;
        else
            success = CreateNewDialogFile(input);;

        if (!success)
        {
            // Error message already handled in CreateNewDialogFile and deeper
            Destroy(inputField.gameObject);
            return;
        }

        GameObject dst = InstantiateDialogSelectableText(input);

        dialogSelectables.Add(dst.GetComponent<ExtendedToggle>());

        Destroy(inputField.gameObject);
    }

    private void DeleteDialog()
    {
        bool success = DeleteDialogFile(dialogFilePaths[selectedDialogIndex]);

        if (!success)
            ErrorMessage.instance.ShowErrorMessage
                ("Something went wrong. The dialog was not deleted. Try deleting it " +
                "directly from the file browser/folder");

        Destroy(dialogSelectables[selectedDialogIndex].gameObject);

        loadButton.interactable = false;
        deleteButton.interactable = false;
    }

    private bool CreateNewDialogFile(string nameOrID)
    {
        string folderPath = pathInputField.text;

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            ErrorMessage.instance.ShowErrorMessage
                ("The folder path you entered is either blank or not valid. " +
                 "Try to use the file chooser to get a valid path.");
            return false;
        }

        // Create a new dialog!!!
        Dialog dialog = new Dialog(nameOrID);

        string newFilePath = FileHandler.CreateNewDialogFile(dialog, folderPath);

        if (!string.IsNullOrEmpty(newFilePath))
            dialogFilePaths.Add(newFilePath); // !
        else return false; // Error message already handled by FileHandler
        
        // Final Validation
        return File.Exists(newFilePath);
    }

    private bool DeleteDialogFile(string path)
    {
        if (path == null || string.IsNullOrWhiteSpace(path) || !path.EndsWith(".udsdialog"))
            return false;

        return FileHandler.DeleteDialogFile(path);
    }

    private void OpenFileBrowser()
    {
        FileBrowser.ShowLoadDialog(path => LoadFolder(path[0]), null, true);
    }

    private void Clear()
    {
        foreach (ExtendedToggle selectable in dialogSelectables)
        {
            Destroy(selectable.gameObject);
        }

        dialogSelectables.Clear();
        dialogFilePaths.Clear();

        folderLoaded = false; // !
    }


}
