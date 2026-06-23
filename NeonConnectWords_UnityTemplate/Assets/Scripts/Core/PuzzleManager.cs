using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Lightweight puzzle mode with seeded boards and move-limited goals.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    [Header("References")]
    public BoardManager boardManager;
    public UIManager uiManager;

    [Header("Puzzle Data")]
    public PuzzleDefinition[] puzzles;

    [Header("Optional UI")]
    public TMP_Text puzzleTitleText;
    public TMP_Text puzzleMovesText;

    public bool IsPuzzleResolved { get; private set; }

    private int activePuzzleIndex;
    private int wordsClearedThisPuzzle;
    private int turnsUsedThisPuzzle;
    private bool puzzleActive;

    private void Start()
    {
        if (boardManager != null)
        {
            boardManager.TilePlaced += HandleTilePlaced;
            boardManager.WordsCleared += HandleWordsCleared;
            boardManager.BoardFull += HandleBoardFull;
        }
    }

    private void OnDestroy()
    {
        if (boardManager != null)
        {
            boardManager.TilePlaced -= HandleTilePlaced;
            boardManager.WordsCleared -= HandleWordsCleared;
            boardManager.BoardFull -= HandleBoardFull;
        }
    }

    public void BeginPuzzleGame()
    {
        puzzleActive = true;
        IsPuzzleResolved = false;
        activePuzzleIndex = Mathf.Clamp(activePuzzleIndex, 0, Mathf.Max(0, puzzles.Length - 1));

        if (puzzles == null || puzzles.Length == 0)
        {
            puzzles = CreateFallbackPuzzles();
        }

        LoadPuzzle(activePuzzleIndex);
    }

    public void StopPuzzle()
    {
        puzzleActive = false;
        IsPuzzleResolved = false;
        uiManager?.ShowPuzzleObjective(string.Empty);
    }

    public void FailCurrentPuzzle(string reason)
    {
        if (!puzzleActive || IsPuzzleResolved)
        {
            return;
        }

        IsPuzzleResolved = true;
        puzzleActive = false;
        GameManager.Instance?.EndGame(reason);
    }

    private void LoadPuzzle(int index)
    {
        if (puzzles == null || puzzles.Length == 0 || boardManager == null)
        {
            return;
        }

        PuzzleDefinition puzzle = puzzles[Mathf.Clamp(index, 0, puzzles.Length - 1)];
        boardManager.ClearBoardImmediate();
        wordsClearedThisPuzzle = 0;
        turnsUsedThisPuzzle = 0;
        IsPuzzleResolved = false;

        for (int i = 0; i < puzzle.presetTiles.Count; i++)
        {
            PresetTile preset = puzzle.presetTiles[i];
            boardManager.SeedTileAt(preset.column, preset.row, preset.letter, preset.ownerIndex, preset.wildcard);
        }

        RefreshPuzzleUI(puzzle);
    }

    private void HandleTilePlaced(int column, int row, LetterTile tile)
    {
        if (!puzzleActive || GameManager.Instance == null || GameManager.Instance.ActiveMode != GameMode.Puzzle)
        {
            return;
        }

        turnsUsedThisPuzzle++;
        RefreshPuzzleUI(GetCurrentPuzzle());
    }

    private void HandleWordsCleared(List<WordResult> words, int cascadeDepth, Vector3 center)
    {
        if (!puzzleActive || GameManager.Instance == null || GameManager.Instance.ActiveMode != GameMode.Puzzle)
        {
            return;
        }

        wordsClearedThisPuzzle += words.Count;

        PuzzleDefinition puzzle = GetCurrentPuzzle();
        RefreshPuzzleUI(puzzle);

        if (wordsClearedThisPuzzle >= puzzle.wordsRequired)
        {
            IsPuzzleResolved = true;
            puzzleActive = false;
            GameManager.Instance?.EndGame($"Puzzle cleared: {puzzle.puzzleName}");
        }
    }

    private void HandleBoardFull()
    {
        if (!puzzleActive || GameManager.Instance == null || GameManager.Instance.ActiveMode != GameMode.Puzzle)
        {
            return;
        }

        FailCurrentPuzzle("Puzzle failed: the board is full.");
    }

    private void RefreshPuzzleUI(PuzzleDefinition puzzle)
    {
        int turnsLeft = Mathf.Max(0, puzzle.moveLimit - turnsUsedThisPuzzle);
        int wordsLeft = Mathf.Max(0, puzzle.wordsRequired - wordsClearedThisPuzzle);

        if (puzzleTitleText != null)
        {
            puzzleTitleText.text = puzzle.puzzleName;
        }

        if (puzzleMovesText != null)
        {
            puzzleMovesText.text = $"Moves {turnsLeft}";
        }

        uiManager?.ShowPuzzleObjective($"{puzzle.description} | {wordsLeft} word(s) left | {turnsLeft} move(s) left");

        if (turnsUsedThisPuzzle >= puzzle.moveLimit && wordsClearedThisPuzzle < puzzle.wordsRequired)
        {
            FailCurrentPuzzle($"Puzzle failed: {puzzle.puzzleName}");
        }
    }

    private PuzzleDefinition GetCurrentPuzzle()
    {
        if (puzzles == null || puzzles.Length == 0)
        {
            return new PuzzleDefinition();
        }

        return puzzles[Mathf.Clamp(activePuzzleIndex, 0, puzzles.Length - 1)];
    }

    private PuzzleDefinition[] CreateFallbackPuzzles()
    {
        return new[]
        {
            new PuzzleDefinition
            {
                puzzleName = "Starter Spark",
                description = "Create one word from the seeded tiles.",
                moveLimit = 4,
                wordsRequired = 1,
                presetTiles = new List<PresetTile>
                {
                    new PresetTile { column = 2, row = 0, letter = 'C', ownerIndex = 0 },
                    new PresetTile { column = 3, row = 0, letter = 'A', ownerIndex = 1 }
                }
            },
            new PuzzleDefinition
            {
                puzzleName = "Diagonal Glow",
                description = "Use the diagonal lane for a two-word turn.",
                moveLimit = 5,
                wordsRequired = 2,
                presetTiles = new List<PresetTile>
                {
                    new PresetTile { column = 1, row = 0, letter = 'N', ownerIndex = 0 },
                    new PresetTile { column = 2, row = 1, letter = 'E', ownerIndex = 1 },
                    new PresetTile { column = 3, row = 2, letter = 'O', ownerIndex = 0 }
                }
            }
        };
    }
}

[System.Serializable]
public class PuzzleDefinition
{
    public string puzzleName = "Puzzle";
    [TextArea(2, 3)] public string description = "Make words before you run out of moves.";
    public int moveLimit = 5;
    public int wordsRequired = 1;
    public List<PresetTile> presetTiles = new List<PresetTile>();
}

[System.Serializable]
public class PresetTile
{
    public int column;
    public int row;
    public char letter = 'A';
    public int ownerIndex;
    public bool wildcard;
}
