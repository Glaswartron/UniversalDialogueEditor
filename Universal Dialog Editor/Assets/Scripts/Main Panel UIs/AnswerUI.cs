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
        idInputUI.dialogComponent = answer;

        string nextPartID = answer.nextDialogPartID;
        string fromTo 
            = answer.dialogPart.id + 
            " --> " + 
            (nextPartID != null ? nextPartID : "End");

        fromToText.SetText(fromTo);
    }
}
