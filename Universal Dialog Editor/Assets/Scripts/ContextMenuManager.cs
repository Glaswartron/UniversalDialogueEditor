using UnityEngine;
using UnityEngine.UI;

public class ContextMenuManager : MonoBehaviour
{
    public static ContextMenuManager instance;

    [HideInInspector]
    public bool contextMenuOpen;

    [Header("Editor Context Menu")]
    public GameObject editorContextMenu;
    public Button createDialogPartButton;

    [Header("Dialog Part Context Menu")]
    public GameObject dialogPartContextMenu;
    public Button addAnswerButton;
    public Button connectDialogPartButton;
    public Button setAsStartButton;
    public Button deleteDialogPartButton;
    public Slider sizeSlider;

    [Header("Answer Context Menu")]
    public GameObject answerContextMenu;
    public Button connectAnswerButton;
    public Button deleteAnswerButton;

    [Header("Connection Context Menu")]
    public GameObject connectionContextMenu;
    public Button deleteConnectionButton;

    private RectTransform[] contextMenuRectTransforms;

    private EditorManager editorManager;

    private void OnEnable()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        createDialogPartButton.onClick.AddListener
            (() => { editorManager.CreateDialogPart(); editorContextMenu.SetActive(false); });



        contextMenuRectTransforms = new RectTransform[] {
            editorContextMenu.GetComponent<RectTransform>(),
            dialogPartContextMenu.GetComponent<RectTransform>(),
            answerContextMenu.GetComponent<RectTransform>(),
            connectionContextMenu.GetComponent<RectTransform>() };
    }

    // Update is called once per frame
    void Update()
    {
        // Shorter :D (Can't be in Start because of Execution Order)
        if (editorManager == null)
            editorManager = EditorManager.instance;

        // No context menues in some UIs
        if (editorManager.ActiveUI == editorManager.startAndSelectUI)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!MouseOverContextMenu())
                DeactivateAllContextMenus();
        }

        // Open the right click menu or close it if it is already open
        if (Input.GetMouseButtonDown(1))
        {
            if (MouseOverContextMenu())
                return;

            DeactivateAllContextMenus();

            // No menu when mouse is over UI/editor panel
            if (editorManager.editorPanel.rect.Contains(Input.mousePosition))
                return;

            // Raycast to see what the user clicked
            RaycastHit2D hit;
            if (hit = Physics2D.GetRayIntersection
                (editorManager.mainCam.ScreenPointToRay(Input.mousePosition)))
            {
                if (hit.collider.CompareTag("DialogPart"))
                {
                    // Select dialog part
                    var diaPart = hit.collider.GetComponent<DialogPartVisual>();
                    editorManager.SelectedDialogPartVisual = diaPart;
                    diaPart.Selected = true; // See DialogPartVisual.Selected

                    OpenContextMenu(dialogPartContextMenu);
                }
                else if (hit.collider.CompareTag("Answer"))
                {
                    // Select answer
                    var answer = hit.collider.GetComponent<AnswerVisual>();
                    editorManager.SelectedAnswerVisual = answer;
                    answer.Selected = true; // See AnswerVisual.Selected

                    OpenContextMenu(answerContextMenu);
                }
                else if (hit.collider.CompareTag("Connection"))
                {
                    editorManager.selectedConnection
                        = hit.collider.transform.parent.gameObject;

                    OpenContextMenu(connectionContextMenu);
                }
                else
                    return;
            }
            else
                OpenContextMenu(editorContextMenu);
        }
    }

    public void OpenContextMenu(GameObject menu)
    {
        if (contextMenuOpen)
            DeactivateAllContextMenus(); // Closes the previously open context menu

        // Moves the menu to the mousePosition + a little offset to the bottom right
        menu.transform.position = Input.mousePosition + new Vector3(110, -30);

        menu.SetActive(true);

        contextMenuOpen = true;
    }

    /// <summary>
    /// Deactivates all context menus there are
    /// </summary>
    public void DeactivateAllContextMenus()
    {
        editorContextMenu.SetActive(false);
        dialogPartContextMenu.SetActive(false);
        answerContextMenu.SetActive(false);
        connectionContextMenu.SetActive(false);
        contextMenuOpen = false;
    }

    private bool MouseOverContextMenu()
    {
        foreach (RectTransform rt in contextMenuRectTransforms)
        {
            if (Utility.GetWorldRect(rt).Contains(Input.mousePosition))
                return true;
        }

        return false;
    }
}
