using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public sealed class Dialog : DialogComponent
{
    public DialogPart[] dialogParts;
    public string startDialogPart;

    internal Dialog(string dialogID)
        : base(dialogID)
    {
        dialogParts = new DialogPart[0];
        startDialogPart = ""; 
    }

    [Serializable]
    public sealed class DialogPart : DialogComponent
    {
        public Answer[] answers;
        [SerializeReference] 
        public Dialog dialog;

        public string nextDialogPartID;

        internal int visualX, visualY;

        internal DialogPart(string dialogPartID, Vector2 visualPos, Dialog dialog)
            : base(dialogPartID) 
        { 
            answers = new Answer[0];
            visualX = (int) visualPos.x;
            visualY = (int) visualPos.y;

            this.dialog = dialog;

            SetProperty("Text", "");
            SetProperty("Text speed", 1);
        }

        [Serializable]
        public sealed class Answer : DialogComponent
        {
            int index;
            [SerializeReference]
            public DialogPart dialogPart;

            public string nextDialogPartID;

            internal Answer(string answerID, int answerIndex, DialogPart dialogPart)
                : base(answerID) 
            { 
                index = answerIndex;
                this.dialogPart = dialogPart;

                SetProperty("Text", "");
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
            throw new UDSException
                (string.Format(UDSException.msg1, id, typeof(T).ToString(), key));
    }

    public (object value, Type type) GetProperty(string key)
    {
        (object value, Type type) value;
        if (properties.TryGetValue(key, out value))
        {
            return value;
        }
        else
            throw new UDSException(string.Format(UDSException.msg1, id, key));
    }

    public string[] GetPropertyKeys()
    {
        return properties.Keys.ToArray();
    }

    internal bool SetProperty<T>(string key, T value)
    {
        bool alreadyThere = HasProperty(key, typeof(T));

        properties[key] = (value, typeof(T));

        return alreadyThere;
    }

    internal bool SetProperty(string key, object value, Type type)
    {
        bool alreadyThere = HasProperty(key, type);

        properties[key] = (value, type);

        return alreadyThere;
    }

    internal bool DeleteProperty(string key)
    {
        return properties.Remove(key);
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
    }

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
    } */
}
