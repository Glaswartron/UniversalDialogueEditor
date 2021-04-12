using System;

public class UDSException : Exception
{
    internal static readonly string msg1
          = "DialogComponent {0} does not contain " +
            "a {1} property with key '{2}'. Consider checking " +
            "DialogComponent.HasProperty() before querying the property";

    internal static readonly string msg2
              = "Property with key {0} of DialogComponent {1} exists but can not " +
                "be converted to {2}. Consider checking its data type.";

    public UDSException(string message)
        : base(message) { }
}
