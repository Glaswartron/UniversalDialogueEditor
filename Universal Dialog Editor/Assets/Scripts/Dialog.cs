using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

[Serializable]
public struct UDSProperty
{
    public object value;
    public Type type;
    public bool required;

    public UDSProperty(object value, Type type, bool required = false)
    {
        this.value = value;
        this.type = type;
        this.required = required;
    }
}

[Serializable]
public sealed class Dialog : DialogComponent, ICloneable
{
    public DialogPart[] dialogParts;
    public string startDialogPartID;

    [JsonConstructor]
    public Dialog() { }

    internal Dialog(string dialogID)
        : base(dialogID)
    {
        dialogParts = new DialogPart[0];
        startDialogPartID = "";

        SetProperty<bool>("Pause during Dialog", true, required: true);
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
        //[SerializeReference] public Dialog dialog;

        public string nextDialogPartID;

        [JsonProperty] internal int visualX;
        [JsonProperty] internal int visualY;

        [JsonConstructor]
        public DialogPart() { }

        internal DialogPart(string dialogPartID, Vector2 visualPos)
            : base(dialogPartID) 
        { 
            answers = new Answer[0];
            visualX = (int) visualPos.x;
            visualY = (int) visualPos.y;

            //this.dialog = dialog;

            SetProperty("Text", "", required: true);
            SetProperty("Text speed", 1f, required: true);
        }

        internal DialogPart Copy(Dialog dialog)
        {
            DialogPart copy = new DialogPart
                (this.id, new Vector2(this.visualX, this.visualY));

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
            //[SerializeReference] public DialogPart dialogPart;

            public string nextDialogPartID;

            public bool conditional;
            public UDSCondition? condition;

            [JsonProperty] internal float angle;

            [JsonConstructor]
            public Answer() { }

            internal Answer(string answerID, int answerIndex, 
                 float angle, bool conditional = false, UDSCondition? condition = null)
                : base(answerID) 
            { 
                index = answerIndex;

                this.conditional = conditional;
                this.condition = condition;

                this.angle = angle;

                SetProperty("Text", "", required: true);
            }

            internal Answer Copy(DialogPart dialogPart)
            {
                Answer copy = new Answer(this.id, this.index,
                    this.angle, this.conditional, this.condition);

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

    [JsonProperty] private readonly Dictionary<string, UDSProperty> properties;

    [JsonConstructor]
    public DialogComponent () { }

    internal DialogComponent(string dialogComponentID)
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

    internal bool SetProperty<T>(string key, T value, bool required = false)
    {
        bool alreadyThere = HasProperty(key, typeof(T));

        properties[key] = new UDSProperty(value, typeof(T), required);

        return alreadyThere;
    }

    internal bool SetProperty(string key, object value, Type type, bool required = false)
    {
        bool alreadyThere = HasProperty(key, type);

        properties[key] = new UDSProperty(value, type, required);

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
        if (!GetProperty(key).required)
            return properties.Remove(key);
        else
        {
            Debug.LogWarning("Called DeleteProperty on a required Property");
            return false;
        }
    }

    internal void DeleteAllProperties()
    {
        foreach (string property in GetPropertyKeys())
            DeleteProperty(property);
    }
}
