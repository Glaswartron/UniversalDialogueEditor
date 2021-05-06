using UnityEngine;
using UnityEngine.UI;

public class BottomButtonUI : MonoBehaviour
{
    [Header("Main UI")]
    public Button globalPropertiesButton;
    public Button clearButton;

    // Start is called before the first frame update
    void Start()
    {
        globalPropertiesButton.onClick.AddListener(
            () =>   
            {
                EditorManager.instance.ActiveMenu = 
                    EditorManager.instance.globalPropertiesMenu;
            }
        );

        clearButton.onClick.AddListener(
            () =>
            {
                /*AreYouSureDialog.instance.Open(
                    "Are you sure that you want to delete everything in the currently selected dialog?",
                    "Yes",
                    "No",
                    EditorManager.instance.ClearEverything,
                    () => { }
                );*/
                // TODO ClearEverything doesnt work here
            }
        );
    }
}
