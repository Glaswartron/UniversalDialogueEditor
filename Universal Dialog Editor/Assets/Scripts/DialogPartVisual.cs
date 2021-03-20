using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DialogPartVisual : MonoBehaviour
{
    /// <summary>
    /// The Dialog Part this visual incapsulates. Very important!
    /// </summary>
    public DialogOld.DialogPart dialogPart;

    [Header("GameObjects and UI")]
    public TextMeshPro idText;
    public GameObject[] answers;
    public GameObject particleSys;
    private RectTransform editorPanel;

    [Header("Colors")]
    public Color normalColor;
    public Color selectedColor;

    private SpriteRenderer spriteRenderer;
    private Camera mainCam;

    public DialogPartVisual ConnectedDP
    {
        set
        {
            connectedDP = value;

            if (value != null)
                dialogPart.nextPartID = value.dialogPart.id;
            else
                dialogPart.nextPartID = string.Empty;
        }

        get { return connectedDP; }
    }
    private DialogPartVisual connectedDP;
    public LineRenderer dpConnection;

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
        editorPanel = EditorManager.instance.editorPanel;
        //dialogPart = new Dialog.DialogPart();
        //dialogPart.answers = new Dialog.Answer[0];
    }

    // Update is called once per frame
    void Update()
    {
        // Constantly updates the idText
        idText.SetText(dialogPart.id);

        if (ConnectedDP == null && dpConnection != null)
            Destroy(dpConnection.gameObject);
        else if (dpConnection != null) // Connections
            dpConnection.SetPositions(new Vector3[] {(Vector2)transform.position,
                                                 (Vector2)connectedDP.transform.position});

        if (ConnectedDP != null)
            dialogPart.nextPartID = connectedDP.dialogPart.id;
    }

    public void OnMouseDrag()
    {
        // The user can drag the visual if it is selected (not if he has just deselected it)
        if (Selected)
        {
            var mousePos = (Vector2)mainCam.ScreenToWorldPoint(Input.mousePosition);
            float sqrDisVisualToMouse = Vector2.SqrMagnitude((Vector2)transform.position - mousePos);
            /* Makes that the visual doesn't jump/move right when clicked (1),
             * that it is impossible to drag it out of the screen (2) and
             * harder to drag it under the UI. (3) */
            if (sqrDisVisualToMouse > Mathf.Pow(0.3f, 2) // (1)
                && mainCam.pixelRect.Contains(Input.mousePosition) // 2
                && !editorPanel.rect.Contains(Input.mousePosition) // 3
                && !EventSystem.current.IsPointerOverGameObject()) // 3
            {
                transform.position = mousePos;
                dialogPart.nodeX = mousePos.x;
                dialogPart.nodeY = mousePos.y;
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
    /// Adds a new blank answer to the DialogPart(Visual)
    /// </summary>
    public void AddAnswer()
    {
        if (dialogPart.answers.Length == 3) // Maximum: 3 answers
            return;

        if (connectedDP != null)
        {
            ErrorMessage.instance.ShowErrorMessage("Du kannst nur Antworten hinzufügen, " +
                "wenn der Dialog Part keine direkte Verbindung zu einem anderen hat!");
            return;
        }

        foreach (GameObject a in answers)
        {
            // Activate one more answer visual
            if (!a.activeSelf)
            {
                a.SetActive(true);
                break;
            }
        }

        // Add the answer to the Dialog Part
        var answersList = new List<DialogOld.Answer>(dialogPart.answers);
        answersList.Add(new DialogOld.Answer());
        dialogPart.answers = answersList.ToArray();
    }

    /// <summary>
    /// Deletes the "newest" answer. Use with caution!
    /// </summary>
    public void DeleteAnswer()
    {
        if (dialogPart.answers.Length == 0)
            return;

        // Deactivate the "newest" answer visual
        answers[dialogPart.answers.Length - 1].SetActive(false);

        // Remove the answer from the Dialog Part
        var answersList = new List<DialogOld.Answer>(dialogPart.answers);
        answersList.RemoveAt(answersList.Count - 1);
        dialogPart.answers = answersList.ToArray();
    }

    /// <summary>
    /// Connects the DialogPartVisual to another one and 
    /// sets all necessary references accordingly. Also
    /// shows the connection to the user using a line renderer.
    /// </summary>
    public void SetConnection(DialogPartVisual dp)
    {
        if (string.IsNullOrWhiteSpace(dp.dialogPart.id))
        {
            ErrorMessage.instance.ShowErrorMessage("Der Dialog Part braucht eine ID, " +
                "damit zu ihn verbinden kannst!");
            return;
        }

        if (dp == this)
        {
            ErrorMessage.instance.ShowErrorMessage("Willst du den Spielern das wirklich antun? " +
                "Du kannst einen Dialog Part nicht mit sich selbst verbinden!");
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
        dpConnection = lineRenderer;
    }

    private void OnDestroy()
    {
        if (dpConnection != null)
            Destroy(dpConnection?.gameObject);

        System.Array.ForEach(answers, a => Destroy(a.gameObject));
    }
}
