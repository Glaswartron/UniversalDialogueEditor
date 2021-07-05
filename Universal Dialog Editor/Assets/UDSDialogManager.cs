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

        [HideInInspector] public bool dialogRunning;

        //public char richTextTagDelimiter;
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

        private bool[] deactivateDuringDialogObjectsToReactivate;

        // Coroutines
        private IEnumerator revealTextGraduallyCo;
        private IEnumerator revealFormattedTextGraduallyCo;
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
            if (currentDialog.GetProperty<bool>("Pause during Dialog")) // with effect
                StartCoroutine(RevealTextGradually(CurrentDialogPartText));
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

                    answerBox.button.onClick.AddListener(
                        () =>
                        {
                            int _i = i;
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
        /// <param name="text">Der Text der aufgedeckt werden soll</param>
        private IEnumerator RevealTextGradually(string text)
        {
            textEffectRunning = true;

            SetTextOnTextBox(dialogTextBox, "");
            //bool insideRichTextTag = false;
            float textRevealSpeed = (float)currentDialogPart.GetProperty<double>("Text speed");
            foreach (char letter in text.ToCharArray())
            {
                /* Es handelt sich um den Anfang oder das Ende eines Tags zur Formattierung (z.B. <color ...> */
                /*if (letter == richTextTagDelimiter)
                {
                    insideRichTextTag = !insideRichTextTag;
                    if (!insideRichTextTag) // Die durch Tags abgetrennte Region ist zuende
                    {
                        // Der End-Delimiter kommt auch noch in den TextBuffer rein
                        textBuffer.Append(letter);
                        // Die Delimiter werden entfernt
                        textBuffer = new StringBuilder(Regex.Replace(textBuffer.ToString(),
                                                                     "\\" + richTextTagDelimiter.ToString(), ""));

                        // Der formattierte Text wird ausgegeben (Synchronisierte Coroutine)
                        yield return RevealFormattedTextGradually(textBuffer.ToString());

                        textBuffer.Clear(); // WICHTIG
                        continue; // WICHTIG
                    }
                }*/

                //if (!insideRichTextTag) // Normal
                //{
                string newText = useTextMeshPro
                    ? dialogTextBox.textTMP.text + letter
                    : dialogTextBox.text.text + letter;

                SetTextOnTextBox(dialogTextBox, newText);

                // WaitForSecondsRealtime, damit es unabhängig von der gestoppten TimeScale ist
                yield return new WaitForSecondsRealtime(1.1f - textRevealSpeed);
                //}
                //else
                //textBuffer.Append(letter); // Der Text wird erstmal gespeichert und noch nicht angezeigt

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
