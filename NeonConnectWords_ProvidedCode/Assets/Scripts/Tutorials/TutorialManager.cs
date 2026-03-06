using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TutorialManager: Drives the three-step tutorial sequence.
/// Step 1 — Basic drops and single-word scoring.
/// Step 2 — Cascade drops and multi-word combos.
/// Step 3 — Power-ups and epic connection combos.
/// Uses overlay panels and highlight glows to guide the player.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector
    // -----------------------------------------------------------------------
    [Header("Tutorial UI")]
    public GameObject tutorialOverlay;
    public TextMeshProUGUI tutorialTitleText;
    public TextMeshProUGUI tutorialBodyText;
    public Button nextButton;
    public Button skipButton;
    public Image highlightArrow;    // animated arrow pointing at target

    [Header("Column Highlights")]
    public GameObject[] columnHighlightObjects;  // one glow per column

    [Header("Step Configs")]
    public TutorialStep[] steps;

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    private int currentStep = 0;
    private bool waitingForPlayerAction = false;
    private Coroutine stepRoutine;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (nextButton) nextButton.onClick.AddListener(AdvanceStep);
        if (skipButton) skipButton.onClick.AddListener(EndTutorial);

        // Subscribe to board events for step completion detection
        BoardManager.Instance.OnTilePlaced += OnTilePlaced;
        BoardManager.Instance.OnWordsFound += OnWordsFound;
    }

    // -----------------------------------------------------------------------
    // Tutorial Flow
    // -----------------------------------------------------------------------
    public void BeginTutorial()
    {
        currentStep = 0;
        if (tutorialOverlay) tutorialOverlay.SetActive(true);
        ShowStep(currentStep);
    }

    private void ShowStep(int index)
    {
        if (index >= steps.Length) { EndTutorial(); return; }

        TutorialStep step = steps[index];
        if (tutorialTitleText) tutorialTitleText.text = step.title;
        if (tutorialBodyText) tutorialBodyText.text = step.body;

        // Hide/show next button based on whether action is required
        if (nextButton) nextButton.gameObject.SetActive(!step.requiresPlayerAction);

        // Highlight suggested columns
        SetColumnHighlights(step.highlightColumns);

        // Animate arrow if target column specified
        if (step.arrowTargetColumn >= 0) StartCoroutine(AnimateArrow(step.arrowTargetColumn));

        waitingForPlayerAction = step.requiresPlayerAction;
    }

    private void AdvanceStep()
    {
        currentStep++;
        ShowStep(currentStep);
    }

    public void EndTutorial()
    {
        if (tutorialOverlay) tutorialOverlay.SetActive(false);
        SetColumnHighlights(new int[0]);
        GameManager.Instance.StartGame(GameMode.Classic);
    }

    // -----------------------------------------------------------------------
    // Board Event Hooks
    // -----------------------------------------------------------------------
    private int tilesDropped = 0;
    private int wordsFound = 0;

    private void OnTilePlaced(int col, int row, char letter, int playerIndex)
    {
        tilesDropped++;

        // Step 0 completes after first drop
        if (currentStep == 0 && tilesDropped >= 1 && waitingForPlayerAction)
            StartCoroutine(DelayedAdvance(0.8f));
    }

    private void OnWordsFound(List<WordResult> results)
    {
        wordsFound += results.Count;

        // Step 1 completes after finding first word
        if (currentStep == 1 && wordsFound >= 1 && waitingForPlayerAction)
            StartCoroutine(DelayedAdvance(1.2f));

        // Step 2 completes after multi-word combo
        if (currentStep == 2 && results.Count >= 2 && waitingForPlayerAction)
            StartCoroutine(DelayedAdvance(1.5f));
    }

    private IEnumerator DelayedAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        AdvanceStep();
    }

    // -----------------------------------------------------------------------
    // Visuals
    // -----------------------------------------------------------------------
    private void SetColumnHighlights(int[] cols)
    {
        for (int i = 0; i < columnHighlightObjects.Length; i++)
        {
            if (columnHighlightObjects[i])
                columnHighlightObjects[i].SetActive(false);
        }
        foreach (int c in cols)
        {
            if (c >= 0 && c < columnHighlightObjects.Length && columnHighlightObjects[c])
                columnHighlightObjects[c].SetActive(true);
        }
    }

    private IEnumerator AnimateArrow(int targetColumn)
    {
        if (!highlightArrow) yield break;
        highlightArrow.gameObject.SetActive(true);

        // Get screen position of target column top
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float bob = Mathf.Sin(Time.time * 4f) * 10f;
            highlightArrow.rectTransform.anchoredPosition += Vector2.up * bob * Time.deltaTime;
            yield return null;
        }
    }
}

// -----------------------------------------------------------------------
// Data
// -----------------------------------------------------------------------

[System.Serializable]
public class TutorialStep
{
    [Header("Content")]
    public string title;
    [TextArea(2, 5)] public string body;

    [Header("Interaction")]
    public bool requiresPlayerAction;   // if true, waits for player event instead of Next button

    [Header("Highlights")]
    public int[] highlightColumns;
    public int arrowTargetColumn = -1;
}
