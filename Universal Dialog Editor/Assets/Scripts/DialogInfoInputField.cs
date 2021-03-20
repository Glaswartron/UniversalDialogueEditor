using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// All possible things the DialogInfoInputField might do.
/// Includes stuff for Dialog Parts, answers and Dialogs (TODO)
/// </summary>
public enum InputType
{
    ID, TEXT, SPEAKER_NAME, NEXT_PART_ID, OPENS_SHOP,
    GAME_VARIABLE, GAME_VARIABLE_VALUE, ITEM_ID,
    ITEM_AMOUNT, CUTSCENE_ID
}

public class DialogInfoInputField : MonoBehaviour
{
    public InputType inputType;

    // UI
    private TMP_InputField inputField;
    private Toggle toggle;

    private void OnEnable()
    {
        // Init
        inputField = GetComponent<TMP_InputField>();
        toggle = GetComponent<Toggle>();

        ShowInfo();
    }

    /// <summary>
    /// Sets the value this InputField is supposed to set based on the inputType
    /// </summary>
    public void SetValue()
    {
        if (EditorManager.instance.editingDialogPart)
        { // The user is editing a Dialog Part
            // Get the current Dialog Part...
            var selectedDialogPart = EditorManager.instance.SelectedDialogPartVisual.dialogPart;
            // ...and edit its values
            switch (inputType)
            {
                case InputType.ID:
                    selectedDialogPart.id = inputField.text;
                    break;
                case InputType.TEXT:
                    if (LanguageButton.currentLang == Language.EN)
                        selectedDialogPart.text = inputField.text;
                    else
                        selectedDialogPart.textDE = inputField.text;
                    break;
                case InputType.SPEAKER_NAME:
                    if (LanguageButton.currentLang == Language.EN)
                        selectedDialogPart.name = inputField.text;
                    else
                        selectedDialogPart.nameDE = inputField.text;
                    break;
                case InputType.GAME_VARIABLE:
                    selectedDialogPart.gameVariable = inputField.text;
                    break;
                case InputType.GAME_VARIABLE_VALUE:
                    selectedDialogPart.gvValue = inputField.text;
                    break;
                case InputType.ITEM_ID:
                    selectedDialogPart.itemID = inputField.text;
                    break;
                case InputType.ITEM_AMOUNT:
                    selectedDialogPart.itemAmount = inputField.text;
                    break;
                case InputType.CUTSCENE_ID:
                    selectedDialogPart.cutsceneToStartID = inputField.text;
                    break;
            }
        }
        else // The user is editing an answer
        {
            // Get the current Answer...
            var selectedAnswer = EditorManager.instance.SelectedAnswerVisual.answer;
            // ...and edit its values
            switch (inputType)
            {
                case InputType.TEXT:
                    if (LanguageButton.currentLang == Language.EN)
                        selectedAnswer.text = inputField.text;
                    else
                        selectedAnswer.textDE = inputField.text;
                    break;
                case InputType.NEXT_PART_ID:
                    selectedAnswer.nextPartID = inputField.text;
                    break;
                case InputType.OPENS_SHOP:
                    if (!string.IsNullOrWhiteSpace(selectedAnswer.nextPartID))
                        if (toggle.isOn)
                        {
                            toggle.isOn = false;
                            ErrorMessage.instance.ShowErrorMessage("Eine Antwort kann dich nicht " +
                                "zu einem Shop bringen, wenn danach noch ein Dialog Part kommt!");
                            return;
                        }
                    selectedAnswer.opensShop = toggle.isOn;
                    break;
                case InputType.GAME_VARIABLE:
                    selectedAnswer.gameVariable = inputField.text;
                    break;
                case InputType.GAME_VARIABLE_VALUE:
                    selectedAnswer.gvValue = inputField.text;
                    break;
                case InputType.CUTSCENE_ID:
                    selectedAnswer.cutsceneToStartID = inputField.text;
                    break;
            }
        }
    }

    /// <summary>
    /// Triggered whenever the user selects something or when the language changes
    /// </summary>
    public void ShowInfo()
    {
        if (EditorManager.instance == null)
            return;

        if (EditorManager.instance.editingDialogPart)
        { // The user has selected a Dialog Part
            // Get the current Dialog Part...
            var selectedDialogPart = EditorManager.instance.SelectedDialogPartVisual.dialogPart;
            // ...and show its values
            switch (inputType)
            {
                case InputType.ID:
                    inputField.text = selectedDialogPart?.id;
                    break;
                case InputType.TEXT:
                    if (LanguageButton.currentLang == Language.EN)
                        inputField.text = selectedDialogPart?.text;
                    else
                        inputField.text = selectedDialogPart?.textDE;
                    break;
                case InputType.SPEAKER_NAME:
                    if (LanguageButton.currentLang == Language.EN)
                        inputField.text = selectedDialogPart?.name;
                    else
                        inputField.text = selectedDialogPart?.nameDE;
                    break;
                case InputType.GAME_VARIABLE:
                    inputField.text = selectedDialogPart?.gameVariable;
                    break;
                case InputType.GAME_VARIABLE_VALUE:
                    inputField.text = selectedDialogPart?.gvValue;
                    break;
                case InputType.ITEM_ID:
                    inputField.text = selectedDialogPart?.itemID;
                    break;
                case InputType.ITEM_AMOUNT:
                    inputField.text = selectedDialogPart?.itemAmount;
                    break;
                case InputType.CUTSCENE_ID:
                    inputField.text = selectedDialogPart?.cutsceneToStartID;
                    break;
            }
        }
        else // The user has selected an answer
        {
            if (EditorManager.instance.SelectedAnswerVisual == null)
                return;

            var selectedAnswer = EditorManager.instance.SelectedAnswerVisual.answer;
            switch (inputType)
            {
                case InputType.TEXT:
                    if (LanguageButton.currentLang == Language.EN)
                        inputField.text = selectedAnswer.text;
                    else
                        inputField.text = selectedAnswer.textDE;
                    break;
                case InputType.NEXT_PART_ID:
                    inputField.text = selectedAnswer.nextPartID;
                    break;
                case InputType.OPENS_SHOP:
                    toggle.isOn = selectedAnswer.opensShop;
                    break;
                case InputType.GAME_VARIABLE:
                    inputField.text = selectedAnswer.gameVariable;
                    break;
                case InputType.GAME_VARIABLE_VALUE:
                    inputField.text = selectedAnswer.gvValue;
                    break;
                case InputType.CUTSCENE_ID:
                    inputField.text = selectedAnswer.cutsceneToStartID;
                    break;
            }
        }
    }
}

