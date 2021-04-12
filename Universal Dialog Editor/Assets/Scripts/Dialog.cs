using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Dialog : DialogComponent
{
    public DialogPart[] dialogParts;
    public string startDialogPart;

    public Dialog(string dialogID)
        : base(dialogID)
    {
        dialogParts = new DialogPart[0];
        startDialogPart = "";
    }

    internal Dialog Clone()
    {
        return (Dialog)MemberwiseClone();
    }

    [Serializable]
    public class DialogPart : DialogComponent
    {
        public Answer[] answers;

        public string nextDialogPartID;

        internal int visualX, visualY;

        public DialogPart(string dialogPartID, Vector2 visualPos)
            : base(dialogPartID) 
        { 
            answers = new Answer[0];
            this.visualX = (int) visualPos.x;
            this.visualY = (int) visualPos.y;
        }

        [Serializable]
        public class Answer : DialogComponent
        {
            int index;

            public string nextDialogPartID;

            public Answer(string answerID, int answerIndex)
                : base(answerID) 
            { 
                index = answerIndex; 
            }
        }
    }
}

[Serializable]
public class DialogComponent
{
    public string id;

    private readonly Dictionary<string, (object value, Type type)> properties;

    public DialogComponent(string dialogComponentID)
    {
        id = dialogComponentID;
        properties = new Dictionary<string, (object value, Type type)>();
    }

    public bool HasProperty(string key)
        => properties.ContainsKey(key);

    public bool HasProperty(string key, Type type)
        => properties.ContainsKey(key) && properties[key].type == type;

    public T GetProperty<T>(string key)
    {
        (object value, Type type) valueRaw = ("", typeof(string));
        if (properties.TryGetValue(key, out valueRaw))
        {
            if (valueRaw.type != typeof(T))
                throw new UDSException
                    (string.Format(UDSException.msg2, key, id, typeof(T).ToString()));

            T value = (T)valueRaw.value;

            return value;
        }
        else
            throw new UDSException(string.Format(UDSException.msg1, id, "int", key));
    }

    /*public string GetStringProperty(string key)
    {
        (string value, Type type) value = ("", typeof(string));
        if (properties.TryGetValue(key, out value))
            return value.value;
        else
            throw new UDSException(string.Format(UDSException.msg1, id, "string", key));
    }

    public int GetIntProperty(string key)
    {
        (string value, Type type) valueStr = ("", typeof(string));
        if (properties.TryGetValue(key, out valueStr))
        {
            if (valueStr.type != typeof(int))
                throw new UDSException(string.Format(UDSException.msg2, key, id, "int"));

            int value = 0;
            if (int.TryParse(valueStr.value, out value))
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
        (string value, Type type) valueStr = ("", typeof(string));
        if (properties.TryGetValue(key, out valueStr))
        {
            if (valueStr.type != typeof(float))
                throw new UDSException(string.Format(UDSException.msg2, key, id, "float"));

            float value = 0.0f;
            if (float.TryParse(valueStr.value, out value))
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
        (string value, Type type) valueStr = ("", typeof(string));
        if (properties.TryGetValue(key, out valueStr))
        {
            if (valueStr.type != typeof(bool))
                throw new UDSException(string.Format(UDSException.msg2, key, id, "bool"));

            bool value = false;
            if (bool.TryParse(valueStr.value, out value))
            {
                return value;
            }
            else
                throw new UDSException(string.Format(UDSException.msg2, key, id, "bool"));
        }
        else
            throw new UDSException(string.Format(UDSException.msg1, id, "bool", key));
    }*/

    internal void SetStringProperty(string key, string value)
    {
        properties[key] = (value, typeof(string));
    }

    internal void SetIntProperty(string key, int value)
    {
        properties[key] = (value.ToString(), typeof(int));
    }

    internal void SetFloatProperty(string key, float value)
    {
        properties[key] = (value.ToString(), typeof(float));
    }

    internal void SetBoolProperty(string key, bool value)
    {
        properties[key] = (value.ToString(), typeof(bool));
    }
}
