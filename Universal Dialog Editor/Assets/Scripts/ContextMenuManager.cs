using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ContextMenuManager : MonoBehaviour
{
    public static ContextMenuManager instance;

    [HideInInspector]
    public bool contextMenuOpen;

    [Header("Main UI")]
    public GameObject contextMenu;
    private RectTransform contextMenuRectTransform;

    [Header("Prefabs")]
    public GameObject contextMenuButton;

    [Header("Connection Context Menu")]
    public GameObject connectionContextMenu;
    public Button deleteConnectionButton;

    private EditorManager editorManager;

    private void OnEnable()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        contextMenuRectTransform = contextMenu.GetComponent<RectTransform>();
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
            if (!IsMouseOverContextMenu())
                DeactivateContextMenu();
        }

        // Open the context menu or close it if it is already open
        if (Input.GetMouseButtonDown(1))
        {
            if (IsMouseOverContextMenu())
                return;

            DeactivateContextMenu();

            // No menu when mouse is over UI/editor panel
            if (editorManager.editorPanel.rect.Contains(Input.mousePosition))
                return;

            // Raycast to see what the user clicked
            RaycastHit2D hit;
            if (hit = Physics2D.GetRayIntersection
                (editorManager.mainCam.ScreenPointToRay(Input.mousePosition)))
            {
                IContextMenu target = hit.transform.GetComponent<IContextMenu>();
                if (target != null)
                {
                    OpenContextMenu(target);
                }

                /*if (hit.collider.CompareTag("DialogPart"))
                {
                    // Select dialog part
                    var diaPart = hit.collider.GetComponent<DialogPartVisual>();
                    editorManager.SelectedDialogPartVisual = diaPart;
                    diaPart.Selected = true; // See DialogPartVisual.Selected

                    dialogPartVisual = diaPart;

                    OpenContextMenu(dialogPartContextMenu);
                }
                else if (hit.collider.CompareTag("Answer"))
                {
                    // Select answer
                    var answer = hit.collider.GetComponent<AnswerVisual>();
                    editorManager.SelectedAnswerVisual = answer;
                    answer.Selected = true; // See AnswerVisual.Selected

                    answerVisual = answer;

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
                OpenContextMenu(editorContextMenu);*/
            }
            else
                ShowEditorContextMenu(); // !
        }
    }

    private void ShowEditorContextMenu()
    {
        AddButton(
             "New Dialog Part",
             EditorManager.instance.CreateDialogPart
         );

        OpenContextMenu(null);
    }

    public void OpenContextMenu(IContextMenu target)
    {
        DeactivateContextMenu(); // Closes the previously open context menu

        // Moves the menu to the mousePosition + a little offset to the bottom right
        contextMenu.transform.position = (Vector2)Input.mousePosition 
                                         + editorManager.menuOffsetFromMouse;

        target?.ShowContextMenu(this);

        contextMenu.SetActive(true);

        contextMenuOpen = true;
    }

    /// <summary>
    /// Deactivates the context menu
    /// </summary>
    public void DeactivateContextMenu()
    {
        if (!contextMenuOpen)
            return;

        contextMenu.SetActive(false);

        foreach (Transform child in contextMenu.transform)
            Destroy(child.gameObject);

        contextMenuOpen = false;
    }

    public void AddButton(string text, UnityAction onClick)
    {
        Button b = Instantiate(contextMenuButton, contextMenu.transform)
                   .GetComponent<Button>();

        b.GetComponentInChildren<TMP_Text>().SetText(text);

        b.onClick.AddListener(onClick + DeactivateContextMenu);
    }

    private bool IsMouseOverContextMenu()
    {
        return contextMenuOpen && Utility.IsMouseOverUI(contextMenuRectTransform);
    }
}
