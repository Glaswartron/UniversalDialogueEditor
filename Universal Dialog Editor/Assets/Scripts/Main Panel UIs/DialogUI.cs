using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogUI : MonoBehaviour
{
    [Header("Main UI")]
    public PropertiesUI propertiesUI;
    public IDInputUI idInputUI;
    public Button saveButton;
    public Button saveAndExitButton;
    public Button discardAndExitButton;
    public TMP_Text infoText;
    public GameObject warningScrollViewContent;

    [Header("Prefabs")]
    public GameObject warningBox;

    private Dialog dialog;

    private string infoTemplate = "Dialog Parts: {0}\n" +
                          "Answers: {1}\n"      +
                          "Connections: {2}\n" +
                          "Start Part ID: {3}\n";

    private void OnEnable()
    {
        dialog = EditorManager.instance.dialog;

        idInputUI.dialogComponent = dialog;

        propertiesUI.Init(dialog); // Super important stuff
    }

    // Start is called before the first frame update
    void Start()
    {
        saveButton.onClick.AddListener(
            () => Save()
        );

        saveAndExitButton.onClick.AddListener(
            () =>
            {
                Save();
                EditorManager.instance.ClearEverything();
            }
        );

        discardAndExitButton.onClick.AddListener(
            () => EditorManager.instance.ClearEverything() // Lol
        );
    }

    // Update is called once per frame
    void Update()
    {
        string newInfo = string.Format(infoTemplate,
            EditorManager.instance.dialogPartVisuals.Count,
            EditorManager.instance.noOfAnswers,
            EditorManager.instance.noOfConnections,
            dialog.startDialogPartID);

        // Update if info differs
        if (!newInfo.Equals(infoText.text))
            infoText.SetText(newInfo);
    }

    private void Save()
    {
        bool success =
                    FileHandler.SaveDialog(dialog, EditorManager.instance.pathToDialog);

        if (success)
            ErrorMessage.instance.ShowErrorMessage("Saved", true);
        // Error message for else case handled by FileHandler.SaveDialog
    }
}
