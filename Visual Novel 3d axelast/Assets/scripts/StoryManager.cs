using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // For List<>

// =====================
// Hoofdstuk 6+ datastructuren
// =====================

[System.Serializable]
public class SewListEntry
{
    public StoryNode node;
    [Tooltip("Optionele ClickList voor deze stap in de hoofdlijn.")]
    public ClickList clickList;
    [Tooltip("Indien true, wordt een click op deze node genegeerd en blijft de interactie bij deze node.")]
    public bool disableClick;
}

[System.Serializable]
public class SewList
{
    [Tooltip("Entries van deze SewList (hoofdlijn van het hoofdstuk).")]
    public List<SewListEntry> entries = new List<SewListEntry>();
}
[System.Serializable]
public class ClickListEntry
{
    public StoryNode node;
    [Tooltip("Schakelt sewbeweging uit voor deze entry.")]
    public bool disableSew;
    [Tooltip("Indien true, activeer ForcedList in plaats van SewList na deze entry.")]
    public bool forceToForcedList;
    [Tooltip("Index van de ForcedList die geactiveerd wordt (indien van toepassing).")]
    public int forcedListIndex;
    [Tooltip("Index van de FateList die geactiveerd wordt (indien van toepassing, 0 = geen FateList).")]
    public int fateListIndex;
    [Tooltip("Index in de EindeVerhalen-lijst (0 = geen einde, 1+ = index in lijst).")]
    public int eindeVerhaalIndex;
}

[System.Serializable]
public class ForcedListEntry
{
    public StoryNode node;
    [Tooltip("Schakelt click uit voor deze entry.")]
    public bool disableClick;
}

[System.Serializable]
public class ForcedList
{
    [Tooltip("Startindex (inclusief) in de ClickList waar deze ForcedList geactiveerd wordt.")]
    public int startIndex;
    [Tooltip("Eindindex (inclusief) in de ClickList waar deze ForcedList geactiveerd wordt.")]
    public int endIndex;
    [Tooltip("Entries van deze ForcedList.")]
    public List<ForcedListEntry> entries = new List<ForcedListEntry>();
}

[System.Serializable]
public class FateListEntry
{
    public StoryNode node;
}

[System.Serializable]
public class FateList
{
    [Tooltip("Index (of bereik) in de ClickList waar deze FateList geactiveerd wordt.")]
    public int triggerIndex;
    [Tooltip("Entries van deze FateList.")]
    public List<FateListEntry> entries = new List<FateListEntry>();
}

[System.Serializable]
public class ClickList
{
    [Tooltip("Entries van deze ClickList.")]
    public List<ClickListEntry> entries = new List<ClickListEntry>();
    [Tooltip("ForcedLists die bij deze ClickList horen.")]
    public List<ForcedList> forcedLists = new List<ForcedList>();
    [Tooltip("FateLists die bij deze ClickList horen.")]
    public List<FateList> fateLists = new List<FateList>();
}

// Define this class here or in its own file if preferred
[System.Serializable]
public class GlobalPenaltyNodeMapping
{
    [Tooltip("Optional description for Inspector clarity.")]
    public string description;
    [Tooltip("Total penalty clicks required to trigger this mapping.")]
    public int requiredTotalPenaltyClicks;
    [Tooltip("The 'strafnode' (penalty node) to go to.")]
    public StoryNode targetStrafNode;
    [Tooltip("If true, after sewing back from targetStrafNode, advance to the next node in the Chapter 6+ Sew Nodes list.")]
    public bool advanceSewListAfterReturningFromThisStraf;
}

public class StoryManager : MonoBehaviour
{
    public Image backgroundImage;
    public TextMeshProUGUI textBox;

    public StoryNode currentNode;

    [Header("UI: Image die zichtbaar is tijdens sewing (spatie ingedrukt)")]
    public Image sewingActiveImage;

    [Header("UI: Role-specifieke elementen")]
    public GameObject userRoleUI;
    public GameObject machineRoleUI;
    public GameObject extraRoleUI;

    [Header("UI: Feedback bij acties")]
    public Image sewingSuccessImage;
    public Image clickedImage;

    [Header("Optional: UI element for error/status messages")]
    public TextMeshProUGUI errorMessageBox;

    [Header("Debug/Verbose")]
    public bool verbose = true;
    public TextMeshProUGUI verboseSewingText;
    public TextMeshProUGUI verboseClickText;
    public TextMeshProUGUI verboseErrorText;

    [Header("UI: Chapter Indicator")]
    public TextMeshProUGUI chapterIndicatorText;

    public AudioClip sewingSoundClip; // Sound to play when space is held
    public AudioSource sewingAudioSource; // AudioSource for the sewing sound
    public AudioSource backgroundAudioSource; // AudioSource for node-specific background sounds
    public AudioSource wordAudioSource; // AudioSource for typing sound
    public AudioClip typingSoundClip; // Single sound to loop/play during text reveal

    [Header("Per-Letter Text Reveal Settings")]
    public float letterReveal_MinDelay = 0.02f;
    public float letterReveal_MaxDelay = 0.1f;
    public float letterReveal_HighlightDuration = 1.0f;
    public float letterReveal_SizeMultiplier = 1.2f; // e.g., 1.2 for 120%
    public Color letterReveal_HighlightColor = new Color(0.85f, 0.85f, 0.85f, 1f); // A light grey

    private Coroutine displayTextCoroutine; // To manage the text display coroutine
    private System.Collections.Generic.List<RevealedChar> activeTextCharacters;
    private System.Text.StringBuilder textBuilder = new System.Text.StringBuilder();
    private float baseFontSize; // To store the original font size of the textBox

    [Header("Chapter & Game State")]
    public StoryNode chapter6StartNode; // Assign in Inspector: the first node of Chapter 6
    public Image passOutScreenImage; // Assign in Inspector: UI Image for Chapter 5 pass-out
    public float chapter5PassOutScreenDuration = 3.0f;

