using UnityEngine;

using UniversalDialogSystem;

public class ExampleDialogManager : UDSDialogManager
{
    protected override void OnDialogStart(Dialog dialog)
    {
        Debug.Log("The dialog is starting!");
    }

    protected override void ShowDialogPartText(in Dialog.DialogPart dialogPart, string text)
    {
        if (dialogPart.HasProperty<string>("Name"))
        {
            string dialogPartnerName = dialogPart.GetProperty<string>("Name");

            if (dialogPartnerName.Equals("Bill"))
                text = "Hi, I'm Bill!";
        }
        else if (text.Length > 160)
        {
            text = "I don't usually say long things";
        }

        ShowDialogPartText(text);
    }

    protected override void ShowAnswer(in Dialog.DialogPart.Answer answer, string text, AnswerBox answerTextBox)
    {
        if (text.Equals("Yes"))
            text = "<color=green>" + text + "</color>";
        else if (text.Equals("No"))
            text = "<color=red>" + text + "</color>";

        SetTextOnTextBox(answerTextBox.textBox, text);
    }
}
