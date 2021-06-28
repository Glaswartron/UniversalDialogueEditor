using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EditorManager : MonoBehaviour
{
    // Singleton
    public static EditorManager instance;

    public static Dictionary<string, UDSProperty> globalProperties;

    public static readonly char[] invalidCharacters =
        {'/', '\\', '<', '>', '|', '?', ':', '"', '*', '@'};

    // The Property Presets selected for new Dialogs in the StartAndSelectUI
    public static string globalDialogPartPropertyPreset = null;
    public static string globalAnswerPropertyPreset = null;

    /// <summary>
    /// The Dialog which is currently loaded and being edited.
    /// HideInInspector extremely important because Dialog 
    /// has recursive references which break the Editor
    /// </summary>
    public Dialog dialog; // This dialog is actually being edited
    public Dialog dialogBackup; // This dialog is loaded once and stays the same

    [HideInInspector]
    public string pathToDialog;

    // All Dialog Part visuals on Screen (each of them stores an actual Dialog.DialogPart)
    [HideInInspector]
    public List<DialogPartVisual> dialogPartVisuals;

    public GameObject ActiveUI
    {
        set
        {
            if (activeUI != null)
                activeUI.SetActive(false);

            activeUI = value;
            activeUI.SetActive(true);
        }

        get { return activeUI;  }
    }
    private GameObject activeUI;

    public GameObject ActiveMenu
    {
        set
        {
            if (activeMenu != null)
                activeMenu.SetActive(false);

            if (value != null)
                value.SetActive(true);

            activeMenu = value;
        }

        get { return activeMenu; }
    }
    private GameObject activeMenu;

    [Header("Main UI")]
    public RectTransform editorPanel;
    public GameObject startAndSelectUI;
    public GameObject dialogUI;
    public GameObject dialogPartUI;
    public GameObject answerUI;

    [Header("Menu UI")]
    public GameObject globalPropertiesMenu;
    public ConditionMenu conditionMenu;
    public SavePresetMenu savePresetMenu;
    public LoadPresetMenu loadPresetMenu;

    [Header("Prefabs")]
    public GameObject dialogPartVisual;
    public GameObject answerVisual;
    public GameObject arrow;

    [Header("Support UI")]
    public RectTransform graphEditorBounds;
    [HideInInspector]
    public Camera mainCam;

    [Space(7)]
    public Vector2 menuOffsetFromMouse;

    [Space(7)]
    // true => Editing Dialog Part; false => Editing answer!
    public bool editingDialogPart;

    public bool inConnectMode;

    //public DialogInfoInputField[] inputFields;

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

            if (value != null)
                value.Selected = true;

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

            if (value != null)
                value.Selected = true;

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
            if (value != null)
            {
                // Previous one not start anymore
                if (startDialogPartVisual != null)
                    startDialogPartVisual.IsStart = false;

                value.IsStart = true;

                startDialogPartVisual = value;

                dialog.startDialogPartID = value.dialogPart.id;
            } 
            else // In case the startDialogPartVisual is destroyed
            {
                startDialogPartVisual = null;

                dialog.startDialogPartID = null;
            }
        }

        get { return startDialogPartVisual; }
    }
    private DialogPartVisual startDialogPartVisual;

    public GameObject selectedConnection;

    public int noOfAnswers;
    public int noOfConnections;

    private LineRenderer connectionHologram;

    // Start is called before the first frame update
    void Start()
    {
        Screen.fullScreen = false;

        // Singleton
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);

#if UNITY_EDITOR
        FileHandler.CreateTestDialog();
