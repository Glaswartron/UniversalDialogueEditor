using UnityEngine;

using UniversalDialogueSystem;

public class ExampleDialogueManager : UDSDialogueManager
{
    protected override void OnDialogueStart(Dialogue dialogue)
    {
        Debug.Log("The dialogue is starting!");
    }

    protected override void ShowDialoguePartText(in Dialogue.DialoguePart dialoguePart, string text)
    {
        if (dialoguePart.HasProperty<string>("Name"))
        {
            string dialoguePartnerName = dialoguePart.GetProperty<string>("Name");

            if (dialoguePartnerName.Equals("Bill"))
                text = "Hi, I'm Bill!";
        }
        else if (text.Length > 160)
        {
            text = "I don't usually say long things";
        }

        ShowDialoguePartText(text);
    }

    protected override void ShowAnswer(in Dialogue.DialoguePart.Answer answer, string text, AnswerBox answerTextBox)
    {
        if (text.Equals("Yes"))
            text = "<color=green>" + text + "</color>";
        else if (text.Equals("No"))
            text = "<color=red>" + text + "</color>";

        SetTextOnTextBox(answerTextBox.textBox, text);
    }
}
