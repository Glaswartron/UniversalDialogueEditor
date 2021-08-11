using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;

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

    [Header("Settings")]
    public Vector2 menuOffsetFromMouse;

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
            if (Utility.IsMouseOverUI(editorManager.editorPanel))
                return;

            // Raycast to see what the user clicked
            RaycastHit2D[] hits;
            hits = Physics2D.GetRayIntersectionAll
                (editorManager.mainCam.ScreenPointToRay(Input.mousePosition));
            if (hits.Length > 0)
            {
                // Find the one with the highest sorting order
                RaycastHit2D hit 
                    = hits.OrderByDescending(
                        h => h.collider.GetComponent<Renderer>().sortingOrder).First();

                IContextMenu target = hit.transform.GetComponent<IContextMenu>();
                if (target != null)
                {
                    OpenContextMenu(target);
                }
            }
            else
                ShowEditorContextMenu(); // !
        }
    }

    private void ShowEditorContextMenu()
    {
        AddButton(
             "New Dialogue Part",
             EditorManager.instance.CreateDialoguePart
         );

        OpenContextMenu(null);
    }

    public void OpenContextMenu(IContextMenu target)
    {
        DeactivateContextMenu(); // Closes the previously open context menu

        /* Moves the menu to the mousePosition + a little offset to the bottom right
         * Important: Done in Viewport space to work in multiple resolutions */
        contextMenu.transform.position = editorManager.mainCam.ViewportToScreenPoint(
            (Vector2)editorManager.mainCam.ScreenToViewportPoint((Vector2)Input.mousePosition)
            + menuOffsetFromMouse);

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