    [Tooltip("Ordered list of sewing nodes for Chapter 4.")]
    public List<StoryNode> chapter4SewNodes;

    [System.Serializable]
    public class Chapter4ClickList
    {
        [Tooltip("Label for this click list (for Inspector clarity)")]
        public string label;
        [Tooltip("The sequence of StoryNodes for this click list.")]
        public List<StoryNode> nodes = new List<StoryNode>();
        [Tooltip("When the last entry is reached, loop to this index (0 = first entry, etc.).")]
        public int loopToIndex = 0;
    }

    [Tooltip("List of click lists for Chapter 4. Each click list has a label and a list of StoryNodes. Default is 4, but you can add more.")]
    public List<Chapter4ClickList> chapter4ClickLists = new List<Chapter4ClickList>()
    {
        new Chapter4ClickList { label = "Click List 1" },
        new Chapter4ClickList { label = "Click List 2" },
        new Chapter4ClickList { label = "Click List 3" },
        new Chapter4ClickList { label = "Click List 4" }
    };

[Tooltip("Per hoofdstuk een eigen hoofdlijn (SewList) voor hoofdstuk 6+.")]
public List<SewList> chapter6PlusSewLists = new List<SewList>();
    [Tooltip("Centrally defined mappings from total penalty clicks to specific penalty nodes for Chapter 6+. Must be sorted by Required Total Penalty Clicks.")]
    public List<GlobalPenaltyNodeMapping> chapter6PlusPenaltyMappings;

    [Header("Hoofdstuk 6+ ClickLists en EindeVerhalen")]
    [Tooltip("ClickLists voor hoofdstuk 6 en verder. Elke ClickList hoort bij een hoofdstuk.")]
    public List<ClickList> chapter6PlusClickLists = new List<ClickList>();
    [Tooltip("Lijst van mogelijke einde-verhalen. Index 1+ wordt gebruikt door ClickListEntry.eindeVerhaalIndex.")]
    public List<StoryNode> eindeVerhalen = new List<StoryNode>();

    [Header("Chapter Transition")]
    [Tooltip("GameObject dat geactiveerd wordt na Chapter 5 (bevat ChapterManager).")]
    public GameObject nextChapter;

    [Tooltip("GameObject met de ChapterManager component waarop de startmethode wordt aangeroepen.")]
    public GameObject chapterManagerObject;

    public enum ChapterStartMethod
    {
        StartChapter,
        StartChapter6
    }
    [Tooltip("Welke startmethode van ChapterManager moet worden aangeroepen?")]
    public ChapterStartMethod chapterStartMethod = ChapterStartMethod.StartChapter6;

    private int currentChapter = 1;
    private int clicksInCurrentChapter = 0;
    private int totalPenaltyClicks = 0;

    // Hoofdstuk 6+ ClickList state
    // Houdt per ClickList (per hoofdstuk 6+) de huidige entry-index bij
    private List<int> clickListEntryIndices = new List<int>();
    // Houdt per ClickList de ForcedList state bij (null als niet actief)
    private List<int?> activeForcedListIndices = new List<int?>();
    // Houdt per ClickList de FateList state bij (null als niet actief)
    private List<int?> activeFateListIndices = new List<int?>();
    // Houdt per ClickList de huidige entry-index in de actieve ForcedList (indien actief)
    private List<int> forcedListEntryIndices = new List<int>();
    // Houdt per ClickList de huidige entry-index in de actieve FateList (indien actief)
    private List<int> fateListEntryIndices = new List<int>();

    // Inspector indicator for current chapter (read-only)
    public int CurrentChapter => currentChapter;
    private StoryNode previousStoryNode; // For "sew back" logic
    private bool inputDisabled = false; // To disable input during sequences like pass-out
    private bool awaitingSewBack = false; // True if the player just made a penalty click and should sew back
    private int currentSewNodeIndex = 0; // Index for chapter6PlusSewNodes
    private bool storedAdvanceSewListFlag = false; // Stores the advance flag from the penalty mapping

    // Chapter 4 state (for chapter4SewNodes and chapter4ClickLists)
    private int chapter4SewNodeIndex = 0;
    private int chapter4ClickListIndex = 0;
    private int chapter4ClickNodeIndex = 0;
    private bool inChapter4ClickList = false;

    // Voeg een nieuw hoofdstuk 6+ ClickList toe (voor add chapter-knop)
    public void AddChapter6PlusClickList()
    {
        chapter6PlusClickLists.Add(new ClickList());
        clickListEntryIndices.Add(0);
        activeForcedListIndices.Add(null);
        forcedListEntryIndices.Add(0);
        activeFateListIndices.Add(null);
        fateListEntryIndices.Add(0);
    }

    [Header("Sewing Mechanic State")]
    public float requiredDistance = 50f; // Adjusted for GetAxis input, needs tuning
    [Tooltip("Max ratio of horizontal to vertical movement allowed for a sewing streak (e.g., 0.5 means horizontal delta can be 0.5 * vertical delta). Strict vertical is 0.")]
    public float maxHorizontalToVerticalRatio = 0.5f;
    [Tooltip("Minimum positive Y movement in a frame to be considered for ratio check, to avoid division by small numbers issues or extreme ratios with tiny Y movements.")]
    public float minYDeltaForRatioCheck = 0.01f;
    [Tooltip("Minimum horizontal movement when Y movement is zero, to be considered as breaking the streak.")]
    public float minXDeltaWhenYIsZero = 0.01f;
    public float mouseSensitivityY_Sewing = 10f;
    public float mouseSensitivityX_Sewing = 10f;

    private bool isSewing = false;
    private bool sewingSoundActiveForThisHold;
    private float accumulatedSewingDistance;
    private bool pendingSewAction = false; // New: wait for space release after required distance

    private StoryNode pendingTextNode = null; // NEW VARIABLE

    private struct RevealedChar
    {
        public char character;
        public float appearanceTime;
        public bool isVisible;
        public bool isSpaceOrNewline;
    }

