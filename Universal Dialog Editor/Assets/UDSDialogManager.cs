using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Newtonsoft.Json;
using System.IO;
using System.ComponentModel;

namespace UniversalDialogSystem
{
    public class UDSDialogManager : MonoBehaviour
    {
        #region Structs and Enums

        [Serializable]
        protected struct TextBox
        {
            public GameObject gameObject;
            public Text text;
            public TMP_Text textTMP;
        }

        [Serializable]
        protected struct AnswerBox
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
        protected enum Platform
        {
            DESKTOP, MOBILE
        }

        [Serializable]
        protected enum LoadMode
        {
            LOAD_ON_START, LOAD_ON_DEMAND
        }
        #endregion

        [HideInInspector] public static UDSDialogManager instance; // Singleton

        #region Settings
        [Header("Important General Settings")]
        [SerializeField] protected Platform platform = Platform.DESKTOP;
        [SerializeField] protected bool useTextMeshPro = false;

        [Header("UI")]
        [SerializeField] protected GameObject dialogUI;
        [SerializeField] protected TextBox dialogTextBox;
        [SerializeField] protected AnswerBox[] answerTextBoxes;
        [SerializeField] protected TextBox nameTextBox;
        [SerializeField] protected GameObject[] deactivateDuringDialog;

        [Header("Input")]
        [SerializeField] protected KeyCode[] interactionKeys = new KeyCode[] { KeyCode.Mouse0 };

        [Header("Global Properties")]
        [SerializeField] protected bool saveGlobalProperties = false;

        [Header("Animation and Delays")]
        [SerializeField] protected float delayBtwUIEnableAndDialogStart;
        [SerializeField] protected float delayBtwDialogEndAndUIDisable;

        [Header("Technical options - Only adjust when needed")]
        [SerializeField] protected LoadMode loadMode = LoadMode.LOAD_ON_START;
        [SerializeField] protected float minTimeBetweenTouches = 0.35f;
        [SerializeField] protected float standardTimeScale = 1f;

        [HideInInspector] public bool dialogRunning;
        [HideInInspector] public bool dialogPaused;

        private string GLOBAL_PROPERTIES_PATH; 
        #endregion

        #region Variables
        protected Dialog currentDialog = null;

        /// <summary>
        /// Very important! Stores all the dialogs loaded from Resources
        /// </summary>
        private Dialog[] dialogs;

        private Dictionary<string, UDSProperty> globalProperties;

        protected Dialog.DialogPart currentDialogPart;

        /// <summary>
        /// Whether there are no answers in currentDialogPart
        /// </summary>
        protected bool noAnswers = false;

        /// <summary>
        /// Whether text is being played gradually right now
        /// </summary>
        protected bool textEffectRunning = false;

        private Dialog.DialogPart.Answer answerBeforePause;

        private string overridenText;

        private float lastTouchTimestamp = Mathf.Infinity;
        private bool justStarted = false;

        private bool[] deactivateDuringDialogObjectsToReactivate;

        // Coroutines
        private Coroutine startCoroutine;
        private Coroutine stopCoroutine;
        private Coroutine revealTextGraduallyCo;
        #endregion

        #region Properties
        /// <summary>
        /// The 'Text' Property of the currentDialogPart or
        /// alternatively the text set by a method overriding
        /// or calling ShowDialogPartText. Null if currentDialogPart
        /// is null (e.g. outside a dialog)
        /// </summary>
        protected string CurrentDialogPartText
        {
            get
            {
                if (currentDialogPart == null)
                    return null;

                return overridenText == null ?
                       currentDialogPart.GetProperty<string>("Text")
                       : overridenText;
            }
        }

        /// <summary>
        /// The 'Name' Property of the currentDialogPart or
        /// Null if currentDialogPart is null (e.g. outside a dialog)
        /// </summary>
        protected string CurrentDialogPartName
        {
            get
            {
                if (currentDialogPart == null)
                    return null;

                if (currentDialogPart.HasProperty("Name"))
                    return currentDialogPart.GetProperty<string>("Name");
                else // Shouldn't happen
                    return null;
            }
        }
        #endregion

