using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SimpleFileBrowser;

public class EditorManager : MonoBehaviour
{
    // Singleton
    public static EditorManager instance;

    public Dialog dialog;
    public Dialog dialogBackup;

    public string pathToDialog;

    // All Dialog Part visuals on Screen (each of them stores an actual Dialog.DialogPart)
    public List<DialogPartVisual> dialogPartVisuals;

    public GameObject ActiveUI
    {
        set
        {
            activeUI.SetActive(false);
            activeUI = value;
            activeUI.SetActive(true);
        }

        get { return activeUI;  }
    }
    private GameObject activeUI;

    [Header("Main UI")]
    public RectTransform editorPanel;
    public GameObject startAndSelectUI;
    public GameObject dialogUI;
    public GameObject dialogPartUI;
    public GameObject answerUI;
    public GameObject areYouSureDialogLoad;
    public GameObject areYouSureDialogSave;
    public GameObject localisationManager;

    [Header("Prefabs")]
    public GameObject dialogPartVisual;
    public GameObject answerVisual;
    public GameObject arrow;

    [Header("Support UI")]
    public RectTransform graphEditorBounds;
    [HideInInspector]
    public Camera mainCam;

    [Space(7)]
    // true => Editing Dialog Part; false => Editing answer!
    public bool editingDialogPart;

    public bool inConnectMode;

    //public DialogInfoInputField[] inputFields;

    private bool contextMenuOpen;

    /// <summary>
    /// The currently selected Dialog Part (visual)
    /// </summary>
    public DialogPartVisual SelectedDialogPartVisual
    {
        set
        {
            // Deselect the previously selected visual
            DeselectPreviouslySelectedVisual();

            // Set
            selectedDialogPartVisual = value;

            editingDialogPart = true;

            if (value == null && ActiveUI != startAndSelectUI)
                ActiveUI = dialogUI;
            else if (value != null)
                ActiveUI = dialogPartUI;
        }

        get { return selectedDialogPartVisual; }
    }
    private DialogPartVisual selectedDialogPartVisual = null;

    /// <summary>
    /// The currently selected answer (visual)
    /// </summary>
    public AnswerVisual SelectedAnswerVisual
    {
        set
        {
            // Deselect the previously selected visual
            DeselectPreviouslySelectedVisual();

            selectedAnswerVisual = value;

            editingDialogPart = false;

            if (value == null && ActiveUI != startAndSelectUI)
                ActiveUI = dialogUI;
            else if (value != null)
                ActiveUI = answerUI;
        }

        get { return selectedAnswerVisual; }
    }
    private AnswerVisual selectedAnswerVisual = null;

    public DialogPartVisual StartDialogPartVisual
    {
        set
        {
            // Previous one not start anymore
            if (startDialogPartVisual != null)
                startDialogPartVisual.IsStart = false; 

            value.IsStart = true;

            startDialogPartVisual = value;
        }

        get { return startDialogPartVisual; }
    }
    private DialogPartVisual startDialogPartVisual;

    public GameObject selectedConnection;

    public int noOfAnswers;
    public int noOfConnections;

    // Start is called before the first frame update
    void Start()
    {
        Screen.fullScreen = false;

        // Singleton
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);