    void Awake()
    {
        // Limit framerate to reduce CPU/GPU usage (adjust as needed)
        Application.targetFrameRate = 30;

        // Zet feedback images standaard uit
        if (sewingSuccessImage != null) sewingSuccessImage.gameObject.SetActive(false);
        if (clickedImage != null) clickedImage.gameObject.SetActive(false);

        // Validate all required Inspector assignments and log errors if missing
        bool criticalError = false;

        if (backgroundImage == null)
        {
            Debug.LogError("StoryManager: backgroundImage is not assigned in the Inspector.");
            criticalError = true;
            ShowErrorMessage("Fout: backgroundImage niet ingesteld in de Inspector.");
        }
        if (textBox == null)
        {
            Debug.LogError("StoryManager: textBox is not assigned in the Inspector.");
            criticalError = true;
            ShowErrorMessage("Fout: textBox niet ingesteld in de Inspector.");
        }
        if (chapter6StartNode == null)
        {
            Debug.LogError("StoryManager: chapter6StartNode is not assigned in the Inspector.");
            ShowErrorMessage("Let op: chapter6StartNode niet ingesteld in de Inspector.");
        }
        if (passOutScreenImage == null)
        {
            Debug.LogWarning("StoryManager: passOutScreenImage is not assigned in the Inspector (Chapter 5 pass-out will not show).");
            ShowErrorMessage("Let op: passOutScreenImage niet ingesteld (Chapter 5 blackout niet zichtbaar).");
        }
        if (chapter6PlusSewLists == null || chapter6PlusSewLists.Count == 0)
        {
            Debug.LogWarning("StoryManager: chapter6PlusSewLists is not assigned or empty (Chapter 6+ progression may not work).");
        }
        else
        {
            // Check for duplicate nodes in all sew lists
            var sewSet = new HashSet<StoryNode>();
            foreach (var sewList in chapter6PlusSewLists)
            {
                foreach (var entry in sewList.entries)
                {
                    var node = entry.node;
                    if (node == null)
                    {
                        Debug.LogWarning("StoryManager: chapter6PlusSewLists contains a null entry.");
                    }
                    else if (!sewSet.Add(node))
                    {
                        Debug.LogWarning($"StoryManager: chapter6PlusSewLists contains duplicate node: {node.name}");
                    }
                }
            }
        }
        if (chapter6PlusPenaltyMappings == null || chapter6PlusPenaltyMappings.Count == 0)
        {
            Debug.LogWarning("StoryManager: chapter6PlusPenaltyMappings is not assigned or empty (Chapter 6+ penalty logic may not work).");
        }
        else
        {
            // Sort penalty mappings by requiredTotalPenaltyClicks and check for duplicates
            chapter6PlusPenaltyMappings.Sort((a, b) => a.requiredTotalPenaltyClicks.CompareTo(b.requiredTotalPenaltyClicks));
            int? lastClicks = null;
            foreach (var mapping in chapter6PlusPenaltyMappings)
            {
                if (mapping == null)
                {
                    Debug.LogWarning("StoryManager: chapter6PlusPenaltyMappings contains a null entry.");
                }
                else
                {
                    if (lastClicks.HasValue && mapping.requiredTotalPenaltyClicks == lastClicks.Value)
                    {
                        Debug.LogWarning($"StoryManager: chapter6PlusPenaltyMappings contains duplicate requiredTotalPenaltyClicks: {mapping.requiredTotalPenaltyClicks}");
                    }
                    lastClicks = mapping.requiredTotalPenaltyClicks;
                    if (mapping.targetStrafNode == null)
                    {
                        Debug.LogWarning($"StoryManager: Penalty mapping with requiredTotalPenaltyClicks={mapping.requiredTotalPenaltyClicks} has no targetStrafNode assigned.");
                    }
                }
            }
        }

        if (passOutScreenImage != null)
        {
            passOutScreenImage.gameObject.SetActive(false);
        }
        if (sewingActiveImage != null)
        {
            sewingActiveImage.gameObject.SetActive(false);
        }

        // AudioSource assignment logic
        if (sewingAudioSource == null)
        {
            Debug.LogWarning("StoryManager: sewingAudioSource was not assigned. Adding new AudioSource.");
            sewingAudioSource = gameObject.AddComponent<AudioSource>();
        }
        sewingAudioSource.loop = true; sewingAudioSource.playOnAwake = false;

        if (backgroundAudioSource == null)
        {
            Debug.LogWarning("StoryManager: backgroundAudioSource was not assigned. Adding new AudioSource.");
            backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        }
        if (backgroundAudioSource == sewingAudioSource && GetComponents<AudioSource>().Length < 2)
        {
            Debug.LogWarning("StoryManager: backgroundAudioSource was pointing to sewingAudioSource. Adding new AudioSource.");
            backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        }
        backgroundAudioSource.loop = true; backgroundAudioSource.playOnAwake = false;

        if (wordAudioSource == null)
        {
            Debug.LogWarning("StoryManager: wordAudioSource was not assigned. Adding new AudioSource.");
            wordAudioSource = gameObject.AddComponent<AudioSource>();
        }
        if ((wordAudioSource == sewingAudioSource || wordAudioSource == backgroundAudioSource) && GetComponents<AudioSource>().Length < 3)
        {
            Debug.LogWarning("StoryManager: wordAudioSource was pointing to another AudioSource. Adding new AudioSource.");
            wordAudioSource = gameObject.AddComponent<AudioSource>();
        }
        wordAudioSource.loop = true; wordAudioSource.playOnAwake = false;

        if (sewingAudioSource == null)
        {
            Debug.LogError("CRITICAL: sewingAudioSource is STILL NULL!");
            criticalError = true;
        }
        if (backgroundAudioSource == null)
        {
            Debug.LogError("CRITICAL: backgroundAudioSource is STILL NULL!");
            criticalError = true;
        }
        if (wordAudioSource == null)
        {
            Debug.LogError("CRITICAL: wordAudioSource is STILL NULL!");
            criticalError = true;
        }

        if (textBox != null)
        {
            baseFontSize = textBox.fontSize;
        }
        else
        {
            baseFontSize = 36;
        }

        if (criticalError)
        {
            Debug.LogError("StoryManager: One or more critical components are missing. Disabling input and further initialization.");
            ShowErrorMessage("Kritieke fout: één of meer essentiële componenten ontbreken. Spel is gepauzeerd.");
            inputDisabled = true;
        }
    }

