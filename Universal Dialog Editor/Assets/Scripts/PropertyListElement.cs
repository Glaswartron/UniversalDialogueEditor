using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertyListElement : MonoBehaviour
{
    [Header("Main UI")]
    public TMP_InputField idInputField;
    public Button localizationButton;
    public Button deleteButton;

    [Header("Input Fields")]
    public TMP_InputField stringIntFloatInputField;
    public Toggle boolToggle;
}
