using System;

[Serializable]
public class DialogOld
{
    public string id;
    public bool revealTextGradually = true;
    public DialogPart[] dialogParts;

    [Serializable]
    public class DialogPart
    {
        public string id;
        public string nextPartID;
        public string name;
        public string nameDE;
        public string text;
        public string textDE;
        public Answer[] answers;
        public string gameVariable;
        public string gvValue;
        public string itemID;
        public string itemAmount;
        public string cutsceneToStartID;

        public float nodeX;
        public float nodeY;
    }

    [Serializable]
    public class Answer
    {
        public string text;
        public string textDE;
        public string nextPartID;
        public bool opensShop;
        public string gameVariable;
        public string gvValue;
        public string cutsceneToStartID;
    }
}
