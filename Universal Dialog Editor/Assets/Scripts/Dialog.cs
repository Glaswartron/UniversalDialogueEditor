using System;
using System.Collections.Generic;

[Serializable]
public class Dialog
{
    public readonly string dialogID;

    public DialogPart[] dialogParts;
    public string startDialogPart;

    public Dialog(string id)
    {
        dialogID = id;
    }

    [Serializable]
    public class DialogPart : DialogComponent
    {
        public Answer[] answers;

        public DialogPart(string dialogPartID)
            : base(dialogPartID) { }

        [Serializable]
        public class Answer : DialogComponent
        {
            public Answer(string answerIndex)
                : base(answerIndex) { }
        }
    }

    [Serializable]
    public class DialogComponent
    {
        public string id;

        public string nextDialogPartID;

        private readonly Dictionary<string, string> properties;

        public DialogComponent(string dialogComponentID)
        {
            id = dialogComponentID;
            properties = new Dictionary<string, string>();
        }

        public bool HasProperty(string key)
            => properties.ContainsKey(key);

        public string GetStringProperty(string key)
        {
            string value = "";
            if (properties.TryGetValue(key, out value))
                return value;
            else
                throw new UDSException(string.Format(UDSException.msg1, id, "string", key));
        }

        public int GetIntProperty(string key)
        {
            string valueStr = "";
            if (properties.TryGetValue(key, out valueStr))
            {
                int value = 0;
                if (int.TryParse(valueStr, out value))
                {
                    return value;
                }
                else
                    throw new UDSException(string.Format(UDSException.msg2, key, id, "int"));
            }
            else
                throw new UDSException(string.Format(UDSException.msg1, id, "int", key));
        }

        public float GetFloatProperty(string key)
        {
            string valueStr = "";
            if (properties.TryGetValue(key, out valueStr))
            {
                float value = 0.0f;
                if (float.TryParse(valueStr, out value))
                {
                    return value;
                }
                else
                    throw new UDSException(string.Format(UDSException.msg2, key, id, "float"));
            }
            else
                throw new UDSException(string.Format(UDSException.msg1, id, "float", key));
        }

        public bool GetBoolProperty(string key)
        {
            string valueStr = "";
            if (properties.TryGetValue(key, out valueStr))
            {
                bool value = false;
                if (bool.TryParse(valueStr, out value))
                {
                    return value;
                }
                else
                    throw new UDSException(string.Format(UDSException.msg2, key, id, "bool"));
            }
            else
                throw new UDSException(string.Format(UDSException.msg1, id, "bool", key));
        }

        internal void SetStringProperty(string key, string value)
        {
            properties[key] = value;
        }

        internal void SetIntProperty(string key, int value)
        {
            properties[key] = value.ToString();
        }

        internal void SetFloatProperty(string key, float value)
        {
            properties[key] = value.ToString();
        }

        internal void SetBoolProperty(string key, bool value)
        {
            properties[key] = value.ToString();
        }
    }
}
