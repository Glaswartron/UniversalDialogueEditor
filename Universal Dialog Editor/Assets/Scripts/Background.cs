using UnityEngine;

public class Background : MonoBehaviour
{
    private void OnMouseDown()
    {
        EditorManager.instance.SelectedDialoguePartVisual = null;
        EditorManager.instance.SelectedAnswerVisual = null;
        ContextMenuManager.instance.DeactivateContextMenu();
    }

}
