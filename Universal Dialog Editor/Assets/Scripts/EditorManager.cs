using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SimpleFileBrowser;

public class EditorManager : MonoBehaviour
{
    // Singleton
    public static EditorManager instance;

    public DialogOld dialog;

    // All Dialog Part visuals on Screen (each of them stores an actual Dialog.DialogPart)
    public List<DialogPartVisual> dialogPartVisuals;

    [Header("Prefabs")]
    public GameObject dialogPartVisual;
    public GameObject arrow;

    [Header("Main UI")]
    public RectTransform editorPanel;
    public GameObject startAndSelectUI;
    public GameObject dialogUI;
    public GameObject dialogPartUI;
    public GameObject answerUI;
    public GameObject areYouSureDialogLoad;
    public GameObject areYouSureDialogSave;
    public GameObject localisationManager;

    [Header("Right Click Menus")]
    public GameObject createRightClickMenu;
    public GameObject dialogPartRightClickMenu;
    public GameObject answerRightClickMenu;
    public GameObject connectionRightClickMenu;

    [Header("File UI")]
    public TMP_InputField savePathInputField;
    public TMP_InputField loadPathInputField;

    [Header("Dialog Edit UI")]
    public TMP_InputField dialogIDInputField;
    public Toggle revealGraduallyToggle;

    [Space(7)]
    // true => Editing Dialog Part; false => Editing answer!
    public bool editingDialogPart;

    public bool inConnectMode;

    //public DialogInfoInputField[] inputFields;

    private bool actionMenuOpen;

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

            // UI: Show Dialog Part editor
            answerUI.SetActive(false);
            startAndSelectUI.SetActive(false);
            dialogUI.SetActive(false);
            dialogPartUI.SetActive(false); // OnEnable has to be triggered
            dialogPartUI.SetActive(value != null);

            if (value == null)
                dialogUI.SetActive(true);
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

            // UI: Show answer editor
            dialogPartUI.SetActive(false);
            startAndSelectUI.SetActive(false);
            answerUI.SetActive(false); // OnEnable has to be triggered
            answerUI.SetActive(value != null);