        #region Start, Update, OnDisable 
        protected virtual void Start()
        {
            // Singleton - Only one instance at a time
            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);

            // Load the Dialogs from the Resources folder
            if (loadMode == LoadMode.LOAD_ON_START)
                dialogs = LoadDialogs();

            // Load the globalProperties from their file
            if (saveGlobalProperties)
            {
                GLOBAL_PROPERTIES_PATH 
                    = Path.Combine(Application.persistentDataPath, "UDSGlobalProperties.json");

                globalProperties = LoadGlobalProperties();
            }
        }

        protected virtual void Update()
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

        private void OnDisable()
        {
            // Save the globalProperties to their file
            if (saveGlobalProperties)
                SaveGlobalProperties();
        }
        #endregion

        #region Messages and Events = Methods to override
        /// <summary>
        /// Called when a Dialog starts 
        /// (after the UI was enabled and the Time.timeScale was set)
        /// </summary>
        /// <seealso cref="OnDialogEnd(Dialog)"/>
        /// <param name="dialog">The Dialog that just starts</param>
        protected virtual void OnDialogStart(Dialog dialog)
        {

        }

        /// <summary>
        /// Called when a Dialog ends
        /// </summary>
        /// <seealso cref="OnDialogStart(Dialog)"/>
        /// <param name="dialog">The Dialog that just ends</param>
        protected virtual void OnDialogEnd(Dialog dialog)
        {

        }

        /// <summary>
        /// Enables the dialogUI. By default functionally equivalent
        /// to 'dialogUI.SetActive(true)'
        /// </summary>
        /// <seealso cref="DisableDialogUI(bool)"/>
        /// <seealso cref="PauseDialog(bool, bool)"/>
        /// <param name="continueAfterPause">Whether or not the dialogUI is being 
        /// reenabled after a pause triggered by PauseDialog(bool, bool)</param>
        protected virtual void EnableDialogUI(bool continueAfterPause = false)
        {
            dialogUI.SetActive(true);
        }

        /// <summary>
        /// Disables the dialogUI. By default functionally equivalent
        /// to 'dialogUI.SetActive(false)'
        /// </summary>
        /// <seealso cref="EnableDialogUI(bool)"/>
        /// <seealso cref="PauseDialog(bool, bool)"/>
        /// <param name="pause">Whether or not the dialogUI is being 
        /// disabled because of a pause triggered by PauseDialog(bool, bool)</param>
        protected virtual void DisableDialogUI(bool pause = false)
        {
            dialogUI.SetActive(false);
        }

        /// <summary>
        /// Shows the text of the current DialogPart to the player.
        /// Called whenever a new DialogPart is being started!
        /// By default functionally equivalent to 'ShowDialogPartText(null)'.
        /// You can override this method to make changes to the text, apply
        /// effects or for localization (selecting the right text from 
        /// multiple ones stored in the dialogPart's properties)
        /// </summary>
        /// <seealso cref="ShowAnswer(in Dialog.DialogPart.Answer, string, AnswerBox)"/>
        /// <param name="dialogPart">The dialogPart that is being played</param>
        /// <param name="text">The text to be shown, by default the dialogPart's
        /// text (Property)</param>
        protected virtual void ShowDialogPartText(in Dialog.DialogPart dialogPart, string text)
        {
            ShowDialogPartText();
        }

