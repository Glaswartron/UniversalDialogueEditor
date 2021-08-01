using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnswerVisual : MonoBehaviour, IContextMenu, IConditional
{
    public int index;

    public TMP_Text idTextField;

    public bool Conditional
    {
        set
        {
            conditional = value;

            answer.conditional = value;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            spriteRenderer.color = GetColor();
        }

        get { return conditional;  }
    }
    private bool conditional;

    public SpriteRenderer indicator;
    public Sprite dialogueEndIndicator;

    public DialoguePartVisual parentDialoguePart;

    /// <summary>
    /// The answer this visual encapsulates. Very important!
    /// HideInInspector extremely important because Dialogue.DialoguePart.Answer 
    /// has recursive references which break the Editor
    /// </summary>
    [HideInInspector]
    public Dialogue.DialoguePart.Answer answer;

    [Header("Colors")]
    public Color normalColor;
    public Color selectedColor;
    public Color conditionalColor;
    public Color conditionalSelectedColor;

    private SpriteRenderer spriteRenderer;
    private Camera mainCam;

    public float Angle
    {
        set
        {
            angle = value;
            answer.angle = value;
        }

        get { return angle; }
    }
    private float angle; // Position

    public DialoguePartVisual ConnectedDP
    {
        set
        {
            connectedDP = value;

            if (value != null)
                answer.nextDialoguePartID = value.dialoguePart.id;
            else
                answer.nextDialoguePartID = string.Empty;
        }

        get { return connectedDP; }
    }
    private DialoguePartVisual connectedDP;
    private Connection connection;
    private LineRenderer connectionRenderer;

    public UDSCondition? Condition
    {
        set
        {
            condition = value;

            answer.condition = value;
        }

        get { return condition; }
    }
    private UDSCondition? condition;

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
        /* Continuously update the dialogue parts answer 
         * to apply the stuff changed in the editor */
        parentDialoguePart.dialoguePart.answers[index] = answer;

        idTextField.SetText(answer.id);

        // Connection
        if (connection != null && ConnectedDP != null)
        {
            connectionRenderer.SetPositions(new Vector3[] {(Vector2)transform.position,
                                                 (Vector2)ConnectedDP.transform.position});

            connection.collSet = false;
        }

        if (ConnectedDP != null)
            answer.nextDialoguePartID = ConnectedDP.dialoguePart.id;

        if (connection != null && ConnectedDP == null)
            Destroy(connection.gameObject);

        // Order important
        if (string.IsNullOrWhiteSpace(answer.nextDialoguePartID))
        {
            indicator.transform.localScale = new Vector3(0.075f, 0.075f, 1);
            indicator.sprite = dialogueEndIndicator;
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
                                    - (Vector2)parentDialoguePart.transform.position;
            circleToMouse.Normalize();

            Angle = Mathf.Atan2(circleToMouse.y, circleToMouse.x);

            transform.position = (Vector2)parentDialoguePart.transform.position
                                 + new Vector2(Mathf.Cos(Angle) * 0.75f, Mathf.Sin(Angle) * 0.75f);
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
                () => 
                {
                    if (EditorManager.globalProperties.Count > 0)
                    {
                        EditorManager.instance.ActiveMenu = 
                            EditorManager.instance.conditionMenu.gameObject;

                        EditorManager.instance.conditionMenu.Init(this);
                    } 
                    else
                    {
                        ErrorMessage.instance.ShowErrorMessage("You need at least one " +
                            "Global Property to use conditions");
                    }
                }
            );
        }

        ContextMenuManager.instance.AddButton(
            "Delete",
            () => 
            {
                parentDialoguePart.DeleteAnswer(this);
                Destroy(gameObject);
            } 
        );
    }

    /// <summary>
    /// Connects the AnswerVisual to another DialoguePartVisual and 
    /// sets all necessary references accordingly. Also shows the 
    /// connection to the user using a line renderer.
    /// </summary>
    public void SetConnection(DialoguePartVisual dp)
    {
        if (dp == parentDialoguePart)
        {
            ErrorMessage.instance.ShowErrorMessage("It's currently not possible to " +
                "connect an answer to its own Dialogue Part");
            return;
        }

        if (string.IsNullOrWhiteSpace(dp.dialoguePart.id))
        {
            ErrorMessage.instance.ShowErrorMessage("The Dialogue Part needs an ID in " +
                "order to be connected");
            return;
        }

        if (connection != null)
        {
            connection.DontUpdateConnectedVisual();
            Destroy(connection.gameObject);
        }

        var a = Instantiate(EditorManager.instance.arrow, 
                            new Vector3(0, 0, 0), Quaternion.identity);
        connection = a.GetComponent<Connection>();
        connectionRenderer = a.GetComponent<LineRenderer>();

        connectionRenderer.SetPositions(new Vector3[] {(Vector2)transform.position,
                                                 (Vector2)dp.transform.position});

        connection.fromA = this;
        connection.toDP = dp;

        ConnectedDP = dp;
    }

    public void SetCondition(UDSCondition condition)
    {
        this.Condition = condition;
    }

    public UDSCondition? GetCondition()
    {
        return Condition;
    }

    private Color GetColor()
    {
        if (Selected)
        {
            if (conditional) return conditionalSelectedColor;
            else return selectedColor;
        }
        else
        {
            if (conditional) return conditionalColor;
            else return normalColor;
        }
    }

    private void OnDestroy()
    {
        if (connection != null)
            Destroy(connection.gameObject);
    }

}
