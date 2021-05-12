using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Condition
{
    public Type type;
}

public class IConditional : MonoBehaviour
{
    public readonly Dictionary<Type, Dictionary<string, Func<object, object, bool>>> typeToOperators;
}
