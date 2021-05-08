using UnityEngine;
using UnityEngine.EventSystems;

public class AnswerVisual : MonoBehaviour, IContextMenu
{
    public int index;

    public bool Conditional
    {
        set
        {

        }

        get { return conditional;  }
    }
    private bool conditional;

    public SpriteRenderer indicator;
    public Sprite dialogEndIndicator;

    public DialogPartVisual parentDialogPart;

    /// <summary>
    /// The answer this visual encapsulates. Very important!
    /// HideInInspector extremely important because Dialog.DialogPart.Answer 
    /// has recursive references which break the Editor
    /// </summary>
    [HideInInspector]
    public Dialog.DialogPart.Answer answer;

    [Header("Colors")]
    public Color normalColor;
    public Color selectedColor;
    public Color conditionalColor;
    public Color conditionalSelectedColor;

    private SpriteRenderer spriteRenderer;
    private Camera mainCam;

    public DialogPartVisual ConnectedDP
    {
        set
        {
            connectedDP = value;

            if (value != null)
                answer.nextDialogPartID = value.dialogPart.id;
            else
                answer.nextDialogPartID = string.Empty;
        }

        get { return connectedDP; }
    }
    private DialogPartVisual connectedDP;
    private LineRenderer connection;

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
        spriteRenderer.color = normalColor;
        mainCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        /* Continuously update the dialog parts answer 
         * to apply the stuff changed in the editor */
        parentDialogPart.dialogPart.answers[index] = answer;

        // Connection
        if (connection != null && ConnectedDP != null)
            connection.SetPositions(new Vector3[] {(Vector2)transform.position,
                                                 (Vector2)ConnectedDP.transform.position});

        if (ConnectedDP != null)
            answer.nextDialogPartID = ConnectedDP.dialogPart.id;

        if (connection != null && ConnectedDP == null)
            Destroy(connection.gameObject);

        // Order important
        if (string.IsNullOrWhiteSpace(answer.nextDialogPartID))
        {
            indicator.transform.localScale = new Vector3(0.075f, 0.075f, 1);
            indicator.sprite = dialogEndIndicator;
        } else
        {
            indicator.transform.localScale = new Vector3(1, 1, 1);
            indicator.sprite = null;
        }
    }

    private void OnMouseDrag()
    {
        if (Selected)
        {
            // Move around in a circle based on mouse movement

            Vector2 circleToMouse = (Vector2)mainCam.ScreenToWorldPoint(Input.mousePosition)
                                    - (Vector2)parentDialogPart.transform.position;
            circleToMouse.Normalize();

            float angle = Mathf.Atan2(circleToMouse.y, circleToMouse.x);

            transform.position = (Vector2)parentDialogPart.transform.position
                                 + new Vector2(Mathf.Cos(angle) * 0.75f, Mathf.Sin(angle) * 0.75f);
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // Pressing on the visual selects/deselects it
        Selected = !Selected;

        EditorManager.instance.inConnectMode = false;

        // Give the EditorManager the currently selected visual
        EditorManager.instance.SelectedAnswerVisual = Selected ? this : null;           
    }

    public void ShowContextMenu(ContextMenuManager menuManager)
    {
        EditorManager.instance.SelectedAnswerVisual = this;

        // TODO
        ContextMenuManager.instance.AddButton(
            "Connect",
            () => 
            {
                EditorManager.instance.inConnectMode = true;
            }
        );

        if (Conditional)
        {
            ContextMenuManager.instance.AddButton(
                "Edit condition",
                () => { Debug.LogWarning("TODO"); }
            );
        }

        ContextMenuManager.instance.AddButton(
            "Delete",
            () => 
            {
                parentDialogPart.DeleteAnswer(this);
                Destroy(gameObject);
            } 
        );
    }

    /// <summary>
    /// Connects the AnswerVisual to another DialogPartVisual and 
    /// sets all necessary references accordingly. Also shows the 
    /// connection to the user using a line renderer.
    /// </summary>
    public void SetConnection(DialogPartVisual dp)
    {
        if (dp == parentDialogPart)
        {
            ErrorMessage.instance.ShowErrorMessage("It's currently not possible to " +
                "connect an answer to its own Dialog Part.");
            return;
        }

        if (string.IsNullOrWhiteSpace(dp.dialogPart.id))
        {
            ErrorMessage.instance.ShowErrorMessage("The Dialog Part needs an ID in " +
                "order to be connected");
            return;
        }

        if (connection != null)
        {
            connection.GetComponent<Connection>().DontUpdateConnectedVisual();
            Destroy(connection.gameObject);
        }

        var a = Instantiate(EditorManager.instance.arrow, 
                            transform.position, Quaternion.identity);
        var lineRenderer = a.GetComponent<LineRenderer>();
        var conn = a.GetComponent<Connection>();

        lineRenderer.SetPositions(new Vector3[] {(Vector2)transform.position,
                                                 (Vector2)dp.transform.position});

        conn.oneA = this;
        conn.two = dp;

        ConnectedDP = dp;
        connection = lineRenderer;

        //answer.nextPartID = dp.dialogPart.id;
    }

    private void OnDestroy()
    {
        if (connection != null)
            Destroy(connection.gameObject);
    }

}
