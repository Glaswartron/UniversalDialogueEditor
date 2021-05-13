using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
    public TMP_Dropdown globalVariableDropdown;
    public TMP_Dropdown operatorDropdown;
    public TMP_InputField compareToInputField;

    // Start is called before the first frame update
    void Start()
    {

    }

    public void Init(IConditional conditional)
    {
        UDSCondition condition = conditional.GetCondition();

        InitGlobalVariableDropdown(condition);
        InitOperatorDropdown(condition);
        InitCompareToInputField(condition);
    }

    private void InitGlobalVariableDropdown(UDSCondition condition)
    {
        // Get the names of all Global Properties
        string[] gpKeys = new string[EditorManager.globalProperties.Keys.Count];
        EditorManager.globalProperties.Keys.CopyTo(gpKeys, 0);

        /* Generate a dropdown menu option for each Global Property
         * and keep track of the index of the currently selected Property */
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        int selectedIndex = 0;
        for (int i = 0; i < gpKeys.Length; i++) {
            string key = gpKeys[i];
            options.Add(new TMP_Dropdown.OptionData(key));
            if (key.Equals(condition.globalPropertyKey))
                selectedIndex = i;
        }

        // Populate the dropdown menu
        globalVariableDropdown.options = options;
        globalVariableDropdown.value = selectedIndex;
    }

    public void InitOperatorDropdown(UDSCondition condition)
    {
        // TODO
    }

    public void InitCompareToInputField(UDSCondition condition)
    {
        // TODO
    }
}
