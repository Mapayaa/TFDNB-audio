using System.Collections.Generic;
using UnityEngine;

namespace VN3D.Shared
{
    public class ChapterManager : MonoBehaviour
    {
        [Header("Chapter Info")]
        public int chapterNumber;

        [Header("Hoofdstuk Overgangen")]
        public ChapterManager nextChapter;
        public StoryNode triggerNextChapterNode;

        private void OnValidate()
        {
            // Probeer nummer uit naam te halen: "Chapter X"
            if (!string.IsNullOrEmpty(gameObject.name))
            {
                var name = gameObject.name.Trim();
                if (name.StartsWith("Chapter "))
                {
                    string numStr = name.Substring("Chapter ".Length);
                    if (int.TryParse(numStr, out int num))
                    {
                        if (chapterNumber != num)
                            chapterNumber = num;
                    }
                }
            }
        }

        [Header("UI Elements (delen met StoryManager)")]
        public UnityEngine.UI.Image backgroundImage;
        public TMPro.TextMeshProUGUI textBox;
        public UnityEngine.UI.Image sewingActiveImage;
        public GameObject userRoleUI;
        public GameObject machineRoleUI;
        public GameObject extraRoleUI;
        public UnityEngine.UI.Image sewingSuccessImage;
        public UnityEngine.UI.Image clickedImage;
        public TMPro.TextMeshProUGUI errorMessageBox;
        public TMPro.TextMeshProUGUI verboseSewingText;
        public TMPro.TextMeshProUGUI verboseClickText;
        public TMPro.TextMeshProUGUI verboseErrorText;
        public AudioClip sewingSoundClip;
        public AudioSource sewingAudioSource;
        public AudioSource backgroundAudioSource;
        public AudioSource wordAudioSource;
        public AudioClip typingSoundClip;

        [Header("SewNodes")]
        public List<GameObject> sewList = new List<GameObject>();

        [Header("ClickLists")]
        public List<GameObject> clickLists = new List<GameObject>();

        [Header("ForcedLists")]
        public List<GameObject> forcedLists = new List<GameObject>();

        [Header("EndLists")]
        public List<GameObject> endLists = new List<GameObject>();

        [Header("Game Over")]
        public GameObject gameOverObject;

        // --- Toegevoegd: runtime state en animatie/geluid velden ---
        private int currentSewIndex = 0;

        // ClickList progressie teller
        private int clickListProgression = -1;

        /// <summary>
        /// Activeert het volgende hoofdstuk en deactiveert dit hoofdstuk.
        /// </summary>
        public void TriggerNextChapter()
        {
            if (nextChapter != null)
            {
                gameObject.SetActive(false);
                nextChapter.gameObject.SetActive(true);
            }
        }
        private int currentClickListIndex = 0;
        private int currentClickEntryIndex = 0;
        private bool inClickList = false;
        private bool inputDisabled = false;

        // ForcedList-modus
        private bool inForcedListMode = false;
        private GameObject activeForcedListGO = null;
        private int forcedListNodeIndex = 0;

        // EndList-modus
        private bool inEndListMode = false;
        private GameObject activeEndListGO = null;
        private int endListNodeIndex = 0;
        private bool isSewing = false;
        private bool sewingSoundActiveForThisHold = false;
        private float accumulatedSewingDistance = 0f;
        private bool sewDistanceReached = false; // Flag om bij te houden of sewDistance is bereikt
        private Coroutine displayTextCoroutine;
        private List<RevealedChar> activeTextCharacters;
        private System.Text.StringBuilder textBuilder = new System.Text.StringBuilder();
        private float baseFontSize = 36f;
        private GameObject currentNodeGO;
        private GameObject pendingTextNodeGO = null;

        [Header("Per-Letter Text Reveal Settings")]
        public float letterReveal_MinDelay = 0.02f;
        public float letterReveal_MaxDelay = 0.1f;
        public float letterReveal_HighlightDuration = 1.0f;
        public float letterReveal_SizeMultiplier = 1.2f;
        public Color letterReveal_HighlightColor = new Color(0.85f, 0.85f, 0.85f, 1f);

        [Header("Sewing Mechanic State")]
        public float requiredDistance = 50f;
        public float maxHorizontalToVerticalRatio = 0.5f;
        public float minYDeltaForRatioCheck = 0.01f;
        public float minXDeltaWhenYIsZero = 0.01f;
        public float mouseSensitivityY_Sewing = 10f;
        public float mouseSensitivityX_Sewing = 10f;

        private struct RevealedChar
        {
            public char character;
            public float appearanceTime;
            public bool isVisible;
            public bool isSpaceOrNewline;
        }

        void Awake()
        {
            if (textBox != null)
                baseFontSize = textBox.fontSize;
            else
                baseFontSize = 36f;

            if (sewingSuccessImage == null)
                Debug.LogWarning("ChapterManager: sewingSuccessImage is NOT assigned in the Inspector!");
            else
                Debug.Log("ChapterManager: sewingSuccessImage is assigned: " + sewingSuccessImage.name);

            if (sewingAudioSource == null)
            {
                sewingAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning("ChapterManager: sewingAudioSource was missing and has been added automatically.");
            }
            if (backgroundAudioSource == null)
            {
                backgroundAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning("ChapterManager: backgroundAudioSource was missing and has been added automatically.");
            }
            if (wordAudioSource == null)
            {
                wordAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning("ChapterManager: wordAudioSource was missing and has been added automatically.");
            }
        }

