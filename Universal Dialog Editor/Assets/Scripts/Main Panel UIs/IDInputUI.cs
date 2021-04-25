using System;
using System.Collections;
using System.Collections.Generic;
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
        if (!string.IsNullOrWhiteSpace(input))
            dialogComponent.id = input;
        else
            dialogIDInputField.SetTextWithoutNotify
            (dialogComponent.id);

        // Deactivates itself after input
        dialogIDInputField.interactable = false;
    }
}
