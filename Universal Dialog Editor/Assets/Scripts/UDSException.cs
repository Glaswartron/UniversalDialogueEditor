using System;


public class UDSException : Exception
{
    internal static readonly string msg1
          = "DialogComponent {0} does not contain " +
            "a {1} Property with key '{2}'. Consider checking " +
            "DialogComponent.HasProperty() before querying the Property";

    internal static readonly string msg2
          = "DialogComponent {0} does not contain " +
            "a Property with key '{1}'. Consider checking " +
            "DialogComponent.HasProperty() before querying the Property";

    internal static readonly string msg3
              = "Property with key {0} of DialogComponent {1} exists but can not " +
                "be converted to {2}. Consider checking its data type.";

    internal static readonly string msg4
        = "Global Property of type {0} with key {1} does not exist. Consider checking " +
        "UDSDialogManager.HasGlobalProperty() before querying the Property";

    internal static readonly string msg5
              = "Global Property with key {0} exists but can not " +
                "be converted to {1}. Consider checking its data type.";

    internal static readonly string msg6
        = "There is no Global Property with key {0} Consider checking " +
          "UDSDialogManager.HasGlobalProperty() before querying the Property";

    public UDSException(string message)
        : base(message) { }
}
