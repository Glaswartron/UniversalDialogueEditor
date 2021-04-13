using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogPartUI : MonoBehaviour
{
    [Header("Main UI")]
    public PropertiesUI propertiesUI;
    public IDInputUI idInputUI;

    public Dialog.DialogPart dialogPart;

    private void OnEnable()
    {
        dialogPart = EditorManager.instance.SelectedDialogPartVisual.dialogPart;

        idInputUI.dialogComponent = dialogPart;
        propertiesUI.dialogComponent = dialogPart;

        propertiesUI.Init(dialogPart); // Super important stuff
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
