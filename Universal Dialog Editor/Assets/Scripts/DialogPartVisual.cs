using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DialogPartVisual : MonoBehaviour
{
    /// <summary>
    /// The Dialog Part this visual encapsulates. Very important!
    /// </summary>
    public Dialog.DialogPart dialogPart;

    [HideInInspector]
    public AnswerVisual[] answers;

    [Header("Prefabs and UI")]
    public TextMeshPro idText;
    public GameObject answerPrefab;

    [Header("Colors")]
    public Color normalColor;
    public Color selectedColor;

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

    public DialogPartVisual ConnectedDP
    {
        set
        {
            connectedDP = value;

            if (value != null)
                dialogPart.nextDialogPartID = value.dialogPart.id;
            else
                dialogPart.nextDialogPartID = string.Empty;
        }

        get { return connectedDP; }
    }
    private DialogPartVisual connectedDP;
    
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
            spriteRenderer.color = value ? selectedColor : normalColor;
        }

        get { return selected; }
    }
    private bool selected;

    // Start is called before the first frame update
    void Start()
    {
        // Init
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;
        spriteRenderer.color = normalColor;
    }

    // Update is called once per frame
    void Update()
    {
        // Constantly updates the idText
        idText.SetText(dialogPart.id);

        if (ConnectedDP == null && dpConnection != null)
            Destroy(dpConnection.gameObject);
        else if (dpConnection != null) // Connections
            connectionLineRenderer.SetPositions(new Vector3[] {(Vector2)transform.position,
                                                 (Vector2)connectedDP.transform.position});

        if (ConnectedDP != null)
            dialogPart.nextDialogPartID = connectedDP.dialogPart.id;
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
                dialogPart.visualX = (int) mousePos.x; 
                dialogPart.visualY = (int) mousePos.y;
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
            EditorManager.instance.SelectedDialogPartVisual = Selected ? this : null;
        else if (EditorManager.instance.SelectedAnswerVisual != null)
        {
            EditorManager.instance.ConnectToSelectedAnswer(this);
            EditorManager.instance.SelectedDialogPartVisual = this;
        }
        else if (EditorManager.instance.SelectedDialogPartVisual != null)
        {
            EditorManager.instance.ConnectToSelectedDP(this);
            EditorManager.instance.SelectedDialogPartVisual = this;
        }
    }

    /// <summary>
    /// Adds a new blank answer to the Dialog Part (Visual)
    /// </summary>
    /// <returns>Successful?</returns>
    public bool AddAnswer()
    {
        if (connectedDP != null)
        {
            ErrorMessage.instance.ShowErrorMessage("Answers can only be added " +
                "to a DialogPart if it has no direct connection to another dialog " +
                "part");
            return false;
        }

        // Add the answer to the Dialog Part
        var answersList = new List<Dialog.DialogPart.Answer>(dialogPart.answers);
        answersList.Add(new Dialog.DialogPart.Answer("", answersList.Count, dialogPart));
        dialogPart.answers = answersList.ToArray();

        return true;
    }

    /// <summary>
    /// Connects the DialogPartVisual to another one and 
    /// sets all necessary references accordingly. Also
    /// shows the connection to the user using a line renderer.
    /// </summary>
    /// <param name="dp">The other DialogPart(Visual) which this one
    /// shall be connected to</param>
    public void SetConnection(DialogPartVisual dp)
    {
        if (string.IsNullOrWhiteSpace(dp.dialogPart.id))
        {
            ErrorMessage.instance.ShowErrorMessage("The Dialog Part needs an ID in " +
                "order to be connected");
            return;
        }

        if (dp == this)
        {
            ErrorMessage.instance.ShowErrorMessage("Connecting a Dialog Part to itself is " +
                "(currently) not possible. Sorry!");
            return;
        }

        if (dpConnection != null)
        {
            dpConnection.GetComponent<Connection>().DontUpdateConnectedVisual();
            Destroy(dpConnection.gameObject);
        }

        var a = Instantiate(EditorManager.instance.arrow,
                            transform.position, Quaternion.identity);
        var lineRenderer = a.GetComponent<LineRenderer>();
        var conn = a.GetComponent<Connection>();

        lineRenderer.SetPositions(new Vector3[] {(Vector2)transform.position,
                                                 (Vector2)dp.transform.position});

        conn.oneDP = this;
        conn.two = dp;

        ConnectedDP = dp;
        dpConnection = lineRenderer.GetComponent<Connection>();
        connectionLineRenderer = lineRenderer;
    }

    private void OnDestroy()
    {
        if (dpConnection != null)
            Destroy(dpConnection?.gameObject);

        try
        {
            System.Array.ForEach(answers, a => Destroy(a.gameObject));
        } catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
}
