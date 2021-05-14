using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConditionMenu : MonoBehaviour
{
    public static readonly Dictionary<Type, Dictionary<string, Func<object, object, bool>>>
        typeToOperators = new Dictionary<Type, Dictionary<string, Func<object, object, bool>>>
        {
            {
                typeof(string),
                new Dictionary<string, Func<object, object, bool>>
                {
                    { "==", (object s1, object s2) => ((string)(s1)).Equals((string)s2) },
                    { "!=", (object s1, object s2) => !((string)(s1)).Equals((string)s2)}
                }
            },
            {
                typeof(int),
                new Dictionary<string, Func<object, object, bool>>
                {
                    { "==", (object i1, object i2) => (int)(i1) == (int)i2 },
                    { "!=", (object i1, object i2) => (int)(i1) != (int)i2 },
                    { ">=", (object i1, object i2) => (int)(i1) >= (int)i2 },
                    { "<=", (object i1, object i2) => (int)(i1) <= (int)i2 },
                    { ">", (object i1, object i2) => (int)(i1) > (int)i2 },
                    { "<", (object i1, object i2) => (int)(i1) < (int)i2 }
                }
            },
            {
                typeof(bool),
                new Dictionary<string, Func<object, object, bool>>
                {
                    { "true", (object i1, object i2) => (bool)(i1) },
                    { "false", (object i1, object i2) => !(bool)(i1) }
                }
            },
            {
                typeof(float),
                new Dictionary<string, Func<object, object, bool>>
                {
                    { "==", (object i1, object i2) => (float)(i1) == (float)i2 },
                    { "!=", (object i1, object i2) => (float)(i1) != (float)i2 },
                    { ">=", (object i1, object i2) => (float)(i1) >= (float)i2 },
                    { "<=", (object i1, object i2) => (float)(i1) <= (float)i2 },
                    { ">", (object i1, object i2) => (float)(i1) > (float)i2 },
                    { "<", (object i1, object i2) => (float)(i1) < (float)i2 }
                }
            }
        };

    [Header("Main UI")]
    public TMP_Dropdown globalPropertyDropdown;
    public TMP_Dropdown operatorDropdown;
    public TMP_InputField compareToInputField;
    public Button closeButton;

    private IConditional currentConditional;
    private Type currentType;

    private void OnDisable()
    {
        // In case it's still deactivated because condition was of type bool
        compareToInputField.gameObject.SetActive(true);

        // Cleanup because the menu is being reused
        globalPropertyDropdown.onValueChanged.RemoveAllListeners();
        operatorDropdown.onValueChanged.RemoveAllListeners();
        compareToInputField.onValueChanged.RemoveAllListeners();
        globalPropertyDropdown.ClearOptions();
        operatorDropdown.ClearOptions();
        compareToInputField.SetTextWithoutNotify("");
    }

    public void Init(IConditional conditional)
    {
        currentConditional = conditional;

        UDSCondition condition = conditional.GetCondition();

        // In case this conditions is being edited for the first time
        if (string.IsNullOrWhiteSpace(condition.globalPropertyKey)
            || string.IsNullOrWhiteSpace(condition.operation))
            InitCondition();

        condition = conditional.GetCondition(); // Important

        // Cache type
        currentType = EditorManager.globalProperties[condition.globalPropertyKey].type;

        // Init every element
        InitGlobalPropertyDropdown(condition);
        InitOperatorDropdown(condition);
        InitCompareToInputField(condition);

        closeButton.onClick.AddListener(
            () =>
            {
                EditorManager.instance.ActiveMenu = null;
            });
    }

    private void SaveCondition()
    {
        // Global Variable
        string gpKey = globalPropertyDropdown.options[globalPropertyDropdown.value].text;

        // Operator
        string op = operatorDropdown.options[operatorDropdown.value].text;

        // Compare To
        string input = compareToInputField.text;
        object compareTo = null;
        if (!string.IsNullOrWhiteSpace(input))
        {
            if (currentType != typeof(float))
                compareTo = TypeDescriptor.GetConverter(currentType).ConvertFromString(input);
            else
                compareTo = float.Parse(input, CultureInfo.CurrentCulture);
        }
        else
        {
            compareTo = currentType != typeof(string) ? Activator.CreateInstance(currentType) : ""; // Default value
            compareToInputField.SetTextWithoutNotify(compareTo.ToString());
        }

        // Save as new UDSCondition
        UDSCondition newCondition = new UDSCondition();
        newCondition.globalPropertyKey = gpKey;
        newCondition.operation = op;
        newCondition.compareTo = compareTo;

        currentConditional.SetCondition(newCondition); // !
    }

    private void InitGlobalPropertyDropdown(UDSCondition condition)
    {
        // Get the names of all Global Properties
        string[] gpKeys = new string[EditorManager.globalProperties.Keys.Count];
        EditorManager.globalProperties.Keys.CopyTo(gpKeys, 0);

        /* Generate a dropdown menu option for each Global Property
         * and keep track of the index of the currently selected Property */
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        int selectedIndex = 0;
        for (int i = 0; i < gpKeys.Length; i++)
        {
            string key = gpKeys[i];
            options.Add(new TMP_Dropdown.OptionData(key));
            if (key.Equals(condition.globalPropertyKey))
                selectedIndex = i;
        }

        // Populate the dropdown menu
        globalPropertyDropdown.options = options;
        globalPropertyDropdown.value = selectedIndex;

        // Save when something is changed
        globalPropertyDropdown.onValueChanged.AddListener(
            (val) =>
            {
                // Important
                UpdateOnGVChange(globalPropertyDropdown.options[val].text);

                SaveCondition();
            });
    }

    private void InitOperatorDropdown(UDSCondition condition)
    {
        // Get the string representation of all operators on the type
        string[] operators = new string[typeToOperators[currentType].Keys.Count];
        typeToOperators[currentType].Keys.CopyTo(operators, 0);

        /* Generate a dropdown menu option for each operator
         * and keep track of the index of the currently selected operator */
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        int selectedIndex = 0;
        for (int i = 0; i < operators.Length; i++)
        {
            string op = operators[i];
            options.Add(new TMP_Dropdown.OptionData(op));
            if (op.Equals(condition.operation))
                selectedIndex = i;
        }

        // Populate the dropdown menu
        operatorDropdown.options = options;
        operatorDropdown.value = selectedIndex;

        // Save when something is changed
        operatorDropdown.onValueChanged.AddListener(
        (val) =>
        {
            SaveCondition();
        });
    }

    private void InitCompareToInputField(UDSCondition condition)
    {
        // Input Field not needed if condition/Global Property is of type bool
        if (currentType == typeof(bool))
        {
            compareToInputField.gameObject.SetActive(false);
            return;
        }

        // Set content type of input field based on type of condition/Global Property
        if (currentType == typeof(string))
            compareToInputField.contentType = TMP_InputField.ContentType.Alphanumeric;
        else if (currentType == typeof(int))
            compareToInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        else if (currentType == typeof(float))
            compareToInputField.contentType = TMP_InputField.ContentType.DecimalNumber;

        // Update input field text
        compareToInputField.SetTextWithoutNotify(condition.compareTo.ToString());

        // Save when something is changed
        compareToInputField.onValueChanged.AddListener(
            (input) =>
            {
                SaveCondition();
            });
    }

    private void UpdateOnGVChange(string newGlobalPropertyKey, bool reInitElements = true)
    {
        UDSProperty newGlobalProperty = EditorManager.globalProperties[newGlobalPropertyKey];

        string[] operators = new string[typeToOperators[newGlobalProperty.type].Keys.Count];
        typeToOperators[newGlobalProperty.type].Keys.CopyTo(operators, 0);

        // Update the condition (!) - Operation and compareTo are set to default values
        currentType = newGlobalProperty.type;
        currentConditional.SetCondition(new UDSCondition()
        {
            globalPropertyKey = newGlobalPropertyKey,
            operation = operators[0], // Default value
            compareTo = currentType != typeof(string) ? Activator.CreateInstance(currentType) : "" // Default value
        });

        // Important
        operatorDropdown.ClearOptions();
        compareToInputField.SetTextWithoutNotify("");

        if (reInitElements)
        {
            // Reinitialize everything
            UDSCondition condition = currentConditional.GetCondition();
            InitOperatorDropdown(condition);
            InitCompareToInputField(condition);
        }
    }

    private void InitCondition()
    {
        /* Get the names of all Global Properties.
         * Note that the ConditionMenu is only being 
         * opened if there are Global Properties */
        string[] gpKeys = new string[EditorManager.globalProperties.Keys.Count];
        EditorManager.globalProperties.Keys.CopyTo(gpKeys, 0);

        string gpKey = gpKeys[0];

        // Sets default values for the operator and Compare To
        UpdateOnGVChange(gpKey, false);
    }
}
