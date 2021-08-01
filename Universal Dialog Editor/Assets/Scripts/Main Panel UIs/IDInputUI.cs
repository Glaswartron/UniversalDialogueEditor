using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IDInputUI : MonoBehaviour, ISubUI
{
    public DialogueComponent dialogueComponent;

    public TMP_InputField dialogueIDInputField;
    public Button editNameButton;

    public void Init(DialogueComponent dialogueComponent)
    {
        this.dialogueComponent = dialogueComponent;

        dialogueIDInputField.SetTextWithoutNotify(dialogueComponent.id);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Edit name button activates the input field to edit the name
        editNameButton.onClick.AddListener(
            () =>
            {
                dialogueIDInputField.interactable = true;
                EventSystem.current.SetSelectedGameObject(dialogueIDInputField.gameObject);

                dialogueIDInputField.Select();
            }
        );

        dialogueIDInputField.onDeselect.AddListener(
            (input) => SubmitIDInput(input)
        );

        dialogueIDInputField.onSubmit.AddListener(
            (input) => SubmitIDInput(input)
        );
    }

    private void SubmitIDInput(string input)
    {
        if (!string.IsNullOrWhiteSpace(input)
            && Array.TrueForAll(EditorManager.invalidCharacters, c => !input.Contains(c.ToString())))
        {
            // Update startDialoguePartID if needed
            if (dialogueComponent.GetType() == typeof(Dialogue.DialoguePart))
                if (dialogueComponent.id == EditorManager.instance.dialogue.startDialoguePartID)
                    EditorManager.instance.dialogue.startDialoguePartID = input;

            dialogueComponent.id = input;
        }
        else // Invalid input
        { 
            dialogueIDInputField.SetTextWithoutNotify
            (dialogueComponent.id);

            ErrorMessage.instance.ShowErrorMessage
                ("Invalid input. Either no text or contains invalid characters");
        }

        // Deactivates itself after input
        dialogueIDInputField.interactable = false;
    }
}