    // Show a message in the errorMessageBox UI element, if assigned
    private void ShowErrorMessage(string msg)
    {
        if (errorMessageBox != null)
        {
            errorMessageBox.text = msg;
            errorMessageBox.gameObject.SetActive(true);
        }
    }

    void Start()
    {
        if (currentNode == null)
        {
            Debug.LogError("StoryManager: CurrentNode (starting node) is not assigned in the Inspector!");
            inputDisabled = true;
            return;
        }
        ShowNode(currentNode);

        // --- Ensure progression indices match the Inspector-assigned starting node ---
        // For Chapter 4 sewing nodes
        if (chapter4SewNodes != null && chapter4SewNodes.Count > 0)
        {
            int idx = chapter4SewNodes.IndexOf(currentNode);
            if (idx != -1)
            {
                chapter4SewNodeIndex = idx;
            }
        }
        // For Chapter 6+ sewing nodes
if (chapter6PlusSewLists != null && chapter6PlusSewLists.Count > 0)
{
    int sewListIdx = currentChapter - 6;
    if (sewListIdx >= 0 && sewListIdx < chapter6PlusSewLists.Count)
    {
        var sewList = chapter6PlusSewLists[sewListIdx];
        int idx = sewList.entries.FindIndex(e => e.node == currentNode);
        if (idx != -1)
        {
            currentSewNodeIndex = idx;
        }
    }
}

        UpdateChapterIndicator();
    }

    void Update()
    {
        if (inputDisabled) return;

        if (isSewing && Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (sewingAudioSource != null && sewingAudioSource.isPlaying) sewingAudioSource.Stop();
            isSewing = false;
            sewingSoundActiveForThisHold = false;
            accumulatedSewingDistance = 0f;
            Debug.Log("Sewing action cancelled by Escape key.");

            if (sewingActiveImage != null)
            {
                sewingActiveImage.gameObject.SetActive(false);
            }
        }

        if (!isSewing && Input.GetKeyDown(KeyCode.Space))
        {
            if (sewingAudioSource == null) Debug.LogError("StoryManager: sewingAudioSource is not assigned.");
            else if (sewingSoundClip == null) Debug.LogError("StoryManager: sewingSoundClip is not assigned.");

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isSewing = true;
            sewingSoundActiveForThisHold = true;
            accumulatedSewingDistance = 0f;
            pendingSewAction = false;

            if (sewingActiveImage != null)
            {
                sewingActiveImage.gameObject.SetActive(true);
            }

            if (sewingSoundActiveForThisHold && sewingAudioSource != null && sewingSoundClip != null && !sewingAudioSource.isPlaying)
            {
                Debug.Log("StoryManager: Starting sewing sound on KeyDown.");
                sewingAudioSource.clip = sewingSoundClip;
                sewingAudioSource.Play();
            }
        }
        else if (isSewing && Input.GetKeyUp(KeyCode.Space))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (sewingAudioSource != null && sewingAudioSource.isPlaying)
            {
                Debug.Log("StoryManager: Stopping sewing sound on space release.");
                sewingAudioSource.Stop();
            }
            isSewing = false;
            sewingSoundActiveForThisHold = false;
            accumulatedSewingDistance = 0f;

            if (sewingActiveImage != null)
            {
                sewingActiveImage.gameObject.SetActive(false);
            }

            // Hide sewing success image on space release
            if (sewingSuccessImage != null)
            {
                sewingSuccessImage.gameObject.SetActive(false);
            }

            // New: If required distance was met, trigger Next("sew") now
            if (pendingSewAction)
            {
                Next("sew");
                pendingSewAction = false;
            }
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

            if (accumulatedSewingDistance >= requiredDistance)
            {
                Debug.Log($"StoryManager: Required distance met ({accumulatedSewingDistance:F2} >= {requiredDistance}). Waiting for space release to trigger 'sew'.");

                if (sewingAudioSource != null && sewingAudioSource.isPlaying)
                {
                    Debug.Log("StoryManager: Stopping sewing sound because required distance met.");
                    sewingAudioSource.Stop();
                }
                sewingSoundActiveForThisHold = false;

                // Show green sewing success image immediately
                if (sewingSuccessImage != null)
                {
                    sewingSuccessImage.color = Color.green;
                    sewingSuccessImage.gameObject.SetActive(true);
                }

                // New: Set flag to trigger Next("sew") on space release
                pendingSewAction = true;

                accumulatedSewingDistance = 0f;
            }
        }

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
                    Debug.Log("Sewing action interrupted by click.");
                }
            }

            // --- Disable Click logica voor SewList (chapter 6+) ---
            bool blockClick = false;
            if (currentChapter >= 6 && chapter6PlusSewLists != null)
            {
                int sewListIdx = currentChapter - 6;
                if (sewListIdx >= 0 && sewListIdx < chapter6PlusSewLists.Count)
                {
                    var sewList = chapter6PlusSewLists[sewListIdx];
                    if (currentSewNodeIndex >= 0 && currentSewNodeIndex < sewList.entries.Count)
                    {
                        var sewEntry = sewList.entries[currentSewNodeIndex];
                        if (sewEntry != null && sewEntry.disableClick)
                        {
                            blockClick = true;
                        }
                    }
                }
            }
            if (blockClick)
            {
                Debug.Log("Click genegeerd: disableClick actief voor deze SewListEntry.");
                // (Optioneel: visuele feedback toevoegen)
            }
            else
            {
                Next("click");
            }
        }

        // Start alsnog de tekstanimatie als de spatiebalk wordt losgelaten en er een pending node is
        if (pendingTextNode != null && !Input.GetKey(KeyCode.Space) && displayTextCoroutine == null)
        {
            displayTextCoroutine = StartCoroutine(AnimateTextCharacterByCharacter(pendingTextNode));
            pendingTextNode = null;
        }
    }

    void ShowNode(StoryNode node)
    {
        if (node == null)
        {
            Debug.LogError("ShowNode: Target node is null. Cannot proceed.");
            inputDisabled = true;
            return;
        }

        if (node.chapter != currentChapter)
        {
            Debug.Log($"Transitioning from Chapter {currentChapter} to Chapter {node.chapter}");
            currentChapter = node.chapter;
            clicksInCurrentChapter = 0;
            awaitingSewBack = false;

            UpdateChapterIndicator();

            if (currentChapter == 5)
            {
                StartCoroutine(PassOutAndProceedCoroutine());
                return;
            }
            else if (currentChapter == 6 && (previousStoryNode == null || previousStoryNode.chapter < 6))
            {
if (chapter6PlusSewLists != null && chapter6PlusSewLists.Count > 0 && chapter6StartNode != null)
{
    int sewListIdx = currentChapter - 6;
    if (sewListIdx >= 0 && sewListIdx < chapter6PlusSewLists.Count)
    {
        var sewList = chapter6PlusSewLists[sewListIdx];
        int idx = sewList.entries.FindIndex(e => e.node == chapter6StartNode);
        if (idx != -1)
        {
            currentSewNodeIndex = idx;
        }
        else
        {
            Debug.LogWarning($"Chapter 6 start node '{chapter6StartNode.name}' not found in SewList. Defaulting sew index to 0.");
            currentSewNodeIndex = 0;
        }
    }
    else
    {
        currentSewNodeIndex = 0;
    }
}
else
{
    currentSewNodeIndex = 0;
}
            }
        }
        else
        {
            UpdateChapterIndicator();
        }

        // Always stop any running text coroutine before starting a new one
        if (displayTextCoroutine != null)
        {
            try
            {
                StopCoroutine(displayTextCoroutine);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"StoryManager: Exception while stopping displayTextCoroutine: {ex.Message}");
            }
            if (wordAudioSource != null && wordAudioSource.isPlaying)
            {
                wordAudioSource.Stop();
            }
            displayTextCoroutine = null;
        }

        currentNode = node;

