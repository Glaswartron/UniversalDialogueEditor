using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialoguePartVisual : MonoBehaviour, IContextMenu
{
    /// <summary>
    /// The Dialogue Part this visual encapsulates. Very important!
    /// HideInInspector extremely important because Dialogue.DialoguePart 
    /// has recursive references which break the Editor
    /// </summary>
    [HideInInspector] 
    public Dialogue.DialoguePart dialoguePart;

    [HideInInspector]
    public List<AnswerVisual> answers;

    [Header("Prefabs and UI")]
    public TextMeshPro idText;

    [Header("Colors")]
    public Color normalColor;
    public Color selectedColor;
    public Color startColor;
    public Color startSelectedColor;

    // Unused
    public int Size
    {
        set
        {
            size = value;
            transform.localScale = new Vector3(value, value, transform.localScale.z);
        }

        get { return size;  }
    }
    private int size = 1;

    private SpriteRenderer spriteRenderer;
    private Camera mainCam;

    public DialoguePartVisual ConnectedDP
    {
        set
        {
            connectedDP = value;

            if (value != null)
                dialoguePart.nextDialoguePartID = value.dialoguePart.id;
            else
                dialoguePart.nextDialoguePartID = string.Empty;
        }

        get { return connectedDP; }
    }
    private DialoguePartVisual connectedDP;
    
    public Connection dpConnection;
    private LineRenderer connectionLineRenderer;

    /// <summary>
    /// Whether or not this visual is currently selected (by the user)
    /// </summary>
    public bool Selected
    {
        set
        {
            selected = value;

            // Changes color if selected
            spriteRenderer.color = GetColor();
        }

        get { return selected; }
    }
    private bool selected;

    public bool IsStart
    {
        set
        {
            isStart = value;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            spriteRenderer.color = GetColor();
        }

        get { return isStart;  }
    }
    private bool isStart;

    // Start is called before the first frame update
    void Start()
    {
        // Init
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = GetColor();
        mainCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        // Constantly updates the idText
        idText.SetText(dialoguePart.id);

        if (ConnectedDP == null && dpConnection != null)
            Destroy(dpConnection.gameObject);
        // Constantly update connection
        else if (dpConnection != null)
        { 
            connectionLineRenderer.SetPositions(new Vector3[] {(Vector2)transform.position,
                                                 (Vector2)connectedDP.transform.position});
            dpConnection.collSet = false; // Update collider
        }

        if (ConnectedDP != null)
            dialoguePart.nextDialoguePartID = connectedDP.dialoguePart.id;
    }

    public void OnMouseDrag()
    {
        // The user can drag the visual if it is selected (not if he has just deselected it)
        if (Selected)
        {
            var mousePos = (Vector2)mainCam.ScreenToWorldPoint(Input.mousePosition);
            float sqrDisVisualToMouse = Vector2.SqrMagnitude((Vector2)transform.position - mousePos);
            /* Makes sure that the visual doesn't jump/move right when clicked (1),
             * that it is impossible to drag it out of the screen (2) and to drag
             * it under the UI. (3) */
            if (sqrDisVisualToMouse > Mathf.Pow(0.3f, 2) // 1
                && mainCam.pixelRect.Contains(Input.mousePosition) // 2
                && Utility.GetWorldRect(EditorManager.instance
                   .graphEditorBounds).Contains(Input.mousePosition)) // 3
            {
                transform.position = mousePos;
                dialoguePart.visualX = (int) mousePos.x; 
                dialoguePart.visualY = (int) mousePos.y;
            }
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // Pressing on the visual selects/deselects it
        Selected = !Selected;

        if (!EditorManager.instance.inConnectMode)
            // Give the EditorManager the currently selected visual
            EditorManager.instance.SelectedDialoguePartVisual = Selected ? this : null;
        else if (EditorManager.instance.SelectedAnswerVisual != null)
        {
            // Answer selected + inConnectMode => Connect Answer to DialoguePart
            EditorManager.instance.ConnectToSelectedAnswer(this);
            EditorManager.instance.SelectedDialoguePartVisual = this;
        }
        else if (EditorManager.instance.SelectedDialoguePartVisual != null)
        {
            // DialoguePart selected + inConnectMode => Connect DialoguePart to DialoguePart
            EditorManager.instance.ConnectToSelectedDP(this);
            EditorManager.instance.SelectedDialoguePartVisual = this;
        }
    }

    public void ShowContextMenu(ContextMenuManager menuManager)
    {
        EditorManager.instance.SelectedDialoguePartVisual = this;

        ContextMenuManager.instance.AddButton(
            "Add answer",
            () => { AddAnswer(); }
        );

        ContextMenuManager.instance.AddButton(
            "Add conditional answer",
            () => { AddAnswer(true); }
        );

        ContextMenuManager.instance.AddButton(
            "Connect",
            () => { EditorManager.instance.inConnectMode = true; }
        );

        ContextMenuManager.instance.AddButton(
            "Set as start",
            () => { EditorManager.instance.StartDialoguePartVisual = this; }
        );

        ContextMenuManager.instance.AddButton(
            "Delete",
            EditorManager.instance.DestroyDialoguePart // Destroys currently selected(!) DP
        );
    }

    /// <summary>
    /// Adds a new blank answer to the Dialogue Part (Visual)
    /// </summary>
    /// <param name="conditional">Whether or not the answer shall be 
    /// conditional = require a Global Property based condition to show up</param>
    /// <returns>Successful?</returns>
    public bool AddAnswer(bool conditional = false)
    {
        if (connectedDP != null)
        {
            ErrorMessage.instance.ShowErrorMessage("Answers can only be added " +
                "to a DialoguePart if it has no direct connection to another dialogue " +
                "part");
            return false;
        }

        if (conditional && EditorManager.globalProperties.Count == 0)
        {
            ErrorMessage.instance.ShowErrorMessage("You need at least one " +
                "Global Property to use conditions");
            return false;
        }

        GameObject answerGO = Instantiate(EditorManager.instance.answerVisual, transform);
        AnswerVisual answerVis = answerGO.GetComponent<AnswerVisual>();

        answers.Add(answerVis);

        answerVis.answer = new Dialogue.DialoguePart.Answer
            ("", answers.Count - 1, 0, conditional);

        answerVis.index = answers.Count - 1;

        if (conditional)
            // Default values - initialized when ConditionMenu is opened at the end *
            answerVis.answer.condition = new UDSCondition(); 

        answerVis.Conditional = conditional;
        answerVis.parentDialoguePart = this;

        // Update the answer array on the Dialogue Part
        dialoguePart.answers = answers.ConvertAll(av => av.answer).ToArray();

        // If a Property Preset for new Dialogue Parts is selected
        if (EditorManager.globalAnswerPropertyPreset != null)
        {
            PropertyPreset? preset =
                FileHandler.LoadPropertyPreset
                (EditorManager.globalAnswerPropertyPreset, PropertyPreset.PropertyPresetType.ANSWER);

            if (preset == null)
                // Error message handled by FileHandler - true because Answer was created
                return true; 

            Dictionary<string, UDSProperty> properties = preset.Value.properties;

            // Add all properties from the preset
            foreach (string p in properties.Keys)
            {
                answerVis.answer.SetProperty
                    (p, properties[p].value, properties[p].type, properties[p].required);
            }

            EditorManager.instance.noOfAnswers++;
        }

        if (conditional)
        {
            // Show Condition menu - Important because it also initializes the Condition *
            EditorManager.instance.ActiveMenu =
                EditorManager.instance.conditionMenu.gameObject;

            EditorManager.instance.conditionMenu.Init(answerVis);
        }

        return true;
    }

    public void DeleteAnswer(AnswerVisual answer)
    {
        answers.Remove(answer);

        // Update the answer array on the Dialogue Part
        dialoguePart.answers = answers.ConvertAll(av => av.answer).ToArray();

        EditorManager.instance.noOfAnswers--;
        EditorManager.instance.ActiveUI = EditorManager.instance.dialogueUI;
    }

    /// <summary>
    /// Connects the DialoguePartVisual to another one and 
    /// sets all necessary references accordingly. Also
    /// shows the connection to the user using a line renderer.
    /// </summary>
    /// <param name="dp">The other DialoguePart(Visual) which this one
    /// shall be connected to</param>
    public void SetConnection(DialoguePartVisual dp)
    {
        if (string.IsNullOrWhiteSpace(dp.dialoguePart.id))
        {
            ErrorMessage.instance.ShowErrorMessage("The Dialogue Part needs an ID in " +
                "order to be connected");
            return;
        }

        if (dp == this)
        {
            ErrorMessage.instance.ShowErrorMessage("Connecting a Dialogue Part to itself is " +
                "(currently) not possible. Sorry!");
            return;
        }

        if (dpConnection != null)
        {
            dpConnection.GetComponent<Connection>().DontUpdateConnectedVisual();
            Destroy(dpConnection.gameObject);
        }

        var a = Instantiate(EditorManager.instance.arrow,
                            new Vector3(0, 0, 0), Quaternion.identity);
        var lineRenderer = a.GetComponent<LineRenderer>();
        var conn = a.GetComponent<Connection>();

        lineRenderer.SetPositions(new Vector3[] {(Vector2)transform.position,
                                                 (Vector2)dp.transform.position});

        conn.fromDP = this;
        conn.toDP = dp;

        ConnectedDP = dp;
        dpConnection = lineRenderer.GetComponent<Connection>();
        connectionLineRenderer = lineRenderer;
    }

    private void OnDestroy()
    {
        // Triggers Connection's OnDestroy, which "informs" the connected DP
        if (dpConnection != null)
            Destroy(dpConnection?.gameObject);

        try
        {
            answers.ForEach(a => Destroy(a.gameObject));
        } catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }

    private Color GetColor()
    {
        if (Selected)
        {
            if (isStart) return startSelectedColor;
            else return selectedColor;
        }
        else
        {
            if (isStart) return startColor;
            else return normalColor;
        }
    }
}
