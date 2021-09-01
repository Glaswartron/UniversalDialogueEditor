using System.Collections.Generic;
using UnityEngine;
using UniversalDialogueSystem;

class ExampleDialogueManager : UDSDialogueManager
{
    /* 
    Overview:
    protected override void OnDialogueStart(Dialogue dialogue) { }
    protected override void OnDialoguePartStart(Dialogue.DialoguePart dialoguePart) { }
    protected override void OnShowCharacter(Dialogue.DialoguePart dialoguePart, char character) { }
    protected override bool OnAnswer(Dialogue.DialoguePart.Answer answer, AnswerBox answerTextBox) { return false; }
    protected override void OnDialogueEnd(Dialogue dialogue) { }

    protected override void OnDialoguePause() { }
    protected override void OnDialogueContinue() { }

    protected override void ShowDialoguePartText(Dialogue.DialoguePart dialoguePart, string text) { }
    protected override void ShowAnswer(Dialogue.DialoguePart.Answer answer, string text, AnswerBox answerTextBox) { }
    protected override void ShowName(Dialogue.DialoguePart dialoguePart, string name, TextBox nameTextBox) { }

    protected override void EnableDialogueUI(bool continueAfterPause = false) { }
    protected override void DisableDialogueUI(bool pause = false) { }

    protected override bool SaveGlobalProperties() { return false; }
    protected override Dictionary<string, UDSProperty> LoadGlobalProperties() { return null; } 
    */

    protected override void OnDialoguePartStart(Dialogue.DialoguePart dialoguePart)
    {
        /* Update the Global Property "atlantisQuestProgress" whenever 
         * a Dialogue Part has a corresponding Property */

        if (dialoguePart.HasProperty<int>("atlantisQuestProgress"))
        {
            SetGlobalProperty("atlantisQuestProgress",
                dialoguePart.GetProperty<int>("atlantisQuestProgress"));
        }
    }

    protected override bool OnAnswer(Dialogue.DialoguePart.Answer answer,
                                    AnswerBox answerTextBox)
    {
        /* Pause the Dialogue and open a shop window whenever an
         * answer has a "Opens shop" Property which is set to true */

        if (answer.HasProperty<bool>("Opens shop")
        && answer.GetProperty<bool>("Opens shop"))
        {
            PauseDialogue(disableDialogueUI: true, resetTimescale: false);

            // Open shop here and call ContinueDialogue() when shop is being closed

            return true;
        }

        return false;
    }

    protected override void ShowAnswer(Dialogue.DialoguePart.Answer answer,
                                    string text, AnswerBox answerTextBox)
    {
        // 'Yes' is always being shown in green, 'No' is always being shown in red

        if (text.Equals("Yes"))
            text = "<color=green>" + text + "</color>";
        else if (text.Equals("No"))
            text = "<color=red>" + text + "</color>";

        base.ShowAnswer(answer, text, answerTextBox);
    }

}
