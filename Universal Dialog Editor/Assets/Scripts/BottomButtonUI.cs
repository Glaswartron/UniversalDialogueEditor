using UnityEngine;
using UnityEngine.UI;

public class BottomButtonUI : MonoBehaviour
{
    [Header("Main UI")]
    public Button globalPropertiesButton;
    public Button settingsButton;

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

        settingsButton.onClick.AddListener(
            () =>
            {
                EditorManager.instance.ActiveMenu =
                    EditorManager.instance.settingsMenu.gameObject;
            }
        );
    }
}
