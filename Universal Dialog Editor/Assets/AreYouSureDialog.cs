using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class AreYouSureDialog : MonoBehaviour
{
    public static AreYouSureDialog instance;

    [Header("Main UI")]
    public GameObject dialog;
    public TMP_Text textUI;
    public Button yesButton;
    public Button noButton;

    private TMP_Text yesButtonText;
    private TMP_Text noButtonText;

    private UnityAction close;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        yesButtonText = yesButton.GetComponentInChildren<TMP_Text>();
        noButtonText = noButton.GetComponentInChildren<TMP_Text>();

        close = () => dialog.SetActive(false);
    }

    public void Open(string text, string yesText, string noText,
                     UnityAction onYes, UnityAction onNo)
    {
        dialog.SetActive(true);

        textUI.SetText(text);

        yesButtonText.SetText(yesText);
        noButtonText.SetText(noText);

        // Important
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(onYes + close);
        noButton.onClick.AddListener(onNo + close);
    }
}