        /// <summary>
        /// Shows an answer to the player in a answerTextBox. By default
        /// functionally equivalent to 'SetTextOnTextBox(...)'.
        /// You can override this method to make changes to the text, apply
        /// effects or for localization (selecting the right text from 
        /// multiple ones stored in the answer's properties)
        /// </summary>
        /// <seealso cref="OnAnswer(in Dialog.DialogPart.Answer, AnswerBox)"/>
        /// <param name="answer">The answer to be shown</param>
        /// <param name="text">The text to be shown, by default the answer's text (Property)</param>
        /// <param name="answerTextBox">The AnswerBox that the answer will be shown in. Includes 
        /// the actual UI components that are involved</param>
        protected virtual void ShowAnswer(in Dialog.DialogPart.Answer answer, string text, AnswerBox answerTextBox)
        {
            SetTextOnTextBox(answerTextBox.textBox, answer.GetProperty<string>("Text"));
        }

        /// <summary>
        /// Called whenever the player selects an answer
        /// </summary>
        /// <seealso cref="ShowAnswer(in Dialog.DialogPart.Answer, string, AnswerBox)"/>
        /// <param name="answer">The answer, which the player selected</param>
        /// <param name="answerTextBox">The AnswerBox that the answer is shown in. Includes 
        /// the actual UI components that are involved</param>
        protected virtual void OnAnswer(in Dialog.DialogPart.Answer answer, AnswerBox answerTextBox)
        {

        }

        /// <summary>
        /// Called whenever a pause is triggered through PauseDialog(bool, bool)
        /// </summary>
        /// <seealso cref="PauseDialog(bool, bool)"/>
        /// <seealso cref="ContinueDialog"/>
        protected virtual void OnDialogPause()
        {

        }

        /// <summary>
        /// Called whenever a Dialog is continued after a pause (ContinueDialog())
        /// </summary>
        /// <seealso cref="ContinueDialog"/>
        /// <seealso cref="PauseDialog(bool, bool)"/>
        protected virtual void OnDialogContinue()
        {

        }
        #endregion

        #region Input
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
        #endregion

