using UnityEngine;

public class DialoguePartUI : MonoBehaviour
{
    [Header("Main UI")]
    public PropertiesUI propertiesUI;
    public IDInputUI idInputUI;

    [HideInInspector] public Dialogue.DialoguePart dialoguePart;

    private void OnEnable()
    {
        dialoguePart = EditorManager.instance.SelectedDialoguePartVisual.dialoguePart;

        idInputUI.Init(dialoguePart);
        propertiesUI.Init(dialoguePart); // Super important stuff
    }
}
