using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogUI : MonoBehaviour
{
    public struct Warning
    {
        public string text;
        public Color color;
    }

    [Header("Main UI")]
    public PropertiesUI propertiesUI;
    public IDInputUI idInputUI;
    public Button saveButton;
    public Button saveAndExitButton;
    public Button discardAndExitButton;
    public TMP_Text infoText;
    public Transform warningScrollViewContent;

    [Header("Prefabs")]
    public GameObject warningBox;

    private Dialog dialog;

    private List<Warning> warnings;
    private List<GameObject> warningBoxes;

    private string infoTemplate = "Dialog Parts: {0}\n" +
                          "Answers: {1}\n"      +
                          "Connections: {2}\n" +
                          "Start Part ID: {3}\n";

    private void OnEnable()
    {
        dialog = EditorManager.instance.dialog;

        // Super important stuff
        idInputUI.Init(dialog);
        propertiesUI.Init(dialog);

        ShowWarnings();
    }

    private void OnDisable()
    {
        ClearWarnings();
    }

    // Start is called before the first frame update
    void Start()
    {
        saveButton.onClick.AddListener(
            () => AreYouSureDialog.instance.Open(
                "Are you sure that you want to save the dialog? This will override the previous version of the Dialog or any dialog with the same ID if there is one",
                "Yes",
                "No",
                onYes: () => Save(),
                onNo: () => { }
            )
        );

        saveAndExitButton.onClick.AddListener(
            () => AreYouSureDialog.instance.Open(
                "Are you sure that you want to save the dialog? This will override the previous version of the Dialog or any dialog with the same ID if there is one",
                "Yes",
                "No",
                onYes: () => {
                    Save();
                    EditorManager.instance.ClearEverything();
                },
                onNo: () => { }
            )
        );

        discardAndExitButton.onClick.AddListener(
            () => AreYouSureDialog.instance.Open(
                "Are you sure that you want to discard all unsaved changes and exit the dialog?",
                "Yes",
                "No",
                onYes: () => EditorManager.instance.ClearEverything(),
                onNo: () => { }
            )
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
            FileHandler.SaveDialog(EditorManager.instance.ConstructDialog(),
                                   EditorManager.instance.pathToDialog);

        if (success)
            ErrorMessage.instance.ShowErrorMessage("Saved", true);
        // Error message for else case handled by FileHandler.SaveDialog
    }

    private void ShowWarnings()
    {
        if (warningBoxes == null)
            warningBoxes = new List<GameObject>();

        ClearWarnings();

        warnings = EditorManager.instance.GenerateWarnings();

        // Sort so that red warnings are on top
        warnings = warnings.OrderByDescending(w => w.color == Color.red).ToList();

        foreach (Warning warning in warnings)
        {
            GameObject newWarningBox = Instantiate(warningBox, warningScrollViewContent);
            warningBoxes.Add(newWarningBox);
            TMP_Text text = newWarningBox.GetComponentInChildren<TMP_Text>();

            text.SetText(warning.text);
            text.color = warning.color;
        }
    }

    private void ClearWarnings()
    {
        if (warningBoxes == null)
            return;

        foreach (GameObject warningBox in warningBoxes)
        {
            Destroy(warningBox.gameObject);
        }

        warningBoxes.Clear();

        if (warnings != null)
            warnings.Clear();
    }
}