        #region Dialog Playback
        /// <summary>
        /// Starts a Dialog. Enables the dialogUI and sets Time.timeScale to 0 
        /// (= pauses the game), if the "Pause during Dialog" Property is 
        /// set to true on the Dialog. Then starts playing the start DialogPart
        /// </summary>
        /// <seealso cref="StartDialog(Dialog)"/>
        public void StartDialog(string dialogID)
        {
            Dialog dialog = null;
            if (loadMode == LoadMode.LOAD_ON_START)
                dialog = dialogs.Where(d => d.id.Equals(dialogID)).First();
            else
                dialog = LoadDialog(dialogID);

            if (dialog != null)
            {
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

                StartDialog(dialog);
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
        /// </summary>
        /// <param name="dialog">The Dialog to be started</param>
        protected void StartDialog(Dialog dialog)
        {
            currentDialog = dialog; // !

            if (dialog.GetProperty<bool>("Pause during Dialog"))
                Time.timeScale = 0f;

            dialogRunning = true;

            EnableDialogUI(); // !

            StartCoroutine(StartDialogDelayed());
        }

        private IEnumerator StartDialogDelayed()
        {
            if (currentDialog.GetProperty<bool>("Pause during Dialog"))
                yield return new WaitForSecondsRealtime(delayBtwUIEnableAndDialogStart);
            else
                yield return new WaitForSeconds(delayBtwUIEnableAndDialogStart);

            OnDialogStart(currentDialog); // !

            // The start DialogPart is being played
            GoThroughDialogPart(
                currentDialog.dialogParts.Where(
                    dp => dp.id.Equals(currentDialog.startDialogPartID)).First());
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
        private void UpdateDialog()
        {
            // Text is currently being shown gradually (effect is running)
            if (textEffectRunning)
            {
                StopDialogPlaybackCoroutines();

                textEffectRunning = false;

                // Show text instantly
                SetTextOnTextBox(dialogTextBox, CurrentDialogPartText);

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
        /// Actually plays back a DialogPart 
        /// => Handles the text and answer boxes
        /// </summary>
        /// <param name="diaPart">The DialogPart to be played</param>
        protected void GoThroughDialogPart(Dialog.DialogPart diaPart)
        {
            // To avoid "overlapping" coroutines
            if (revealTextGraduallyCo != null) StopCoroutine(revealTextGraduallyCo);
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

            /* Show the text in the UI controlled by the Text Speed
             * and the user's potential override */
            ShowDialogPartText(currentDialogPart, CurrentDialogPartText);

            // Answers
            int answerCount = diaPart.answers.Length;
            if (answerCount > 0) // Are there even answers
            {
                // If so, go through all of them
                for (int i = 0; i < answerCount; i++)
                {
                    Dialog.DialogPart.Answer answer = diaPart.answers[i];

                    // Activate an AnswerBox for each Answer
                    var answerBox = answerTextBoxes[i];

                    ShowAnswer(answer, answer.GetProperty<string>("Text"), answerBox);

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
        /// Starts the playback of the current DialogParts text
        /// in the UI. If it's Text Speed Property is set to 0,
        /// this will just instantly show the text, otherwise it
        /// will be played gradually (= inside a coroutine with 
        /// delay between the letters and using an algorithm to
        /// deal with Rich Text). 
        /// Called by ShowDialogPartText(Dialog.DialogPart, string),
        /// which you can override.
        /// </summary>
        /// <seealso cref="ShowDialogPartText(in Dialog.DialogPart, string)"/>
        /// <param name="text">Optional parameter. If it is null or
        /// not given at all, the text of the current DialogPart will
        /// be played. Otherwise the given text will be played instead. 
        /// Can be used for making changes to the text or for localization</param>
        protected void ShowDialogPartText(string text = null)
        {
            if (text == null)
                text = CurrentDialogPartText;
            else
                overridenText = text;

            // Show text!
            if (currentDialogPart.GetProperty<float>("Text speed") > 0) // with effect
                revealTextGraduallyCo = StartCoroutine(RevealTextGradually(text));
            else // instantaneously
                SetTextOnTextBox(dialogTextBox, text);
        }

        /// <summary>
        /// Called by the Answer buttons whenever one is clicked
        /// </summary>
        /// <param name="index">The index of the Answer that was chosen</param>
        private void TakeAnswer(int index)
        {
            StopCoroutine(revealTextGraduallyCo);

            Dialog.DialogPart.Answer answer = currentDialogPart.answers[index];

            OnAnswer(answer, answerTextBoxes[index]);

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
        /// Pauses/Interrupts the currently running Dialog. You can start other
        /// things (e.g. a shop screen) now. 
        /// The Dialog will continue when ContinueDialog() is being called
        /// </summary>
        /// <seealso cref="ContinueDialog"/>
        /// <param name="disableDialogUI">Whether or not the dialogUI shall be 
        /// disabled during the pause</param>
        /// <param name="resetTimescale">Whether or not the Time.timeScale shall 
        /// be reset to standardTimeScale during the pause (only applies when 
        /// the Dialog's 'Pause during Dialog' is set to true)</param>
        public void PauseDialog(bool disableDialogUI, bool resetTimescale)
        {
            if (currentDialog == null)
                return;

            dialogPaused = true; // !

            StopDialogPlaybackCoroutines();

            if (disableDialogUI)
                DisableDialogUI(true);

            if (resetTimescale && currentDialog.GetProperty<bool>("Pause during Dialog"))
                Time.timeScale = standardTimeScale;

            OnDialogPause(); // !
        }

        /// <summary>
        /// Continues the currently running Dialog after a pause/interrupt 
        /// triggered by PauseDialog(bool, bool). Continues the Dialog by 
        /// either 1) instantly taking the answer which was clicked right before
        /// the pause, 2) playing the current DialogPart (again) or
        /// 3) (re)starting the current Dialog. One of these is being chosen 
        /// based on when PauseDialog was called.
        /// </summary>
        /// <seealso cref="PauseDialog(bool, bool)"/>
        public void ContinueDialog()
        {
            if (!dialogPaused || currentDialog == null)
                return;

            if (dialogUI.activeSelf == false)
                EnableDialogUI(true);

            if (currentDialog.GetProperty<bool>("Pause during Dialog"))
                Time.timeScale = 0;

            OnDialogContinue(); // !

            // Find the right way to continue after a pause/interrupt
            if (answerBeforePause != null)
                TakeAnswer(answerBeforePause.index);
            else if (currentDialogPart != null)
                GoThroughDialogPart(currentDialogPart);
            else
                StartDialog(currentDialog);
        }

        /// <summary>
        /// Finishes the current Dialog and resets all the UI
        /// </summary>
        protected void FinishDialog()
        {
            foreach (AnswerBox answerBox in answerTextBoxes)
            {
                SetTextOnTextBox(answerBox.textBox, "");

                answerBox.button.onClick.RemoveAllListeners(); // !

                answerBox.textBox.gameObject.SetActive(false);
            }

            OnDialogEnd(currentDialog); // !

            dialogRunning = false;

            StartCoroutine(FinishDialogDelayed());
        }

        private IEnumerator FinishDialogDelayed()
        {
            if (currentDialog.GetProperty<bool>("Pause during Dialog"))
                yield return new WaitForSecondsRealtime(delayBtwDialogEndAndUIDisable);
            else
                yield return new WaitForSeconds(delayBtwDialogEndAndUIDisable);

            DisableDialogUI();

            for (int i = 0; i < deactivateDuringDialogObjectsToReactivate.Length; i++)
            {
                if (deactivateDuringDialogObjectsToReactivate[i])
                    deactivateDuringDialog[i].SetActive(true);
            }

            if (currentDialog.GetProperty<bool>("Pause during Dialog"))
                Time.timeScale = standardTimeScale;

            currentDialog = null;
            currentDialogPart = null;
            overridenText = null;
        }
        #endregion

        #region Global Properties
        public bool SetGlobalProperty<T>(string key, T value)
        {
            if (globalProperties == null)
                globalProperties = new Dictionary<string, UDSProperty>();

            bool alreadyThere = globalProperties.ContainsKey(key);

            globalProperties[key] = new UDSProperty(value, typeof(T));

            return alreadyThere;
        }

        public T GetGlobalProperty<T>(string key)
        {
            UDSProperty valueRaw = default;
            if (globalProperties.TryGetValue(key, out valueRaw))
            {
                if (valueRaw.type != typeof(T))
                    throw new UDSException
                        (string.Format(UDSException.msg5, key, typeof(T).ToString()));


                T value = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(valueRaw.value.ToString());

                return value;
            }
            else
                throw new UDSException
                    (string.Format(UDSException.msg4, key, typeof(T).ToString()));
        }

        public UDSProperty GetGlobalProperty(string key)
        {
            UDSProperty value;
            if (globalProperties.TryGetValue(key, out value))
            {
                return value;
            }
            else
                throw new UDSException(string.Format(UDSException.msg6, key));
        }

        public string[] GetGlobalPropertyKeys()
        {
            return globalProperties.Keys.ToArray();
        }

        public bool DeleteGlobalProperty(string key)
        {
            return globalProperties.Remove(key);
        }

        public void DeleteAllGlobalProperties()
        {
            foreach (string property in GetGlobalPropertyKeys())
                DeleteGlobalProperty(property);
        }

        public bool HasGlobalProperty(string key)
        => globalProperties.ContainsKey(key);

        public bool HasGlobalProperty<T>(string key)
        => globalProperties.ContainsKey(key) && globalProperties[key].type == typeof(T);

        public bool HasGlobalProperty(string key, Type type)
            => globalProperties.ContainsKey(key) && globalProperties[key].type == type;

        protected virtual bool SaveGlobalProperties()
        {
            if (globalProperties == null)
                return false;

            FileStream stream = null;
            StreamWriter writer = null;

            try
            {
                stream = new FileStream(GLOBAL_PROPERTIES_PATH, FileMode.Create);
                writer = new StreamWriter(stream);

                string propertiesJSON = JsonConvert.SerializeObject
                    (globalProperties, Formatting.Indented,
                    new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });

                writer.Write(propertiesJSON);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);

                return false;
            }
            finally
            {
                writer?.Flush();
                writer?.Close();
            }

            return true;
        }

        protected virtual Dictionary<string, UDSProperty> LoadGlobalProperties()
        {
            if (!File.Exists(GLOBAL_PROPERTIES_PATH))
                return null;

            FileStream stream = null;
            StreamReader reader = null;

            try
            {
                stream = new FileStream(GLOBAL_PROPERTIES_PATH, FileMode.Open);
                reader = new StreamReader(stream);

                string propertiesJSON = reader.ReadToEnd();
                Dictionary<string, UDSProperty> properties
                    = JsonConvert.DeserializeObject<Dictionary<string, UDSProperty>>(propertiesJSON);

                return properties;
            }
            catch (Exception e)
            {
                Debug.LogError("Path: " + GLOBAL_PROPERTIES_PATH + " -- " + e.Message);

                return null;
            }
            finally
            {
                reader?.Close();
            }
        }
        #endregion

        #region Text effect
        /// <summary>
        /// The text is being revealed gradually, meaning one
        /// char at a time. Also includes the algorithm to
        /// handle Rich Text 
        /// (https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html)
        /// </summary>
        /// <param name="baseText">The text to be revealed</param>
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
                int _textStartIndex = cursor;
                string _text = baseText.Substring(_textStartIndex, currentContext.endIndex - cursor + 1);

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

                            if (!startTag.Contains(" "))
                            {
                                // _text with everything before and including the startTag cut off
                                string _textFromEndOfStartTag = _text.Substring(_text.IndexOf(">") + 1);

                                // Is there a closing tag (</...>) ahead?
                                if (_textFromEndOfStartTag.Contains("</" + startTag[1])
                                    && _textFromEndOfStartTag.Contains('>'))
                                {
                                    // Sneaky workaround with +startTag[1] to get the right tag
                                    int endTagIndex = _textFromEndOfStartTag.IndexOf("</" + startTag[1]);
                                    string _textFromStartOfEndTag = _textFromEndOfStartTag.Substring(endTagIndex);

                                    if (_textFromStartOfEndTag.Contains('>'))
                                    {
                                        // Cut out the closing tag
                                        endTag = _textFromStartOfEndTag.Substring
                                            (0, _textFromStartOfEndTag.IndexOf(">") + 1);

                                        if (!endTag.Contains(" "))
                                        {
                                            // Only what's in between the "< >" and "</ >"
                                            string startTagContent = startTag.Replace("<", null).Replace(">", null);
                                            string endTagContent = endTag.Replace("</", null).Replace(">", null);

                                            // Just another check: If it's well-formed, startTagContent starts with endTagContent
                                            if (startTagContent.StartsWith(endTagContent))
                                            {
                                                // Write the tags to the text box, the actual text will go in between
                                                newText = useTextMeshPro
                                                          ? dialogTextBox.textTMP.text.Insert(cursor, startTag + endTag)
                                                          : dialogTextBox.text.text.Insert(cursor, startTag + endTag);

                                                SetTextOnTextBox(dialogTextBox, newText);

                                                // Very important! Push current context back onto the stack for later
                                                contexts.Push(currentContext);

                                                /* For the new RichTextContext, start after the startTag 
                                                 * (in the original text) and write till before the end tag, 
                                                 * then resume after the end tag when you leave the context */
                                                RichTextContext newContext = new RichTextContext
                                                {
                                                    startIndex = cursor + startTag.Length,
                                                    endIndex = _textStartIndex + _text.IndexOf(endTag) - 1,
                                                    resumeOffset = endTag.Length + 1
                                                };

                                                contexts.Push(newContext);

                                                break; // Very important! We're heading to a new loop now
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    newText = useTextMeshPro
                        ? dialogTextBox.textTMP.text.Insert(cursor, baseText[cursor].ToString())
                        : dialogTextBox.text.text.Insert(cursor, baseText[cursor].ToString());

                    SetTextOnTextBox(dialogTextBox, newText);

                    if (currentDialog.GetProperty<bool>("Pause during Dialog"))
                        yield return new WaitForSecondsRealtime(1.035f - textRevealSpeed);
                    else
                        yield return new WaitForSeconds(1.035f - textRevealSpeed);
                }

                // Decrement cursor because it has been incremented once too often by the loop
                cursor--;

                // Add the resumeOffset to the cursor when leaving the context
                if (cursor == currentContext.endIndex)
                    cursor += currentContext.resumeOffset;
            }

            textEffectRunning = false;
        }
        #endregion

        #region Loading and Utility
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
                    "the Resources\\Dialogs folder exists\n\n" + e.Message);
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
                dialogAsset = Resources.Load<TextAsset>(Path.Combine("Dialogs", dialogID) + ".udsdialog");
            }
            catch (Exception e)
            {
                Debug.LogError("Error while loading a dialog. Please make sure that " +
                    "the Resources\\Dialogs\\" + dialogID + ".udsdialog.json asset exists and is valid\n\n" + e.Message);
            }

            if (dialogAsset == null)
                return null;

            Dialog dialogInstance = JsonConvert.DeserializeObject<Dialog>(dialogAsset.text);

            return dialogInstance;
        }

        /// <summary>
        /// Determines whether a dialogPart leads directly to the end
        /// of the Dialog. That is, it has no answers, all parts that
        /// come after it have no answers and one of the following
        /// parts is a end of the Dialog, which also means that there
        /// are no cycles "beyond" that dialogPart.
        /// 
        /// (works iteratively to avoid StackOverflows and
        /// excessive memory usage)
        /// </summary>
        /// <param name="dialogPart">The dialogPart to be evaluated</param>
        /// <returns>Whether or not this dialogPart leads directly to the end of the dialog</returns>
        protected bool IsEndBranch(Dialog.DialogPart dialogPart)
        {
            // The start of the branch doesn't exist
            if (dialogPart == null)
                return true;

            // The part that is currently being evaluated
            Dialog.DialogPart currentPart = dialogPart;

            // HashSet for loop detection
            HashSet<Dialog.DialogPart> alreadyVisited = new HashSet<Dialog.DialogPart>();

            while (!alreadyVisited.Contains(currentPart))
            {
                alreadyVisited.Add(currentPart);

                // There are answers in the currentPart
                if (currentPart.answers.Length > 0)
                    return false;
                else if (string.IsNullOrWhiteSpace(currentPart.nextDialogPartID)) // End reached
                    return true;
                else // Find and check next part
                {
                    var followingPart = Array.Find(currentDialog.dialogParts,
                                                   dp => dp.id.Equals(dialogPart.nextDialogPartID));

                    currentPart = followingPart;
                }
            }

            return false; // Loop was detected
        }

        /// <summary>
        /// Writes a text to a textBox based on whether TMP is
        /// being used or not
        /// </summary>
        /// <param name="textBox">The textBox to write to</param>
        /// <param name="text">The text to be written</param>
        protected void SetTextOnTextBox(TextBox textBox, string text)
        {
            if (useTextMeshPro)
                textBox.textTMP.SetText(text);
            else
                textBox.text.text = text;
        }

        /// <summary>
        /// Stops all coroutines that are possibly being started internally by
        /// the UDSDialogManager. These are the coroutines for gradually revealing 
        /// and formatting the text, the dialog start coroutine and the dialog 
        /// end coroutine
        /// </summary>
        protected void StopDialogPlaybackCoroutines()
        {
            if (revealTextGraduallyCo != null) StopCoroutine(revealTextGraduallyCo);
            if (startCoroutine != null) StopCoroutine(startCoroutine);
            if (stopCoroutine != null) StopCoroutine(stopCoroutine);
        }
        #endregion
    }
}
