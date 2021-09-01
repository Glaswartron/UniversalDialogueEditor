using System.Collections.Generic;
using UnityEngine;
using UniversalDialogueSystem;

public class UDSPlayerDialogue3D : MonoBehaviour
{
    /// <summary>
    /// Singleton = There is always just one instance at a time,
    /// and it can be called easily via UDSPlayerDialogue.instance
    /// </summary>
    public static UDSPlayerDialogue3D instance;

    [Header("Connected UI")]
    [SerializeField] private GameObject dialogueNotification;

    /// <summary>
    /// A list that contains all UDSDialogueStarters in whose range 
    /// the player currently is, that is all the Dialogue Partners 
    /// the player is currently close enough to to start the Dialogue.
    /// When the player actually starts a Dialogue, the closest
    /// one is being picked.
    /// </summary>
    private static List<UDSDialogueStarter3D> dialogueStartersInRange;

    private void Start()
    {
        // Singleton
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        dialogueStartersInRange = new List<UDSDialogueStarter3D>();
    }

    // Update is called once per frame
    void Update()
    {
        /* Start a Dialogue when the player hits E */
        if (!UDSDialogueManager.instance.dialogueRunning && Input.GetKeyDown(KeyCode.E))
        {
            UDSDialogueStarter3D dialogueStarter = GetClosestDialogueStarterInRange();
            if (dialogueStarter != null) // null if there is no UDSDialogueStarter close to the player
            {
                string dialogueID = dialogueStarter.dialogueID;

                UDSDialogueManager.instance.StartDialogue(dialogueID);
            }
        }
    }

    /// <summary>
    /// Called by an UDSDialogueStarter 
    /// when the player enters its radius
    /// </summary>
    /// <param name="ds">The UDSDialogueStarter whose interaction radius the player has just entered</param>
    public void DialogueStarterEnter(UDSDialogueStarter3D ds)
    {
        dialogueStartersInRange.Add(ds);

        if (!instance.dialogueNotification.activeSelf)
            instance.dialogueNotification.SetActive(true);
    }

    /// <summary>
    /// Called by an UDSDialogueStarter 
    /// when the player leaves its radius
    /// </summary>
    /// <param name="ds">The UDSDialogueStarter whose interaction radius the player has just left</param>
    public void DialogueStarterExit(UDSDialogueStarter3D ds)
    {
        dialogueStartersInRange.Remove(ds);

        if (instance.dialogueNotification.activeSelf)
            instance.dialogueNotification.SetActive(false);
    }

    /// <summary>
    /// Determines the UDSDialogueStarter in whose range the player is and
    /// which is closest to the player (by transform.position) out of 
    /// all in range.
    /// All UDSDialogueStarters are being considered that have called
    /// UDSPlayerDialogue.instance.DialogueStarterEntered(this) but not yet
    /// called UDSPlayerDialogue.instance.DialogueStarterExit(this).
    /// </summary>
    /// <returns>The closest possible Dialogue Partner to the player in whose range
    /// the player is.</returns>
    public UDSDialogueStarter3D GetClosestDialogueStarterInRange()
    {
        if (dialogueStartersInRange.Count <= 0)
            return null;

        return dialogueStartersInRange.Find // Find the Dialogue starter (DS)...
                (ds => dialogueStartersInRange.TrueForAll // ...who is for all other DS in the list...
                    (otherDS => // ... closer to this transform (SqrMagnitude for performance reasons)
                        Vector3.SqrMagnitude(ds.transform.position - transform.position)
                        <= Vector3.SqrMagnitude(otherDS.transform.position - transform.position)));
    }
}
