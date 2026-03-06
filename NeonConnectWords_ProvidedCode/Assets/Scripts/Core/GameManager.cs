using UnityEngine;
using NeonConnectWords.Simulation;

/// <summary>
/// GameManager now treats the simulator as the source of truth and updates the
/// scene/HUD from simulator state snapshots.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public GameMode currentMode = GameMode.Classic;
    public int classicTargetScore = 100;
    public float timedDuration = 120f;
    public int playerCount = 2;

    [Header("Letter Pool")]
    [Tooltip("Weighted letter distribution for the simulator.")]
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
    public SimulationService simulationService;

    public GameMode ActiveMode { get; private set; }
    public int CurrentPlayerIndex => GetState() != null ? GetState().CurrentPlayerIndex : 0;

    private bool gameOver;

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
        if (uiManager != null)
        {
            uiManager.ShowMainMenu();
        }
    }

    private void Update()
    {
        if (ActiveMode != GameMode.Timed || gameOver || simulationService == null)
        {
            return;
        }

        bool ended = simulationService.TickTimedMode(Time.deltaTime);
        SimulationState state = GetState();
        if (state != null && uiManager != null)
        {
            uiManager.UpdateTimerDisplay(state.TimedRemaining);
        }

        if (ended && state != null)
        {
            EndGame(state.GameOverMessage);
        }
    }

    public void StartGame(GameMode mode)
    {
        if (simulationService == null)
        {
            Debug.LogError("[GameManager] SimulationService is required.");
            return;
        }

        SyncSimulationConfig();
        simulationService.RebuildSimulator();
        simulationService.StartSimulation(mode);

        ActiveMode = mode;
        gameOver = false;

        if (boardManager != null)
        {
            boardManager.StartGame();
        }

        if (uiManager != null)
        {
            uiManager.ShowHUD();
        }

        RefreshPresentation();

        if (mode == GameMode.Tutorial && tutorialManager != null)
        {
            tutorialManager.BeginTutorial();
        }

        if (mode == GameMode.Puzzle && PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.LoadPuzzle(PuzzleManager.Instance.currentPuzzleIndex);
        }

        if (audioManager != null)
        {
            audioManager.PlayMusic(mode == GameMode.Timed);
        }
    }

    public void HandleSimulationTurnResult(SimulationTurnResult result)
    {
        if (result == null)
        {
            return;
        }

        if (!result.Success)
        {
            uiManager?.ShowMessage(result.FailureReason);
            return;
        }

        SimulationState state = GetState();
        if (state != null && audioManager != null)
        {
            if (result.PointsAwarded > 0 && state.ConsecutiveWordTurns >= streakThreshold)
            {
                audioManager.RaiseTempo(state.ConsecutiveWordTurns);
            }
            else if (result.PointsAwarded == 0)
            {
                audioManager.ResetTempo();
            }
        }

        RefreshPresentation();

        if (result.PointsAwarded > 0)
        {
            uiManager?.ShowMessage($"+{result.PointsAwarded} points");
        }

        if (result.GameOver)
        {
            EndGame(result.GameOverMessage);
        }
    }

    public void EndGame(string message)
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        boardManager?.StopGame();
        audioManager?.StopMusic();

        SimulationState state = GetState();
        uiManager?.ShowGameOver(state != null ? state.Scores : new int[playerCount], message);
        Debug.Log("[GameManager] " + message);
    }

    public void ReturnToMainMenu()
    {
        gameOver = true;
        boardManager?.StopGame();
        audioManager?.StopMusic();
        uiManager?.ShowMainMenu();
    }

    public char GetCurrentPlayerLetter()
    {
        SimulationState state = GetState();
        if (state == null || state.CurrentLetters == null || state.CurrentLetters.Length == 0)
        {
            return 'A';
        }

        return state.CurrentLetters[state.CurrentPlayerIndex];
    }

    public void SetCurrentLetter(char c)
    {
        if (c == '*' && simulationService != null && simulationService.ArmWildcard())
        {
            RefreshPresentation();
        }
    }

    public void RefreshPresentation()
    {
        SimulationState state = GetState();
        if (state == null || uiManager == null)
        {
            return;
        }

        uiManager.RefreshAll(state.Scores, state.CurrentPlayerIndex, state.ComboMultiplier);
        uiManager.SetCurrentLetter(state.CurrentLetters[state.CurrentPlayerIndex], state.CurrentPlayerIndex);
        uiManager.UpdateStreakBar(state.ConsecutiveWordTurns, streakThreshold);

        if (ActiveMode == GameMode.Timed)
        {
            uiManager.UpdateTimerDisplay(state.TimedRemaining);
        }

        powerUpManager?.RefreshFromSimulation();
    }

    private SimulationState GetState()
    {
        return simulationService != null ? simulationService.State : null;
    }

    private void SyncSimulationConfig()
    {
        if (simulationService == null)
        {
            return;
        }

        simulationService.columns = boardManager != null ? boardManager.columns : simulationService.columns;
        simulationService.rows = boardManager != null ? boardManager.rows : simulationService.rows;
        simulationService.playerCount = playerCount;
        simulationService.classicTargetScore = classicTargetScore;
        simulationService.timedDuration = timedDuration;
        simulationService.letterPool = letterPool;
        simulationService.comboMultiplierStep = comboMultiplierStep;
        simulationService.maxComboMultiplier = maxComboMultiplier;
        simulationService.streakThreshold = streakThreshold;

        if (powerUpManager != null)
        {
            simulationService.wildcardUnlockAt = powerUpManager.wildcardUnlockAt;
            simulationService.bombUnlockAt = powerUpManager.bombUnlockAt;
            simulationService.swapUnlockAt = powerUpManager.swapUnlockAt;
            simulationService.wildcardEvery = powerUpManager.wildcardEvery;
            simulationService.bombEvery = powerUpManager.bombEvery;
            simulationService.swapEvery = powerUpManager.swapEvery;
        }
    }
}

public enum GameMode
{
    Classic,
    Timed,
    Puzzle,
    Tutorial
}
