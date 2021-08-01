using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class EditorManager : MonoBehaviour
{
    // Singleton
    public static EditorManager instance;

    public static Dictionary<string, UDSProperty> globalProperties;

    public static readonly char[] invalidCharacters =
        {'/', '\\', '<', '>', '|', '?', ':', '"', '*', '@'};

    // The Property Presets selected for new Dialogues in the StartAndSelectUI
    public static string globalDialoguePartPropertyPreset = null;
    public static string globalAnswerPropertyPreset = null;

    /// <summary>
    /// The Dialogue which is currently loaded and being edited.
    /// HideInInspector extremely important because Dialogue 
    /// has recursive references which break the Editor
    /// </summary>
    public Dialogue dialogue; // This dialogue is actually being edited
    public Dialogue dialogueBackup; // This dialogue is loaded once and stays the same

    [HideInInspector]
    public string pathToDialogue;

    // All Dialogue Part visuals on Screen (each of them stores an actual Dialogue.DialoguePart)
    [HideInInspector]
    public List<DialoguePartVisual> dialoguePartVisuals;

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
    public Canvas mainCanvas;
    public RectTransform editorPanel;
    public GameObject startAndSelectUI;
    public GameObject dialogueUI;
    public GameObject dialoguePartUI;
    public GameObject answerUI;

    [Header("Menu UI")]
    public GameObject globalPropertiesMenu;
    public ConditionMenu conditionMenu;
    public SavePresetMenu savePresetMenu;
    public LoadPresetMenu loadPresetMenu;
    public SettingsMenu settingsMenu;

    [Header("Prefabs")]
    public GameObject dialoguePartVisual;
    public GameObject answerVisual;
    public GameObject arrow;

    [Header("Support UI")]
    public RectTransform graphEditorBounds;
    [HideInInspector]
    public Camera mainCam;

    [Space(7)]
    public ColorTheme[] colorThemes;

    [Space(7)]
    // true => Editing Dialogue Part; false => Editing answer!
    public bool editingDialoguePart;

    public bool inConnectMode;

    public ColorTheme ActiveColorTheme
    {
        set => ChangeColorTheme(value);
        get => activeColorTheme;
    }
    private ColorTheme activeColorTheme;

    /// <summary>
    /// The currently selected Dialogue Part (visual)
    /// </summary>
    public DialoguePartVisual SelectedDialoguePartVisual
    {
        set
        {
            // Deselect the previously selected visual
            DeselectPreviouslySelectedVisual();

            // Set
            selectedDialoguePartVisual = value;

            if (value != null)
                value.Selected = true;

            editingDialoguePart = true;
            
            if (value == null && ActiveUI != startAndSelectUI)
                ActiveUI = dialogueUI;
            else if (value != null)
                ActiveUI = dialoguePartUI;
        }

        get { return selectedDialoguePartVisual; }
    }
    private DialoguePartVisual selectedDialoguePartVisual = null;

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

            editingDialoguePart = false;

            if (value == null && ActiveUI != startAndSelectUI)
                ActiveUI = dialogueUI;
            else if (value != null)
                ActiveUI = answerUI;
        }

        get { return selectedAnswerVisual; }
    }
    private AnswerVisual selectedAnswerVisual = null;

    public DialoguePartVisual StartDialoguePartVisual
    {
        set
        {
            if (value != null)
            {
                // Previous one not start anymore
                if (startDialoguePartVisual != null)
                    startDialoguePartVisual.IsStart = false;

                value.IsStart = true;

                startDialoguePartVisual = value;

                dialogue.startDialoguePartID = value.dialoguePart.id;
            } 
            else // In case the startDialoguePartVisual is destroyed
            {
                startDialoguePartVisual = null;

                dialogue.startDialoguePartID = null;
            }
        }

        get { return startDialoguePartVisual; }
    }
    private DialoguePartVisual startDialoguePartVisual;

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
        FileHandler.CreateTestDialogue();
