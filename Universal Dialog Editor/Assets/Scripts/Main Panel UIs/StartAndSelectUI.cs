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
    public GameObject dialoguesScrollViewContent;
    public ScrollRect dialoguesScrollRect;
    public ToggleGroup dialoguesScrollViewToggleGroup;
    public TMP_Dropdown dialoguePartPresetDropdown;
    public TMP_Dropdown answerPresetDropdown;
    public Button importPresetButton;
    public Button exportDialoguePartPresetButton;
    public Button exportAnswerPresetButton;

    [Header("Prefabs")]
    public GameObject selectableText;
    public GameObject newDialogueInputField;

    private RectTransform loadButtonRectTransform;
    private RectTransform deleteButtonRectTransform;

    private List<string> dialogueFilePaths;
    private List<ExtendedToggle> dialogueSelectables;

    private bool folderLoaded = false;
    private string folderPath = "";

    private int selectedDialogueIndex;

    // Start is called before the first frame update
    void Start()
    {
        loadButtonRectTransform = loadButton.GetComponent<RectTransform>();
        deleteButtonRectTransform = deleteButton.GetComponent<RectTransform>();

        dialogueFilePaths = new List<string>();
        dialogueSelectables = new List<ExtendedToggle>();

        fileBrowserButton.onClick.AddListener(OpenFolderFileBrowser);

        submitButton.onClick.AddListener(
            () =>
            {
                LoadFolder(pathInputField.text);
            }
        );

        newButton.onClick.AddListener(CreateNewDialogue);
        loadButton.onClick.AddListener(LoadSelectedDialogue);

        deleteButton.onClick.AddListener(DeleteDialogue);

        importPresetButton.onClick.AddListener(ImportPreset);

        exportDialoguePartPresetButton.onClick.AddListener(
            () =>
            {
                ExportPreset(dialoguePartPresetDropdown.options[dialoguePartPresetDropdown.value].text,
                             PropertyPreset.PropertyPresetType.DIALOG_PART);
            }
        );

        exportAnswerPresetButton.onClick.AddListener(
            () =>
            {
                ExportPreset(answerPresetDropdown.options[answerPresetDropdown.value].text,
                             PropertyPreset.PropertyPresetType.ANSWER);
            }
        );

        if ((folderPath = PlayerPrefs.GetString("startAndSelectUIDirPath", null)) != null)
            LoadFolder(folderPath);
    }

    private void OnEnable()
    {
        // Refresh (e.g. if the user returns from a dialogue)
        if (folderLoaded)
            LoadFolder(folderPath);

        InitPresetDropdowns();
    }

    private void OnDisable()
    {
        // Deselect selected Dialogue when UI is disabled (-1 check necessary to avoid exception)
        if (dialogueSelectables.Count > 0 && selectedDialogueIndex != -1)
            dialogueSelectables[selectedDialogueIndex].isOn = false;
    }

    private void LoadFolder(string path)
    {
        Clear();

        if (string.IsNullOrWhiteSpace(path))
            return;

        folderPath = path;
        PlayerPrefs.SetString("startAndSelectUIDirPath", folderPath); // Save path

        pathInputField.SetTextWithoutNotify(path);

        string[] files = FileHandler.GetAllDialoguePathsFromDir(path);

        /* Make a toggle/selectable text for every dialogue.
         * Note that there is no deserialization here, all
         * dialogues are only stored as paths and only the
         * selected one will be deserialized */
        foreach (string file in files)
        {
            dialogueFilePaths.Add(file);

            // Dialogues are always saved in the format '.../.../nameOrID.udsdialogue.json'
            string dialogueName
                = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));

            // That's where the magic happens
            InstantiateDialogueSelectableText(dialogueName);
        }

        folderLoaded = true; // !

        newButton.interactable = true;
    }

    private GameObject InstantiateDialogueSelectableText(string text)
    {
        GameObject toggleGO = Instantiate(selectableText,
                                          dialoguesScrollViewContent.transform);

        toggleGO.GetComponentInChildren<TMP_Text>().SetText(text);

        ExtendedToggle toggle = toggleGO.GetComponent<ExtendedToggle>();
        dialogueSelectables.Add(toggle);

        // Set the onValueChanged event for the toggle/selectable text
        toggle.onValueChanged.AddListener(
            (value) =>
            {
                if (value)
                {
                    ExtendedToggle me = toggle;

                    // Selected => Activate loadButton, deleteButton + store index
                    loadButton.interactable = true;
                    deleteButton.interactable = true;
                    selectedDialogueIndex = dialogueSelectables.IndexOf(me);
                }
                // Deselected => Deactivate loadButton and deleteButton
                else
                {
                    loadButton.interactable = false; deleteButton.interactable = false;
                    selectedDialogueIndex = -1;
                }
            }
            );

        // Load on submit / when Enter key is pressed
        toggle.onSubmit.AddListener(
            () => LoadSelectedDialogue()
        );

        // Set toggle/selectable text up to "deselect itself" correctly
        toggle.group = dialoguesScrollViewToggleGroup;
        toggle.deselectOnUnrelatedClick = true;
        toggle.relatedUIElements = new RectTransform[] { loadButtonRectTransform,
                                                         deleteButtonRectTransform };

        return toggleGO;
    }

    private void CreateNewDialogue()
    {
        if (!folderLoaded)
        {
            ErrorMessage.instance.ShowErrorMessage
                ("Please first load a folder by entering a path " +
                 "and clicking the submit button (green arrow)!");

            return;
        }

        // Create the input field for inputting a name
        GameObject inputFieldGO = Instantiate(newDialogueInputField,
                                              dialoguesScrollViewContent.transform);

        TMP_InputField inputField = inputFieldGO.GetComponent<TMP_InputField>();

        inputField.Select();

        // Continue when inputField is either deselected or submit (enter) is hit
        inputField.onDeselect.AddListener(
            (input) =>
            {
                CreateDialogueFileAndSelectable(input, inputField);
            }
        );

        inputField.onSubmit.AddListener(
            (input) =>
            {
                CreateDialogueFileAndSelectable(input, inputField);
            }
        );

        // Scroll to bottom where the input field is (important if there are many Dialogues)
        dialoguesScrollRect.verticalNormalizedPosition = -1;
    }

    private void LoadSelectedDialogue()
    {
        string path = dialogueFilePaths[selectedDialogueIndex];

        Dialogue dialogue = FileHandler.LoadDialogueFile(path);

        if (dialogue == null)
            return; // Error message handled by FileHandler.LoadDialogueFile

        EditorManager.instance.LoadDialogue(dialogue, path); // Also switches the UI
    }

    private void CreateDialogueFileAndSelectable(string input, TMP_InputField inputField)
    {
        bool success;
        if (string.IsNullOrWhiteSpace(input))
            success = false;
        else
            success = CreateNewDialogueFile(input);

        if (!success)
        {
            // Error message already handled in CreateNewDialogueFile and deeper
            Destroy(inputField.gameObject);
            return;
        }

        InstantiateDialogueSelectableText(input);

        Destroy(inputField.gameObject);
    }

    private void DeleteDialogue()
    {
        bool success = DeleteDialogueFile(dialogueFilePaths[selectedDialogueIndex]);

        if (!success)
        {
            ErrorMessage.instance.ShowErrorMessage
                ("Something went wrong. The dialogue was not deleted. Try deleting it " +
                "directly from the file browser/folder");

            return;
        }

        var selectableToDestroy = dialogueSelectables[selectedDialogueIndex];

        dialogueSelectables.RemoveAt(selectedDialogueIndex);
        dialogueFilePaths.RemoveAt(selectedDialogueIndex);

        Destroy(selectableToDestroy.gameObject);

        loadButton.interactable = false;
        deleteButton.interactable = false;
    }

    private bool CreateNewDialogueFile(string nameOrID)
    {
        string folderPath = pathInputField.text;

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            ErrorMessage.instance.ShowErrorMessage
                ("The folder path you entered is either blank or not valid. " +
                 "Try to use the file browser to get a valid path.");

            return false;
        }

        // Create a new dialogue!!!
        Dialogue dialogue = new Dialogue(nameOrID);

        string newFilePath = FileHandler.CreateNewDialogueFile(dialogue, folderPath);

        if (!string.IsNullOrEmpty(newFilePath))
            dialogueFilePaths.Add(newFilePath); // !
        else return false; // Error message already handled by FileHandler

        // Final Validation
        return File.Exists(newFilePath);
    }

    private bool DeleteDialogueFile(string path)
    {
        if (path == null || string.IsNullOrWhiteSpace(path) || !path.EndsWith(".udsdialogue.json"))
            return false;

        return FileHandler.DeleteDialogueFile(path);
    }

    private void OpenFolderFileBrowser()
    {
        FileBrowser.ShowLoadDialog(path => LoadFolder(path[0]), null, true);
    }

    private void Clear()
    {
        foreach (ExtendedToggle selectable in dialogueSelectables)
        {
            if (selectable != null)
                Destroy(selectable.gameObject);
        }

        dialogueSelectables.Clear();
        dialogueFilePaths.Clear();

        folderLoaded = false; // !
    }

    private void InitPresetDropdowns()
    {
        PopulatePresetDropdowns();

        dialoguePartPresetDropdown.onValueChanged.AddListener(
            (value) =>
            {
                if (value > 0)
                {
                    EditorManager.globalDialoguePartPropertyPreset
                        = dialoguePartPresetDropdown.options[value].text;
                }
                else
                    EditorManager.globalDialoguePartPropertyPreset = null;
            }
        );

        answerPresetDropdown.onValueChanged.AddListener(
            (value) =>
            {
                if (value > 0)
                {
                    EditorManager.globalAnswerPropertyPreset
                        = answerPresetDropdown.options[value].text;
                }
                else
                    EditorManager.globalAnswerPropertyPreset = null;
            }
        );
    }

    private void PopulatePresetDropdowns()
    {
        dialoguePartPresetDropdown.ClearOptions();
        answerPresetDropdown.ClearOptions();

        string[] dialoguePartPresets =
            FileHandler.GetAllPropertyPresetIDs(PropertyPreset.PropertyPresetType.DIALOG_PART);

        string[] answerPresets =
            FileHandler.GetAllPropertyPresetIDs(PropertyPreset.PropertyPresetType.ANSWER);

        List<string> dialoguePartDropdownOptions = new List<string>(dialoguePartPresets);
        dialoguePartDropdownOptions.Insert(0, "None");

        List<string> answerDropdownOptions = new List<string>(answerPresets);
        answerDropdownOptions.Insert(0, "None");

        dialoguePartPresetDropdown.AddOptions(dialoguePartDropdownOptions);
        answerPresetDropdown.AddOptions(answerDropdownOptions);
    }

    private void ImportPreset()
    {
        FileBrowser.ShowLoadDialog(
            onSuccess: (path) =>
            {
                if (path[0].EndsWith(".udspreset.json"))
                {
                    FileHandler.ImportPropertyPreset(path[0]);
                    InitPresetDropdowns();
                }
                else
                {
                    ErrorMessage.instance.ShowErrorMessage("The file you selected is not a valid .udspreset.json file");
                }
            },
            onCancel: () => { },
            allowMultiSelection: false
        );
    }

    private void ExportPreset(string id, PropertyPreset.PropertyPresetType type)
    {
        if (type == PropertyPreset.PropertyPresetType.DIALOG_PART && dialoguePartPresetDropdown.value == 0
            || type == PropertyPreset.PropertyPresetType.ANSWER && answerPresetDropdown.value == 0)
        {
            ErrorMessage.instance.ShowErrorMessage("You first have to select a Preset in the dropdown menu");
            return;
        }

        FileBrowser.ShowSaveDialog(
            onSuccess: (path) =>
            {
                FileHandler.ExportPropertyPreset(id, type, path[0]);
            },
            onCancel: () => { },
            folderMode: true,
            allowMultiSelection: false
        );
    }

}