            if (value == null)
                dialogUI.SetActive(true);
        }

        get { return selectedAnswerVisual; }
    }
    private AnswerVisual selectedAnswerVisual = null;

    private GameObject clickedConnection;

    private Camera mainCam;

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

            DeactivateAllActionMenus();
        }

        // Open the right click menu or close it if it is already open
        if (Input.GetMouseButtonDown(1))
        {
            // No menu when mouse is over editor panel (left)
            if (editorPanel.rect.Contains(Input.mousePosition))
                return;

            // Deselect everything
            inConnectMode = false;
            SelectedDialogPartVisual = null;
            SelectedAnswerVisual = null;

            // Raycast to see what the user clicked at
            RaycastHit2D hit;
            if (hit = Physics2D.GetRayIntersection(mainCam.ScreenPointToRay(Input.mousePosition)))
            {
                if (hit.collider.CompareTag("DialogPart"))
                {
                    // Select dialog part
                    var diaPart = hit.collider.GetComponent<DialogPartVisual>();
                    SelectedDialogPartVisual = diaPart;
                    diaPart.Selected = true; // --> DialogPart
                    OpenDialogPartActionMenu();
                }
                else if (hit.collider.CompareTag("Answer"))
                {
                    // Select answer
                    var answer = hit.collider.GetComponent<AnswerVisual>();
                    SelectedAnswerVisual = answer;
                    answer.Selected = true;
                    OpenAnswerActionMenu();
                }
                else if (hit.collider.CompareTag("Connection"))
                {
                    clickedConnection = hit.collider.transform.parent.gameObject;
                    OpenConnectionActionMenu();
                }
                else
                    return;
            }
            else
                OpenCreateDPActionMenu();
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

        dialog = new DialogOld
        {
            id = dialogIDInputField.text,
            revealTextGradually = revealGraduallyToggle.isOn,

            dialogParts = new DialogOld.DialogPart[dialogPartVisuals.Count]
        };

        var dialogParts = dialogPartVisuals.ConvertAll(dpv => dpv.dialogPart);

        DialogOld.DialogPart first = dialogParts.Find(dp => dp.id.Equals("start"));

        if (first == null)
        {
            ErrorMessage.instance.ShowErrorMessage("Kein Dialog Part hat die ID 'start'!" +
                " Jeder Dialog braucht einen Anfang, der durch die ID 'start' gekennzeichnet " +
                "sein muss!");
            return false;
        }

        dialog.dialogParts[0] = first;

        for (int i = 0; i < dialogParts.Count; i++)
        {
            var dp = dialogParts[i];

            if (dialog.dialogParts[i] != null || dp.id.Equals("start"))
                continue;

            dialog.dialogParts[i] = dialogParts[i];
        }

        if (dialog.revealTextGradually)
            AddRichTextTagDelimiters();
        else
            DeleteRichTextTagDelimiters();

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

        if (string.IsNullOrWhiteSpace(dialogIDInputField.text))
        {
            ErrorMessage.instance.ShowErrorMessage("Der Dialog braucht noch eine ID! Achte" +
                " auch darauf, dass sie einzigartig ist und nicht schon in CoT vorkommt!");
            return false;
        }

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
        loadPathInputField.text = paths[0];
    }

    /// <summary>
    /// Called by the file browser once a path has been selected.
    /// Do not use otherwise!
    /// </summary>
    private void OnSaveSelectSuccess(string[] paths)
    {
        savePathInputField.text = paths[0];
    }

    /// <summary>
    /// Loads the given dialog. 
    /// Shows the dialog in the editor, assigns all necessary references
    /// and allows the user to edit the dialog.
    /// </summary>
    /// <param name="dia">The dialog to load.</param>
    public void LoadDialog(DialogOld dia)
    {
        ClearEverything();

        dialog = dia;
        dialogIDInputField.text = dia.id;
        revealGraduallyToggle.isOn = dia.revealTextGradually;
        HashSet<AnswerVisual> answers = new HashSet<AnswerVisual>();

        foreach (DialogOld.DialogPart diaPart in dia.dialogParts)
        {
            GameObject visualGO = Instantiate(dialogPartVisual,
                new Vector2(diaPart.nodeX, diaPart.nodeY), Quaternion.identity);
            DialogPartVisual visual = visualGO.GetComponent<DialogPartVisual>();
            dialogPartVisuals.Add(visual);

            visual.dialogPart = diaPart;
            visual.idText.SetText(diaPart.id);
            for (int i = 0; i < diaPart.answers.Length; i++)
            {
                visual.answers[i].SetActive(true);
                var answerV = visual.answers[i].GetComponent<AnswerVisual>();
                answerV.answer = diaPart.answers[i];
                answers.Add(answerV);
            }
        }

        foreach (AnswerVisual aVisual in answers)
        {
            if (!string.IsNullOrWhiteSpace(aVisual.answer.nextPartID))
            {
                aVisual.SetConnection(
                    Array.Find(dialogPartVisuals.ToArray(),
                    dpv => dpv.dialogPart.id.Equals(aVisual.answer.nextPartID)));
            }
        }

        foreach (DialogPartVisual dpVisual in dialogPartVisuals)
        {
            if (!string.IsNullOrWhiteSpace(dpVisual.dialogPart.nextPartID))
            {
                dpVisual.SetConnection(
                    Array.Find(dialogPartVisuals.ToArray(),
                    dpv => dpv.dialogPart.id.Equals(dpVisual.dialogPart.nextPartID)));
            }
        }
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
    /// Clears everything. Use with caution!
    /// </summary>
    public void ClearEverything()
    {
        areYouSureDialogLoad.SetActive(false);
        areYouSureDialogSave.SetActive(false);

        foreach (DialogPartVisual dpv in dialogPartVisuals)
            Destroy(dpv.gameObject);

        dialogPartVisuals = new List<DialogPartVisual>();

        inConnectMode = false;

        dialog = null;

        SelectedDialogPartVisual = null;
        SelectedAnswerVisual = null;
        clickedConnection = null;
    }

    #region Action Menus
    private void OpenCreateDPActionMenu()
    {
        if (actionMenuOpen)
        {
            DeactivateAllActionMenus();
            return;
        }

        actionMenuOpen = true;

        // Works great without conversion to world space! :)
        createRightClickMenu.transform.position = Input.mousePosition
                                                + new Vector3(110, -30);

        createRightClickMenu.gameObject.SetActive(!createRightClickMenu.activeSelf);
    }

    private void OpenDialogPartActionMenu()
    {
        if (actionMenuOpen)
        {
            DeactivateAllActionMenus();
            return;
        }

        actionMenuOpen = true;

        // Works great without conversion to world space! :)
        dialogPartRightClickMenu.transform.position = Input.mousePosition
                                                  + new Vector3(110, -30);

        dialogPartRightClickMenu.gameObject.SetActive(!dialogPartRightClickMenu.activeSelf);
    }


    private void OpenAnswerActionMenu()
    {
        if (actionMenuOpen)
        {
            DeactivateAllActionMenus();
            return;
        }

        actionMenuOpen = true;

        // Works great without conversion to world space! :)
        answerRightClickMenu.transform.position = Input.mousePosition
                                                  + new Vector3(110, -30);

        answerRightClickMenu.gameObject.SetActive(!answerRightClickMenu.activeSelf);
    }

    private void OpenConnectionActionMenu()
    {
        if (actionMenuOpen)
        {
            DeactivateAllActionMenus();
            return;
        }

        actionMenuOpen = true;

        // Works great without conversion to world space! :)
        connectionRightClickMenu.transform.position = Input.mousePosition
                                                  + new Vector3(110, -30);

        connectionRightClickMenu.gameObject.SetActive(!connectionRightClickMenu.activeSelf);
    }

    /// <summary>
    /// Deactivates all "right-click menues" there are
    /// </summary>
    public void DeactivateAllActionMenus()
    {
        createRightClickMenu.SetActive(false);
        dialogPartRightClickMenu.SetActive(false);
        answerRightClickMenu.SetActive(false);
        connectionRightClickMenu.SetActive(false);
        actionMenuOpen = false;
    }
    #endregion

    /// <summary>
    /// Creates a new Dialog Part visual at the mouse pos, adds it to the
    /// dialogPartVisuals list and deactivates the right click action menu
    /// </summary>
    public void CreateDialogPart()
    {
        Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        GameObject dpGO = Instantiate(dialogPartVisual, mousePos, Quaternion.identity);
        DialogPartVisual dpVisual = dpGO.GetComponent<DialogPartVisual>();

        DeactivateAllActionMenus();

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
        dpVisual.dialogPart = new DialogOld.DialogPart();

        // Do se magic
        dpVisual.dialogPart.id = SelectedDialogPartVisual.dialogPart.id;
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
        dpVisual.dialogPart.cutsceneToStartID = SelectedDialogPartVisual.dialogPart.cutsceneToStartID;

        dialogPartVisuals.Add(dpVisual);
    }

    /// <summary>
    /// Connects a dialog part (visual) to the currently selected answer
    /// </summary>
    public void ConnectToSelectedAnswer(DialogPartVisual dp)
    {
        SelectedAnswerVisual.SetConnection(dp);

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

        inConnectMode = false;
    }

    public void SwitchToConnectMode()
    {
        inConnectMode = true;
        DeactivateAllActionMenus();
    }

    /// <summary>
    /// Destroys the currently selected dialog part. Use with caution!
    /// </summary>
    public void DestroyDialogPart()
    {
        if (SelectedDialogPartVisual == null)
            return;

        dialogPartVisuals.Remove(SelectedDialogPartVisual);

        Instantiate(SelectedDialogPartVisual.particleSys,
            SelectedDialogPartVisual.transform.position, Quaternion.identity);

        Destroy(SelectedDialogPartVisual.gameObject);
        SelectedDialogPartVisual = null;
        DeactivateAllActionMenus();
    }

    public void DestroyConnection()
    {
        Destroy(clickedConnection.gameObject);
        DeactivateAllActionMenus();
    }

    public void AddAnswerToSelectedPart()
    {
        selectedDialogPartVisual.AddAnswer();
    }

    public void RemoveAnswerFromSelectedPart()
    {
        selectedDialogPartVisual.DeleteAnswer();
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
        foreach (DialogOld.DialogPart diaPart in dialog.dialogParts)
        {
            if (diaPart.text.Contains("<color") && !diaPart.text.Contains("|<color"))
                diaPart.text = diaPart.text.Replace("<color", "|<color");

            if (diaPart.text.Contains("</color>") && !diaPart.text.Contains("</color>|"))
                diaPart.text = diaPart.text.Replace("</color>", "</color>|");

            if (diaPart.textDE.Contains("<color") && !diaPart.textDE.Contains("|<color"))
                diaPart.textDE = diaPart.textDE.Replace("<color", "|<color");

            if (diaPart.textDE.Contains("</color>") && !diaPart.textDE.Contains("</color>|"))
                diaPart.textDE = diaPart.textDE.Replace("</color>", "</color>|");
        }
    }

    /// <summary>
    /// Goes through all dialog parts and texts and deletes the 
    /// "|" delimiter in front of and behind color tags
    /// </summary>
    private void DeleteRichTextTagDelimiters()
    {
        foreach (DialogOld.DialogPart diaPart in dialog.dialogParts)
        {
            if (diaPart.text.Contains("|<color"))
                diaPart.text = diaPart.text.Replace("|<color", "<color");

            if (diaPart.text.Contains("</color>|"))
                diaPart.text = diaPart.text.Replace("</color>|", "</color>");

            if (diaPart.textDE.Contains("|<color"))
                diaPart.textDE = diaPart.textDE.Replace("|<color", "<color");

            if (diaPart.textDE.Contains("</color>|"))
                diaPart.textDE = diaPart.textDE.Replace("</color>|", "</color>");
        }
    }
}
