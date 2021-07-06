using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Newtonsoft.Json;
using System.IO;

namespace UniversalDialogSystem
{
    public class UDSDialogManager : MonoBehaviour
    {
        #region Structs and Enums

        [Serializable]
        internal struct TextBox
        {
            public GameObject gameObject;
            public Text text;
            public TMP_Text textTMP;
        }

        [Serializable]
        internal struct AnswerBox
        {
            public TextBox textBox;
            public Button button;
        }

        private struct RichTextContext
        {
            //public int writeIndex;
            public int startIndex;
            public int endIndex;
            public int resumeOffset;
        }

        [Serializable]
        internal enum Platform
        {
            DESKTOP, MOBILE
        }

        [Serializable]
        internal enum LoadMode
        {
            LOAD_ON_START, LOAD_ON_DEMAND
        }
        #endregion

        #region Variables
        [HideInInspector] public static UDSDialogManager instance; // Singleton

        [Header("Important General Settings")]
        [SerializeField] internal Platform platform = Platform.DESKTOP;
        [SerializeField] internal bool useTextMeshPro = false;

        [Header("UI")]
        [SerializeField] internal GameObject dialogUI;
        [SerializeField] internal TextBox dialogTextBox;
        [SerializeField] internal AnswerBox[] answerTextBoxes;
        [SerializeField] internal TextBox nameTextBox;
        [SerializeField] internal GameObject[] deactivateDuringDialog;

        [Header("Input")]
        [SerializeField] internal KeyCode[] interactionKeys = new KeyCode[] { KeyCode.Mouse0 };

        [Header("Technical options - Only adjust when needed")]
        [SerializeField] internal LoadMode loadMode = LoadMode.LOAD_ON_START;
        [SerializeField] internal float minTimeBetweenTouches = 0.35f;
        [SerializeField] internal float standardTimeScale = 1f;

        [HideInInspector] public bool dialogRunning;

        internal Dialog currentDialog = null;
        internal string currentName;

        /// <summary>
        /// Very important! Stores all the dialogs loaded from Resources
        /// </summary>
        private Dialog[] dialogs;

        private Dialog.DialogPart currentDialogPart;

        /// <summary>
        /// // Whether there are no answers in currentDialogPart
        /// </summary>
        private bool noAnswers = false;

        /// <summary>
        /// Whether text is being played gradually right now
        /// </summary>
        private bool textEffectRunning = false;

        private float lastTouchTimestamp = Mathf.Infinity;
        private bool justStarted = false;

        private bool[] deactivateDuringDialogObjectsToReactivate;

        // Coroutines
        private Coroutine revealTextGraduallyCo;
        private Coroutine revealFormattedTextGraduallyCo;
        #endregion

        #region Properties
        protected string CurrentDialogPartText
        {
            get
            {
                return currentDialogPart.GetProperty<string>("Text");
            }
        }

        protected string CurrentDialogPartName
        {
            get
            {
                if (currentDialogPart.HasProperty("Name"))
                    return currentDialogPart.GetProperty<string>("Name");
                else // TODO: Shouldn't happen in the final version
                    return null;
            }
        }
        #endregion

        void Start()
        {
            // Singleton - Only one instance at a time
            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);

            if (loadMode == LoadMode.LOAD_ON_START)
                dialogs = LoadDialogs();
        }

        private void Update()
        {
            if (dialogRunning)
            {
                if (!justStarted)
                {
                    if (platform == Platform.DESKTOP)
                    {
                        ProcessMouseInput();
                    }

                    if (platform == Platform.MOBILE)
                    {
                        // To avoid registering a touch more than once
                        if (Time.realtimeSinceStartup - lastTouchTimestamp > minTimeBetweenTouches)
                            ProcessTouchInput();
                    }
                }
                else justStarted = false;
            }
        }

        private void ProcessTouchInput()
        {
            if (Input.touchCount > 0) // One Touch!
                // Another check to avoid registering a touch twice
                if (Input.GetTouch(0).phase == TouchPhase.Began)
                    /* Only proceed if the touch is not above a button
                     * (ideally dialogTextBox shouldn't be a raycast target) */
                    if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    {
                        lastTouchTimestamp = Time.realtimeSinceStartup;

                        UpdateDialog();
                    }
        }

