using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PuzzleManager seeds puzzle boards through the simulator-backed BoardManager.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [Header("Puzzles")]
    public PuzzleDefinition[] puzzles;
    public int currentPuzzleIndex = 0;

    [Header("UI")]
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI movesRemainingText;
    public GameObject puzzleCompletePanel;
    public GameObject puzzleFailPanel;
    public TextMeshProUGUI puzzleCompleteText;
    public Button nextPuzzleButton;
    public Button retryButton;

    private PuzzleDefinition activePuzzle;
    private int movesUsed;
    private int wordsFoundThisPuzzle;
    private int targetWordsRequired;

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
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.OnWordsFound += HandleWordsFound;
            BoardManager.Instance.OnTilePlaced += HandleTilePlaced;
        }

        if (nextPuzzleButton) nextPuzzleButton.onClick.AddListener(LoadNextPuzzle);
        if (retryButton) retryButton.onClick.AddListener(RetryPuzzle);
    }

    public void LoadPuzzle(int index)
    {
        if (index >= puzzles.Length)
        {
            Debug.Log("[PuzzleManager] All puzzles complete.");
            return;
        }

        currentPuzzleIndex = index;
        activePuzzle = puzzles[index];
        movesUsed = 0;
        wordsFoundThisPuzzle = 0;
        targetWordsRequired = activePuzzle.wordsRequired;

        BoardManager.Instance.InitBoard();
        foreach (PresetTile tile in activePuzzle.presetTiles)
        {
            BoardManager.Instance.DropLetter(tile.column, tile.letter, 1);
        }

        RefreshUI();
        if (puzzleCompletePanel) puzzleCompletePanel.SetActive(false);
        if (puzzleFailPanel) puzzleFailPanel.SetActive(false);
    }

    private void RefreshUI()
    {
        if (activePuzzle == null)
        {
            return;
        }

        if (objectiveText)
            objectiveText.text = $"Find {targetWordsRequired - wordsFoundThisPuzzle} more word(s). {activePuzzle.movesAllowed - movesUsed} moves left.";
        if (movesRemainingText)
            movesRemainingText.text = $"Moves: {activePuzzle.movesAllowed - movesUsed}";
    }

    private void HandleTilePlaced(int col, int row, char letter, int player)
    {
        if (GameManager.Instance.ActiveMode != GameMode.Puzzle)
        {
            return;
        }

        movesUsed++;

        if (movesUsed >= activePuzzle.movesAllowed && wordsFoundThisPuzzle < targetWordsRequired)
        {
            StartCoroutine(ShowResult(false));
        }

        RefreshUI();
    }

    private void HandleWordsFound(List<WordResult> words)
    {
        if (GameManager.Instance.ActiveMode != GameMode.Puzzle)
        {
            return;
        }

        wordsFoundThisPuzzle += words.Count;

        if (wordsFoundThisPuzzle >= targetWordsRequired)
            StartCoroutine(ShowResult(true));

        RefreshUI();
    }

    private IEnumerator ShowResult(bool success)
    {
        yield return new WaitForSeconds(0.5f);
        BoardManager.Instance.StopGame();

        if (success)
        {
            if (puzzleCompletePanel) puzzleCompletePanel.SetActive(true);
            if (puzzleCompleteText) puzzleCompleteText.text = $"Puzzle {currentPuzzleIndex + 1} Complete!";
            AudioManager.Instance?.PlayWordComplete(6, 3);
        }
        else
        {
            if (puzzleFailPanel) puzzleFailPanel.SetActive(true);
        }
    }

    private void LoadNextPuzzle()
    {
        LoadPuzzle(currentPuzzleIndex + 1);
    }

    private void RetryPuzzle()
    {
        LoadPuzzle(currentPuzzleIndex);
    }
}

[System.Serializable]
public class PuzzleDefinition
{
    public string puzzleName;
    [TextArea(1, 3)] public string description;
    public PresetTile[] presetTiles;
    public int movesAllowed = 5;
    public int wordsRequired = 1;
}

[System.Serializable]
public class PresetTile
{
    public int column;
    public char letter;
}
