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

namespace UniversalDialogueSystem
{
    public class UDSDialogueManager : MonoBehaviour
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

        [HideInInspector] public static UDSDialogueManager instance; // Singleton

        #region Settings
        [Header("Important General Settings")]
        [SerializeField] protected Platform platform = Platform.DESKTOP;
        [SerializeField] protected bool useTextMeshPro = false;

        [Header("UI")]
        [SerializeField] protected GameObject DialogueUI;
        [SerializeField] protected TextBox DialogueTextBox;
        [SerializeField] protected AnswerBox[] answerTextBoxes;
        [SerializeField] protected TextBox nameTextBox;
        [SerializeField] protected GameObject[] deactivateDuringDialogue;

        [Header("Input")]
        [SerializeField] protected KeyCode[] interactionKeys = new KeyCode[] { KeyCode.Mouse0 };

        [Header("Global Properties")]
        [SerializeField] protected bool saveGlobalProperties = false;

        [Header("Text Effect, Animation and Delays")]
        [SerializeField] [Range(0.5f, 1.5f)] protected float baseTextSpeed = 1f;
        [SerializeField] protected float delayBtwUIEnableAndDialogueStart;
        [SerializeField] protected float delayBtwDialogueEndAndUIDisable;

        [Header("Technical options - Only adjust when needed")]
        [SerializeField] protected LoadMode loadMode = LoadMode.LOAD_ON_START;
        [SerializeField] protected float minTimeBetweenTouches = 0.35f;
        [SerializeField] protected float standardTimeScale = 1f;

        [HideInInspector] public bool DialogueRunning;
        [HideInInspector] public bool DialoguePaused;

        private string GLOBAL_PROPERTIES_PATH; 
        #endregion

        #region Variables
        protected Dialogue currentDialogue = null;

        /// <summary>
        /// Very important! Stores all the Dialogues loaded from Resources
        /// </summary>
        private Dialogue[] Dialogues;

        private Dictionary<string, UDSProperty> globalProperties; // !

        protected Dialogue.DialoguePart currentDialoguePart;

        /// <summary>
        /// Whether there are no answers in currentDialoguePart
        /// </summary>
        protected bool noAnswers = false;

        /// <summary>
        /// Whether text is being played gradually right now
        /// </summary>
        protected bool textEffectRunning = false;

        private bool DialogueStarting;
        private bool DialogueEnding;

        private Dialogue.DialoguePart.Answer answerBeforePause;

        private string overridenText;

        private float lastTouchTimestamp = Mathf.Infinity;
        private bool justStarted = false;

        private bool[] deactivateDuringDialogueObjectsToReactivate;

        // Coroutines
        private Coroutine startCoroutine;
        private Coroutine stopCoroutine;
        private Coroutine revealTextGraduallyCo;
        #endregion

        #region Properties
        /// <summary>
        /// The 'Text' Property of the currentDialoguePart or
        /// alternatively the text set by a method overriding
        /// or calling ShowDialoguePartText. Null if currentDialoguePart
        /// is null (e.g. outside a Dialogue)
        /// </summary>
        protected string CurrentDialoguePartText
        {
            get
            {
                if (currentDialoguePart == null)
                    return null;

                return overridenText == null ?
                       currentDialoguePart.GetProperty<string>("Text")
                       : overridenText;
            }
        }