        private void ProcessMouseInput()
        {
            if (interactionKeys.Any(k => Input.GetKeyDown(k)))
            {
                /* Only proceed if the pointer is not above a button
                 * (ideally dialogTextBox shouldn't be a raycast target) */
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    UpdateDialog();
                }
            }
        }

        /// <summary>
        /// Called as part of the Update loop whenever the player 
        /// touches the screen or presses the interactionKey during
        /// a dialog. Moves the dialog into it's next state, which
        /// means either
        /// going to the next DialogPart,
        /// showing the entirety of the text at once (stopping revealing),
        /// or ending the dialog.
        /// </summary>
        public void UpdateDialog()
        {
            // Text is currently being shown gradually (effect is running)
            if (textEffectRunning)
            {
                // Stop the effect
                //StopAllCoroutines();
                if (revealTextGraduallyCo != null) StopCoroutine(revealTextGraduallyCo);
                if (revealFormattedTextGraduallyCo != null) StopCoroutine(revealFormattedTextGraduallyCo);

                textEffectRunning = false;

                // Show text instantly
                string text = currentDialogPart.GetProperty<string>("Text");
                SetTextOnTextBox(dialogTextBox, text);

                return;
            }

            /* No answers -> Go to next dialog part OR finish dialog 
             * (otherwise done by clicking on answers) */
            if (noAnswers)
            {
                var allDiaParts = currentDialog.dialogParts;

                // Check whether this is the last DialogPart
                if (string.IsNullOrWhiteSpace(currentDialogPart.nextDialogPartID))
                {
                    FinishDialog();
                    return;
                }

                // Continue to the next DialogPart
                GoThroughDialogPart(
                    currentDialog.dialogParts.Where(
                        dp => dp.id.Equals(currentDialogPart.nextDialogPartID)).First());
            }
        }

        /// <summary>
        /// Starts a Dialog. Enables the dialogUI and sets Time.timeScale to 0 
        /// (= pauses the game), if the "Pause during Dialog" Property is 
        /// set to true on the Dialog.
        /// </summary>
        public void StartDialog(string dialogID)
        {
            Dialog dialog = null;
            if (loadMode == LoadMode.LOAD_ON_START)
                dialog = dialogs.Where(d => d.id.Equals(dialogID)).First();
            else
                dialog = LoadDialog(dialogID);

            if (dialog != null)
            {
                StartDialog(dialog);

                justStarted = true;

                deactivateDuringDialogObjectsToReactivate = new bool[deactivateDuringDialog.Length];
                for (int i = 0; i < deactivateDuringDialog.Length; i++)
                {
                    if (deactivateDuringDialog[i].activeSelf) // Only if it is active
                    {
                        deactivateDuringDialog[i].SetActive(false);

                        // Schedule for reactivation after the Dialog
                        deactivateDuringDialogObjectsToReactivate[i] = true;
                    }
                }
            }
            else
                Debug.LogWarning("Dialog with ID " + dialogID + " was started but " +
                    "couldn't be found! Try checking the spelling on the ID and " +
                    "whether you actually imported it into Resources/Dialogs");
        }

        /// <summary>
        /// Starts a Dialog. Enables the dialogUI and sets Time.timeScale to 0 
        /// (= pauses the game), if the "Pause during Dialog" Property is 
        /// set to true on the Dialog. Called by StartDialog(dialogID).
        /// This method can and should be overriden to introduce your
        /// own custom functionalities like disabling the other game UI
        /// during the Dialog!
        /// </summary>
        protected virtual void StartDialog(Dialog dialog)
        {
            currentDialog = dialog; // !

            if (dialog.GetProperty<bool>("Pause during Dialog"))
                Time.timeScale = 0f;

            dialogRunning = true;

            dialogUI.SetActive(true);

            // The start DialogPart is being played
            GoThroughDialogPart(
                currentDialog.dialogParts.Where(
                    dp => dp.id.Equals(currentDialog.startDialogPartID)).First());
        }

        /// <summary>
        /// Finishes the current Dialog and resets all the UI
        /// </summary>
        public void FinishDialog()
        {
            foreach (AnswerBox answerBox in answerTextBoxes)
            {
                SetTextOnTextBox(answerBox.textBox, "");

                answerBox.button.onClick.RemoveAllListeners(); // !

                answerBox.textBox.gameObject.SetActive(false);
            }

            dialogRunning = false;

            dialogUI.SetActive(false);

            for (int i = 0; i < deactivateDuringDialogObjectsToReactivate.Length; i++)
            {
                if (deactivateDuringDialogObjectsToReactivate[i])
                    deactivateDuringDialog[i].SetActive(true);
            }

            if (currentDialog.GetProperty<bool>("Pause during Dialog"))
                Time.timeScale = standardTimeScale;

            currentDialog = null;
        }

        /// <summary>
        /// Actually plays back a DialogPart 
        /// => Handles the text and answer boxes, sets all the
        ///    variables and provides interfaces (TODO)
        /// </summary>
        /// <param name="diaPart">Der DialogPart, der durchlaufen werden soll</param>
        private void GoThroughDialogPart(Dialog.DialogPart diaPart)
        {
            // To avoid "overlapping" coroutines
            if (revealTextGraduallyCo != null) StopCoroutine(revealTextGraduallyCo);
            if (revealFormattedTextGraduallyCo != null) StopCoroutine(revealFormattedTextGraduallyCo);
            textEffectRunning = false;

            // noAnswers is false by default
            noAnswers = false;

            // All answer boxes are inactive by default
            foreach (AnswerBox answerBox in answerTextBoxes)
                answerBox.textBox.gameObject.SetActive(false);

            currentDialogPart = diaPart; // !

            // Name box
            if (!string.IsNullOrWhiteSpace(CurrentDialogPartName))
                SetTextOnTextBox(nameTextBox, CurrentDialogPartName);
            else
                nameTextBox.gameObject.SetActive(false);

            // Show text!
            if (currentDialogPart.GetProperty<float>("Text speed") > 0) // with effect
                revealTextGraduallyCo = StartCoroutine(RevealTextGradually(CurrentDialogPartText));
            else // instantaneously
                SetTextOnTextBox(dialogTextBox, CurrentDialogPartText);

            // Answers
            int answerCount = diaPart.answers.Length;
            if (answerCount > 0) // Are there even answers
            {
                for (int i = 0; i < answerCount; i++)
                {
                    Dialog.DialogPart.Answer answer = diaPart.answers[i];

                    // Activate an AnswerBox for each Answer
                    var answerBox = answerTextBoxes[i];

                    SetTextOnTextBox(answerBox.textBox, answer.GetProperty<string>("Text"));

                    answerBox.button.onClick.RemoveAllListeners(); // !

                    int _i = i; // Important
                    answerBox.button.onClick.AddListener(
                        () =>
                        {
                            TakeAnswer(_i);
                        }
                    );

                    answerBox.textBox.gameObject.SetActive(true);
                }
            }
            else // No answers
                noAnswers = true;
        }

        /// <summary>
        /// Called by the Answer buttons whenever one is clicked.
        /// </summary>
        /// <param name="index">The index of the Answer that was chosen</param>
        private void TakeAnswer(int index)
        {
            Dialog.DialogPart.Answer answer = currentDialogPart.answers[index];

            // Check whether the end of the dialog was reached
            if (string.IsNullOrWhiteSpace(answer.nextDialogPartID))
            {
                FinishDialog();

                return;
            }

            // Continue to next Dialog Part
            GoThroughDialogPart(
                currentDialog.dialogParts.Where(
                    dp => dp.id.Equals(currentDialog.startDialogPartID)).First());
        }

        /// <summary>
        /// Der Text wird nach und nach mit Effekt aufgedeckt
        /// Enthält auch die Sonderbehandlung für Tags wie z.B. <color ...>
        /// </summary>
        /// <param name="baseText">Der Text der aufgedeckt werden soll</param>
        private IEnumerator RevealTextGradually(string baseText)
        {
            textEffectRunning = true; // !

            SetTextOnTextBox(dialogTextBox, "");
            float textRevealSpeed = currentDialogPart.GetProperty<float>("Text speed");

            // Global cursor that points to the index in the baseText where we're currently at
            int cursor = 0;

            Stack<RichTextContext> contexts = new Stack<RichTextContext>();

            RichTextContext baseContext = new RichTextContext
            {
                startIndex = 0,
                endIndex = baseText.Length - 1,
                resumeOffset = 0
            };
            contexts.Push(baseContext);

            RichTextContext currentContext;
            while (contexts.Count > 0)
            { 
                currentContext = contexts.Pop();

                // Advance the cursor if some tags where skipped for this context
                cursor = Mathf.Max(cursor, currentContext.startIndex);

                string newText;

                // Go through the text (in your context "frame")
                string _text = baseText.Substring(currentContext.startIndex,
                                                  currentContext.endIndex - currentContext.startIndex + 1);
                // Letter by letter, increment cursor!
                for (int i = 0; i < _text.Length; i++, cursor++) 
                {
                    char letter = _text[i];

                    if (letter == '<') // Might be the start of a tag
                    {
                        // Is there a '>' ahead to close the tag?
                        if (_text.Contains('>') && _text.IndexOf('>') > i)
                        {
                            // If so, identify the startTag (<...>) and endTag (</...>)

                            string startTag, endTag;

                            // startTag = From current letter (= '<') to next '>'
                            startTag = _text.Substring(i, _text.IndexOf(">") - i + 1);

                            // _text with everything before and including the startTag cut off
                            string _textFromEndOfStartTag = _text.Substring(_text.IndexOf(">") + 1);

                            // Is there a closing tag (</...>) ahead?
                            if (_textFromEndOfStartTag.Contains("</") && _textFromEndOfStartTag.Contains('>')
                                && _textFromEndOfStartTag.IndexOf('>') > _textFromEndOfStartTag.IndexOf("</"))
                            {
                                // If so, cut out that closing tag
                                endTag = _textFromEndOfStartTag.Substring
                                    (_textFromEndOfStartTag.IndexOf("</"),
                                     _textFromEndOfStartTag.IndexOf(">") - _textFromEndOfStartTag.IndexOf("</") - 1);

                                // Only what's in between the "< >" and "</ >" (with whitespaces removed)
                                string startTagContent = startTag.Replace("<", null).Replace(">", null).Replace(" ", null);
                                string endTagContent = endTag.Replace("</", null).Replace(">", null).Replace(" ", null);

                                // Just another check: If it's well-formed, startTagContent starts with endTagContent
                                if (startTagContent.StartsWith(endTagContent))
                                {
                                    // Write the tags to the text box, the actual text will go in between
                                    newText = useTextMeshPro
                                              ? dialogTextBox.textTMP.text + startTag + endTag
                                              : dialogTextBox.text.text + startTag + endTag;

                                    SetTextOnTextBox(dialogTextBox, newText);

                                    /* For the new RichTextContext, start after the startTag 
                                     * (in the original text) and write till before the end tag, 
                                     * then resume after the end tag when you leave the context */
                                    RichTextContext newContext = new RichTextContext
                                    {
                                        startIndex = cursor + startTag.Length,
                                        endIndex = baseText.IndexOf(_text) + _text.IndexOf(endTag)
                                                                           - 1,
                                        resumeOffset = endTag.Length + 1
                                    };

                                    contexts.Push(newContext);

                                    break; // Very important! We're heading to a new loop now
                                }
                            }
                        }
                    }

                    newText = useTextMeshPro
                        ? dialogTextBox.textTMP.text + baseText[cursor]
                        : dialogTextBox.text.text + baseText[cursor];

                    SetTextOnTextBox(dialogTextBox, newText);

                    if (currentDialog.GetProperty<bool>("Pause during Dialog"))
                        yield return new WaitForSecondsRealtime(1.035f - textRevealSpeed);
                    else
                        yield return new WaitForSeconds(1.035f - textRevealSpeed);
                }

                // Add the resumeOffset to the cursor when leaving the context
                if (cursor == currentContext.endIndex)
                    cursor += currentContext.resumeOffset;
            }

            textEffectRunning = false;
        }

        /// <summary>
        /// Coroutine. Gibt ein Stück Text mit Rich-Text-Formatierung darin Stück 
        /// für Stück aus ohne dem Spieler die Tags zu zeigen.
        /// Frag mich nie wie das funktioniert, aber es funktioniert... :D
        /// </summary>
        /// <param name="text">Das Textstück, das ausgegeben werden soll</param>
        /*private IEnumerator RevealFormattedTextGradually(string text)
        {
            /* Zuerst werden die Tags "geschrieben"; 
             * dabei wird der Index in der Mitte gespeichert (wo der Text hinkommt) 
            int cursor = text.IndexOf(">") + 1;
            int dialogTextCursor = 0;
            dialogText.text += text.Substring(0, cursor);
            dialogTextCursor = dialogText.text.Length - 1;
            dialogText.text += text.Substring(text.LastIndexOf('<'),
                                                  text.Length - text.LastIndexOf('<'));

            // Dann wird der Text Stück für Stück zwischen die Tags geschrieben
            string textContentSection = text.Substring(cursor,
                                                    text.LastIndexOf('<') - 1 - text.IndexOf('>'));
            foreach (char _letter in textContentSection)
            {
                dialogText.SetText(dialogText.text.Insert(++dialogTextCursor, _letter.ToString()));
                yield return new WaitForSecondsRealtime(textRevealSpeed);
            }
        }*/

        /// <summary>
        /// Bestimmt rekursiv, ob ein Branch des Dialogs ohne weitere 
        /// Antwortmöglichkeiten zum Ende führt.
        /// </summary>
        /// <param name="answer">Der DialogPart, von dem aus geprüft werden soll</param>
        /// <returns>Ob der Dialog-Branch ab DialogPart zum Ende des 
        /// Dialogs führt (true) oder nicht (false)</returns>
        private bool IsEndBranch(Dialog.DialogPart dialogPart)
        {
            // Wenn gleich der Anfang des Branches nicht existiert
            if (dialogPart == null)
                return true;

            // Es gibt weitere Antwortmöglichkeiten => Kein Ende
            if (dialogPart.answers.Length > 0)
                return false;
            else if (string.IsNullOrWhiteSpace(dialogPart.nextDialogPartID)) // Ende
                return true;
            else // Nächsten Part raussuchen und ihn überprüfen
            {
                var followingPart = Array.Find(currentDialog.dialogParts,
                    dp => dp.id.Equals(dialogPart.nextDialogPartID));

                return IsEndBranch(followingPart); // Nächsten überprüfen
            }
        }

        //public static void FormatDialogs()
        //{
        //    foreach (Dialog dialog in GameStuffHolder.instance.dialogs)
        //        foreach (Dialog.DialogPart diaPart in dialog.dialogParts)
        //        {
        //            if (diaPart.text.Contains("<color") && !diaPart.text.Contains("|<color"))
        //            {
        //                diaPart.text = diaPart.text.Replace("<color", "|<color");
        //                diaPart.text = diaPart.text.Replace("</color>", "</color>|");
        //            }
        //
        //            if (diaPart.textDE.Contains("<color") && !diaPart.textDE.Contains("|<color"))
        //            {
        //                diaPart.textDE = diaPart.textDE.Replace("<color", "|<color");
        //                diaPart.textDE = diaPart.textDE.Replace("</color>", "</color>|");
        //            }
        //        }
        //}

        private Dialog[] LoadDialogs()
        {
            List<Dialog> dialogs = new List<Dialog>();

            TextAsset[] dialogAssets = null;
            try
            {
                dialogAssets = Resources.LoadAll("Dialogs", typeof(TextAsset))
                                        .Cast<TextAsset>().ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError("Error while loading dialogs. Please make sure that " +
                    "the Resources/Dialogs folder exists\n\n" + e.Message);
            }

            if (dialogAssets == null)
                return dialogs.ToArray();

            foreach (TextAsset dialogFile in dialogAssets)
            {
                Dialog dialogInstance = JsonConvert.DeserializeObject<Dialog>(dialogFile.text);
                dialogs.Add(dialogInstance);
            }

            return dialogs.ToArray();
        }

        private Dialog LoadDialog(string dialogID)
        {
            TextAsset dialogAsset = null;
            try
            {
                dialogAsset
                    = (TextAsset)Resources.Load(Path.Combine("Dialogs", dialogID), typeof(TextAsset));
            }
            catch (Exception e)
            {
                Debug.LogError("Error while loading dialogs. Please make sure that " +
                    "the Resources/Dialogs folder exists\n\n" + e.Message);
            }

            if (dialogAsset == null)
                return null;

            Dialog dialogInstance = JsonConvert.DeserializeObject<Dialog>(dialogAsset.text);

            return dialogInstance;
        }

        private void SetTextOnTextBox(TextBox textBox, string text)
        {
            if (useTextMeshPro)
                textBox.textTMP.SetText(text);
            else
                textBox.text.text = text;
        }
    }
}
