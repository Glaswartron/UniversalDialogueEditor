using UnityEngine;

public class UDSDialogueStarter2D : MonoBehaviour
{
    /// <summary>
    /// The ID of the attached Dialogue.
    /// This is the Dialogue that is being started by this UDSDialogueStarter.
    /// </summary>
    [Header("Dialogue")]
    public string dialogueID;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            UDSPlayerDialogue2D.instance.DialogueStarterEnter(this);

            Debug.Log("Player has entered Dialogue radius of " + gameObject.name);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            UDSPlayerDialogue2D.instance.DialogueStarterExit(this);

            Debug.Log("Player has left Dialogue radius of " + gameObject.name);
        }
    }
}
