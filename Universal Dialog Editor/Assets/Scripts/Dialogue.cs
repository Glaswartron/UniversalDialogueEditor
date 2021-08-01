using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.ComponentModel;


[Serializable]
public struct UDSProperty
{
    [JsonProperty] public object value { get; set; }
    [JsonProperty] public Type type { get; set; }
    [JsonProperty] public bool required { get; set; }

    public UDSProperty(object value, Type type, bool required = false)
    {
        this.value = value;
        this.type = type;
        this.required = required;
    }
}

[Serializable]
public sealed class Dialogue : DialogueComponent, ICloneable
{
    [JsonProperty] public DialoguePart[] dialogueParts { get; set; }
    [JsonProperty] public string startDialoguePartID { get; set; }

    [JsonConstructor]
    public Dialogue() { }

    internal Dialogue(string dialogueID)
        : base(dialogueID)
    {
        dialogueParts = new DialoguePart[0];
        startDialoguePartID = "";

        SetProperty<bool>("Pause during Dialogue", true, required: true);
    }

    public object Clone()
    {
        Dialogue copy = new Dialogue(this.id);

        foreach (string key in this.GetPropertyKeys())
        {
            UDSProperty val = this.GetProperty(key);

            copy.SetProperty(key, val.value, val.type);
        }

        List<DialoguePart> diaParts = new List<DialoguePart>();
        foreach (DialoguePart dp in this.dialogueParts)
            diaParts.Add(dp.Copy(copy));
        copy.dialogueParts = diaParts.ToArray();

        copy.startDialoguePartID = this.startDialoguePartID;

        return copy;
    }

    [Serializable]
    public sealed class DialoguePart : DialogueComponent
    {
        [JsonProperty] public Answer[] answers { get; set; }

        [JsonProperty] public string nextDialoguePartID { get; set; }

        [JsonProperty] internal float visualX;
        [JsonProperty] internal float visualY;

        [JsonConstructor]
        public DialoguePart() { }

        internal DialoguePart(string dialoguePartID, Vector2 visualPos)
            : base(dialoguePartID)
        {
            answers = new Answer[0];
            visualX = visualPos.x;
            visualY = visualPos.y;

            SetProperty("Text", "", required: true);
            SetProperty("Name", "", required: true);
            SetProperty("Text speed", 1.0f, required: true);
        }

        internal DialoguePart Copy(Dialogue dialogue)
        {
            DialoguePart copy = new DialoguePart
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

            copy.nextDialoguePartID = this.nextDialoguePartID;

            return copy;
        }

        [Serializable]
        public sealed class Answer : DialogueComponent
        {
            [JsonProperty] public int index { get; set; }

            [JsonProperty] public string nextDialoguePartID { get; set; }

            [JsonProperty] public bool conditional { get; set; }
            [JsonProperty] public UDSCondition? condition { get; set; }

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

            internal Answer Copy(DialoguePart dialoguePart)
            {
                Answer copy = new Answer(this.id, this.index,
                    this.angle, this.conditional, this.condition);

                foreach (string key in this.GetPropertyKeys())
                {
                    UDSProperty val = this.GetProperty(key);

                    copy.SetProperty(key, val.value, val.type);
                }

                copy.nextDialoguePartID = this.nextDialoguePartID;

                return copy;
            }
        }
    }
}

[Serializable]
public class DialogueComponent
{
    [JsonProperty] public string id { get; set; }

    [JsonProperty] private readonly Dictionary<string, UDSProperty> properties;

    [JsonConstructor]
    public DialogueComponent() { }

    internal DialogueComponent(string dialogueComponentID)
    {
        id = dialogueComponentID;
        properties = new Dictionary<string, UDSProperty>();
    }

    public bool HasProperty(string key)
        => properties.ContainsKey(key);

    public bool HasProperty<T>(string key)
    => properties.ContainsKey(key) && properties[key].type == typeof(T);

    public bool HasProperty(string key, Type type)
        => properties.ContainsKey(key) && properties[key].type == type;

    public T GetProperty<T>(string key)
    {
        UDSProperty valueRaw = default;
        if (properties.TryGetValue(key, out valueRaw))
        {
            if (valueRaw.type != typeof(T))
                throw new UDSException
                    (string.Format(UDSException.msg3, key, id, typeof(T).ToString()));


            T value = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(valueRaw.value.ToString());

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