        void Start()
        {
            StartChapter();
        }

        void Update()
        {
            if (inputDisabled) return;

            // ForcedList: clicks negeren, alleen sew toestaan
            if (inForcedListMode)
            {
                Debug.Log("DEBUG: inForcedListMode - forcedList blok bereikt in Update()");
                // Clicks negeren, maar GEEN directe Next("sew") op spatie
                // SewDistance-logica blijft actief hieronder
                // return; // NIET meer returnen, zodat sewDistance-check altijd mogelijk is
            }

            // --- Sewing input ---
            if (isSewing && Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (sewingAudioSource != null && sewingAudioSource.isPlaying) sewingAudioSource.Stop();
                isSewing = false;
                sewingSoundActiveForThisHold = false;
                accumulatedSewingDistance = 0f;
                if (sewingActiveImage != null)
                    sewingActiveImage.gameObject.SetActive(false);
            }

            if (!isSewing && Input.GetKeyDown(KeyCode.Space))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                isSewing = true;
                sewingSoundActiveForThisHold = true;
                accumulatedSewingDistance = 0f;
                if (sewingActiveImage != null)
                    sewingActiveImage.gameObject.SetActive(true);
                if (sewingSoundActiveForThisHold && sewingAudioSource != null && sewingSoundClip != null && !sewingAudioSource.isPlaying)
                {
                    sewingAudioSource.clip = sewingSoundClip;
                    sewingAudioSource.Play();
                }
            }
            else if (isSewing && Input.GetKeyUp(KeyCode.Space))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (sewingAudioSource != null && sewingAudioSource.isPlaying)
                    sewingAudioSource.Stop();
                isSewing = false;
                sewingSoundActiveForThisHold = false;
                
                // Check of sewDistance was bereikt voordat spatiebalk werd losgelaten
                if (sewDistanceReached)
                {
                    Debug.Log("DEBUG: Sew actie voltooid bij loslaten spatiebalk");
                    Next("sew");
                    sewDistanceReached = false; // Reset de flag
                }
                
                accumulatedSewingDistance = 0f;
                if (sewingActiveImage != null)
                    sewingActiveImage.gameObject.SetActive(false);
            }

            if (isSewing)
            {
                float rawDeltaY = Input.GetAxis("Mouse Y");
                float rawDeltaX = Input.GetAxis("Mouse X");
                float effectiveDeltaY = rawDeltaY * mouseSensitivityY_Sewing;
                float effectiveDeltaX = rawDeltaX * mouseSensitivityX_Sewing;

                bool movementIsValidForStreak = true;

                if (effectiveDeltaY < 0)
                {
                    movementIsValidForStreak = false;
                }
                else if (effectiveDeltaY > minYDeltaForRatioCheck)
                {
                    if (Mathf.Abs(effectiveDeltaX) > (effectiveDeltaY * maxHorizontalToVerticalRatio))
                    {
                        movementIsValidForStreak = false;
                    }
                }
                else if (Mathf.Approximately(effectiveDeltaY, 0) && Mathf.Abs(effectiveDeltaX) > (minXDeltaWhenYIsZero * mouseSensitivityX_Sewing))
                {
                    movementIsValidForStreak = false;
                }

                if (movementIsValidForStreak && effectiveDeltaY > 0)
                {
                    accumulatedSewingDistance += effectiveDeltaY;
                }
                else if (!movementIsValidForStreak && (Mathf.Abs(rawDeltaX) > 0.001f || Mathf.Abs(rawDeltaY) > 0.001f))
                {
                    accumulatedSewingDistance = 0f;
                }

                if (accumulatedSewingDistance >= requiredDistance && !sewDistanceReached)
                {
                    Debug.Log("DEBUG: sewDistance gehaald - accumulatedSewingDistance=" + accumulatedSewingDistance + ", requiredDistance=" + requiredDistance + ", inForcedListMode=" + inForcedListMode);
                    if (sewingAudioSource != null && sewingAudioSource.isPlaying)
                        sewingAudioSource.Stop();
                    sewingSoundActiveForThisHold = false;

                    // Toon succesfull sew image direct bij behalen sewDistance
                    if (sewingSuccessImage != null)
                    {
                        Debug.Log("SewingSuccessImage: SetActive(true) bij sewDistance gehaald");
                        sewingSuccessImage.gameObject.SetActive(true);
                        Debug.Log("SewingSuccessImage: SetActive(true) uitgevoerd");
                        StartCoroutine(HideSewingSuccessImage());
                    }

                    // Zet flag dat sewDistance is bereikt, maar roep Next("sew") nog niet aan
                    sewDistanceReached = true;
                    Debug.Log("DEBUG: sewDistanceReached flag gezet, wacht op loslaten spatiebalk");
                    accumulatedSewingDistance = 0f;
                }
            }