        // Init
        mainCam = Camera.main;
        dialogPartVisuals = new List<DialogPartVisual>();
        activeUI = startAndSelectUI;
    }

    private void Update()
    {
        // Left mouse button deselects and closes everything
        if (Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject() &&
                !Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition)))
            {
                SelectedDialogPartVisual = null;
                SelectedAnswerVisual = null;
                inConnectMode = false;
            }

            //DeactivateAllContextMenus();
        }

        /* Right mouse button deselect everything 
         * (and opens a context menu --> ContextMenuManager) */
        if (Input.GetMouseButtonDown(1))
        {
            inConnectMode = false;
            SelectedDialogPartVisual = null;
            SelectedAnswerVisual = null;
        }

        // Delete key deletes currenty selected Dialog Part
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (SelectedDialogPartVisual != null)
                DestroyDialogPart();
        }

        // Ctrl + D duplicates currently selected Dialog Part
        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl))
            && Input.GetKeyDown(KeyCode.D))
        {
            CopySelectedDialogPart();
        }
    }

    /// <summary>
    /// Builds a Dialog object from the Dialog Parts and other information
    /// and assigns it to the dialog reference.
    /// </summary>
    /// <returns>Whether or not the dialog was successfully created. If not,
    /// the dialog reference is still null.</returns>
    public bool ConstructDialog()
    {
        if (!ValidateDialog())
            return false;

        /*dialog = new DialogOld
        {
            id = dialogIDInputField.text,
            revealTextGradually = revealGraduallyToggle.isOn,

            dialogParts = new DialogOld.DialogPart[dialogPartVisuals.Count]
        };*/

        var dialogParts = dialogPartVisuals.ConvertAll(dpv => dpv.dialogPart);

        //DialogOld.DialogPart first = dialogParts.Find(dp => dp.id.Equals("start"));

        /*if (first == null)
        {
            ErrorMessage.instance.ShowErrorMessage("Kein Dialog Part hat die ID 'start'!" +
                " Jeder Dialog braucht einen Anfang, der durch die ID 'start' gekennzeichnet " +
                "sein muss!");
            return false;
        }*/

        //dialog.dialogParts[0] = first;

        for (int i = 0; i < dialogParts.Count; i++)
        {
            var dp = dialogParts[i];

            if (dialog.dialogParts[i] != null || dp.id.Equals("start"))
                continue;

            //dialog.dialogParts[i] = dialogParts[i];
        }

        /*if (dialog.revealTextGradually)
            AddRichTextTagDelimiters();
        else
            DeleteRichTextTagDelimiters();*/

        return true;
    }

    /// <summary>
    /// Checks, if the Dialog is valid and "finished" and if all criteria are met.
    /// These are:
    /// - At least one dialog part is there
    /// - The dialogID is not empty
    /// - All DialogParts have an ID
    /// - All DialogPartIDs are unique
    /// Displays an error message if at least one criterion is not met!
    /// </summary>
    /// <returns>Whether or not the dialog is valid</returns>
    public bool ValidateDialog()
    {
        if (dialogPartVisuals.Count == 0)
        {
            ErrorMessage.instance.ShowErrorMessage("Bitte mach zuerst irgendwas :D");
            return false;
        }

        /*if (string.IsNullOrWhiteSpace(dialogIDInputField.text))
        {
            ErrorMessage.instance.ShowErrorMessage("Der Dialog braucht noch eine ID! Achte" +
                " auch darauf, dass sie einzigartig ist und nicht schon in CoT vorkommt!");
            return false;
        }*/

        HashSet<string> diapartIDs = new HashSet<string>();
        foreach (var diapart in dialogPartVisuals)
        {
            if (string.IsNullOrWhiteSpace(diapart.dialogPart.id))
            {
                ErrorMessage.instance.ShowErrorMessage("Ein Dialog Part hat noch keine ID!");
                return false;
            }
            if (diapartIDs.Contains(diapart.dialogPart.id))
            {
                ErrorMessage.instance.ShowErrorMessage("Eine Dialog Part ID kommt doppelt vor!");
                return false;
            }
            diapartIDs.Add(diapart.dialogPart.id);
        }

        return true;
    }

    /// <summary>
    /// Shows the file browser UI from which the user can select a file (path)
    /// </summary>
    public void ShowLoadFileBrowser()
    {
        FileBrowser.ShowLoadDialog(OnLoadSelectSuccess, null);
    }

    /// <summary>
    /// Shows the file browser UI from which the user can select a folder (path)
    /// </summary>
    public void ShowSaveFileBrowser()
    {
        FileBrowser.ShowSaveDialog(OnSaveSelectSuccess, null, true);
    }

    /// <summary>
    /// Shows the "Are you sure" dialog for loading
    /// </summary>
    public void ShowLoadDialog()
        => areYouSureDialogLoad.SetActive(true);

    /// <summary>
    /// Shows the "Are you sure" dialog for saving
    /// </summary>
    public void ShowSaveDialog()
        => areYouSureDialogSave.SetActive(true);

    /// <summary>
    /// Called by the file browser once a path has been selected.
    /// Do not use otherwise!
    /// </summary>
    private void OnLoadSelectSuccess(string[] paths)
    {
        //loadPathInputField.text = paths[0];
    }

    /// <summary>
    /// Called by the file browser once a path has been selected.
    /// Do not use otherwise!
    /// </summary>
    private void OnSaveSelectSuccess(string[] paths)
    {
        //savePathInputField.text = paths[0];
    }

    /// <summary>
    /// Loads the given dialog. 
    /// Shows the dialog in the editor, assigns all necessary references
    /// and allows the user to edit the dialog.
    /// </summary>
    /// <param name="dia">The dialog to load</param>
    /// <param name="path">The path to the .udsdialog file where the dialog is stored</param>
    public void LoadDialog(Dialog dialog, string path)
    {
        ClearEverything();

        this.dialog = dialog;
        this.pathToDialog = path;
        //this.dialogBackup = dialog.Clone(); // Backup for potential fallback/discard

        List<AnswerVisual> answers = new List<AnswerVisual>();

        // Go over all Dialog Parts in the dialog and...
        foreach (Dialog.DialogPart diaPart in dialog.dialogParts)
        {
            // ... instantiate a corresponding visual in the editor
            GameObject visualGO = Instantiate(dialogPartVisual,
                new Vector2(diaPart.visualX, diaPart.visualY), Quaternion.identity);

            DialogPartVisual visual = visualGO.GetComponent<DialogPartVisual>();
            dialogPartVisuals.Add(visual);

            visual.dialogPart = diaPart;

            if (diaPart.id.Equals(dialog.startDialogPartID))
                StartDialogPartVisual = visual;

            // Add all the answers (for each Dialog Part)
            for (int i = 0; i < diaPart.answers.Length; i++)
            {
                // Mathzzz
                float angle = (i + 1) * ((2 * Mathf.PI) / diaPart.answers.Length);

                Vector2 middle = visualGO.transform.position;
                Vector2 position = new Vector2(middle.x + Mathf.Cos(angle) * 0.75f,
                                               middle.y + Mathf.Sin(angle) * 0.75f);
                // ---

                GameObject answerVisualGO = Instantiate
                    (answerVisual, position, Quaternion.identity);
                
                // Setup the Answer Visual
                var answerVis = answerVisualGO.GetComponent<AnswerVisual>();
                answerVisualGO.transform.parent = visualGO.transform;
                answerVis.parentDialogPart = visual;
                answerVis.answer = diaPart.answers[i];
                answerVis.index = i;
                answers.Add(answerVis);

                noOfAnswers++; // Count how many answers there are in total
            }

            visual.answers = answers.ToArray(); // !
        }

        // Add connections to all answers (that have connections)
        foreach (AnswerVisual aVisual in answers)
        {
            if (!string.IsNullOrWhiteSpace(aVisual.answer.nextDialogPartID))
            {
                aVisual.SetConnection(
                    Array.Find(dialogPartVisuals.ToArray(),
                    dpv => dpv.dialogPart.id.Equals(aVisual.answer.nextDialogPartID)));

                noOfConnections++; // Count how many connections there are in total
            }
        }

        // Add connections to all Dialog Parts (that have connections)
        foreach (DialogPartVisual dpVisual in dialogPartVisuals)
        {
            if (!string.IsNullOrWhiteSpace(dpVisual.dialogPart.nextDialogPartID))
            {
                dpVisual.SetConnection(
                    Array.Find(dialogPartVisuals.ToArray(),
                    dpv => dpv.dialogPart.id.Equals(dpVisual.dialogPart.nextDialogPartID)));

                noOfConnections++; // Count how many connections there are in total
            }
        }

        // Go from StartAndSelectUI to DialogUI
        ActiveUI = dialogUI;
    }

    /// <summary>
    /// Goes over all input fields and checkboxes and updates them
    /// based on the currently selected visual.
    /// </summary>
    /*public void UpdateInputFields()
    {
        foreach (DialogInfoInputField inF in inputFields)
        {
            if (inF.gameObject.activeSelf)
            {
                inF.ShowInfo();
            }
        }
    }*/

    /// <summary>
    /// Clears everything and returns to StartAndSelectUI.
    /// Discards any unsaved data.
    /// Use with caution!
    /// </summary>
    public void ClearEverything()
    {
        ContextMenuManager.instance.DeactivateAllContextMenus();

        foreach (DialogPartVisual dpv in dialogPartVisuals)
            Destroy(dpv.gameObject);

        dialogPartVisuals = new List<DialogPartVisual>();

        inConnectMode = false;

        dialog = null;

        // Important that this happens before set is called on the properties (below)
        ActiveUI = startAndSelectUI; 

        SelectedDialogPartVisual = null;
        SelectedAnswerVisual = null;
        selectedConnection = null;

        noOfAnswers = 0;
        noOfConnections = 0;
    }

    /// <summary>
    /// Creates a new Dialog Part visual at the mouse pos and 
    /// adds it to the dialogPartVisuals list
    /// </summary>
    public void CreateDialogPart()
    {
        Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        GameObject dpGO = Instantiate(dialogPartVisual, mousePos, Quaternion.identity);
        DialogPartVisual dpVisual = dpGO.GetComponent<DialogPartVisual>();

        dialogPartVisuals.Add(dpVisual);
    }

    /// <summary>
    /// Copies the currently selected dialog part and creates a
    /// new visual below it.
    /// </summary>
    public void CopySelectedDialogPart()
    {
        Vector3 posOffset = new Vector2(0, -0.5f);
        Vector2 pos = SelectedDialogPartVisual.transform.position + posOffset;

        GameObject dpGO = Instantiate(dialogPartVisual, pos, Quaternion.identity);

        DialogPartVisual dpVisual = dpGO.GetComponent<DialogPartVisual>();
        //dpVisual.dialogPart = new DialogOld.DialogPart();

        // Do se magic
        /*dpVisual.dialogPart.id = SelectedDialogPartVisual.dialogPart.id;
        dpVisual.dialogPart.nextPartID = SelectedDialogPartVisual.dialogPart.nextPartID;
        dpVisual.dialogPart.name = SelectedDialogPartVisual.dialogPart.name;
        dpVisual.dialogPart.nameDE = SelectedDialogPartVisual.dialogPart.nameDE;
        dpVisual.dialogPart.text = SelectedDialogPartVisual.dialogPart.text;
        dpVisual.dialogPart.textDE = SelectedDialogPartVisual.dialogPart.textDE;
        dpVisual.dialogPart.answers = SelectedDialogPartVisual.dialogPart.answers;
        dpVisual.dialogPart.gameVariable = SelectedDialogPartVisual.dialogPart.gameVariable;
        dpVisual.dialogPart.gvValue = SelectedDialogPartVisual.dialogPart.gvValue;
        dpVisual.dialogPart.itemID = SelectedDialogPartVisual.dialogPart.itemID;
        dpVisual.dialogPart.itemAmount = SelectedDialogPartVisual.dialogPart.itemAmount;
        dpVisual.dialogPart.cutsceneToStartID = SelectedDialogPartVisual.dialogPart.cutsceneToStartID;*/

        dialogPartVisuals.Add(dpVisual);
    }

    /// <summary>
    /// Connects a dialog part (visual) to the currently selected answer
    /// </summary>
    public void ConnectToSelectedAnswer(DialogPartVisual dp)
    {
        SelectedAnswerVisual.SetConnection(dp);

        noOfConnections++;

        inConnectMode = false;
    }

    /// <summary>
    /// Connects a dialog part (visual) directly to the currently selected dialog part
    /// </summary>
    public void ConnectToSelectedDP(DialogPartVisual dp)
    {
        if (SelectedDialogPartVisual.dialogPart.answers.Length > 0)
        {
            ErrorMessage.instance.ShowErrorMessage("Nur Dialog Parts ohne Antwort " +
                "können direkt zu einem anderen verbunden werden!");
            inConnectMode = false;
            return;
        }

        SelectedDialogPartVisual.SetConnection(dp);

        noOfConnections++;

        inConnectMode = false;
    }

    public void SwitchToConnectMode()
    {
        inConnectMode = true;
        ContextMenuManager.instance.DeactivateAllContextMenus();
    }

    /// <summary>
    /// Destroys the currently selected dialog part. Use with caution!
    /// </summary>
    public void DestroyDialogPart()
    {
        if (SelectedDialogPartVisual == null)
            return;

        dialogPartVisuals.Remove(SelectedDialogPartVisual);

        Destroy(SelectedDialogPartVisual.gameObject);
        SelectedDialogPartVisual = null;
        ContextMenuManager.instance.DeactivateAllContextMenus();
    }

    public void DestroyConnection()
    {
        Destroy(selectedConnection.gameObject);
        noOfConnections--;

        ContextMenuManager.instance.DeactivateAllContextMenus();
    }

    public void AddAnswerToSelectedPart()
    {
        bool success = selectedDialogPartVisual.AddAnswer();

        if (success) 
            noOfAnswers++;
    }

    public void RemoveAnswerFromSelectedPart()
    {
        //noOfAnswers--;
        //selectedDialogPartVisual.DeleteAnswer();
    }

    private void DeselectPreviouslySelectedVisual()
    {
        if (selectedDialogPartVisual != null)
        {
            selectedDialogPartVisual.Selected = false;
            selectedDialogPartVisual = null;
        }

        if (selectedAnswerVisual != null)
        {
            selectedAnswerVisual.Selected = false;
            selectedAnswerVisual = null;
        }
    }

    /// <summary>
    /// Goes through all dialog parts and texts and adds the 
    /// "|" delimiter in front of and behind color tags
    /// </summary>
    private void AddRichTextTagDelimiters()
    {
        foreach (Dialog.DialogPart diaPart in dialog.dialogParts)
        {
            /*if (diaPart.text.Contains("<color") && !diaPart.text.Contains("|<color"))
                diaPart.text = diaPart.text.Replace("<color", "|<color");

            if (diaPart.text.Contains("</color>") && !diaPart.text.Contains("</color>|"))
                diaPart.text = diaPart.text.Replace("</color>", "</color>|");

            if (diaPart.textDE.Contains("<color") && !diaPart.textDE.Contains("|<color"))
                diaPart.textDE = diaPart.textDE.Replace("<color", "|<color");

            if (diaPart.textDE.Contains("</color>") && !diaPart.textDE.Contains("</color>|"))
                diaPart.textDE = diaPart.textDE.Replace("</color>", "</color>|");*/
        }
    }

    /// <summary>
    /// Goes through all dialog parts and texts and deletes the 
    /// "|" delimiter in front of and behind color tags
    /// </summary>
    private void DeleteRichTextTagDelimiters()
    {
        foreach (Dialog.DialogPart diaPart in dialog.dialogParts)
        {
            /*if (diaPart.text.Contains("|<color"))
                diaPart.text = diaPart.text.Replace("|<color", "<color");

            if (diaPart.text.Contains("</color>|"))
                diaPart.text = diaPart.text.Replace("</color>|", "</color>");

            if (diaPart.textDE.Contains("|<color"))
                diaPart.textDE = diaPart.textDE.Replace("|<color", "<color");

            if (diaPart.textDE.Contains("</color>|"))
                diaPart.textDE = diaPart.textDE.Replace("</color>|", "</color>");*/
        }
    }
}
