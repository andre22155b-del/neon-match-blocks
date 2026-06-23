using System.Collections.Generic;
using UnityEngine;

public enum GameMode
{
    Classic,
    Timed,
    Puzzle
}

public enum PowerUpType
{
    Wildcard,
    Bomb,
    Swap
}

/// <summary>
/// Owns match state, turn order, scoring, and game mode rules.
/// BoardManager resolves placement and cascades, while GameManager scores the result.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Mode Rules")]
    public GameMode defaultMode = GameMode.Classic;
    public int playerCount = 2;
    public int classicTargetScore = 60;
    public float timedDuration = 120f;

    [Header("Letter Pool")]
    [Tooltip("Weighted pool used for random draws.")]
    public string weightedLetterPool = "EEEEEEEEEEEEAAAAAAAAAIIIIIIIIOOOOOOOONNNNNNRRRRRRTTTTTTLLLLSSSSUUUUDDDDGGGBBCCMMPPFFHHVVWWYYKJXQZ";

    [Header("Scoring")]
    [Tooltip("Added for each extra word found in the same resolve wave.")]
    public float multiWordBonusStep = 0.35f;
    [Tooltip("Added for each extra cascade wave.")]
    public float cascadeBonusStep = 0.5f;
    public float maxDisplayedCombo = 4.5f;

    [Header("References")]
    public BoardManager boardManager;
    public UIManager uiManager;
    public AudioManager audioManager;
    public PowerUpManager powerUpManager;
    public TutorialManager tutorialManager;
    public PuzzleManager puzzleManager;

    public GameMode ActiveMode { get; private set; }
    public int CurrentPlayerIndex { get; private set; }
    public bool IsGameRunning { get; private set; }
    public bool IsTutorialRunning { get; private set; }
    public float RemainingTime => remainingTime;
    public int[] Scores => scores;
    public int TurnCount => turnCount;

    private int[] scores = new int[2];
    private char[] heldLetters = new char[2];
    private float displayedCombo = 1f;
    private float remainingTime;
    private int turnCount;

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
        if (boardManager != null)
        {
            boardManager.TilePlaced += HandleTilePlaced;
            boardManager.WordsCleared += HandleWordsCleared;
            boardManager.TurnFinished += HandleTurnFinished;
            boardManager.BoardFull += HandleBoardFull;
        }

        if (uiManager != null)
        {
            uiManager.ShowMainMenu();
        }
    }

    private void OnDestroy()
    {
        if (boardManager != null)
        {
            boardManager.TilePlaced -= HandleTilePlaced;
            boardManager.WordsCleared -= HandleWordsCleared;
            boardManager.TurnFinished -= HandleTurnFinished;
            boardManager.BoardFull -= HandleBoardFull;
        }
    }

    private void Update()
    {
        if (!IsGameRunning || ActiveMode != GameMode.Timed)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        uiManager?.UpdateTimerDisplay(Mathf.Max(remainingTime, 0f), timedDuration);

        if (remainingTime <= 0f)
        {
            EndGame(BuildWinnerMessage("Time Up"));
        }
    }

    public void StartGame(GameMode mode)
    {
        StartGame(mode, false);
    }

    public void StartTutorialMode()
    {
        StartGame(GameMode.Classic, true);
    }

    public void StartGame(GameMode mode, bool tutorialMode)
    {
        ActiveMode = mode;
        IsTutorialRunning = tutorialMode;
        IsGameRunning = true;
        CurrentPlayerIndex = 0;
        displayedCombo = 1f;
        remainingTime = timedDuration;
        turnCount = 0;

        playerCount = Mathf.Max(1, playerCount);
        scores = new int[playerCount];
        heldLetters = new char[playerCount];

        for (int i = 0; i < playerCount; i++)
        {
            heldLetters[i] = DrawRandomLetter();
        }

        boardManager?.StartBoard();
        powerUpManager?.ResetForNewGame();
        puzzleManager?.StopPuzzle();
        tutorialManager?.HideTutorial();

        if (mode == GameMode.Puzzle)
        {
            puzzleManager?.BeginPuzzleGame();
        }

        if (tutorialMode)
        {
            tutorialManager?.BeginTutorial();
        }

        audioManager?.PlayMusic(mode == GameMode.Timed);
        uiManager?.ShowGameHUD();
        uiManager?.UpdateTimerVisibility(mode == GameMode.Timed);
        uiManager?.ShowPuzzleObjective(string.Empty);
        RefreshHUD();
    }

    public void RestartCurrentGame()
    {
        if (IsTutorialRunning)
        {
            StartTutorialMode();
            return;
        }

        StartGame(ActiveMode, false);
    }

    public void ReturnToMainMenu()
    {
        IsGameRunning = false;
        IsTutorialRunning = false;
        boardManager?.StopBoard();
        puzzleManager?.StopPuzzle();
        tutorialManager?.HideTutorial();
        powerUpManager?.ClearQueuedState();
        audioManager?.StopMusic();
        uiManager?.ShowMainMenu();
    }

    public void EndGame(string title)
    {
        if (!IsGameRunning)
        {
            return;
        }

        IsGameRunning = false;
        IsTutorialRunning = false;

        boardManager?.StopBoard();
        puzzleManager?.StopPuzzle();
        tutorialManager?.HideTutorial();
        powerUpManager?.ClearQueuedState();
        audioManager?.StopMusic();
        uiManager?.ShowGameOver(title, scores);
    }

    public void CompleteTutorial()
    {
        if (!IsGameRunning)
        {
            return;
        }

        IsTutorialRunning = false;
        RefreshHUD();
    }

    public char GetCurrentPlayerLetter()
    {
        if (heldLetters == null || heldLetters.Length == 0)
        {
            return 'A';
        }

        return heldLetters[CurrentPlayerIndex];
    }

    public void RerollCurrentLetter(bool showFeedback)
    {
        heldLetters[CurrentPlayerIndex] = DrawRandomLetter();

        if (showFeedback)
        {
            uiManager?.ShowMessage($"Player {CurrentPlayerIndex + 1} swapped to {heldLetters[CurrentPlayerIndex]}");
        }

        RefreshHUD();
    }

    public void ForceCurrentLetter(char letter)
    {
        heldLetters[CurrentPlayerIndex] = char.ToUpperInvariant(letter);
        RefreshHUD();
    }

    private void HandleTilePlaced(int column, int row, LetterTile tile)
    {
        if (!IsGameRunning)
        {
            return;
        }

        uiManager?.ShowMessage($"Player {CurrentPlayerIndex + 1} dropped {(tile.IsWildcard ? "Wildcard" : tile.Letter.ToString())}", 0.65f);
    }

    private void HandleWordsCleared(List<WordResult> words, int cascadeDepth, Vector3 centerPoint)
    {
        if (!IsGameRunning || words == null || words.Count == 0)
        {
            return;
        }

        int baseLetters = 0;
        for (int i = 0; i < words.Count; i++)
        {
            baseLetters += words[i].points;
        }

        float multiWordBonus = 1f + Mathf.Max(0, words.Count - 1) * multiWordBonusStep;
        float cascadeBonus = 1f + Mathf.Max(0, cascadeDepth - 1) * cascadeBonusStep;
        displayedCombo = Mathf.Min(maxDisplayedCombo, multiWordBonus * cascadeBonus);

        int awardedPoints = Mathf.RoundToInt(baseLetters * displayedCombo);
        scores[CurrentPlayerIndex] += awardedPoints;

        string floatingPrefix = cascadeDepth > 1 ? $"Cascade {cascadeDepth}" : words.Count > 1 ? "Combo" : "Word";
        uiManager?.ShowFloatingScore(awardedPoints, centerPoint, floatingPrefix);
        uiManager?.ShowMessage(BuildWordSummary(words, awardedPoints), 1.2f);
        audioManager?.PlayWordClear(words.Count, cascadeDepth);

        if (words.Count > 1 || cascadeDepth > 1)
        {
            CameraController.Instance?.PlayComboShot();
            ParticleManager.Instance?.SpawnComboBurst(centerPoint, cascadeDepth);
        }
        else
        {
            ParticleManager.Instance?.SpawnWordBurst(centerPoint);
        }

        RefreshHUD();
    }

    private void HandleTurnFinished(bool scoredThisTurn)
    {
        if (!IsGameRunning)
        {
            return;
        }

        int actingPlayer = CurrentPlayerIndex;
        turnCount++;

        if (!scoredThisTurn)
        {
            displayedCombo = 1f;
        }

        powerUpManager?.EvaluateUnlocks(turnCount);

        if (ActiveMode == GameMode.Puzzle && puzzleManager != null && puzzleManager.IsPuzzleResolved)
        {
            return;
        }

        if (ActiveMode == GameMode.Classic && scores[actingPlayer] >= classicTargetScore)
        {
            EndGame($"Player {actingPlayer + 1} reached {classicTargetScore} points");
            return;
        }

        heldLetters[actingPlayer] = DrawRandomLetter();
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % playerCount;
        RefreshHUD();
    }

    private void HandleBoardFull()
    {
        if (!IsGameRunning)
        {
            return;
        }

        if (ActiveMode == GameMode.Puzzle && puzzleManager != null)
        {
            puzzleManager.FailCurrentPuzzle("The board filled before the puzzle was cleared.");
            return;
        }

        EndGame(BuildWinnerMessage("Board Full"));
    }

    private void RefreshHUD()
    {
        uiManager?.RefreshHUD(scores, CurrentPlayerIndex, GetCurrentPlayerLetter(), displayedCombo, ActiveMode, IsTutorialRunning);
        uiManager?.UpdateTimerDisplay(remainingTime, timedDuration);
    }

    private char DrawRandomLetter()
    {
        if (string.IsNullOrEmpty(weightedLetterPool))
        {
            return 'E';
        }

        int index = Random.Range(0, weightedLetterPool.Length);
        return char.ToUpperInvariant(weightedLetterPool[index]);
    }

    private string BuildWordSummary(List<WordResult> words, int points)
    {
        if (words.Count == 1)
        {
            return $"{words[0].word} for {points} points";
        }

        return $"{words.Count} words cleared for {points} points";
    }

    private string BuildWinnerMessage(string reason)
    {
        if (scores == null || scores.Length == 0)
        {
            return reason;
        }

        int bestScore = scores[0];
        int bestPlayer = 0;
        bool isTie = false;

        for (int i = 1; i < scores.Length; i++)
        {
            if (scores[i] > bestScore)
            {
                bestScore = scores[i];
                bestPlayer = i;
                isTie = false;
            }
            else if (scores[i] == bestScore)
            {
                isTie = true;
            }
        }

        return isTie ? $"{reason} - tie game" : $"{reason} - Player {bestPlayer + 1} wins";
    }
}
