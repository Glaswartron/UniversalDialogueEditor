using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public struct UDSProperty
{
    public object value;
    public Type type;

    public UDSProperty(object value, Type type)
    {
        this.value = value;
        this.type = type;
    }
}

[Serializable]
public sealed class Dialog : DialogComponent, ICloneable
{
    public DialogPart[] dialogParts;
    public string startDialogPartID;

    internal Dialog(string dialogID)
        : base(dialogID)
    {
        dialogParts = new DialogPart[0];
        startDialogPartID = ""; 
    }

    public object Clone()
    {
        Dialog copy = new Dialog(this.id);

        foreach (string key in this.GetPropertyKeys())
        {
            UDSProperty val = this.GetProperty(key);

            copy.SetProperty(key, val.value, val.type);
        }

        List<DialogPart> diaParts = new List<DialogPart>();
        foreach (DialogPart dp in this.dialogParts)
            diaParts.Add(dp.Copy(copy));
        copy.dialogParts = diaParts.ToArray();

        copy.startDialogPartID = this.startDialogPartID;

        return copy;
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

        internal DialogPart Copy(Dialog dialog)
        {
            DialogPart copy = new DialogPart
                (this.id, new Vector2(this.visualX, this.visualY), dialog);

            foreach (string key in this.GetPropertyKeys())
            {
                UDSProperty val = this.GetProperty(key);

                copy.SetProperty(key, val.value, val.type);
            }

            List<Answer> ans = new List<Answer>();
            foreach (Answer a in this.answers)
                ans.Add(a.Copy(this));
            copy.answers = ans.ToArray();

            copy.nextDialogPartID = this.nextDialogPartID;

            return copy;
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

            internal Answer Copy(DialogPart dialogPart)
            {
                Answer copy = new Answer(this.id, this.index, dialogPart);

                foreach (string key in this.GetPropertyKeys())
                {
                    UDSProperty val = this.GetProperty(key);

                    copy.SetProperty(key, val.value, val.type);
                }

                copy.nextDialogPartID = this.nextDialogPartID;

                return copy;
            }
        }
    }
}

[Serializable]
public class DialogComponent
{
    public string id;

    [SerializeField]
    private readonly Dictionary<string, UDSProperty> properties;

    public DialogComponent(string dialogComponentID)
    {
        id = dialogComponentID;
        properties = new Dictionary<string, UDSProperty>();
    }

    public bool HasProperty(string key)
        => properties.ContainsKey(key);

    public bool HasProperty(string key, Type type)
        => properties.ContainsKey(key) && properties[key].type == type;

    public T GetProperty<T>(string key)
    {
        UDSProperty valueRaw = default;
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

    public UDSProperty GetProperty(string key)
    {
        UDSProperty value;
        if (properties.TryGetValue(key, out value))
        {
            return value;
        }
        else
            throw new UDSException(string.Format(UDSException.msg2, id, key));
    }

    public string[] GetPropertyKeys()
    {
        return properties.Keys.ToArray();
    }

    internal Dictionary<string, UDSProperty> GetProperties()
    {
        return properties;
    }

    internal bool SetProperty<T>(string key, T value)
    {
        bool alreadyThere = HasProperty(key, typeof(T));

        properties[key] = new UDSProperty(value, typeof(T));

        return alreadyThere;
    }

    internal bool SetProperty(string key, object value, Type type)
    {
        bool alreadyThere = HasProperty(key, type);

        properties[key] = new UDSProperty(value, type);

        return alreadyThere;
    }

    internal bool UpdateProperty(string previousKey, string newKey, object newValue, Type type)
    {
        bool alreadyThere = !DeleteProperty(previousKey);

        if (alreadyThere)
        {
            Debug.LogWarning("Called UpdateProperty with key " + previousKey + " although that " +
                "property doesn't exist");
        }

        SetProperty(newKey, newValue, type);

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
