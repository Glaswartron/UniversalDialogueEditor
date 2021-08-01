using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UniversalDialogueSystem;


[Serializable]
public struct UDSCondition
{
    [JsonProperty] public string globalPropertyKey { get; set; }
    [JsonProperty] public string operation { get; set; }
    [JsonProperty] public object compareTo { get; set; }

    public bool IsMet()
    {
        if (!UDSDialogueManager.instance.HasGlobalProperty(globalPropertyKey))
            return false;

        UDSProperty globalProperty = UDSDialogueManager.instance.GetGlobalProperty(globalPropertyKey);

        return typeToOperators[globalProperty.type][operation]
            .Invoke(globalProperty.value, compareTo);
    }

    private static readonly Dictionary<Type, Dictionary<string, Func<object, object, bool>>>
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

    public override string ToString()
    {
        return $"{globalPropertyKey} {operation} {compareTo.ToString()}";
    }
}