#endif

        // Init
        Dictionary<string, UDSProperty> savedGlobalProperties 
            = FileHandler.LoadGlobalProperties();

        if (savedGlobalProperties == null)
            globalProperties = new Dictionary<string, UDSProperty>();
        else
            globalProperties = new Dictionary<string, UDSProperty>(savedGlobalProperties);

        mainCam = Camera.main;
        dialogPartVisuals = new List<DialogPartVisual>();
        ActiveUI = startAndSelectUI;
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

        // Show to the user that he is inConnectMode and where he is pointing
        if (inConnectMode)
        {
            if (connectionHologram == null)
            {
                connectionHologram = Instantiate(arrow).GetComponent<LineRenderer>();

                // Deactivate the arrow tip
                connectionHologram.transform.GetChild(1).gameObject.SetActive(false);
            }

            if (SelectedDialogPartVisual != null)
            {
                connectionHologram.SetPosition(0, SelectedDialogPartVisual.transform.position);
                connectionHologram.SetPosition(1, mainCam.ScreenToWorldPoint(Input.mousePosition));
            } 
            else if (SelectedAnswerVisual != null)
            {
                connectionHologram.SetPosition(0, SelectedAnswerVisual.transform.position);
                connectionHologram.SetPosition(1, mainCam.ScreenToWorldPoint(Input.mousePosition));
            }
        }
        else if (connectionHologram != null)
        {
            // Happens after the user connected two visuals
            Destroy(connectionHologram.gameObject);
        }
    }

    /// <summary>
    /// Updates the currently open Dialog (EditorManager.instance.dialog)
    /// based on the Dialog Parts, Answers, ... created in the editor and
    /// returns the Dialog object so it can be saved, etc.
    /// Conducts internal validation and shows various Error Messages if
    /// something is wrong with the dialog. (See EditorManager.ValidateDialog())
    /// Note that EditorManager.instance.dialogBackup exists and is not
    /// changed by this method
    /// </summary>
    /// <returns>An updated version of the currently open dialog which 
    /// includes for example all Dialog Parts and Answers visible on screen.
    /// Null if something went wrong (= if the Dialog is not valid)</returns>
    public Dialog ConstructDialog()
    {
        if (!ValidateDialog())
            return null;

        var dialogParts = dialogPartVisuals.ConvertAll(dpv => dpv.dialogPart).ToArray();

        dialog.dialogParts = dialogParts;

        return dialog;
    }

    /// <summary>
    /// Checks, if the Dialog is valid and "finished" and if all criteria are met.
    /// These are:
    /// - At least one dialog part is there (1)
    /// - The Dialog ID is not empty (2)
    /// - There is a start Dialog Part (3)
    /// - All Dialog Parts have an ID (4)
    /// - All Dialog Part IDs are unique (5)
    /// - All Answers have an ID (6)
    /// - All Answer IDs are unique (7)
    /// Displays an error message if at least one criterion is not met!
    /// </summary>
    /// <returns>Whether or not the dialog is valid</returns>
    public bool ValidateDialog()
    {
        // 1
        if (dialogPartVisuals.Count == 0)
        {
            ErrorMessage.instance.ShowErrorMessage
                ("Failed. A dialog has to include at least one Dialog Part");
            return false;
        }

        // 2
        if (string.IsNullOrWhiteSpace(dialog.id))
        {
            ErrorMessage.instance.ShowErrorMessage("Failed. The Dialog does not have an ID");
            return false;
        }

        // 3
        if (string.IsNullOrWhiteSpace(dialog.startDialogPartID))
        {
            ErrorMessage.instance.ShowErrorMessage("Failed. The Dialog has to have a start Dialog Part");
            return false;
        }

        HashSet<string> diapartIDs = new HashSet<string>();
        foreach (var diapart in dialogPartVisuals)
        {
            // 4
            if (string.IsNullOrWhiteSpace(diapart.dialogPart.id))
            {
                ErrorMessage.instance.ShowErrorMessage("Failed. There is a Dialog Part without an ID");
                return false;
            }

            // 5
            if (diapartIDs.Contains(diapart.dialogPart.id))
            {
                ErrorMessage.instance.ShowErrorMessage(
                    string.Format(
                        "Failed. All IDs have to be unique. Dialog Part ID {0} appears twice",
                        diapart.dialogPart.id));

                return false;
            }
            diapartIDs.Add(diapart.dialogPart.id);

            HashSet<string> answerIDs = new HashSet<string>();
            foreach (AnswerVisual answer in diapart.answers)
            {
                // 6
                if (string.IsNullOrWhiteSpace(answer.answer.id))
                {
                    ErrorMessage.instance.ShowErrorMessage(
                        "Failed. There is an Answer without an ID in Dialog Part " 
                        + diapart.dialogPart.id);

                    return false;
                }

                // 7
                if (answerIDs.Contains(answer.answer.id))
                {
                    ErrorMessage.instance.ShowErrorMessage(
                        string.Format(
                            "Failed. All IDs have to be unique. Answer ID {0} appears twice within Dialog Part " 
                            + diapart.dialogPart.id,
                            answer.answer.id));

                    return false;
                }

                answerIDs.Add(answer.answer.id);

                diapartIDs.Add(diapart.dialogPart.id);
            }
        }

        return true;
    }

    /// <summary>
    /// Loads the given dialog. 
    /// Shows the dialog in the editor, assigns all necessary references
    /// and allows the user to edit the dialog.
    /// </summary>
    /// <param name="dia">The dialog to load</param>
    /// <param name="path">The path to the .udsdialog.json file where the dialog is stored</param>
    public void LoadDialog(Dialog dialog, string path)
    {
        ClearEverything();

        this.dialog = dialog;
        this.pathToDialog = path;
        this.dialogBackup = (Dialog) dialog.Clone(); // Backup for potential fallback/discard

        List<AnswerVisual> allAnswers = new List<AnswerVisual>();

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

            List<AnswerVisual> answers = new List<AnswerVisual>();

            // Add all the answers (for each Dialog Part)
            for (int i = 0; i < diaPart.answers.Length; i++)
            {
                // Position Mathzzz
                float angle = diaPart.answers[i].angle;

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

                answerVis.Conditional = diaPart.answers[i].conditional;
                if (answerVis.Conditional)
                    answerVis.Condition = diaPart.answers[i].condition.Value;
                
                answerVis.index = i;

                answers.Add(answerVis);

                noOfAnswers++; // Count how many answers there are in total
            }

            allAnswers.AddRange(answers);

            visual.answers = answers; // !
        }

        // Add connections to all answers (that have connections)
        foreach (AnswerVisual aVisual in allAnswers)
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
    /// Variant of ValidateDialog that generates DialogUI.Warnings for the DialogUI
    /// Criteria:
    /// - At least one dialog part is there (1) - red
    /// - The Dialog ID is not empty (2) - red
    /// - There is a start Dialog Part (3) - red
    /// - All Dialog Parts have an ID (4) - red
    /// - All Dialog Part IDs are unique (5) - red
    /// - All Answers have an ID (6) - red
    /// - All Answer IDs are unique (7) - red
    /// - There is no empty Text Property on an Answer (8) - yellow
    /// - All Dialog Parts are reachable (9) - yellow
    /// - There is no empty Text Property on a Dialog Part (10) - yellow
    /// </summary>
    /// <returns>A list with warnings for the warning field in the DialogUI</returns>
    public List<DialogUI.Warning> GenerateWarnings()
    {
        List<DialogUI.Warning> warnings = new List<DialogUI.Warning>();

        bool[] warningFlags = new bool[10];

        // 1
        if (dialogPartVisuals.Count == 0)
        {
            warnings.Add(new DialogUI.Warning
            {
                text = "A dialog has to contain at least one Dialog Part",
                color = Color.red
            });

            warningFlags[0] = true;
        }

        // 2
        if (string.IsNullOrWhiteSpace(dialog.id))
        {
            warnings.Add(new DialogUI.Warning
            {
                text = "The Dialog requires a name",
                color = Color.red
            });

            warningFlags[1] = true;
        }

        // 3
        if (string.IsNullOrEmpty(dialog.startDialogPartID))
        {
            warnings.Add(new DialogUI.Warning
            {
                text = "The Dialog requires a start Dialog Part",
                color = Color.red
            });

            warningFlags[2] = true;
        }

        HashSet<string> diapartIDs = new HashSet<string>();
        foreach (var diapart in dialogPartVisuals)
        {
            // 4
            if (!warningFlags[3])
            {
                if (string.IsNullOrWhiteSpace(diapart.dialogPart.id))
                {
                    warnings.Add(new DialogUI.Warning
                    {
                        text = "There is a DialogPart without an ID",
                        color = Color.red
                    });

                    warningFlags[3] = true;
                }
            }

            // 5
            if (!warningFlags[4])
            {
                if (diapartIDs.Contains(diapart.dialogPart.id))
                {
                    warnings.Add(new DialogUI.Warning
                    {
                        text = string.Format(
                            "All IDs have to be unique. Dialog Part ID {0} appears twice",
                            diapart.dialogPart.id),
                        color = Color.red
                    });

                    warningFlags[4] = true;
                }
            }

            diapartIDs.Add(diapart.dialogPart.id);

            HashSet<string> answerIDs = new HashSet<string>();
            foreach (AnswerVisual answer in diapart.answers)
            {
                if (!warningFlags[5])
                {
                    // 6
                    if (string.IsNullOrWhiteSpace(answer.answer.id))
                    {
                        warnings.Add(new DialogUI.Warning
                        {
                            text = "There is an " +
                            "answer without an ID in Dialog Part " + diapart.dialogPart.id,
                            color = Color.red
                        });

                        warningFlags[5] = true;
                    }
                }

                // 7
                if (!warningFlags[6])
                {
                    if (answerIDs.Contains(answer.answer.id))
                    {
                        warnings.Add(new DialogUI.Warning
                        {
                            text = string.Format(
                            "All IDs have to be unique. Answer ID {0} appears twice within " 
                            + diapart.dialogPart.id,
                            answer.answer.id),
                            color = Color.red
                        });

                        warningFlags[6] = true;
                    }
                }

                // 8
                if (!warningFlags[7])
                {
                    if (string.IsNullOrWhiteSpace(answer.answer.GetProperty<string>("Text")))
                    {
                        warnings.Add(new DialogUI.Warning
                        {
                            text = string.Format(
                                    "The Text on Answer {0} is empty",
                                    answer.answer.id),
                            color = Color.yellow
                        });

                        warningFlags[7] = true;
                    }
                }

                answerIDs.Add(answer.answer.id);
            }

            // 9 
            if (!warningFlags[8])
            {
                if (!diapart.IsStart)
                {
                    bool connected = false;
                    foreach (var otherDiapart in dialogPartVisuals)
                    {
                        if (otherDiapart == diapart)
                            continue;

                        if (otherDiapart.ConnectedDP == diapart)
                        {
                            connected = true;
                            break;
                        }

                        foreach (var answer in otherDiapart.answers)
                        {
                            if (answer.ConnectedDP == diapart)
                            {
                                connected = true;
                                break;
                            }
                        }

                        if (connected)
                            break;
                    }

                    if (!connected)
                    {
                        warnings.Add(new DialogUI.Warning
                        {
                            text = string.Format(
                                "Dialog Part {0} is unreachable",
                                diapart.dialogPart.id),
                            color = Color.yellow
                        });

                        warningFlags[8] = true;
                    }
                }
            }

            // 10
            if (!warningFlags[9])
            {
                if (string.IsNullOrWhiteSpace(diapart.dialogPart.GetProperty<string>("Text")))
                {
                    warnings.Add(new DialogUI.Warning
                    {
                        text = string.Format(
                                "The Text on Dialog Part {0} is empty",
                                diapart.dialogPart.id),
                        color = Color.yellow
                    });

                    warningFlags[9] = true;
                }
            }
        }

        return warnings;
    }

    /// <summary>
    /// Clears everything and returns to StartAndSelectUI.
    /// Discards any unsaved data.
    /// Use with caution!
    /// </summary>
    public void ClearEverything()
    {
        ContextMenuManager.instance.DeactivateContextMenu();

        foreach (DialogPartVisual dpv in dialogPartVisuals)
            Destroy(dpv.gameObject);

        dialogPartVisuals = new List<DialogPartVisual>();

        inConnectMode = false;

        dialog = null;
        pathToDialog = null;

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

        dpVisual.dialogPart = new Dialog.DialogPart("", dpVisual.transform.position);

        // If it's the first part in the Dialog
        if (dialogPartVisuals.Count == 1)
            dpVisual.IsStart = true;

        // If a Property Preset for new Dialog Parts is selected
        if (globalDialogPartPropertyPreset != null)
        {
            PropertyPreset? preset = 
                FileHandler.LoadPropertyPreset
                (globalDialogPartPropertyPreset, PropertyPreset.PropertyPresetType.DIALOG_PART);

            if (!preset.HasValue)
                return; // Error message handled by FileHandler

            Dictionary<string, UDSProperty> properties = preset.Value.properties;

            // Add all properties from the preset
            foreach (string p in properties.Keys)
            {
                dpVisual.dialogPart.SetProperty
                    (p, properties[p].value, properties[p].type, properties[p].required);
            }
        }
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
            ErrorMessage.instance.ShowErrorMessage("Only Dialog Parts without an (non-conditional) Answer + " +
                "can be connected directly to other Dialog Parts");
            inConnectMode = false;
            return;
        }

        SelectedDialogPartVisual.SetConnection(dp);

        noOfConnections++;

        inConnectMode = false;
    }

    /// <summary>
    /// Destroys the currently selected dialog part. Use with caution!
    /// </summary>
    public void DestroyDialogPart()
    {
        if (SelectedDialogPartVisual == null)
            return;

        // Important
        if (SelectedDialogPartVisual.IsStart)
            StartDialogPartVisual = null;

        dialogPartVisuals.Remove(SelectedDialogPartVisual);

        Destroy(SelectedDialogPartVisual.gameObject);
        SelectedDialogPartVisual = null;
    }

    public void DestroyConnection()
    {
        Destroy(selectedConnection.gameObject);
        noOfConnections--;
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
}