#endif

        // Init
        Dictionary<string, UDSProperty> savedGlobalProperties 
            = FileHandler.LoadGlobalProperties();

        if (savedGlobalProperties == null)
            globalProperties = new Dictionary<string, UDSProperty>();
        else
            globalProperties = new Dictionary<string, UDSProperty>(savedGlobalProperties);

        mainCam = Camera.main;
        dialoguePartVisuals = new List<DialoguePartVisual>();
        ActiveUI = startAndSelectUI;

        if (PlayerPrefs.HasKey("ColorTheme"))
            ChangeColorTheme(colorThemes.Where(ct => ct.themeName.Equals(PlayerPrefs.GetString("ColorTheme"))).First());
    }

    private void Update()
    {
        // Left mouse button deselects and closes everything
        if (Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject() &&
                !Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition)))
            {
                SelectedDialoguePartVisual = null;
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
            SelectedDialoguePartVisual = null;
            SelectedAnswerVisual = null;
        }

        // Delete key deletes currenty selected Dialogue Part
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (SelectedDialoguePartVisual != null)
                DestroyDialoguePart();
        }

        // Ctrl + D duplicates currently selected Dialogue Part
        if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl))
            && Input.GetKeyDown(KeyCode.D))
        {
            CopySelectedDialoguePart();
        }

        // Show to the user that he is inConnectMode and where he is pointing
        if (inConnectMode)
        {
            if (connectionHologram == null)
            {
                connectionHologram = Instantiate(arrow).GetComponent<LineRenderer>();

                // Deactivate the arrow tip
                connectionHologram.transform.GetChild(0).gameObject.SetActive(false);
            }

            if (SelectedDialoguePartVisual != null)
            {
                connectionHologram.SetPosition(0, SelectedDialoguePartVisual.transform.position);
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

    public void ChangeColorTheme(ColorTheme newTheme)
    {
        activeColorTheme = newTheme;

        IColorThemed[] elements 
            = mainCanvas.transform.GetComponentsInChildren<IColorThemed>(true);

        Array.ForEach(elements, e => e.ChangeTheme(newTheme));

        Camera.main.backgroundColor = newTheme.cameraBackgroundColor;

        Connection[] connections 
            = Array.ConvertAll(GameObject.FindGameObjectsWithTag("Connection"),
                               go => go.GetComponent<Connection>());

        Array.ForEach(connections, c => c.UpdateColor()); 

    }

    /// <summary>
    /// Updates the currently open Dialogue (EditorManager.instance.dialogue)
    /// based on the Dialogue Parts, Answers, ... created in the editor and
    /// returns the Dialogue object so it can be saved, etc.
    /// Conducts internal validation and shows various Error Messages if
    /// something is wrong with the dialogue. (See EditorManager.ValidateDialogue())
    /// Note that EditorManager.instance.dialogueBackup exists and is not
    /// changed by this method
    /// </summary>
    /// <returns>An updated version of the currently open dialogue which 
    /// includes for example all Dialogue Parts and Answers visible on screen.
    /// Null if something went wrong (= if the Dialogue is not valid)</returns>
    public Dialogue ConstructDialogue()
    {
        if (!ValidateDialogue())
            return null;

        var dialogueParts = dialoguePartVisuals.ConvertAll(dpv => dpv.dialoguePart).ToArray();

        dialogue.dialogueParts = dialogueParts;

        return dialogue;
    }

    /// <summary>
    /// Checks, if the Dialogue is valid and "finished" and if all criteria are met.
    /// These are:
    /// - At least one dialogue part is there (1)
    /// - The Dialogue ID is not empty (2)
    /// - There is a start Dialogue Part (3)
    /// - All Dialogue Parts have an ID (4)
    /// - All Dialogue Part IDs are unique (5)
    /// - All Answers have an ID (6)
    /// - All Answer IDs are unique (7)
    /// - The Dialogue has an end (8)
    /// Displays an error message if at least one criterion is not met!
    /// </summary>
    /// <returns>Whether or not the dialogue is valid</returns>
    public bool ValidateDialogue()
    {
        // 1
        if (dialoguePartVisuals.Count == 0)
        {
            ErrorMessage.instance.ShowErrorMessage
                ("Failed. A Dialogue has to include at least one Dialogue Part");
            return false;
        }

        // 2
        if (string.IsNullOrWhiteSpace(dialogue.id))
        {
            ErrorMessage.instance.ShowErrorMessage("Failed. The Dialogue does not have an ID");
            return false;
        }

        // 3
        if (string.IsNullOrWhiteSpace(dialogue.startDialoguePartID))
        {
            ErrorMessage.instance.ShowErrorMessage("Failed. The Dialogue has to have a start Dialogue Part");
            return false;
        }

        // 8
        bool hasEnd = false;

        HashSet<string> diapartIDs = new HashSet<string>();
        foreach (var diapart in dialoguePartVisuals)
        {
            // 4
            if (string.IsNullOrWhiteSpace(diapart.dialoguePart.id))
            {
                ErrorMessage.instance.ShowErrorMessage("Failed. There is a Dialogue Part without an ID");
                return false;
            }

            // 5
            if (diapartIDs.Contains(diapart.dialoguePart.id))
            {
                ErrorMessage.instance.ShowErrorMessage(
                    string.Format(
                        "Failed. All IDs have to be unique. Dialogue Part ID {0} appears twice",
                        diapart.dialoguePart.id));

                return false;
            }
            diapartIDs.Add(diapart.dialoguePart.id);

            if (diapart.answers.Count == 0 && 
                string.IsNullOrWhiteSpace(diapart.dialoguePart.nextDialoguePartID))
                hasEnd = true;

            HashSet<string> answerIDs = new HashSet<string>();
            foreach (AnswerVisual answer in diapart.answers)
            {
                // 6
                if (string.IsNullOrWhiteSpace(answer.answer.id))
                {
                    ErrorMessage.instance.ShowErrorMessage(
                        "Failed. There is an Answer without an ID in Dialogue Part " 
                        + diapart.dialoguePart.id);

                    return false;
                }

                // 7
                if (answerIDs.Contains(answer.answer.id))
                {
                    ErrorMessage.instance.ShowErrorMessage(
                        string.Format(
                            "Failed. All IDs have to be unique. Answer ID {0} appears twice within Dialogue Part " 
                            + diapart.dialoguePart.id,
                            answer.answer.id));

                    return false;
                }

                if (string.IsNullOrWhiteSpace(answer.answer.nextDialoguePartID))
                    hasEnd = true;

                answerIDs.Add(answer.answer.id);

                diapartIDs.Add(diapart.dialoguePart.id);
            }
        }

        // 8
        if (!hasEnd)
        {
            ErrorMessage.instance.ShowErrorMessage("Failed. The Dialogues doesn't have an end");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Loads the given dialogue. 
    /// Shows the dialogue in the editor, assigns all necessary references
    /// and allows the user to edit the dialogue.
    /// </summary>
    /// <param name="dia">The dialogue to load</param>
    /// <param name="path">The path to the .udsdialogue.json file where the dialogue is stored</param>
    public void LoadDialogue(Dialogue dialogue, string path)
    {
        ClearEverything();

        this.dialogue = dialogue;
        this.pathToDialogue = path;
        this.dialogueBackup = (Dialogue) dialogue.Clone(); // Backup for potential fallback/discard

        List<AnswerVisual> allAnswers = new List<AnswerVisual>();

        // Go over all Dialogue Parts in the dialogue and...
        foreach (Dialogue.DialoguePart diaPart in dialogue.dialogueParts)
        {
            // ... instantiate a corresponding visual in the editor
            GameObject visualGO = Instantiate(dialoguePartVisual,
                new Vector2(diaPart.visualX, diaPart.visualY), Quaternion.identity);

            DialoguePartVisual visual = visualGO.GetComponent<DialoguePartVisual>();
            dialoguePartVisuals.Add(visual);

            visual.dialoguePart = diaPart;

            if (diaPart.id.Equals(dialogue.startDialoguePartID))
                StartDialoguePartVisual = visual;

            List<AnswerVisual> answers = new List<AnswerVisual>();

            // Add all the answers (for each Dialogue Part)
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
                answerVis.parentDialoguePart = visual;
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
            if (!string.IsNullOrWhiteSpace(aVisual.answer.nextDialoguePartID))
            {
                aVisual.SetConnection(
                    Array.Find(dialoguePartVisuals.ToArray(),
                    dpv => dpv.dialoguePart.id.Equals(aVisual.answer.nextDialoguePartID)));

                noOfConnections++; // Count how many connections there are in total
            }
        }

        // Add connections to all Dialogue Parts (that have connections)
        foreach (DialoguePartVisual dpVisual in dialoguePartVisuals)
        {
            if (!string.IsNullOrWhiteSpace(dpVisual.dialoguePart.nextDialoguePartID))
            {
                dpVisual.SetConnection(
                    Array.Find(dialoguePartVisuals.ToArray(),
                    dpv => dpv.dialoguePart.id.Equals(dpVisual.dialoguePart.nextDialoguePartID)));

                noOfConnections++; // Count how many connections there are in total
            }
        }

        // Go from StartAndSelectUI to DialogueUI
        ActiveUI = dialogueUI;
    }

    /// <summary>
    /// Variant of ValidateDialogue that generates DialogueUI.Warnings for the DialogueUI
    /// Criteria:
    /// - At least one dialogue part is there (1) - red
    /// - The Dialogue ID is not empty (2) - red
    /// - There is a start Dialogue Part (3) - red
    /// - All Dialogue Parts have an ID (4) - red
    /// - All Dialogue Part IDs are unique (5) - red
    /// - All Answers have an ID (6) - red
    /// - All Answer IDs are unique (7) - red
    /// - The Dialogue has an end (8) - red
    /// - There is no empty Text Property on an Answer (9) - yellow
    /// - All Dialogue Parts are reachable (10) - yellow
    /// - There is no empty Text Property on a Dialogue Part (11) - yellow
    /// </summary>
    /// <returns>A list with warnings for the warning field in the DialogueUI</returns>
    public List<DialogueUI.Warning> GenerateWarnings()
    {
        List<DialogueUI.Warning> warnings = new List<DialogueUI.Warning>();

        bool[] warningFlags = new bool[11];

        // 1
        if (dialoguePartVisuals.Count == 0)
        {
            warnings.Add(new DialogueUI.Warning
            {
                text = "A Dialogue has to contain at least one Dialogue Part",
                color = Color.red
            });

            warningFlags[0] = true;
        }

        // 2
        if (string.IsNullOrWhiteSpace(dialogue.id))
        {
            warnings.Add(new DialogueUI.Warning
            {
                text = "The Dialogue requires a name",
                color = Color.red
            });

            warningFlags[1] = true;
        }

        // 3
        if (string.IsNullOrEmpty(dialogue.startDialoguePartID))
        {
            warnings.Add(new DialogueUI.Warning
            {
                text = "The Dialogue requires a start Dialogue Part",
                color = Color.red
            });

            warningFlags[2] = true;
        }

        // 8
        bool hasEnd = false;

        HashSet<string> diapartIDs = new HashSet<string>();
        foreach (var diapart in dialoguePartVisuals)
        {
            // 4
            if (!warningFlags[3])
            {
                if (string.IsNullOrWhiteSpace(diapart.dialoguePart.id))
                {
                    warnings.Add(new DialogueUI.Warning
                    {
                        text = "There is a DialoguePart without an ID",
                        color = Color.red
                    });

                    warningFlags[3] = true;
                }
            }

            // 5
            if (!warningFlags[4])
            {
                if (diapartIDs.Contains(diapart.dialoguePart.id))
                {
                    warnings.Add(new DialogueUI.Warning
                    {
                        text = string.Format(
                            "All IDs have to be unique. Dialogue Part ID {0} appears twice",
                            diapart.dialoguePart.id),
                        color = Color.red
                    });

                    warningFlags[4] = true;
                }
            }

            if (diapart.answers.Count == 0 && 
                string.IsNullOrWhiteSpace(diapart.dialoguePart.nextDialoguePartID))
                hasEnd = true;

            diapartIDs.Add(diapart.dialoguePart.id);

            HashSet<string> answerIDs = new HashSet<string>();
            foreach (AnswerVisual answer in diapart.answers)
            {
                if (!warningFlags[5])
                {
                    // 6
                    if (string.IsNullOrWhiteSpace(answer.answer.id))
                    {
                        warnings.Add(new DialogueUI.Warning
                        {
                            text = "There is an " +
                            "answer without an ID in Dialogue Part " + diapart.dialoguePart.id,
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
                        warnings.Add(new DialogueUI.Warning
                        {
                            text = string.Format(
                            "All IDs have to be unique. Answer ID {0} appears twice within " 
                            + diapart.dialoguePart.id,
                            answer.answer.id),
                            color = Color.red
                        });

                        warningFlags[6] = true;
                    }
                }

                // 9
                if (!warningFlags[8])
                {
                    if (string.IsNullOrWhiteSpace(answer.answer.GetProperty<string>("Text")))
                    {
                        warnings.Add(new DialogueUI.Warning
                        {
                            text = string.Format(
                                    "The Text on Answer {0} is empty",
                                    answer.answer.id),
                            color = Color.yellow
                        });

                        warningFlags[8] = true;
                    }
                }

                if (string.IsNullOrWhiteSpace(answer.answer.nextDialoguePartID))
                    hasEnd = true;

                answerIDs.Add(answer.answer.id);
            }

            // 10 
            if (!warningFlags[9])
            {
                if (!diapart.IsStart)
                {
                    bool connected = false;
                    foreach (var otherDiapart in dialoguePartVisuals)
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
                        warnings.Add(new DialogueUI.Warning
                        {
                            text = string.Format(
                                "Dialogue Part {0} is unreachable",
                                diapart.dialoguePart.id),
                            color = Color.yellow
                        });

                        warningFlags[9] = true;
                    }
                }
            }

            // 11
            if (!warningFlags[9])
            {
                if (string.IsNullOrWhiteSpace(diapart.dialoguePart.GetProperty<string>("Text")))
                {
                    warnings.Add(new DialogueUI.Warning
                    {
                        text = string.Format(
                                "The Text on Dialogue Part {0} is empty",
                                diapart.dialoguePart.id),
                        color = Color.yellow
                    });

                    warningFlags[10] = true;
                }
            }
        }

        if (!hasEnd)
        {
            warningFlags[7] = true;

            warnings.Add(new DialogueUI.Warning
            {
                text = string.Format("The Dialogue doesn't have an end"),
                color = Color.red
            });
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

        foreach (DialoguePartVisual dpv in dialoguePartVisuals)
            Destroy(dpv.gameObject);

        dialoguePartVisuals = new List<DialoguePartVisual>();

        inConnectMode = false;

        dialogue = null;
        pathToDialogue = null;

        // Important that this happens before set is called on the properties (below)
        ActiveUI = startAndSelectUI;

        SelectedDialoguePartVisual = null;
        SelectedAnswerVisual = null;
        selectedConnection = null;

        noOfAnswers = 0;
        noOfConnections = 0;
    }

    /// <summary>
    /// Creates a new Dialogue Part visual at the mouse pos and 
    /// adds it to the dialoguePartVisuals list
    /// </summary>
    public void CreateDialoguePart()
    {
        Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        GameObject dpGO = Instantiate(dialoguePartVisual, mousePos, Quaternion.identity);
        DialoguePartVisual dpVisual = dpGO.GetComponent<DialoguePartVisual>();

        dialoguePartVisuals.Add(dpVisual);

        dpVisual.dialoguePart = new Dialogue.DialoguePart("", dpVisual.transform.position);

        // If it's the first part in the Dialogue
        if (dialoguePartVisuals.Count == 1)
            StartDialoguePartVisual = dpVisual;

        // If a Property Preset for new Dialogue Parts is selected
        if (globalDialoguePartPropertyPreset != null)
        {
            PropertyPreset? preset = 
                FileHandler.LoadPropertyPreset
                (globalDialoguePartPropertyPreset, PropertyPreset.PropertyPresetType.DIALOG_PART);

            if (!preset.HasValue)
                return; // Error message handled by FileHandler

            Dictionary<string, UDSProperty> properties = preset.Value.properties;

            // Add all properties from the preset
            foreach (string p in properties.Keys)
            {
                dpVisual.dialoguePart.SetProperty
                    (p, properties[p].value, properties[p].type, properties[p].required);
            }
        }
    }

    /// <summary>
    /// Copies the currently selected dialogue part and creates a
    /// new visual below it.
    /// </summary>
    public void CopySelectedDialoguePart()
    {
        Vector3 posOffset = new Vector2(0, -0.5f);
        Vector2 pos = SelectedDialoguePartVisual.transform.position + posOffset;

        GameObject dpGO = Instantiate(dialoguePartVisual, pos, Quaternion.identity);

        DialoguePartVisual dpVisual = dpGO.GetComponent<DialoguePartVisual>();

        dialoguePartVisuals.Add(dpVisual);
    }

    /// <summary>
    /// Connects a dialogue part (visual) to the currently selected answer
    /// </summary>
    public void ConnectToSelectedAnswer(DialoguePartVisual dp)
    {
        SelectedAnswerVisual.SetConnection(dp);

        noOfConnections++;

        inConnectMode = false;
    }

    /// <summary>
    /// Connects a Dialogue Part (visual) directly to the currently selected Dialogue Part (visual)
    /// (if it has no no non-conditional Answers)
    /// </summary>
    public void ConnectToSelectedDP(DialoguePartVisual dp)
    {
        if (SelectedDialoguePartVisual.dialoguePart.answers.Length > 0 &&
            !Array.TrueForAll(SelectedDialoguePartVisual.dialoguePart.answers, a => a.conditional))
        {
            ErrorMessage.instance.ShowErrorMessage("Only Dialogue Parts without an (non-conditional) Answer + " +
                "can be connected directly to other Dialogue Parts");
            inConnectMode = false;
            return;
        }

        SelectedDialoguePartVisual.SetConnection(dp);

        noOfConnections++;

        inConnectMode = false;
    }

    /// <summary>
    /// Destroys the currently selected dialogue part. Use with caution!
    /// </summary>
    public void DestroyDialoguePart()
    {
        if (SelectedDialoguePartVisual == null)
            return;

        // Important
        if (SelectedDialoguePartVisual.IsStart)
            StartDialoguePartVisual = null;

        dialoguePartVisuals.Remove(SelectedDialoguePartVisual);

        Destroy(SelectedDialoguePartVisual.gameObject);
        SelectedDialoguePartVisual = null;
    }

    private void DeselectPreviouslySelectedVisual()
    {
        if (selectedDialoguePartVisual != null)
        {
            selectedDialoguePartVisual.Selected = false;
            selectedDialoguePartVisual = null;
        }

        if (selectedAnswerVisual != null)
        {
            selectedAnswerVisual.Selected = false;
            selectedAnswerVisual = null;
        }
    }
}