        /// <summary>
        /// The 'Name' Property of the currentDialoguePart or
        /// Null if currentDialoguePart is null (e.g. outside a Dialogue)
        /// </summary>
        protected string CurrentDialoguePartName
        {
            get
            {
                if (currentDialoguePart == null)
                    return null;

                if (currentDialoguePart.HasProperty<string>("Name"))
                    return currentDialoguePart.GetProperty<string>("Name");
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

            // Load the Dialogues from the Resources folder
            if (loadMode == LoadMode.LOAD_ON_START)
                Dialogues = LoadDialogues();

            // Load the globalProperties from their file
            if (saveGlobalProperties)
            {
                // Can be changed if needed
                GLOBAL_PROPERTIES_PATH 
                    = Path.Combine(Application.persistentDataPath, "UDSGlobalProperties.json");

                globalProperties = LoadGlobalProperties();

                if (globalProperties == null) // Haven't been saved yet
                {
                    globalProperties = new Dictionary<string, UDSProperty>();

                    SaveGlobalProperties();
                }
            }
            else // Global Properties don't get saved => Just create a new Dictionary
            {
                globalProperties = new Dictionary<string, UDSProperty>();
            }
        }

        protected virtual void Update()
        {
            if (DialogueRunning)
            {
                if (!justStarted && !DialogueStarting && !DialogueEnding)
                {
                    if (platform == Platform.DESKTOP)
                    {
                        ProcessMouseInput();
                    }

                    if (platform == Platform.MOBILE)
                    {
#if UNITY_EDITOR
                        ProcessMouseInput();
                        return;
#endif

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
        /// Called when a Dialogue starts 
        /// (after the UI was enabled and the Time.timeScale was set)
        /// </summary>
        /// <seealso cref="OnDialogueEnd(Dialogue)"/>
        /// <param name="Dialogue">The Dialogue that just starts</param>
        protected virtual void OnDialogueStart(Dialogue dialogue)
        {

        }

        /// <summary>
        /// Called when a Dialogue ends
        /// </summary>
        /// <seealso cref="OnDialogueStart(Dialogue)"/>
        /// <param name="Dialogue">The Dialogue that just ends</param>
        protected virtual void OnDialogueEnd(Dialogue dialogue)
        {
            
        }

        /// <summary>
        /// Enables the DialogueUI. By default functionally equivalent
        /// to 'DialogueUI.SetActive(true)'
        /// </summary>
        /// <seealso cref="DisableDialogueUI(bool)"/>
        /// <seealso cref="PauseDialogue(bool, bool)"/>
        /// <param name="continueAfterPause">Whether or not the DialogueUI is being 
        /// reenabled after a pause triggered by PauseDialogue(bool, bool)</param>
        protected virtual void EnableDialogueUI(bool continueAfterPause = false)
        {
            DialogueUI.SetActive(true);
        }

        /// <summary>
        /// Disables the DialogueUI. By default functionally equivalent
        /// to 'DialogueUI.SetActive(false)'
        /// </summary>
        /// <seealso cref="EnableDialogueUI(bool)"/>
        /// <seealso cref="PauseDialogue(bool, bool)"/>
        /// <param name="pause">Whether or not the DialogueUI is being 
        /// disabled because of a pause triggered by PauseDialogue(bool, bool)</param>
        protected virtual void DisableDialogueUI(bool pause = false)
        {
            DialogueUI.SetActive(false);
        }

        /// <summary>
        /// Called when playback of a DialoguePart starts
        /// </summary>
        /// <seealso cref="OnDialogueStart(Dialogue)"/>
        /// <seealso cref="ShowDialoguePartText(Dialogue.DialoguePart, string)"/>
        /// <param name="DialoguePart">The DialoguePart that just starts</param>
        protected virtual void OnDialoguePartStart(Dialogue.DialoguePart dialoguePart)
        {

        }

        /// <summary>
        /// Shows the text of the current DialoguePart to the player.
        /// Similiar to OnDialoguePartStart in that it is called 
        /// whenever a new DialoguePart is being started.
        /// By default functionally equivalent to 'ShowDialoguePartText(null)'.
        /// You can override this method to make changes to the text, apply
        /// effects or for localization (selecting the right text from 
        /// multiple ones stored in the DialoguePart's properties)
        /// </summary>
        /// <seealso cref="OnDialoguePartStart(Dialogue.DialoguePart)"/> 
        /// <seealso cref="ShowAnswer(Dialogue.DialoguePart.Answer, string, AnswerBox)"/>
        /// <param name="DialoguePart">The DialoguePart that is being played</param>
        /// <param name="text">The text to be shown, by default the value of the DialoguePart's
        /// text Property</param>
        protected virtual void ShowDialoguePartText(Dialogue.DialoguePart dialoguePart, string text)
        {
            ShowDialoguePartText();
        }

        /// <summary>
        /// Shows the Dialogue partner name of the current DialoguePart to the player.
        /// Called whenever a new DialoguePart is being started!
        /// By default functionally equivalent to checking if name is null, 
        /// activating the nameTextBox and then calling 'SetTextOnTextBox(...)'.
        /// You can override this method to make changes to the name, apply
        /// effects or for localization (selecting the right name from 
        /// multiple ones stored in the DialoguePart's properties)
        /// </summary>
        /// <param name="DialoguePart">The DialoguePart that is being played</param>
        /// <param name="name">The name to be shown, by default the value of the DialoguePart's
        /// name Property</param>
        /// <param name="nameTextBox">The TextBox that the answer will be shown in. Includes 
        /// the actual UI components that are involved</param>
        protected virtual void ShowName(Dialogue.DialoguePart dialoguePart, string name, TextBox nameTextBox)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                nameTextBox.gameObject.SetActive(true);
                SetTextOnTextBox(nameTextBox, name);
            }
            else
                nameTextBox.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows an answer to the player in a answerTextBox. By default
        /// functionally equivalent to activating the answerTextBox's textBox
        /// and calling 'SetTextOnTextBox(...)' on it.
        /// You can override this method to make changes to the text, apply
        /// effects or for localization (selecting the right text from 
        /// multiple ones stored in the answer's properties)
        /// </summary>
        /// <seealso cref="OnAnswer(Dialogue.DialoguePart.Answer, AnswerBox)"/>
        /// <param name="answer">The answer to be shown</param>
        /// <param name="text">The text to be shown, by default the answer's text (Property)</param>
        /// <param name="answerTextBox">The AnswerBox that the answer will be shown in. Includes 
        /// the actual UI components that are involved</param>
        protected virtual void ShowAnswer(Dialogue.DialoguePart.Answer answer, string text, AnswerBox answerTextBox)
        {
            SetTextOnTextBox(answerTextBox.textBox, answer.GetProperty<string>("Text"));
            answerTextBox.textBox.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called whenever the player selects an answer
        /// </summary>
        /// <seealso cref="ShowAnswer(Dialogue.DialoguePart.Answer, string, AnswerBox)"/>
        /// <param name="answer">The answer, which the player selected</param>
        /// <param name="answerTextBox">The AnswerBox that the answer is shown in. Includes 
        /// the actual UI components that are involved</param>
        /// <returns>Whether or not Dialogue playback is being paused by this answer (using PauseDialogue())</returns>
        protected virtual bool OnAnswer(Dialogue.DialoguePart.Answer answer, AnswerBox answerTextBox)
        {
            return false;
        }

        /// <summary>
        /// Called whenever a pause is triggered through PauseDialogue(bool, bool)
        /// </summary>
        /// <seealso cref="PauseDialogue(bool, bool)"/>
        /// <seealso cref="ContinueDialogue"/>
        protected virtual void OnDialoguePause()
        {

        }

        /// <summary>
        /// Called whenever a Dialogue is continued after a pause (ContinueDialogue())
        /// </summary>
        /// <seealso cref="ContinueDialogue"/>
        /// <seealso cref="PauseDialogue(bool, bool)"/>
        protected virtual void OnDialogueContinue()
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
                     * (ideally DialogueTextBox shouldn't be a raycast target) */
                    if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    {
                        lastTouchTimestamp = Time.realtimeSinceStartup;

                        UpdateDialogue();
                    }
        }

        private void ProcessMouseInput()
        {
            if (interactionKeys.Any(k => Input.GetKeyDown(k)))
            {
                /* Only proceed if the pointer is not above a button
                 * (ideally DialogueTextBox shouldn't be a raycast target) */
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    UpdateDialogue();
                }
            }
        }
        #endregion

        #region Dialogue Playback (The important part)
        /// <summary>
        /// Starts a Dialogue. Enables the DialogueUI and sets Time.timeScale to 0 
        /// (= pauses the game), if the "Pause during Dialogue" Property is 
        /// set to true on the Dialogue. Then starts playing the start DialoguePart
        /// </summary>
        /// <seealso cref="StartDialogue(Dialogue)"/>
        public void StartDialogue(string DialogueID)
        {
            Dialogue Dialogue = null;
            if (loadMode == LoadMode.LOAD_ON_START)
                Dialogue = Dialogues.Where(d => d.id.Equals(DialogueID)).FirstOrDefault();
            else
                Dialogue = LoadDialogue(DialogueID);

            if (Dialogue != null)
            {
                justStarted = true;

                deactivateDuringDialogueObjectsToReactivate = new bool[deactivateDuringDialogue.Length];
                for (int i = 0; i < deactivateDuringDialogue.Length; i++)
                {
                    if (deactivateDuringDialogue[i].activeSelf) // Only if it is active
                    {
                        deactivateDuringDialogue[i].SetActive(false);

                        // Schedule for reactivation after the Dialogue
                        deactivateDuringDialogueObjectsToReactivate[i] = true;
                    }
                }

                StartDialogue(Dialogue);
            }
            else
                Debug.LogWarning("Dialogue with ID " + DialogueID + " was started but " +
                    "couldn't be found! Try checking the spelling on the ID and " +
                    "whether you actually imported it into Resources\\Dialogues");
        }

        /// <summary>
        /// Starts a Dialogue. Enables the DialogueUI and sets Time.timeScale to 0 
        /// (= pauses the game), if the "Pause during Dialogue" Property is 
        /// set to true on the Dialogue. Called by StartDialogue(DialogueID).
        /// </summary>
        /// <param name="Dialogue">The Dialogue to be started</param>
        protected void StartDialogue(Dialogue Dialogue)
        {
            currentDialogue = Dialogue; // !

            if (Dialogue.GetProperty<bool>("Pause during Dialogue"))
                Time.timeScale = 0f;

            DialogueRunning = true;

            EnableDialogueUI(); // !

            StartCoroutine(StartDialogueDelayed());
        }

        private IEnumerator StartDialogueDelayed()
        {
            DialogueStarting = true;

            if (currentDialogue.GetProperty<bool>("Pause during Dialogue"))
                yield return new WaitForSecondsRealtime(delayBtwUIEnableAndDialogueStart);
            else
                yield return new WaitForSeconds(delayBtwUIEnableAndDialogueStart);

            OnDialogueStart(currentDialogue); // !

            // The start DialoguePart is being played
            GoThroughDialoguePart(
                currentDialogue.dialogueParts.Where(
                    dp => dp.id.Equals(currentDialogue.startDialoguePartID)).First());

            DialogueStarting = false;
        }

        /// <summary>
        /// Called as part of the Update loop whenever the player 
        /// touches the screen or presses the interactionKey during
        /// a Dialogue. Moves the Dialogue into it's next state, which
        /// means either
        /// going to the next DialoguePart,
        /// showing the entirety of the text at once (stopping revealing),
        /// or ending the Dialogue.
        /// </summary>
        private void UpdateDialogue()
        {
            // Text is currently being shown gradually (effect is running)
            if (textEffectRunning)
            {
                StopDialoguePlaybackCoroutines();

                textEffectRunning = false;

                // Show text instantly
                SetTextOnTextBox(DialogueTextBox, CurrentDialoguePartText);

                return;
            }

            /* No answers -> Go to next Dialogue part OR finish Dialogue 
             * (otherwise done by clicking on answers) */
            if (noAnswers)
            {
                var allDiaParts = currentDialogue.dialogueParts;

                // Check whether this is the last DialoguePart
                if (string.IsNullOrWhiteSpace(currentDialoguePart.nextDialoguePartID))
                {
                    FinishDialogue();
                    return;
                }

                // Continue to the next DialoguePart
                GoThroughDialoguePart(
                    currentDialogue.dialogueParts.Where(
                        dp => dp.id.Equals(currentDialoguePart.nextDialoguePartID)).First());
            }
        }

        /// <summary>
        /// Actually plays back a DialoguePart 
        /// => Handles the text and the answer boxes
        /// </summary>
        /// <param name="diaPart">The DialoguePart to be played</param>
        protected void GoThroughDialoguePart(Dialogue.DialoguePart diaPart)
        {
            // To avoid "overlapping" coroutines
            if (revealTextGraduallyCo != null) StopCoroutine(revealTextGraduallyCo);
            textEffectRunning = false;

            overridenText = null; // Important

            // noAnswers is false by default
            noAnswers = false;

            // All answer boxes are inactive by default
            foreach (AnswerBox answerBox in answerTextBoxes)
                answerBox.textBox.gameObject.SetActive(false);

            currentDialoguePart = diaPart; // !

            OnDialoguePartStart(diaPart); // !

            // Name box
            if (nameTextBox.gameObject != null)
                ShowName(diaPart, CurrentDialoguePartName, nameTextBox);

            /* Show the text in the UI controlled by the Text Speed
             * and the user's potential override */
            ShowDialoguePartText(currentDialoguePart, CurrentDialoguePartText);

            // Answers
            int answerCount = diaPart.answers.Length;
            if (answerCount > 0 // Are there even answers
                // Is there at least one Answer that is not conditional or whose condition is met
                && Array.Exists(diaPart.answers, a => !a.conditional || a.condition.Value.IsMet())) 
            {
                // If so, go through all of them
                for (int i = 0; i < answerCount; i++)
                {
                    Dialogue.DialoguePart.Answer answer = diaPart.answers[i];

                    // If this is a conditional answer, check the condition
                    if (answer.conditional)
                    {
                        if (!answer.condition.HasValue)
                            Debug.LogWarning("Encountered an answer that is set to " +
                                "conditional but does not have a condition. " +
                                "This might indicate that the Dialogue is corrupted");
                        /* Only show the answer if the condition is 
                         * met, otherwise continue to the next answer */
                        else if (!answer.condition.Value.IsMet())
                            continue;
                    }

                    // Activate an AnswerBox for each Answer
                    var answerBox = answerTextBoxes[i];

                    answerBox.button.onClick.RemoveAllListeners(); // !

                    int _i = i; // Important
                    answerBox.button.onClick.AddListener(
                        () =>
                        {
                            TakeAnswer(_i);
                        }
                    );

                    ShowAnswer(answer, answer.GetProperty<string>("Text"), answerBox); // !
                }
            }
            else // No answers
                noAnswers = true;
        }

        /// <summary>
        /// Starts the playback of the current DialogueParts text
        /// in the UI. If it's Text Speed Property is set to 0,
        /// this will just instantly show the text, otherwise it
        /// will be played gradually (= inside a coroutine with 
        /// delay between the letters and using an algorithm to
        /// deal with Rich Text). 
        /// Called by ShowDialoguePartText(Dialogue.DialoguePart, string),
        /// which you can override.
        /// </summary>
        /// <seealso cref="ShowDialoguePartText(in Dialogue.DialoguePart, string)"/>
        /// <param name="text">Optional parameter. If it is null or
        /// not given at all, the text of the current DialoguePart will
        /// be played. Otherwise the given text will be played instead. 
        /// Can be used for making changes to the text or for localization</param>
        protected void ShowDialoguePartText(string text = null)
        {
            if (text == null)
                text = CurrentDialoguePartText;
            else
                overridenText = text;

            // Show text!
            if (currentDialoguePart.GetProperty<float>("Text speed") > 0) // with effect
                revealTextGraduallyCo = StartCoroutine(RevealTextGradually(text));
            else // instantaneously
                SetTextOnTextBox(DialogueTextBox, text);
        }

        /// <summary>
        /// Called by the Answer buttons whenever one is clicked
        /// </summary>
        /// <param name="index">The index of the Answer that was chosen</param>
        private void TakeAnswer(int index, bool withoutNotify = false)
        {
            StopDialoguePlaybackCoroutines();

            Dialogue.DialoguePart.Answer answer = currentDialoguePart.answers[index];

            answerBeforePause = answer; // In case the Dialogue is being paused during OnAnswer

            if (!withoutNotify)
            {
                bool pause = OnAnswer(answer, answerTextBoxes[index]); // !

                if (pause)
                    return;
                else
                    answerBeforePause = null;
            }

            // Check whether the end of the Dialogue was reached
            if (string.IsNullOrWhiteSpace(answer.nextDialoguePartID))
            {
                FinishDialogue();

                return;
            }

            // Continue to next Dialogue Part
            GoThroughDialoguePart(
                currentDialogue.dialogueParts.Where(
                    dp => dp.id.Equals(answer.nextDialoguePartID)).First());
        }

        /// <summary>
        /// Pauses/Interrupts the currently running Dialogue. You can start other
        /// things (e.g. a shop screen) now. 
        /// The Dialogue will continue when ContinueDialogue() is being called
        /// </summary>
        /// <seealso cref="ContinueDialogue"/>
        /// <param name="disableDialogueUI">Whether or not the DialogueUI shall be 
        /// disabled during the pause</param>
        /// <param name="resetTimescale">Whether or not the Time.timeScale shall 
        /// be reset to standardTimeScale during the pause (only applies when 
        /// the Dialogue's 'Pause during Dialogue' is set to true)</param>
        public void PauseDialogue(bool disableDialogueUI, bool resetTimescale)
        {
            if (!DialogueRunning || currentDialogue == null)
                return;

            DialoguePaused = true; // !

            StopDialoguePlaybackCoroutines();

            if (disableDialogueUI)
                DisableDialogueUI(true);

            if (resetTimescale && currentDialogue.GetProperty<bool>("Pause during Dialogue"))
                Time.timeScale = standardTimeScale;

            OnDialoguePause(); // !
        }

        /// <summary>
        /// Continues the currently running Dialogue after a pause/interrupt 
        /// triggered by PauseDialogue(bool, bool). Continues the Dialogue by 
        /// either 1) instantly taking the answer which was clicked right before
        /// the pause, 2) playing the current DialoguePart (again) or
        /// 3) (re)starting the current Dialogue. One of these is being chosen 
        /// based on when PauseDialogue was called (and the value of continueWithAnswer).
        /// </summary>
        /// <seealso cref="PauseDialogue(bool, bool)"/>
        /// <param name="continueWithAnswer">Important when the pause was triggered by 
        /// OnAnswer. Determines whether or not the Dialogue continues by "taking" the previously 
        /// chosen answer (and continuing with the next DialoguePart) or shows the current DialoguePart</param>
        public void ContinueDialogue(bool continueWithAnswer = true)
        {
            if (!DialoguePaused || currentDialogue == null)
                return;

            if (DialogueUI.activeSelf == false)
                EnableDialogueUI(true);

            if (Time.timeScale != 0 && currentDialogue.GetProperty<bool>("Pause during Dialogue"))
                Time.timeScale = 0;

            OnDialogueContinue(); // !

            // Find the right way to continue after a pause/interrupt
            if (continueWithAnswer && answerBeforePause != null)
                TakeAnswer(answerBeforePause.index, true); // (1)
            else if (currentDialoguePart != null)
                GoThroughDialoguePart(currentDialoguePart); // (2)
            else
                StartDialogue(currentDialogue); // (3)
        }

        /// <summary>
        /// Finishes the current Dialogue and resets all the UI
        /// </summary>
        protected void FinishDialogue()
        {
            SetTextOnTextBox(DialogueTextBox, "");
            SetTextOnTextBox(nameTextBox, "");

            foreach (AnswerBox answerBox in answerTextBoxes)
            {
                SetTextOnTextBox(answerBox.textBox, "");

                answerBox.button.onClick.RemoveAllListeners(); // !

                answerBox.textBox.gameObject.SetActive(false);
            }

            OnDialogueEnd(currentDialogue); // !

            DialogueRunning = false;

            StartCoroutine(FinishDialogueDelayed());
        }

        private IEnumerator FinishDialogueDelayed()
        {
            DialogueEnding = true;

            if (currentDialogue.GetProperty<bool>("Pause during Dialogue"))
                yield return new WaitForSecondsRealtime(delayBtwDialogueEndAndUIDisable);
            else
                yield return new WaitForSeconds(delayBtwDialogueEndAndUIDisable);

            DisableDialogueUI();

            DialogueEnding = false;

            for (int i = 0; i < deactivateDuringDialogueObjectsToReactivate.Length; i++)
            {
                if (deactivateDuringDialogueObjectsToReactivate[i])
                    deactivateDuringDialogue[i].SetActive(true);
            }

            if (currentDialogue.GetProperty<bool>("Pause during Dialogue"))
                Time.timeScale = standardTimeScale;

            currentDialogue = null;
            currentDialoguePart = null;
            overridenText = null;
        }
        #endregion

        #region Global Properties
        /// <summary>
        /// Sets the Global Property of type T with key key to value value 
        /// (creates the Global Property if it doesn't exist)
        /// </summary>
        /// <typeparam name="T">The type of the Global Property to set, one of (string, int, bool, float)</typeparam>
        /// <param name="key">The key of the Global Property to set</param>
        /// <param name="value">The value to set the Global Property to</param>
        /// <returns>Whether or not the Global Property existed before (thus false if it was newly created)</returns>
        public bool SetGlobalProperty<T>(string key, T value)
        {
            bool alreadyThere = globalProperties.ContainsKey(key);

            globalProperties[key] = new UDSProperty(value, typeof(T));

            return alreadyThere;
        }

        /// <summary>
        /// Gets the value of the Global Property of type T with key key.
        /// Throws an UDSException if there is no such Global Property, so 
        /// HasGlobalProperty should be checked before GetGlobalProperty.
        /// </summary>
        /// <seealso cref="HasGlobalProperty{T}(string)"/>
        /// <typeparam name="T">The type of the Global Property, one of (string, int, bool, float)</typeparam>
        /// <param name="key">The key of the Global Property</param>
        /// <returns>The value of the Global Property with key key</returns>
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

        /// <summary>
        /// Gets the Property struct with key key. Since the struct 
        /// contains the Global Property value as an object seperated from its 
        /// type, this version of GetGlobalProperty should not be used unless 
        /// with good reason. Consider instead using GetGlobalProperty with
        /// a type parameter.
        /// Throws an UDSException if there is no such Global Property, so 
        /// HasGlobalProperty should be checked before GetGlobalProperty.
        /// </summary>
        /// <seealso cref="GetProperty{T}(string)"/>
        /// <seealso cref="HasProperty(string)"/>
        /// <param name="key">The key of the Global Property</param>
        /// <returns>The UDSProperty struct with key key</returns>
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

        /// <summary>
        /// Deletes the Global Property with key key and 
        /// returns whether it existed
        /// </summary>
        /// <param name="key">The key of the Global Property to delete</param>
        /// <returns>Whether or not the Global Property existed</returns>
        public bool DeleteGlobalProperty(string key)
        {
            return globalProperties.Remove(key);
        }

        /// <summary>
        /// Deletes all Global Properties - use with caution, 
        /// especially when Global Properties are being saved
        /// </summary>
        public void DeleteAllGlobalProperties()
        {
            foreach (string property in GetGlobalPropertyKeys())
                DeleteGlobalProperty(property);
        }

        /// <summary>
        /// Checks whether there is a Global Property of type T with key key
        /// </summary>
        /// <seealso cref="HasGlobalProperty{T}(string)"/>
        /// <seealso cref="HasGlobalProperty(string)"/>
        /// <typeparam name="T">The type of Global Property to be looked for</typeparam>
        /// <param name="key">The Global Property key to be looked for</param>
        /// <returns>Whether or not the DialogueComponent has a Global Property 
        /// of type T with key key</returns>
        public bool HasGlobalProperty<T>(string key)
            => globalProperties.ContainsKey(key) && globalProperties[key].type == typeof(T);

        /// <summary>
        /// Checks whether there is a Global Property of type T with key key
        /// </summary>
        /// <seealso cref="HasGlobalProperty{T}(string)"/>
        /// <seealso cref="HasGlobalProperty(string)"/>
        /// <typeparam name="T">The type of Global Property to be looked for</typeparam>
        /// <param name="key">The Global Property key to be looked for</param>
        /// <returns>Whether or not the DialogueComponent has a Global Property 
        /// of type T with key key</returns>
        public bool HasGlobalProperty(string key)
            => globalProperties.ContainsKey(key);

        /// <summary>
        /// Checks whether there is a Global Property of type type with key key
        /// </summary>
        /// <seealso cref="HasGlobalProperty{T}(string)"/>
        /// <seealso cref="HasGlobalProperty(string)"/>
        /// <param name="type">The type of Global Property to be looked for</typeparam>
        /// <param name="key">The Global Property key to be looked for</param>
        /// <returns>Whether or not the DialogueComponent has a Global Property 
        /// of type type with key key</returns>
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

            SetTextOnTextBox(DialogueTextBox, "");
            float textRevealSpeed = currentDialoguePart.GetProperty<float>("Text speed");

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
                                                          ? DialogueTextBox.textTMP.text.Insert(cursor, startTag + endTag)
                                                          : DialogueTextBox.text.text.Insert(cursor, startTag + endTag);

                                                SetTextOnTextBox(DialogueTextBox, newText);

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
                        ? DialogueTextBox.textTMP.text.Insert(cursor, baseText[cursor].ToString())
                        : DialogueTextBox.text.text.Insert(cursor, baseText[cursor].ToString());

                    SetTextOnTextBox(DialogueTextBox, newText);

                    float actualTextSpeed 
                        = 1.53f - 0.5f * baseTextSpeed - textRevealSpeed;

                    if (currentDialogue.GetProperty<bool>("Pause during Dialogue"))
                        yield return new WaitForSecondsRealtime(actualTextSpeed);
                    else
                        yield return new WaitForSeconds(actualTextSpeed);
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
        private Dialogue[] LoadDialogues()
        {
            List<Dialogue> dialogues = new List<Dialogue>();

            TextAsset[] dialogueAssets = null;
            try
            {
                dialogueAssets = Resources.LoadAll("Dialogues", typeof(TextAsset))
                                        .Cast<TextAsset>().ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError("Error while loading Dialogues. Please make sure that " +
                    "the Resources\\Dialogues folder exists\n\n" + e.Message);
            }

            if (dialogueAssets == null)
                return dialogues.ToArray();

            foreach (TextAsset dialogueFile in dialogueAssets)
            {
                Dialogue dialogueInstance = JsonConvert.DeserializeObject<Dialogue>(dialogueFile.text);
                dialogues.Add(dialogueInstance);
            }

            return dialogues.ToArray();
        }

        private Dialogue LoadDialogue(string DialogueID)
        {
            TextAsset dialogueAsset = null;
            try
            {
                dialogueAsset = Resources.Load<TextAsset>(Path.Combine("Dialogues", DialogueID) + ".udsDialogue");
            }
            catch (Exception e)
            {
                Debug.LogError("Error while loading a Dialogue. Please make sure that " +
                    "the Resources\\Dialogues\\" + DialogueID + ".udsDialogue.json asset exists and is valid\n\n" + e.Message);
            }

            if (dialogueAsset == null)
                return null;

            Dialogue dialogueInstance = JsonConvert.DeserializeObject<Dialogue>(dialogueAsset.text);

            return dialogueInstance;
        }

        /// <summary>
        /// Determines whether a DialoguePart leads directly to the end
        /// of the Dialogue. That is, it has no answers, all parts that
        /// come after it have no answers and one of the following
        /// parts is a end of the Dialogue, which also means that there
        /// are no cycles "beyond" that DialoguePart.
        /// 
        /// (works iteratively to avoid StackOverflows and
        /// excessive memory usage)
        /// </summary>
        /// <seealso cref="IsEndBranch(Dialogue.DialoguePart.Answer)"/>
        /// <param name="dialoguePart">The DialoguePart to be evaluated</param>
        /// <returns>Whether or not this DialoguePart leads directly to the end of the Dialogue</returns>
        protected bool IsEndBranch(Dialogue.DialoguePart dialoguePart)
        {
            // The start of the branch doesn't exist
            if (dialoguePart == null)
                return true;

            // The part that is currently being evaluated
            Dialogue.DialoguePart currentPart = dialoguePart;

            // HashSet for loop detection
            HashSet<Dialogue.DialoguePart> alreadyVisited = new HashSet<Dialogue.DialoguePart>();

            while (!alreadyVisited.Contains(currentPart))
            {
                alreadyVisited.Add(currentPart);

                // There are answers in the currentPart
                if (currentPart.answers.Length > 0)
                    return false;
                else if (string.IsNullOrWhiteSpace(currentPart.nextDialoguePartID)) // End reached
                    return true;
                else // Find and check next part
                {
                    var followingPart = Array.Find(currentDialogue.dialogueParts,
                                                   dp => dp.id.Equals(dialoguePart.nextDialoguePartID));

                    currentPart = followingPart;
                }
            }

            return false; // Loop was detected
        }

        /// <summary>
        /// Determines whether an answer leads directly to the end
        /// of the Dialogue. That is, all parts that
        /// come after it have no answers and one of the following
        /// parts is a end of the Dialogue, which also means that there
        /// are no cycles "beyond" that answer.
        /// 
        /// (works iteratively to avoid StackOverflows and
        /// excessive memory usage)
        /// </summary>
        /// <seealso cref="IsEndBranch(Dialogue.DialoguePart)"/>
        /// <param name="answer">The DialoguePart to be evaluated</param>
        /// <returns>Whether or not this answer leads directly to the end of the Dialogue</returns>
        protected bool IsEndBranch(Dialogue.DialoguePart.Answer answer)
        {
            // The start of the branch doesn't exist
            if (answer == null || string.IsNullOrWhiteSpace(answer.nextDialoguePartID))
                return true;

            // Evaluate the branch from the DialoguePart following the answer
            var followingDP 
                = Array.Find(currentDialogue.dialogueParts, dp => dp.id.Equals(answer.nextDialoguePartID));

            // Do so using the already defined method IsEndBranch(DialoguePart)
            return IsEndBranch(followingDP);
        }

        /// <summary>
        /// Determines the index of an Answer (that is, which Answer Box
        /// belongs to it) considering Conditional Answers whose condition
        /// is not met. Always use this instead of answer.index!
        /// </summary>
        /// <param name="answer">The Answer whose index shall be determined</param>
        /// <returns>The index of the Answer considering Conditional Answers whose conditions is not met</returns>
        protected int GetAnswerIndex(Dialogue.DialoguePart.Answer answer)
        {
            int i, j;
            for (i = 0, j = 0; i < answer.index; i++, j++)
            {
                if (currentDialoguePart.answers[i].conditional
                    && !currentDialoguePart.answers[i].condition.Value.IsMet())
                {
                    j--;
                }
            }

            return j;
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
        /// the UDSDialogueManager. These are the coroutines for gradually revealing 
        /// and formatting the text, the Dialogue start coroutine and the Dialogue 
        /// end coroutine
        /// </summary>
        protected void StopDialoguePlaybackCoroutines()
        {
            if (revealTextGraduallyCo != null) StopCoroutine(revealTextGraduallyCo);
            textEffectRunning = false;
            if (startCoroutine != null) StopCoroutine(startCoroutine);
            if (stopCoroutine != null) StopCoroutine(stopCoroutine);
        }
        #endregion
    }
}
