using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IDInputUI : MonoBehaviour
{
    public TMP_InputField dialogIDInputField;
    public Button editNameButton;

    public DialogComponent associatedDialogComponent;

    // Start is called before the first frame update
    void Start()
    {
        // Edit name button activates the input field to edit the name
        editNameButton.onClick.AddListener(
            () =>
            {
                dialogIDInputField.interactable = true;
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

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SubmitIDInput(string input)
    {
        if (!string.IsNullOrWhiteSpace(input))
            associatedDialogComponent.id = input;
        else
            dialogIDInputField.SetTextWithoutNotify
            (associatedDialogComponent.id);

        // Deactivates itself after input
        dialogIDInputField.interactable = false;
    }
}