if (currentChapter >= 6 && chapter6PlusSewLists != null && chapter6PlusSewLists.Count > 0)
{
    int sewListIdx = currentChapter - 6;
    if (sewListIdx >= 0 && sewListIdx < chapter6PlusSewLists.Count)
    {
        var sewList = chapter6PlusSewLists[sewListIdx];
        int idx = sewList.entries.FindIndex(e => e.node == currentNode);
        if (idx != -1)
        {
            currentSewNodeIndex = idx;
        }
    }
}

        InitializeTextForAnimation(currentNode.nodeText);
        if (textBox != null) textBox.text = "";

        // --- Verbose UI ---
        if (verbose)
        {
            UpdateVerboseUI();
        }
        else
        {
            if (verboseSewingText != null) verboseSewingText.text = "";
            if (verboseClickText != null) verboseClickText.text = "";
            if (verboseErrorText != null) verboseErrorText.text = "";
        }
        // --- einde Verbose UI ---

        // --- Role UI logica ---
        if (userRoleUI != null) userRoleUI.SetActive(false);
        if (machineRoleUI != null) machineRoleUI.SetActive(false);
        if (extraRoleUI != null) extraRoleUI.SetActive(false);

        if (currentNode != null)
        {
            switch (currentNode.nodeRole)
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
        }
        // --- einde Role UI logica ---

        // Start de tekstanimatie alleen als de spatiebalk NIET is ingedrukt
        if (!Input.GetKey(KeyCode.Space))
        {
            if (displayTextCoroutine == null)
            {
                displayTextCoroutine = StartCoroutine(AnimateTextCharacterByCharacter(node));
            }
            else
            {
                Debug.LogWarning("StoryManager: Tried to start AnimateTextCharacterByCharacter while coroutine was already running.");
            }
            pendingTextNode = null;
        }
        else
        {
            // Onthoud dat we deze node nog moeten animeren
            pendingTextNode = node;
        }

        if (backgroundImage != null)
        {
            if (currentNode.background != null)
            {
                backgroundImage.sprite = currentNode.background;
                backgroundImage.gameObject.SetActive(true);
            }
            else { /* backgroundImage.gameObject.SetActive(false); */ }
        }
        else
        {
            Debug.LogWarning("ShowNode: backgroundImage UI element is not assigned in the Inspector.");
        }

        if (backgroundAudioSource != null)
        {
            if (backgroundAudioSource.isPlaying)
            {
                backgroundAudioSource.Stop();
            }
            if (currentNode.backgroundSound != null)
            {
                backgroundAudioSource.clip = currentNode.backgroundSound;
                backgroundAudioSource.Play();
            }
        }
    }

    private void UpdateChapterIndicator()
    {
        if (chapterIndicatorText != null)
        {
            chapterIndicatorText.text = $"Chapter: {currentChapter}";
            chapterIndicatorText.gameObject.SetActive(verbose);
        }
    }

    void Next(string method)
    {
        if (inputDisabled) return;

        // --- Extra disableClick check voor SewList (chapter 6+) ---
        if (method == "click" && currentChapter >= 6 && chapter6PlusSewLists != null)
        {
            int sewListIdx = currentChapter - 6;
            if (sewListIdx >= 0 && sewListIdx < chapter6PlusSewLists.Count)
            {
                var sewList = chapter6PlusSewLists[sewListIdx];
                if (currentSewNodeIndex >= 0 && currentSewNodeIndex < sewList.entries.Count)
                {
                    var sewEntry = sewList.entries[currentSewNodeIndex];
                    if (sewEntry != null && sewEntry.disableClick)
                    {
                        Debug.Log("Click genegeerd in Next(): disableClick actief voor deze SewListEntry.");
                        return;
                    }
                }
            }
        }

        // Feedback UI tonen
        if (method == "sew" && sewingSuccessImage != null)
        {
            StartCoroutine(ShowTempImage(sewingSuccessImage, 0.5f));
        }
        if (method == "click" && clickedImage != null)
        {
            StartCoroutine(ShowTempImage(clickedImage, 0.5f));
        }

        StoryNode nextNode = null;
        bool isSewBackAction = false;

        // Always stop any running text coroutine before advancing
        if (displayTextCoroutine != null)
        {
            try
            {
                StopCoroutine(displayTextCoroutine);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"StoryManager: Exception while stopping displayTextCoroutine in Next(): {ex.Message}");
            }
            if (wordAudioSource != null && wordAudioSource.isPlaying)
            {
                wordAudioSource.Stop();
            }
            displayTextCoroutine = null;
            if (currentNode != null && textBox != null)
            {
                textBox.text = currentNode.nodeText;
            }
        }

        if (currentNode == null)
        {
            Debug.LogError("Next: currentNode is null. Cannot determine next action.");
            return;
        }

        if (method == "click")
        {
            // Alleen click/penalty tellen als de huidige node dat toestaat
            if (currentNode != null && currentNode.meetellenAlsClick)
            {
                clicksInCurrentChapter++;
                if (currentChapter >= 6)
                {
                    totalPenaltyClicks++;
                    Debug.Log($"Total Penalty Clicks: {totalPenaltyClicks}");
                }
            }
            // Debug.Log($"Clicks in Chapter {currentChapter}: {clicksInCurrentChapter}");

            previousStoryNode = currentNode;
            awaitingSewBack = false;
            storedAdvanceSewListFlag = false;

            switch (currentChapter)
            {
                case 1:
                case 2:
                case 3:
                    nextNode = currentNode.nextOnClick;
                    break;
                case 4:
                    // Flexible click list logic for Chapter 4
                    if (chapter4ClickLists != null && chapter4ClickLists.Count > 0)
                    {
                        if (!inChapter4ClickList)
                        {
                            inChapter4ClickList = true;
                            chapter4ClickNodeIndex = 0;
                        }
                        else
                        {
                            chapter4ClickNodeIndex++;
                        }
                        int useClickList = Mathf.Min(chapter4ClickListIndex, chapter4ClickLists.Count - 1);
                        var chapter4ClickList = chapter4ClickLists[useClickList];
                        if (chapter4ClickList != null && chapter4ClickList.nodes != null && chapter4ClickList.nodes.Count > 0)
                        {
                            // Clamp and loop index if needed
                            if (chapter4ClickNodeIndex >= chapter4ClickList.nodes.Count)
                            {
                                chapter4ClickNodeIndex = Mathf.Clamp(chapter4ClickList.loopToIndex, 0, chapter4ClickList.nodes.Count - 1);
                            }
                            int clickIdx = Mathf.Clamp(chapter4ClickNodeIndex, 0, chapter4ClickList.nodes.Count - 1);
                            nextNode = chapter4ClickList.nodes[clickIdx];
                        }
                    }
                    break;
                case 5:
                    Debug.LogWarning("Click received during Chapter 5 (Pass-out). Input should be disabled.");
                    break;
                default: // Chapter 6 and onwards
                    // --- Nieuwe ClickList/ForcedList/FateList/EindeVerhaal logica ---
                    int clickListIdx = currentChapter - 6;
                    if (clickListIdx < 0 || clickListIdx >= chapter6PlusClickLists.Count)
                    {
                        Debug.LogWarning($"Geen ClickList voor hoofdstuk {currentChapter} (index {clickListIdx}).");
                        break;
                    }
                    // Initialiseer indices als nodig
                    while (clickListEntryIndices.Count <= clickListIdx) clickListEntryIndices.Add(0);
                    while (activeForcedListIndices.Count <= clickListIdx) activeForcedListIndices.Add(null);
                    while (forcedListEntryIndices.Count <= clickListIdx) forcedListEntryIndices.Add(0);
                    while (activeFateListIndices.Count <= clickListIdx) activeFateListIndices.Add(null);
                    while (fateListEntryIndices.Count <= clickListIdx) fateListEntryIndices.Add(0);

                    var clickList = chapter6PlusClickLists[clickListIdx];

                    // ForcedList actief?
                    if (activeForcedListIndices[clickListIdx].HasValue)
                    {
                        int forcedIdx = activeForcedListIndices[clickListIdx].Value;
                        ForcedList forcedList = (forcedIdx >= 0 && forcedIdx < clickList.forcedLists.Count) ? clickList.forcedLists[forcedIdx] : null;
                        int entryIdx = forcedListEntryIndices[clickListIdx];
                        if (forcedList != null && entryIdx < forcedList.entries.Count)
                        {
                            ForcedListEntry entry = forcedList.entries[entryIdx];
                            // Click uitgeschakeld?
                            if (entry.disableClick)
                            {
                                Debug.Log("Click is uitgeschakeld in deze ForcedList entry.");
                                break;
                            }
                            nextNode = entry.node;
                            forcedListEntryIndices[clickListIdx]++;
                            // Einde ForcedList?
                            if (forcedListEntryIndices[clickListIdx] >= forcedList.entries.Count)
                            {
                                activeForcedListIndices[clickListIdx] = null;
                                forcedListEntryIndices[clickListIdx] = 0;
                                // Na ForcedList: doorgaan met hoofdlist (ClickList)
                                clickListEntryIndices[clickListIdx]++;
                            }
                        }
                        else
                        {
                            // ForcedList index out of range, reset
                            activeForcedListIndices[clickListIdx] = null;
                            forcedListEntryIndices[clickListIdx] = 0;
                        }
                        break;
                    }

                    // FateList actief?
                    if (activeFateListIndices[clickListIdx].HasValue)
                    {
                        int fateIdx = activeFateListIndices[clickListIdx].Value;
                        FateList fateList = (fateIdx >= 0 && fateIdx < clickList.fateLists.Count) ? clickList.fateLists[fateIdx] : null;
                        int entryIdx = fateListEntryIndices[clickListIdx];
                        if (fateList != null && entryIdx < fateList.entries.Count)
                        {
                            FateListEntry entry = fateList.entries[entryIdx];
                            nextNode = entry.node;
                            fateListEntryIndices[clickListIdx]++;
                            // Einde FateList?
                            if (fateListEntryIndices[clickListIdx] >= fateList.entries.Count)
                            {
                                activeFateListIndices[clickListIdx] = null;
                                fateListEntryIndices[clickListIdx] = 0;
                                // Na FateList: terug naar ClickList, NIET doorklikken
                            }
                        }
                        else
                        {
                            // FateList index out of range, reset
                            activeFateListIndices[clickListIdx] = null;
                            fateListEntryIndices[clickListIdx] = 0;
                        }
                        break;
                    }

                    // Normale ClickList entry
                    int entryIdxCL = clickListEntryIndices[clickListIdx];
                    if (entryIdxCL < clickList.entries.Count)
                    {
                        ClickListEntry entry = clickList.entries[entryIdxCL];
                        nextNode = entry.node;

                        // EindeVerhaal?
                        if (entry.eindeVerhaalIndex > 0 && entry.eindeVerhaalIndex <= eindeVerhalen.Count)
                        {
                            nextNode = eindeVerhalen[entry.eindeVerhaalIndex - 1];
                            // Optioneel: reset state
                            clickListEntryIndices[clickListIdx] = 0;
                            activeForcedListIndices[clickListIdx] = null;
                            forcedListEntryIndices[clickListIdx] = 0;
                            activeFateListIndices[clickListIdx] = null;
                            fateListEntryIndices[clickListIdx] = 0;
                            break;
                        }

                        // FateList activeren?
                        if (entry.fateListIndex > 0 && entry.fateListIndex <= clickList.fateLists.Count)
                        {
                            activeFateListIndices[clickListIdx] = entry.fateListIndex - 1;
                            fateListEntryIndices[clickListIdx] = 0;
                            // Toon eerste entry van FateList
                            var fateList = clickList.fateLists[entry.fateListIndex - 1];
                            if (fateList.entries.Count > 0)
                                nextNode = fateList.entries[0].node;
                            break;
                        }

                        // ForcedList activeren?
                        if (entry.forceToForcedList && entry.forcedListIndex >= 0 && entry.forcedListIndex < clickList.forcedLists.Count)
                        {
                            activeForcedListIndices[clickListIdx] = entry.forcedListIndex;
                            forcedListEntryIndices[clickListIdx] = 0;
                            // Toon eerste entry van ForcedList
                            var forcedList = clickList.forcedLists[entry.forcedListIndex];
                            if (forcedList.entries.Count > 0)
                                nextNode = forcedList.entries[0].node;
                            break;
                        }

                        // Normale click: naar volgende entry
                        clickListEntryIndices[clickListIdx]++;
                        // Einde ClickList? Ga naar SewList
                        if (clickListEntryIndices[clickListIdx] >= clickList.entries.Count)
                        {
int sewListIdx = currentChapter - 6;
if (chapter6PlusSewLists != null && sewListIdx >= 0 && sewListIdx < chapter6PlusSewLists.Count)
{
    var sewList = chapter6PlusSewLists[sewListIdx];
    if (currentSewNodeIndex < sewList.entries.Count - 1)
    {
        currentSewNodeIndex++;
        nextNode = sewList.entries[currentSewNodeIndex].node;
        clickListEntryIndices[clickListIdx] = 0;
    }
}
                        }
                    }
                    break;
            }
        }
        else if (method == "sew")
        {
            sewingSoundActiveForThisHold = false;

            switch (currentChapter)
            {
                case 1:
                case 2:
                case 3:
                    // For chapters 1-3, use currentNode.nextOnSew
                    nextNode = currentNode.nextOnSew;
                    awaitingSewBack = false;
                    break;
                case 4:
                    // Resume sewing sequence in Chapter 4
                    if (chapter4SewNodes != null && chapter4SewNodes.Count > 0)
                    {
                        if (chapter4SewNodeIndex < chapter4SewNodes.Count)
                        {
                            nextNode = chapter4SewNodes[chapter4SewNodeIndex];
                            chapter4SewNodeIndex++;
                        }
                        else
                        {
                            Debug.LogWarning("Sew node index out of range for Chapter 4. Resetting.");
                            chapter4SewNodeIndex = 0;
                        }
                    }
                    break;
                default: // Chapter 6 and onwards
                    int sewListIdx = currentChapter - 6;
                    if (sewListIdx < 0 || sewListIdx >= chapter6PlusSewLists.Count)
                    {
                        Debug.LogWarning($"Geen SewList voor hoofdstuk {currentChapter} (index {sewListIdx}).");
                        break;
                    }
                    if (chapter6PlusSewLists != null)
                    {
                        int sewListIdx2 = currentChapter - 6;
                        if (sewListIdx2 >= 0 && sewListIdx2 < chapter6PlusSewLists.Count)
                        {
                            var sewList2 = chapter6PlusSewLists[sewListIdx2];
                            if (currentSewNodeIndex < sewList2.entries.Count - 1)
                            {
                                currentSewNodeIndex++;
                                nextNode = sewList2.entries[currentSewNodeIndex].node;
                            }
                        }
                    }
                    break;
            }
        }

        if (nextNode != null)
        {
            ShowNode(nextNode);
        }
        else
        {
            Debug.LogWarning("Next node is null. Cannot proceed.");
        }
    }

    // --- Coroutine to handle pass-out and proceed to the next chapter ---
    private System.Collections.IEnumerator PassOutAndProceedCoroutine()
    {
        inputDisabled = true;
        if (passOutScreenImage != null)
        {
            passOutScreenImage.gameObject.SetActive(true);
            yield return new WaitForSeconds(chapter5PassOutScreenDuration);
            passOutScreenImage.gameObject.SetActive(false);
        }
        currentChapter = 6; // Force chapter 6
        clicksInCurrentChapter = 0;
        awaitingSewBack = false;
        UpdateChapterIndicator();

        // Reset all indices and states for chapter 6
        int sewListIdx = currentChapter - 6;
        if (chapter6PlusSewLists != null && sewListIdx >= 0 && sewListIdx < chapter6PlusSewLists.Count)
        {
            var sewList = chapter6PlusSewLists[sewListIdx];
            currentSewNodeIndex = 0;
        }
        else
        {
            currentSewNodeIndex = 0;
        }

        // Optionally, reset penalty clicks or other states here

        Debug.Log("Proceeding to Chapter 6...");
        // Optionally, play a sound or animation here

        if (nextChapter != null)
        {
            nextChapter.SetActive(true);
        }
        if (chapterManagerObject != null)
        {
            var cm = chapterManagerObject.GetComponent<VN3D.Shared.ChapterManager>();
            if (cm != null)
            {
                if (chapterStartMethod == ChapterStartMethod.StartChapter6)
                    cm.StartChapter6();
                else
                    cm.StartChapter();
            }
        }
        gameObject.SetActive(false);
        if (nextChapter != null)
        {
            nextChapter.SetActive(true);
        }
        if (chapterManagerObject != null)
        {
            var cm = chapterManagerObject.GetComponent<VN3D.Shared.ChapterManager>();
            if (cm != null)
            {
                if (chapterStartMethod == ChapterStartMethod.StartChapter6)
                    cm.StartChapter6();
                else
                    cm.StartChapter();
            }
        }
        gameObject.SetActive(false);

        inputDisabled = false;
    }

    // --- Debug: Handmatige activatie van nextChapter via Inspector ---
    public void ActivateNextChapterManual()
    {
        if (nextChapter != null)
        {
            nextChapter.SetActive(true);
        }
        if (chapterManagerObject != null)
        {
            var cm = chapterManagerObject.GetComponent<VN3D.Shared.ChapterManager>();
            if (cm != null)
            {
                if (chapterStartMethod == ChapterStartMethod.StartChapter6)
                    cm.StartChapter6();
                else
                    cm.StartChapter();
            }
        }
        gameObject.SetActive(false);
    }

    // --- Coroutine to animate text character by character ---
    private System.Collections.IEnumerator AnimateTextCharacterByCharacter(StoryNode node)
    {
        if (node == null || node.nodeText == null)
        {
            Debug.LogWarning("AnimateTextCharacterByCharacter: Node or nodeText is null.");
            yield break;
        }

        textBox.text = ""; // Clear the text box

        // Start typing sound while animating text
        if (wordAudioSource != null && typingSoundClip != null)
        {
            wordAudioSource.clip = typingSoundClip;
            wordAudioSource.loop = true;
            wordAudioSource.Play();
        }

        float delay = 0.05f; // Base delay between characters
        foreach (char c in node.nodeText)
        {
            textBox.text += c; // Add one character at a time
            yield return new WaitForSeconds(delay);
        }

        // Stop typing sound after animation
        if (wordAudioSource != null && wordAudioSource.isPlaying)
        {
            wordAudioSource.Stop();
        }

        // Wait for the entire text to be displayed
        yield return new WaitForSeconds(0.5f);

        // Optionally, add a highlight or effect to the entire text
        /*
        textBox.fontSize = baseFontSize * 1.2f;
        yield return new WaitForSeconds(0.2f);
        textBox.fontSize = baseFontSize;
        */

        displayTextCoroutine = null; // Clear the coroutine reference
    }

    // --- Initialize text for animation, preserving rich text tags ---
    private void InitializeTextForAnimation(string rawText)
    {
        if (textBox == null) return;

        // Reset text properties
        textBox.fontSize = baseFontSize;
        textBox.color = Color.white;

        // --- Rich text handling ---
        // Basic example: wrap in color tags
        string coloredText = "<color=#FFFFFF>" + rawText + "</color>";
        textBox.text = coloredText;

        // --- Advanced: parse for specific tags and apply effects ---

        // Example: Bold tag
        if (rawText.Contains("[b]"))
        {
            textBox.fontStyle = FontStyles.Bold;
        }
        else
        {
            textBox.fontStyle = FontStyles.Normal;
        }

        // Example: Italic tag
        if (rawText.Contains("[i]"))
        {
            textBox.fontStyle |= FontStyles.Italic;
        }
        else
        {
            textBox.fontStyle &= ~FontStyles.Italic;
        }

        // Reset the rich text parsing (if needed)
        textBox.ForceMeshUpdate();
    }

    // --- Update verbose UI elements ---
    private void UpdateVerboseUI()
    {
        if (verboseSewingText != null)
        {
            verboseSewingText.text = $"Sewing: {isSewing}, Accumulated: {accumulatedSewingDistance:F2}";
        }
        if (verboseClickText != null)
        {
            verboseClickText.text = $"Clicks: {clicksInCurrentChapter}, Total Penalty Clicks: {totalPenaltyClicks}";
        }
        if (verboseErrorText != null)
        {
            verboseErrorText.text = ""; // Clear errors
        }
    }

    // --- Show temporary image feedback (sewing success or clicked) ---
    private System.Collections.IEnumerator ShowTempImage(Image img, float duration)
    {
        if (img == null) yield break;
        img.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        img.gameObject.SetActive(false);
    }
}
