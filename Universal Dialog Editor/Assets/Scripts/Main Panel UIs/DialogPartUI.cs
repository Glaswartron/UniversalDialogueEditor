using UnityEngine;

public class DialogPartUI : MonoBehaviour
{
    [Header("Main UI")]
    public PropertiesUI propertiesUI;
    public IDInputUI idInputUI;

    public Dialog.DialogPart dialogPart;

    private void OnEnable()
    {
        dialogPart = EditorManager.instance.SelectedDialogPartVisual.dialogPart;

        idInputUI.Init(dialogPart);
        propertiesUI.Init(dialogPart); // Super important stuff
    }
}