            // --- Debug tellers ---
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log($"DEBUG TELLERS: currentSewIndex={currentSewIndex}, clickListProgression={clickListProgression}, currentClickListIndex={currentClickListIndex}, currentClickEntryIndex={currentClickEntryIndex}, inClickList={inClickList}, inForcedListMode={inForcedListMode}, inEndListMode={inEndListMode}");
            }

            // --- Click input ---
            if (Input.GetMouseButtonDown(0))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    if (isSewing)
                    {
                        isSewing = false;
                        sewingSoundActiveForThisHold = false;
                        if (sewingAudioSource != null && sewingAudioSource.isPlaying) sewingAudioSource.Stop();
                        accumulatedSewingDistance = 0f;
                    }
                }
                
                // In EndList mode, click goes to next endlist entry
                if (inEndListMode)
                {
                    Next("endlist");
                }
                else
                {
                    Next("click");
                }
            }

            // Start alsnog de tekstanimatie als de spatiebalk wordt losgelaten en er een pending node is
            if (pendingTextNodeGO != null && !Input.GetKey(KeyCode.Space) && displayTextCoroutine == null)
            {
                displayTextCoroutine = StartCoroutine(AnimateTextCharacterByCharacter(pendingTextNodeGO));
                pendingTextNodeGO = null;
            }
        }

        /// <summary>
        /// Synchroniseert de lijsten met de scene-hierarchie.
        /// </summary>
        public void SyncLists()
        {
            sewList.Clear();
            clickLists.Clear();
            forcedLists.Clear();
            endLists.Clear();

            foreach (Transform child in transform)
            {
                if (child.name == "SewNodes")
                {
                    foreach (Transform sewNode in child)
                        sewList.Add(sewNode.gameObject);
                }
                else if (child.name == "ClickLists")
                {
                    foreach (Transform clickList in child)
                        clickLists.Add(clickList.gameObject);
                }
                else if (child.name == "ForcedLists")
                {
                    foreach (Transform forcedList in child)
                        forcedLists.Add(forcedList.gameObject);
                }
                else if (child.name == "EndLists")
                {
                    foreach (Transform endList in child)
                        endLists.Add(endList.gameObject);
                }
            }
        }

        /// <summary>
        /// Start hoofdstuk 6: initialiseer UI en logica.
        /// </summary>
        public void StartChapter6()
        {
            Debug.Log("ChapterManager: StartChapter6() aangeroepen. (Hier UI/animatie/audio initialiseren)");
            gameObject.SetActive(true);
            // Altijd resetten naar begin
            currentSewIndex = 0;
            currentClickListIndex = 0;
            currentClickEntryIndex = 0;
            clickListProgression = -1;
            inClickList = false;
            inForcedListMode = false;
            inEndListMode = false;
            activeForcedListGO = null;
            activeEndListGO = null;
            forcedListNodeIndex = 0;
            endListNodeIndex = 0;
            inputDisabled = false;
            // Altijd starten bij eerste node in sewList (indien aanwezig)
            if (sewList != null && sewList.Count > 0 && sewList[0] != null)
            {
                ShowNodeGO(sewList[0]);
            }
            else
            {
                ShowNode("Geen startnode gevonden in sewList!");
            }
        }

        /// <summary>
        /// Start hoofdstuk vanaf eerste node in sewList (voor universeel gebruik)
        /// </summary>
        public void StartChapter()
        {
            gameObject.SetActive(true);
            currentSewIndex = 0;
            currentClickListIndex = 0;
            currentClickEntryIndex = 0;
            clickListProgression = -1;
            inClickList = false;
            inForcedListMode = false;
            inEndListMode = false;
            activeForcedListGO = null;
            activeEndListGO = null;
            forcedListNodeIndex = 0;
            endListNodeIndex = 0;
            inputDisabled = false;
            if (sewList != null && sewList.Count > 0 && sewList[0] != null)
            {
                ShowNodeGO(sewList[0]);
            }
            else
            {
                ShowNode("Geen startnode gevonden in sewList!");
            }
        }

        /// <summary>
        /// Toon tekst en update UI voor hoofdstuk 6.
        /// </summary>
        public void ShowNode(string tekst)
        {
            if (textBox != null)
                textBox.text = tekst;
            if (backgroundImage != null)
                backgroundImage.gameObject.SetActive(true);
        }

        // --- Nieuw: Toon node via GameObject (met StoryNode) ---
        public void ShowNodeGO(GameObject nodeGO)
        {
            if (nodeGO == null) return;
            currentNodeGO = nodeGO;
            var storyNode = nodeGO.GetComponent<StoryNode>();
            if (storyNode != null)
            {
                // Controleer of deze node de trigger is voor het volgende hoofdstuk
                if (storyNode == triggerNextChapterNode)
                {
                    TriggerNextChapter();
                }

                // Deactiveer alle role UI's
                if (userRoleUI != null) userRoleUI.SetActive(false);
                if (machineRoleUI != null) machineRoleUI.SetActive(false);
                if (extraRoleUI != null) extraRoleUI.SetActive(false);

                // Activeer juiste role UI
                switch (storyNode.nodeRole)
                {
                    case StoryNode.NodeRole.User:
                        if (userRoleUI != null) userRoleUI.SetActive(true);
                        break;
                    case StoryNode.NodeRole.Machine:
                        if (machineRoleUI != null) machineRoleUI.SetActive(true);
                        break;
                    case StoryNode.NodeRole.Extra:
                        if (extraRoleUI != null) extraRoleUI.SetActive(true);
                        break;
                }

                // Activate GameObjects
                storyNode.ActivateGameObjects();

                InitializeTextForAnimation(storyNode.nodeText);
                if (textBox != null) textBox.text = "";
                if (!Input.GetKey(KeyCode.Space))
                {
                    if (displayTextCoroutine == null)
                        displayTextCoroutine = StartCoroutine(AnimateTextCharacterByCharacter(nodeGO));
                    pendingTextNodeGO = null;
                }
                else
                {
                    pendingTextNodeGO = nodeGO;
                }
                if (backgroundImage != null && storyNode.background != null)
                {
                    backgroundImage.sprite = storyNode.background;
                    backgroundImage.gameObject.SetActive(true);
                }
                if (backgroundAudioSource != null)
                {
                    if (backgroundAudioSource.isPlaying)
                        backgroundAudioSource.Stop();
                    if (storyNode.backgroundSound != null)
                    {
                        backgroundAudioSource.clip = storyNode.backgroundSound;
                        backgroundAudioSource.Play();
                    }
                }
                
                // Update verbose text to show what next nodes will be
                UpdateNextNodePreviews();
            }
        }
        
        // Helper method to update the preview text for next sew and click nodes
        private void UpdateNextNodePreviews()
        {
            // Determine next sew node
            string nextSewNodeText = "";
            GameObject nextSewNode = DetermineNextNode("sew");
            if (nextSewNode != null)
            {
                var sewStoryNode = nextSewNode.GetComponent<StoryNode>();
                if (sewStoryNode != null && !string.IsNullOrEmpty(sewStoryNode.nodeText))
                {
                    // Take first 50 characters of the text as preview
                    string preview = sewStoryNode.nodeText;
                    if (preview.Length > 50)
                        preview = preview.Substring(0, 47) + "...";
                    nextSewNodeText = $"Sew → {nextSewNode.name}: {preview}";
                }
                else
                {
                    nextSewNodeText = $"Sew → {nextSewNode.name}";
                }
            }
            else
            {
                nextSewNodeText = "Sew → (geen volgende node)";
            }
            
            // Determine next click node
            string nextClickNodeText = "";
            GameObject nextClickNode = DetermineNextNode("click");
            if (nextClickNode != null)
            {
                var clickStoryNode = nextClickNode.GetComponent<StoryNode>();
                if (clickStoryNode != null && !string.IsNullOrEmpty(clickStoryNode.nodeText))
                {
                    // Take first 50 characters of the text as preview
                    string preview = clickStoryNode.nodeText;
                    if (preview.Length > 50)
                        preview = preview.Substring(0, 47) + "...";
                    nextClickNodeText = $"Click → {nextClickNode.name}: {preview}";
                }
                else
                {
                    nextClickNodeText = $"Click → {nextClickNode.name}";
                }
            }
            else
            {
                nextClickNodeText = "Click → (geen volgende node)";
            }
            
            // Update UI text elements
            if (verboseSewingText != null)
                verboseSewingText.text = nextSewNodeText;
            
            if (verboseClickText != null)
                verboseClickText.text = nextClickNodeText;
        }
        
        // Helper method to determine what the next node would be for a given action
        private GameObject DetermineNextNode(string method)
        {
            GameObject nextNodeGO = null;
            
            if (method == "click")
            {
                if (inForcedListMode) return null; // Clicks blocked in forcedList mode
                
                // Click in sewList: check if it would activate clickList
            if (!inClickList && sewList.Count > 0 && currentSewIndex < sewList.Count)
            {
                var sewNode = sewList[currentSewIndex].GetComponent<StoryNode>();
                if (sewNode != null)
                {
                    if (sewNode.disableClick)
                        return null;
                    if (sewNode.meetellenAlsClick)
                    {
                        int nextClickListIndex = clickListProgression + 1;
                        int useIndex = Mathf.Clamp(nextClickListIndex, 0, clickLists.Count - 1);
                        if (clickLists.Count > 0 && useIndex < clickLists.Count && clickLists[useIndex].transform.childCount > 0)
                        {
                            return clickLists[useIndex].transform.GetChild(0).gameObject;
                        }
                    }
                }
            }
                
                // Click in clicklist
                if (inClickList && clickLists.Count > 0)
                {
                    int useClickList = Mathf.Clamp(currentClickListIndex, 0, clickLists.Count - 1);
                    var clickListGO = clickLists[useClickList];
                    if (clickListGO != null && clickListGO.transform.childCount > 0)
                    {
                        int nextEntry = currentClickEntryIndex + 1;
                        if (nextEntry >= clickListGO.transform.childCount)
                            nextEntry = 0;
                        return clickListGO.transform.GetChild(nextEntry).gameObject;
                    }
                }
            }
            else if (method == "sew")
            {
                // Sew in clicklist: would return to sewList
                if (inClickList && !inForcedListMode && !inEndListMode && clickLists.Count > 0)
                {
                    int useClickList = Mathf.Clamp(currentClickListIndex, 0, clickLists.Count - 1);
                    var clickListGO = clickLists[useClickList];
                    if (clickListGO != null && clickListGO.transform.childCount > 0)
                    {
                        var entryGO = clickListGO.transform.GetChild(currentClickEntryIndex).gameObject;
                        var storyNode = entryGO.GetComponent<StoryNode>();
                        if (storyNode == null || !storyNode.disableSew)
                        {
                            int nextSewIndex = currentSewIndex;
                            if (nextSewIndex < sewList.Count - 1)
                                nextSewIndex++;
                            return sewList[Mathf.Clamp(nextSewIndex, 0, sewList.Count - 1)];
                        }
                    }
                }
                
                // ForcedList sew
                if (inForcedListMode && activeForcedListGO != null)
                {
                    int nextIndex = forcedListNodeIndex + 1;
                    if (nextIndex < activeForcedListGO.transform.childCount)
                    {
                        return activeForcedListGO.transform.GetChild(nextIndex).gameObject;
                    }
                    else
                    {
                        // Would return to sewList
                        if (sewList.Count > 0)
                        {
                            int nextSewIndex = currentSewIndex;
                            if (nextSewIndex < sewList.Count - 1)
                                nextSewIndex++;
                            return sewList[Mathf.Clamp(nextSewIndex, 0, sewList.Count - 1)];
                        }
                    }
                }
                
                // Normal sew in sewList
                if (!inClickList && !inForcedListMode && !inEndListMode)
                {
                    if (sewList.Count > 0)
                    {
                        int nextSewIndex = currentSewIndex;
                        if (nextSewIndex < sewList.Count - 1)
                            nextSewIndex++;
                        return sewList[Mathf.Clamp(nextSewIndex, 0, sewList.Count - 1)];
                    }
                }
            }
            
            return nextNodeGO;
        }

        // --- Nieuw: Next logica voor sew/click acties ---
        public void Next(string method)
        {
            if (inputDisabled) return;
            
            // Deactivate GameObjects from the CURRENT node before moving to the next
            if (currentNodeGO != null)
            {
                var currentStoryNode = currentNodeGO.GetComponent<StoryNode>();
                if (currentStoryNode != null)
                {
                    currentStoryNode.DeactivateGameObjects();
                }
            }

            GameObject nextNodeGO = null;

            // Stop tekstanimatie
            if (displayTextCoroutine != null)
            {
                try { StopCoroutine(displayTextCoroutine); } catch { }
                if (wordAudioSource != null && wordAudioSource.isPlaying)
                    wordAudioSource.Stop();
                displayTextCoroutine = null;
                if (currentNodeGO != null && textBox != null)
                {
                    var sn = currentNodeGO.GetComponent<StoryNode>();
                    if (sn != null)
                        textBox.text = sn.nodeText;
                }
            }

            if (method == "click")
            {
                if (inForcedListMode) return; // Clicks blokkeren in forcedList-modus
                Debug.Log("DEBUG: Next('click') aangeroepen, inClickList=" + inClickList + ", currentSewIndex=" + currentSewIndex + ", sewList.Count=" + sewList.Count);
                StartCoroutine(ShowClickedImage());
                // Click in sewList: activeer clickList als 'meetellenAlsClick' true is
if (!inClickList && sewList.Count > 0 && currentSewIndex < sewList.Count)
{
    var sewNode = sewList[currentSewIndex].GetComponent<StoryNode>();
    Debug.Log("DEBUG: sewNode.meetellenAlsClick=" + (sewNode != null ? sewNode.meetellenAlsClick.ToString() : "null"));
    // FIX: Check ook op disableClick
    if (sewNode != null && sewNode.meetellenAlsClick)
    {
        if (sewNode.disableClick)
        {
            Debug.Log("ClickList activatie geblokkeerd: disableClick is actief op deze sewNode.");
            return;
        }
        Debug.Log("DEBUG: clickListProgression vóór = " + clickListProgression);
        // Gebruik clickListProgression + 1 omdat we nog niet verhoogd hebben
        int nextClickListIndex = clickListProgression + 1;
        currentClickListIndex = Mathf.Clamp(nextClickListIndex, 0, clickLists.Count - 1);
        inClickList = true;
        currentClickEntryIndex = 0;
        if (clickLists.Count > 0 && currentClickListIndex < clickLists.Count && clickLists[currentClickListIndex].transform.childCount > 0)
        {
            ShowNodeGO(clickLists[currentClickListIndex].transform.GetChild(0).gameObject);
            clickListProgression = currentClickListIndex; // Sync progression met de index
            Debug.Log("DEBUG: clickListProgression na click in sewList = " + clickListProgression);
            return;
        }
    }
}

                // Click in clicklist: check disableClick
                if (inClickList && clickLists.Count > 0)
                {
                    int useClickList = Mathf.Clamp(currentClickListIndex, 0, clickLists.Count - 1);
                    var clickListGO = clickLists[useClickList];
                    if (clickListGO != null && clickListGO.transform.childCount > 0)
                    {
                        var entryGO = clickListGO.transform.GetChild(currentClickEntryIndex).gameObject;
                        var storyNode = entryGO.GetComponent<StoryNode>();
                        if (storyNode != null && storyNode.disableClick)
                        {
                            Debug.Log("Click is uitgeschakeld voor deze entry ('disableClick' aan).");
                            return;
                        }
                    }
                }

                // --- EndList check ---
                if (!inEndListMode && clickLists.Count > 0 && endLists.Count > 0)
                {
                    int useClickList = Mathf.Clamp(currentClickListIndex, 0, clickLists.Count - 1);
                    var clickListGO = clickLists[useClickList];
                    string clickListName = clickListGO != null ? clickListGO.name : "";
                    int entryIndex = currentClickEntryIndex; // Gebruik actuele index, net als bij forcedList
                    foreach (var endListGO in endLists)
                    {
                        if (endListGO == null) continue;
                        // Verwacht naam: E# [H#.C# van-tot]
                        string endName = endListGO.name;
                        int bracketStart = endName.IndexOf('[');
                        int bracketEnd = endName.IndexOf(']');
                        if (bracketStart >= 0 && bracketEnd > bracketStart)
                        {
                            string inside = endName.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();
                            // inside: H#.C# van-tot
                            var parts = inside.Split(' ');
                            if (parts.Length == 2)
                            {
                                string endClickListName = parts[0].Trim();
                                string[] range = parts[1].Split('-');
                                if (range.Length == 2 && int.TryParse(range[0], out int from) && int.TryParse(range[1], out int to))
                                {
                                    Debug.Log($"ENDLIST check: clickList={clickListName}, entryIndex={entryIndex}, endList={endName}, range={from}-{to}");
                                    if (clickListName == endClickListName && entryIndex >= from && entryIndex <= to)
                                    {
                                        Debug.Log("ENDLIST ACTIEF");
                                        inClickList = false; // Verlaat clickList
                                        inEndListMode = true;
                                        activeEndListGO = endListGO;
                                        endListNodeIndex = 0;
                                        if (activeEndListGO.transform.childCount > 0)
                                        {
                                            ShowNodeGO(activeEndListGO.transform.GetChild(0).gameObject);
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }


                if (!inClickList && clickLists.Count > 0)
                {
                    inClickList = true;
                    currentClickEntryIndex = 0;
                }
                else
                {
                    currentClickEntryIndex++;
                }
                if (clickLists.Count > 0)
                {
                    int useClickList = Mathf.Clamp(currentClickListIndex, 0, clickLists.Count - 1);
                    var clickListGO = clickLists[useClickList];
                    if (clickListGO != null && clickListGO.transform.childCount > 0)
                    {
                        if (currentClickEntryIndex >= clickListGO.transform.childCount)
                            currentClickEntryIndex = 0;
                        nextNodeGO = clickListGO.transform.GetChild(currentClickEntryIndex).gameObject;
                    }
                }
            }
            else if (method == "sew")
            {
                // EndList-modus: sew naar volgende entry
                if (inEndListMode && activeEndListGO != null)
                {
                    endListNodeIndex++;
                    if (endListNodeIndex < activeEndListGO.transform.childCount)
                    {
                        ShowNodeGO(activeEndListGO.transform.GetChild(endListNodeIndex).gameObject);
                    }
                    else
                    {
                        // EndList klaar, spel uit
                        Debug.Log("GAME OVER");
                        inEndListMode = false;
                        activeEndListGO = null;
                        endListNodeIndex = 0;
                        
                        // Activeer game over object en deactiveer ChapterManager
                        if (gameOverObject != null)
                        {
                            gameOverObject.SetActive(true);
                        }
                        gameObject.SetActive(false);
                    }
                    return;
                }
                
                // --- ForcedList check bij sew-actie ---
                if (inClickList && !inForcedListMode && clickLists.Count > 0 && forcedLists.Count > 0)
                {
                    int useClickList = Mathf.Clamp(currentClickListIndex, 0, clickLists.Count - 1);
                    var clickListGO = clickLists[useClickList];
                    string clickListName = clickListGO != null ? clickListGO.name : "";
                    int entryIndex = currentClickEntryIndex; // Gebruik de actuele index
                    foreach (var forcedListGO in forcedLists)
                    {
                        if (forcedListGO == null) continue;
                        // Verwacht naam: F# [H#.C# van-tot]
                        string forcedName = forcedListGO.name;
                        int bracketStart = forcedName.IndexOf('[');
                        int bracketEnd = forcedName.IndexOf(']');
                        if (bracketStart >= 0 && bracketEnd > bracketStart)
                        {
                            string inside = forcedName.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();
                            // inside: H#.C# van-tot
                            var parts = inside.Split(' ');
                            if (parts.Length == 2)
                            {
                                string forcedClickListName = parts[0].Trim();
                                string[] range = parts[1].Split('-');
                                if (range.Length == 2 && int.TryParse(range[0], out int from) && int.TryParse(range[1], out int to))
                                {
                                    Debug.Log($"FORCEDLIST sew check: clickList={clickListName}, entryIndex={entryIndex}, forcedList={forcedName}, range={from}-{to}");
                                    if (clickListName == forcedClickListName && entryIndex >= from && entryIndex <= to)
                                    {
                                        Debug.Log("FORCEDLIST ACTIEF via sew");
                                        inClickList = false; // Verlaat clickList
                                        // Verhoog clickListProgression alleen als we daadwerkelijk de clickList verlaten
                                        inForcedListMode = true;
                                        activeForcedListGO = forcedListGO;
                                        forcedListNodeIndex = 0;
                                        if (activeForcedListGO.transform.childCount > 0)
                                        {
                                            ShowNodeGO(activeForcedListGO.transform.GetChild(0).gameObject);
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                // Sew-actie in clicklist: terug naar sewList (tenzij disableSew)
                if (inClickList && !inForcedListMode && !inEndListMode && clickLists.Count > 0)
                {
                    int useClickList = Mathf.Clamp(currentClickListIndex, 0, clickLists.Count - 1);
                    var clickListGO = clickLists[useClickList];
                    if (clickListGO != null && clickListGO.transform.childCount > 0)
                    {
                        var entryGO = clickListGO.transform.GetChild(currentClickEntryIndex).gameObject;
                        var storyNode = entryGO.GetComponent<StoryNode>();
                        if (storyNode == null || !storyNode.disableSew)
                        {
                            inClickList = false;
                            // NIET verhogen van clickListProgression hier - dit gebeurt al bij het activeren van een clickList
                            // De huidige clickList is "voltooid" en de volgende keer wordt automatisch de volgende gebruikt
                            Debug.Log($"DEBUG: clickList verlaten via sew, clickListProgression blijft {clickListProgression}");
                            
                            // Ga verder in sewList alleen als we niet al aan het einde zijn
                            if (currentSewIndex < sewList.Count - 1)
                            {
                                currentSewIndex++;
                            }
                            currentClickEntryIndex = 0; // Reset voor de volgende clickList
                            ShowNodeGO(sewList[Mathf.Clamp(currentSewIndex, 0, sewList.Count - 1)]);
                            return;
                        }
                    }
                }

                // ForcedList sew-modus
                if (inForcedListMode && activeForcedListGO != null)
                {
                    forcedListNodeIndex++;
                    if (forcedListNodeIndex < activeForcedListGO.transform.childCount)
                    {
                        ShowNodeGO(activeForcedListGO.transform.GetChild(forcedListNodeIndex).gameObject);
                    }
                    else
                    {
                        // ForcedList klaar, terug naar sewList
                        inForcedListMode = false;
                        activeForcedListGO = null;
                        forcedListNodeIndex = 0;
                        if (sewList.Count > 0)
                        {
                            if (currentSewIndex < sewList.Count - 1)
                            {
                                currentSewIndex++;
                            }
                            Debug.Log("DEBUG: currentSewIndex na forcedList = " + currentSewIndex);
                            nextNodeGO = sewList[Mathf.Clamp(currentSewIndex, 0, sewList.Count - 1)];
                        }
                    }
                }
                else if (!inClickList && !inForcedListMode && !inEndListMode)
                {
                    // Normale sew in sewList (niet in een speciale lijst)
                    if (sewList.Count > 0)
                    {
                        if (currentSewIndex < sewList.Count - 1)
                        {
                            currentSewIndex++;
                        }
                        currentClickEntryIndex = 0;
                        nextNodeGO = sewList[Mathf.Clamp(currentSewIndex, 0, sewList.Count - 1)];
                    }
                }
            }
            else if (method == "endlist")
            {
                // EndList-modus: click en sew allebei naar volgende entry
                if (inEndListMode && activeEndListGO != null)
                {
                    endListNodeIndex++;
                    if (endListNodeIndex < activeEndListGO.transform.childCount)
                    {
                        ShowNodeGO(activeEndListGO.transform.GetChild(endListNodeIndex).gameObject);
                    }
                    else
                    {
                        // EndList klaar, spel uit
                        Debug.Log("GAME OVER");
                        inEndListMode = false;
                        activeEndListGO = null;
                        endListNodeIndex = 0;
                        
                        // Activeer game over object en deactiveer ChapterManager
                        if (gameOverObject != null)
                        {
                            gameOverObject.SetActive(true);
                        }
                        gameObject.SetActive(false);
                    }
                    return;
                }
            }

            if (nextNodeGO != null)
            {
                // Log waarschuwing voor de volgende entry
                string currentNodeName = currentNodeGO != null ? currentNodeGO.name : "null";
                string nextNodeName = nextNodeGO.name;
                
                if (method == "click")
                {
                    string locationInfo = "";
                    if (inClickList)
                    {
                        locationInfo = $"clickList {currentClickListIndex}, entry {currentClickEntryIndex}";
                    }
                    else if (inEndListMode)
                    {
                        locationInfo = $"endList, entry {endListNodeIndex}";
                    }
                    else
                    {
                        locationInfo = $"sewList index {currentSewIndex}";
                    }
                    
                    Debug.LogWarning($"CLICK: Van {currentNodeName} ({locationInfo}) → Naar: {nextNodeName}");
                }
                else if (method == "sew")
                {
                    string locationInfo = "";
                    if (inForcedListMode)
                    {
                        locationInfo = $"forcedList, entry {forcedListNodeIndex}";
                    }
                    else if (inEndListMode)
                    {
                        locationInfo = $"endList, entry {endListNodeIndex}";
                    }
                    else if (inClickList)
                    {
                        locationInfo = $"verlaat clickList {currentClickListIndex}, naar sewList {currentSewIndex}";
                    }
                    else
                    {
                        locationInfo = $"sewList index {currentSewIndex}";
                    }
                    
                    Debug.LogWarning($"SEW: Van {currentNodeName} ({locationInfo}) → Naar: {nextNodeName}");
                }
                
                ShowNodeGO(nextNodeGO);
            }
        }

        // --- Nieuw: Tekstanimatie en helpers ---

        private System.Collections.IEnumerator HideSewingSuccessImage()
        {
            yield return new WaitForSeconds(1f);
            if (sewingSuccessImage != null)
                sewingSuccessImage.gameObject.SetActive(false);
        }

        // Feedback bij click-acties
        private System.Collections.IEnumerator ShowClickedImage()
        {
            if (clickedImage != null)
            {
                clickedImage.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);
                clickedImage.gameObject.SetActive(false);
            }
        }

        private void InitializeTextForAnimation(string textToAnimate)
        {
            if (activeTextCharacters == null)
                activeTextCharacters = new List<RevealedChar>();
            activeTextCharacters.Clear();
            if (string.IsNullOrEmpty(textToAnimate)) return;
            foreach (char c in textToAnimate)
            {
                activeTextCharacters.Add(new RevealedChar
                {
                    character = c,
                    appearanceTime = float.MaxValue,
                    isVisible = false,
                    isSpaceOrNewline = (c == ' ' || c == '\n' || c == '\r')
                });
            }
        }

        private void UpdateDisplayedText()
        {
            if (textBox == null || activeTextCharacters == null) return;
            if (baseFontSize <= 0 && textBox != null) baseFontSize = textBox.fontSize;
            textBuilder.Clear();
            float currentTime = Time.time;
            string highlightColorHex = ColorUtility.ToHtmlStringRGB(letterReveal_HighlightColor);
            string sizeTagStart = $"<size={letterReveal_SizeMultiplier * 100f}%>";
            const string sizeTagEnd = "</size>";
            string colorTagStart = $"<color=#{highlightColorHex}>";
            const string colorTagEnd = "</color>";
            for (int i = 0; i < activeTextCharacters.Count; i++)
            {
                RevealedChar charInfo = activeTextCharacters[i];
                if (!charInfo.isVisible)
                    break;
                if (charInfo.isSpaceOrNewline)
                {
                    textBuilder.Append(charInfo.character);
                }
                else if (currentTime < charInfo.appearanceTime + letterReveal_HighlightDuration)
                {
                    textBuilder.Append(sizeTagStart);
                    textBuilder.Append(colorTagStart);
                    textBuilder.Append(charInfo.character);
                    textBuilder.Append(colorTagEnd);
                    textBuilder.Append(sizeTagEnd);
                }
                else
                {
                    textBuilder.Append(charInfo.character);
                }
            }
            textBox.text = textBuilder.ToString();
        }

        private System.Collections.IEnumerator AnimateTextCharacterByCharacter(GameObject nodeGO)
        {
            if (textBox == null)
            {
                displayTextCoroutine = null;
                yield break;
            }
            var storyNode = nodeGO != null ? nodeGO.GetComponent<StoryNode>() : null;
            if (activeTextCharacters == null || activeTextCharacters.Count == 0)
            {
                if (storyNode != null && !string.IsNullOrEmpty(storyNode.nodeText))
                    InitializeTextForAnimation(storyNode.nodeText);
                else
                {
                    if (textBox != null) textBox.text = "";
                    if (wordAudioSource != null && wordAudioSource.isPlaying) wordAudioSource.Stop();
                    displayTextCoroutine = null;
                    yield break;
                }
            }
            if (wordAudioSource != null && typingSoundClip != null && !wordAudioSource.isPlaying)
            {
                wordAudioSource.clip = typingSoundClip;
                wordAudioSource.loop = true;
                wordAudioSource.Play();
            }
            for (int i = 0; i < activeTextCharacters.Count; i++)
            {
                RevealedChar charInfo = activeTextCharacters[i];
                charInfo.isVisible = true;
                charInfo.appearanceTime = Time.time;
                activeTextCharacters[i] = charInfo;
                UpdateDisplayedText();
                if (!charInfo.isSpaceOrNewline)
                {
                    yield return new WaitForSeconds(UnityEngine.Random.Range(letterReveal_MinDelay, letterReveal_MaxDelay));
                }
                else if (charInfo.character == '\n')
                {
                    yield return new WaitForSeconds(UnityEngine.Random.Range(letterReveal_MinDelay, letterReveal_MaxDelay) * 2f);
                }
            }
            float animationEndTime = Time.time + letterReveal_HighlightDuration;
            while (Time.time < animationEndTime)
            {
                UpdateDisplayedText();
                bool allNormal = true;
                float currentTime = Time.time;
                for (int i = 0; i < activeTextCharacters.Count; ++i)
                {
                    if (activeTextCharacters[i].isVisible && !activeTextCharacters[i].isSpaceOrNewline &&
                        currentTime < activeTextCharacters[i].appearanceTime + letterReveal_HighlightDuration)
                    {
                        allNormal = false;
                        break;
                    }
                }
                if (allNormal) break;
                yield return null;
            }
            if (storyNode != null && textBox != null) textBox.text = storyNode.nodeText;
            if (wordAudioSource != null && wordAudioSource.isPlaying)
                wordAudioSource.Stop();
            displayTextCoroutine = null;
        }
    }
}
