using UnityEngine;

/// <summary>
/// Step-based tutorial that watches live gameplay events.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("References")]
    public BoardManager boardManager;
    public UIManager uiManager;
    public PowerUpManager powerUpManager;

    [Header("Optional Visual Highlights")]
    public GameObject[] columnHighlights;

    [Header("Steps")]
    public TutorialStepData[] steps;

    public bool IsActive { get; private set; }

    private int currentStepIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (steps == null || steps.Length == 0)
        {
            steps = CreateDefaultSteps();
        }

        if (boardManager != null)
        {
            boardManager.TilePlaced += HandleTilePlaced;
            boardManager.WordsCleared += HandleWordsCleared;
        }

        if (uiManager != null)
        {
            if (uiManager.tutorialNextButton != null)
            {
                uiManager.tutorialNextButton.onClick.AddListener(AdvanceFromButton);
            }

            if (uiManager.tutorialSkipButton != null)
            {
                uiManager.tutorialSkipButton.onClick.AddListener(HideTutorial);
            }
        }
    }

    private void OnDestroy()
    {
        if (boardManager != null)
        {
            boardManager.TilePlaced -= HandleTilePlaced;
            boardManager.WordsCleared -= HandleWordsCleared;
        }
    }

    public void BeginTutorial()
    {
        IsActive = true;
        currentStepIndex = 0;
        powerUpManager?.GrantTutorialLoadout();
        ShowCurrentStep();
    }

    public void HideTutorial()
    {
        bool wasActive = IsActive;
        IsActive = false;
        SetHighlights(null);
        uiManager?.HideTutorialOverlay();

        if (wasActive && GameManager.Instance != null)
        {
            GameManager.Instance.CompleteTutorial();
        }
    }

    public void NotifyPowerUpUsed(PowerUpType type)
    {
        if (!IsActive)
        {
            return;
        }

        TutorialStepData step = GetCurrentStep();
        if (step.objective == TutorialObjective.UsePowerUp)
        {
            AdvanceStep();
        }
    }

    private void HandleTilePlaced(int column, int row, LetterTile tile)
    {
        if (!IsActive)
        {
            return;
        }

        TutorialStepData step = GetCurrentStep();
        if (step.objective == TutorialObjective.PlaceTile)
        {
            AdvanceStep();
        }
    }

    private void HandleWordsCleared(System.Collections.Generic.List<WordResult> words, int cascadeDepth, Vector3 center)
    {
        if (!IsActive)
        {
            return;
        }

        TutorialStepData step = GetCurrentStep();
        if (step.objective == TutorialObjective.ClearWord && words.Count > 0)
        {
            AdvanceStep();
        }
    }

    private void AdvanceFromButton()
    {
        if (!IsActive)
        {
            return;
        }

        TutorialStepData step = GetCurrentStep();
        if (step.objective == TutorialObjective.None)
        {
            AdvanceStep();
        }
    }

    private void AdvanceStep()
    {
        currentStepIndex++;
        if (currentStepIndex >= steps.Length)
        {
            HideTutorial();
            uiManager?.ShowMessage("Tutorial complete");
            return;
        }

        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        TutorialStepData step = GetCurrentStep();
        uiManager?.ShowTutorialStep(step.title, step.body, step.objective == TutorialObjective.None);
        SetHighlights(step.highlightColumns);
    }

    private void SetHighlights(int[] highlightedColumns)
    {
        if (columnHighlights == null)
        {
            return;
        }

        for (int i = 0; i < columnHighlights.Length; i++)
        {
            if (columnHighlights[i] != null)
            {
                columnHighlights[i].SetActive(false);
            }
        }

        if (highlightedColumns == null)
        {
            return;
        }

        for (int i = 0; i < highlightedColumns.Length; i++)
        {
            int column = highlightedColumns[i];
            if (column >= 0 && column < columnHighlights.Length && columnHighlights[column] != null)
            {
                columnHighlights[column].SetActive(true);
            }
        }
    }

    private TutorialStepData GetCurrentStep()
    {
        if (steps == null || steps.Length == 0)
        {
            return new TutorialStepData();
        }

        return steps[Mathf.Clamp(currentStepIndex, 0, steps.Length - 1)];
    }

    private TutorialStepData[] CreateDefaultSteps()
    {
        return new[]
        {
            new TutorialStepData
            {
                title = "Drop A Letter",
                body = "Hover a column and place your first neon letter block.",
                objective = TutorialObjective.PlaceTile,
                highlightColumns = new[] { 2, 3, 4 }
            },
            new TutorialStepData
            {
                title = "Make A Word",
                body = "Connect letters horizontally, vertically, or diagonally to clear a word.",
                objective = TutorialObjective.ClearWord,
                highlightColumns = new[] { 1, 2, 3, 4, 5 }
            },
            new TutorialStepData
            {
                title = "Try A Power Up",
                body = "Use wildcard, bomb, or swap to manipulate the board and keep combos going.",
                objective = TutorialObjective.UsePowerUp,
                highlightColumns = new[] { 3 }
            }
        };
    }
}

public enum TutorialObjective
{
    None,
    PlaceTile,
    ClearWord,
    UsePowerUp
}

[System.Serializable]
public class TutorialStepData
{
    public string title = "Tutorial";
    [TextArea(2, 5)] public string body = "Learn the basics.";
    public TutorialObjective objective = TutorialObjective.None;
    public int[] highlightColumns;
}
