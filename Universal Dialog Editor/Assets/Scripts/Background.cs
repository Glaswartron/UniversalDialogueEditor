using UnityEngine;

public class Background : MonoBehaviour
{
    private void OnMouseDown()
    {
        EditorManager.instance.SelectedDialogPartVisual = null;
        EditorManager.instance.SelectedAnswerVisual = null;
        EditorManager.instance.DeactivateAllActionMenus();
    }
}
