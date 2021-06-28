using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AnswerUI : MonoBehaviour
{
    [Header("Main UI")]
    public PropertiesUI propertiesUI;
    public IDInputUI idInputUI;
    public TMP_Text fromToText;

    public Dialog.DialogPart.Answer answer;

    private void OnEnable()
    {
        answer = EditorManager.instance.SelectedAnswerVisual.answer;

        propertiesUI.Init(answer);
        idInputUI.Init(answer);

        string nextPartID = answer.nextDialogPartID;
        string fromTo 
            = EditorManager.instance.SelectedAnswerVisual.parentDialogPart.dialogPart.id + 
            " --> " + 
            (nextPartID != null ? nextPartID : "End");

        fromToText.SetText(fromTo);
    }
}
