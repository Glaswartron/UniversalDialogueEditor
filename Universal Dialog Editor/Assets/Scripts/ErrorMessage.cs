using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ErrorMessage : MonoBehaviour
{
    public static ErrorMessage instance;

    public float messageLength;

    private TextMeshProUGUI text;
    private Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);

        // Init
        text = GetComponentInChildren<TextMeshProUGUI>();
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Displays and error message to the user.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="green">Whether the color of the text should be 
    /// green (true) or red (false, standard)</param>
    public void ShowErrorMessage(string message, bool green = false)
    {
        if (green)
            text.color = Color.green;
        else
            text.color = Color.red;

        text.SetText(message);
        StopAllCoroutines();
        StartCoroutine(ErrorMessageCo());
    }

    private IEnumerator ErrorMessageCo()
    {
        anim.SetBool("messageShowing", true);
        yield return new WaitForSeconds(messageLength);
        anim.SetBool("messageShowing", false);
    }
}
