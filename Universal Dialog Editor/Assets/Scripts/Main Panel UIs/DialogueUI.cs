using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
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

    private Dialogue dialogue;

    private List<Warning> warnings;
    private List<GameObject> warningBoxes;

    private string infoTemplate = "Dialogue Parts: {0}\n" +
                          "Answers: {1}\n"      +
                          "Connections: {2}\n" +
                          "Start Part ID: {3}\n";

    private void OnEnable()
    {
        dialogue = EditorManager.instance.dialogue;

        // Super important stuff
        idInputUI.Init(dialogue);
        propertiesUI.Init(dialogue);

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
                "Are you sure that you want to save the dialogue? This will override the previous version of the Dialogue or any dialogue with the same ID if there is one",
                "Yes",
                "No",
                onYes: () => Save(),
                onNo: () => { }
            )
        );

        saveAndExitButton.onClick.AddListener(
            () => AreYouSureDialog.instance.Open(
                "Are you sure that you want to save the dialogue? This will override the previous version of the Dialogue or any dialogue with the same ID if there is one",
                "Yes",
                "No",
                onYes: () => {
                    bool success = Save();
                    if (success) 
                        EditorManager.instance.ClearEverything();
                },
                onNo: () => { }
            )
        );

        discardAndExitButton.onClick.AddListener(
            () => AreYouSureDialog.instance.Open(
                "Are you sure that you want to discard all unsaved changes and exit the dialogue?",
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
            EditorManager.instance.dialoguePartVisuals.Count,
            EditorManager.instance.noOfAnswers,
            EditorManager.instance.noOfConnections,
            dialogue.startDialoguePartID);

        // Update if info differs
        if (!newInfo.Equals(infoText.text))
            infoText.SetText(newInfo);
    }

    private bool Save()
    {
        Dialogue dialogue = EditorManager.instance.ConstructDialogue();

        bool success = dialogue != null;
        if (success)
            success &= FileHandler.SaveDialogue(dialogue, EditorManager.instance.pathToDialogue);

        if (success)
            ErrorMessage.instance.ShowErrorMessage("Saved", true);
        // Error message for else case handled by ConstructDialogue and FileHandler.SaveDialogue

        return success;
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
