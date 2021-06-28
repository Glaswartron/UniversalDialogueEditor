using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IDInputUI : MonoBehaviour, ISubUI
{
    public DialogComponent dialogComponent;

    public TMP_InputField dialogIDInputField;
    public Button editNameButton;

    public void Init(DialogComponent dialogComponent)
    {
        this.dialogComponent = dialogComponent;

        dialogIDInputField.SetTextWithoutNotify(dialogComponent.id);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Edit name button activates the input field to edit the name
        editNameButton.onClick.AddListener(
            () =>
            {
                dialogIDInputField.interactable = true;
                EventSystem.current.SetSelectedGameObject(dialogIDInputField.gameObject);

                dialogIDInputField.Select();
            }
        );

        dialogIDInputField.onDeselect.AddListener(
            (input) => SubmitIDInput(input)
        );

        dialogIDInputField.onSubmit.AddListener(
            (input) => SubmitIDInput(input)
        );
    }

    private void SubmitIDInput(string input)
    {
        if (!string.IsNullOrWhiteSpace(input)
            && Array.TrueForAll(EditorManager.invalidCharacters, c => !input.Contains(c.ToString())))
        {
            // Update startDialogPartID if needed
            if (dialogComponent.GetType() == typeof(Dialog.DialogPart))
                if (dialogComponent.id == EditorManager.instance.dialog.startDialogPartID)
                    EditorManager.instance.dialog.startDialogPartID = input;

            dialogComponent.id = input;
        }
        else // Invalid input
        { 
            dialogIDInputField.SetTextWithoutNotify
            (dialogComponent.id);

            ErrorMessage.instance.ShowErrorMessage
                ("Invalid input. Either no text or contains invalid characters");
        }

        // Deactivates itself after input
        dialogIDInputField.interactable = false;
    }
}
