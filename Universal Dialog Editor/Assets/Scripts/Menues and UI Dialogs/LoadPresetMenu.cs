using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadPresetMenu : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject scrollViewContent;
    public Button loadButton;
    public Button closeButton;

    [Header("Prefabs")]
    public GameObject selectableText;

    private PropertiesUI caller;
    private PropertyPreset.PropertyPresetType type;

    private string[] presetIDs;
    private List<ExtendedToggle> presetSelectables;
    private int selectedPresetIndex;

    private RectTransform loadButtonRectTransform;

    private ToggleGroup presetScrollViewToggleGroup;

    // Start is called before the first frame update
    void Start()
    {
        // Init
        loadButtonRectTransform = loadButton.GetComponent<RectTransform>();
        presetScrollViewToggleGroup = scrollViewContent.GetComponent<ToggleGroup>();

        loadButton.onClick.AddListener(LoadSelectedPreset);

        closeButton.onClick.AddListener(
            () =>
            {
                EditorManager.instance.ActiveMenu = null;
            }
        );
    }

    private void OnDisable()
    {
        ClearScrollView();
    }

    public void Init(PropertiesUI caller, PropertyPreset.PropertyPresetType type)
    {
        if (presetScrollViewToggleGroup == null)
            presetScrollViewToggleGroup = scrollViewContent.GetComponent<ToggleGroup>();

        if (loadButtonRectTransform == null)
            loadButtonRectTransform = loadButton.GetComponent<RectTransform>();

        this.caller = caller;
        this.type = type;

        presetSelectables = new List<ExtendedToggle>();

        presetIDs = FileHandler.GetAllPropertyPresetIDs(type);

        ClearScrollView();

        foreach (string p in presetIDs)
        {
            InstantiatePresetSelectableText(p);
        }
    }

    private void LoadSelectedPreset()
    {
        PropertyPreset? preset = 
            FileHandler.LoadPropertyPreset(presetIDs[selectedPresetIndex], type);

        if (preset == null)
            return; // Error message handled by FileHandler

        caller.LoadPropertyPreset(preset);

        EditorManager.instance.ActiveMenu = null; // Deactivates itself
    }

    private GameObject InstantiatePresetSelectableText(string text)
    {
        GameObject toggleGO = Instantiate(selectableText,
                                          scrollViewContent.transform);

        toggleGO.GetComponentInChildren<TMP_Text>().SetText(text);

        ExtendedToggle toggle = toggleGO.GetComponent<ExtendedToggle>();
        presetSelectables.Add(toggle); // !

        int index = presetSelectables.Count - 1; // Important

        // Set the onValueChanged event for the toggle/selectable text
        toggle.onValueChanged.AddListener(
            (value) =>
            {
                if (value)
                {
                    // Selected => Activate loadButton + store index
                    loadButton.interactable = true;
                    selectedPresetIndex = index;
                }
                // Deselected => Deactivate loadButton and deleteButton
                else { loadButton.interactable = false; }
            }
            );

        // Load on submit / when Enter key is pressed
        toggle.onSubmit.AddListener(
            () => LoadSelectedPreset()
        );

        // Set toggle/selectable text up to "deselect itself" correctly
        toggle.group = presetScrollViewToggleGroup;
        toggle.deselectOnUnrelatedClick = true;
        toggle.relatedUIElements = new RectTransform[] { loadButtonRectTransform };

        return toggleGO;
    }

    private void ClearScrollView()
    {
        /* Must go backwards to prevent InvalidOperationException
         * (similar to ConcurrentModificationException in Java) */
        for (int i = presetSelectables.Count - 1; i >= 0; i--)
        {
            if (presetSelectables[i] != null && presetSelectables[i].gameObject != null)
            {
                GameObject go = presetSelectables[i].gameObject;
                Destroy(go);
            }
        }

        presetSelectables.Clear();
    }
}
