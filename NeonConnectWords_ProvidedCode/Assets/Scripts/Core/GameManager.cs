using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameManager: Orchestrates game flow — modes, turns, scoring, streaks, win conditions.
/// Acts as the central state machine for the game.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector Config
    // -----------------------------------------------------------------------
    [Header("Game Settings")]
    public GameMode currentMode = GameMode.Classic;
    public int classicTargetScore = 100;
    public float timedDuration = 120f;
    public int playerCount = 2;

    [Header("Letter Pool")]
    [Tooltip("Weighted letter distribution (26 entries). Adjust frequency to taste.")]
    public string letterPool = "EEEEEEEEEEEEAAAAAAAAAIIIIIIOOOOOOUUUURRRRRRTTTTTTNNNNNNSSSSSSLLLLCCCCPPPPMMMMDDDDGGGGBBBBFFVVWWYYKKJJXXZZQQ";

    [Header("Scoring")]
    public float comboMultiplierStep = 0.5f;
    public float maxComboMultiplier = 5f;
    public int streakThreshold = 3;

    [Header("References")]
    public BoardManager boardManager;
    public UIManager uiManager;
    public AudioManager audioManager;
    public TutorialManager tutorialManager;
    public PowerUpManager powerUpManager;

    // -----------------------------------------------------------------------
    // Runtime State
    // -----------------------------------------------------------------------
    public int CurrentPlayerIndex { get; private set; } = 0;
    public GameMode ActiveMode { get; private set; }

    private int[] scores;
    private char[] playerCurrentLetters;
    private int consecutiveWordTurns = 0;    // streak counter
    private float comboMultiplier = 1f;
    private bool gameOver = false;
    private float timedRemaining;

    // -----------------------------------------------------------------------
    // Unity Lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to board events
        boardManager.OnWordsFound += HandleWordsFound;
        boardManager.OnBoardFull += HandleBoardFull;
    }

    private void Update()
    {
        if (ActiveMode == GameMode.Timed && !gameOver)
        {
            timedRemaining -= Time.deltaTime;
            uiManager.UpdateTimerDisplay(timedRemaining);
            if (timedRemaining <= 0f) EndGame("Time's up!");
        }
    }

    // -----------------------------------------------------------------------
    // Game Lifecycle
    // -----------------------------------------------------------------------
    public void StartGame(GameMode mode)
    {
        ActiveMode = mode;
        gameOver = false;
        scores = new int[playerCount];
        playerCurrentLetters = new char[playerCount];
        consecutiveWordTurns = 0;
        comboMultiplier = 1f;
        CurrentPlayerIndex = 0;
        timedRemaining = timedDuration;

        for (int i = 0; i < playerCount; i++)
            playerCurrentLetters[i] = DrawLetter();

        boardManager.StartGame();
        uiManager.RefreshAll(scores, CurrentPlayerIndex, comboMultiplier);
        uiManager.SetCurrentLetter(playerCurrentLetters[CurrentPlayerIndex], CurrentPlayerIndex);

        if (mode == GameMode.Tutorial)
            tutorialManager.BeginTutorial();

        audioManager.PlayMusic(mode == GameMode.Timed);
        Debug.Log($"[GameManager] Game started. Mode: {mode}");
    }

    public void EndTurn()
    {
        if (gameOver) return;

        // Draw new letter for current player
        playerCurrentLetters[CurrentPlayerIndex] = DrawLetter();

        // Advance turn
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % playerCount;

        uiManager.ShowTurnIndicator(CurrentPlayerIndex);
        uiManager.SetCurrentLetter(playerCurrentLetters[CurrentPlayerIndex], CurrentPlayerIndex);

        // Reset per-turn combo if no words formed
        // (combo resets are handled inside HandleWordsFound / EndTurn based on streaks)
        if (consecutiveWordTurns == 0)
        {
            comboMultiplier = 1f;
            uiManager.UpdateComboMultiplier(comboMultiplier);
            audioManager.ResetTempo();
        }
    }

    // -----------------------------------------------------------------------
    // Scoring
    // -----------------------------------------------------------------------
    private void HandleWordsFound(List<WordResult> words)
    {
        if (words == null || words.Count == 0)
        {
            consecutiveWordTurns = 0;
            return;
        }

        consecutiveWordTurns++;

        // Update multiplier based on streak
        if (consecutiveWordTurns >= streakThreshold)
        {
            comboMultiplier = Mathf.Min(comboMultiplier + comboMultiplierStep, maxComboMultiplier);
            audioManager.RaiseTempo(consecutiveWordTurns);
        }

        int totalPoints = 0;
        foreach (WordResult wr in words)
        {
            int wordPoints = Mathf.RoundToInt(wr.points * comboMultiplier);
            totalPoints += wordPoints;
            uiManager.ShowFloatingScore(wordPoints, GetWordCenterWorld(wr));
        }

        // Multi-word bonus
        if (words.Count > 1)
        {
            int bonus = Mathf.RoundToInt(words.Count * 10 * comboMultiplier);
            totalPoints += bonus;
            uiManager.ShowFloatingScore(bonus, Vector3.zero, "COMBO!");
        }

        scores[CurrentPlayerIndex] += totalPoints;
        uiManager.UpdateScore(CurrentPlayerIndex, scores[CurrentPlayerIndex]);
        uiManager.UpdateComboMultiplier(comboMultiplier);
        uiManager.UpdateStreakBar(consecutiveWordTurns, streakThreshold);

        // Unlock power-ups progressively
        powerUpManager.OnPointsEarned(scores[CurrentPlayerIndex]);

        // Win check
        if (ActiveMode == GameMode.Classic && scores[CurrentPlayerIndex] >= classicTargetScore)
            EndGame($"Player {CurrentPlayerIndex + 1} wins!");
    }

    private Vector3 GetWordCenterWorld(WordResult wr)
    {
        // Approximate centre of word (UIManager will convert to screen space)
        return Vector3.zero;
    }

    // -----------------------------------------------------------------------
    // Win / Loss
    // -----------------------------------------------------------------------
    private void HandleBoardFull()
    {
        int winner = 0;
        for (int i = 1; i < playerCount; i++)
            if (scores[i] > scores[winner]) winner = i;

        EndGame($"Board full! Player {winner + 1} wins with {scores[winner]} pts!");
    }

    public void EndGame(string message)
    {
        gameOver = true;
        boardManager.StopGame();
        audioManager.StopMusic();
        uiManager.ShowGameOver(scores, message);
        Debug.Log($"[GameManager] {message}");
    }

    // -----------------------------------------------------------------------
    // Letter Management
    // -----------------------------------------------------------------------
    public char GetCurrentPlayerLetter()
    {
        return playerCurrentLetters[CurrentPlayerIndex];
    }

    private char DrawLetter()
    {
        return letterPool[Random.Range(0, letterPool.Length)];
    }

    // Called by PowerUpManager when Wildcard is activated
    public void SetCurrentLetter(char c)
    {
        playerCurrentLetters[CurrentPlayerIndex] = c;
        uiManager.SetCurrentLetter(c, CurrentPlayerIndex);
    }
}

public enum GameMode { Classic, Timed, Puzzle, Tutorial }
