using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeonConnectWords.Simulation;

/// <summary>
/// BoardManager is now a visual/input presenter over simulator state.
/// It asks the simulator to resolve actions, then animates/syncs the board.
/// </summary>
public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("Board Configuration")]
    public int columns = 7;
    public int rows = 6;
    public float cellSize = 1.2f;
    public float dropSpeed = 8f;
    public float bounceForce = 0.3f;

    [Header("References")]
    public GameObject letterPrefab;
    public Transform boardOrigin;
    public WordChecker wordChecker;
    public UIManager uiManager;
    public AudioManager audioManager;
    public PowerUpManager powerUpManager;
    public ParticleManager particleManager;
    public SimulationService simulationService;

    [Header("Visual Settings")]
    public Material[] playerMaterials;
    public float dropPreviewAlpha = 0.35f;
    public LayerMask boardLayerMask;

    private LetterTile[,] board;
    private bool isAnimating;
    private bool gameActive;
    private GameObject previewTile;
    private int lastPreviewColumn = -1;

    public System.Action<int, int, char, int> OnTilePlaced;
    public System.Action<List<WordResult>> OnWordsFound;
    public System.Action OnBoardFull;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        board = new LetterTile[columns, rows];
    }

    private void Update()
    {
        if (!gameActive || isAnimating)
        {
            return;
        }

        HandlePointerInput();
    }

    public void InitBoard()
    {
        board = new LetterTile[columns, rows];
        if (simulationService != null)
        {
            simulationService.ResetBoardOnly();
        }

        ClearBoardVisuals();
    }

    public void StartGame()
    {
        InitBoard();
        gameActive = true;
        SyncVisualBoardFromSimulation();
    }

    public void StopGame()
    {
        gameActive = false;
        DestroyPreview();
    }

    private void ClearBoardVisuals()
    {
        DestroyPreview();

        if (boardOrigin == null)
        {
            return;
        }

        for (int i = boardOrigin.childCount - 1; i >= 0; i--)
        {
            Destroy(boardOrigin.GetChild(i).gameObject);
        }

        board = new LetterTile[columns, rows];
    }

    private void HandlePointerInput()
    {
        if (Camera.main == null)
        {
            return;
        }

        if (PointerInputUtility.TryGetPreviewPointer(out Vector2 previewScreenPos, out int previewPointerId))
        {
            if (PointerInputUtility.IsPointerOverUi(previewPointerId))
            {
                DestroyPreview();
            }
            else
            {
                UpdatePreviewFromScreenPosition(previewScreenPos);
            }
        }
        else
        {
            DestroyPreview();
        }

        if (PointerInputUtility.TryGetTapOrClick(out Vector2 tapScreenPos, out int tapPointerId))
        {
            if (PointerInputUtility.IsPointerOverUi(tapPointerId))
            {
                return;
            }

            int column = ScreenToColumn(tapScreenPos);
            if (column >= 0)
            {
                TryDropLetter(column);
            }
        }
    }

    private void UpdatePreviewFromScreenPosition(Vector2 screenPosition)
    {
        int column = ScreenToColumn(screenPosition);
        if (column >= 0)
        {
            UpdatePreview(column);
        }
        else
        {
            DestroyPreview();
        }
    }

    private int ScreenToColumn(Vector2 screenPosition)
    {
        if (Camera.main == null)
        {
            return -1;
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, boardLayerMask))
        {
            return -1;
        }

        int column = WorldToColumn(hit.point.x);
        return column >= 0 && column < columns ? column : -1;
    }

    private int WorldToColumn(float worldX)
    {
        float boardLeft = boardOrigin.position.x - (columns * cellSize * 0.5f);
        int column = Mathf.FloorToInt((worldX - boardLeft) / cellSize);
        return Mathf.Clamp(column, 0, columns - 1);
    }

    private Vector3 GetCellWorldPos(int col, int row)
    {
        float x = boardOrigin.position.x - (columns * cellSize * 0.5f) + col * cellSize + cellSize * 0.5f;
        float y = boardOrigin.position.y + row * cellSize;
        float z = boardOrigin.position.z;
        return new Vector3(x, y, z);
    }

    private void UpdatePreview(int col)
    {
        if (col == lastPreviewColumn)
        {
            return;
        }

        lastPreviewColumn = col;
        DestroyPreview();

        if (!CanDrop(col))
        {
            return;
        }

        char currentLetter = GameManager.Instance != null ? GameManager.Instance.GetCurrentPlayerLetter() : 'A';
        int targetRow = GetColumnHeight(col);
        Vector3 pos = GetCellWorldPos(col, targetRow);

        previewTile = Instantiate(letterPrefab, pos + Vector3.up * (rows * cellSize), Quaternion.identity, boardOrigin);
        LetterTile tile = previewTile.GetComponent<LetterTile>();
        tile.SetLetter(currentLetter);
        tile.SetOwner(GameManager.Instance != null ? GameManager.Instance.CurrentPlayerIndex : 0, GetPlayerMaterial(GameManager.Instance != null ? GameManager.Instance.CurrentPlayerIndex : 0));
        if (currentLetter == '*')
        {
            tile.SetWildcard(true);
        }
        tile.SetPreviewMode(dropPreviewAlpha, GameManager.Instance != null ? GameManager.Instance.CurrentPlayerIndex : 0);
    }

    private void DestroyPreview()
    {
        if (previewTile != null)
        {
            Destroy(previewTile);
        }

        previewTile = null;
        lastPreviewColumn = -1;
    }

    public bool TryDropLetter(int col)
    {
        if (isAnimating || !gameActive)
        {
            return false;
        }

        if (!CanDrop(col))
        {
            uiManager?.ShowMessage("Column full!");
            return false;
        }

        if (simulationService == null)
        {
            uiManager?.ShowMessage("Simulation service missing.");
            return false;
        }

        SimulationTurnResult result = simulationService.DropAtColumn(col);
        if (!result.Success)
        {
            uiManager?.ShowMessage(result.FailureReason);
            return false;
        }

        StartCoroutine(PresentTurnResult(result));
        return true;
    }

    // Used by puzzle setup to seed tiles without consuming turns.
    public void DropLetter(int col, char letter, int playerIndex)
    {
        if (simulationService == null)
        {
            return;
        }

        bool wildcard = letter == '*';
        if (simulationService.SeedTileInColumn(col, wildcard ? 'A' : letter, playerIndex, wildcard))
        {
            SyncVisualBoardFromSimulation();
        }
    }

    public LetterTile GetTile(int col, int row)
    {
        return board[col, row];
    }

    public bool IsBoardFull()
    {
        for (int c = 0; c < columns; c++)
        {
            if (GetColumnHeight(c) < rows)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsColumnFull(int col)
    {
        return GetColumnHeight(col) >= rows;
    }

    public bool ClearRow(int row)
    {
        if (!gameActive)
        {
            return false;
        }

        if (isAnimating)
        {
            uiManager?.ShowMessage("Wait for the current action to finish.");
            return false;
        }

        if (simulationService == null)
        {
            return false;
        }

        SimulationTurnResult result = simulationService.BombRow(row);
        if (result.Success)
        {
            StartCoroutine(PresentTurnResult(result));
            return true;
        }
        else
        {
            uiManager?.ShowMessage(result.FailureReason);
            return false;
        }
    }

    public bool ClearColumn(int col)
    {
        if (!gameActive)
        {
            return false;
        }

        if (isAnimating)
        {
            uiManager?.ShowMessage("Wait for the current action to finish.");
            return false;
        }

        if (simulationService == null)
        {
            return false;
        }

        SimulationTurnResult result = simulationService.BombColumn(col);
        if (result.Success)
        {
            StartCoroutine(PresentTurnResult(result));
            return true;
        }
        else
        {
            uiManager?.ShowMessage(result.FailureReason);
            return false;
        }
    }

    public bool SwapTiles(Vector2Int posA, Vector2Int posB)
    {
        if (!gameActive)
        {
            return false;
        }

        if (isAnimating)
        {
            uiManager?.ShowMessage("Wait for the current action to finish.");
            return false;
        }

        if (simulationService == null)
        {
            return false;
        }

        SimulationTurnResult result = simulationService.SwapTiles(posA, posB);
        if (!result.Success)
        {
            uiManager?.ShowMessage(result.FailureReason);
            return false;
        }

        StartCoroutine(PresentTurnResult(result));
        return true;
    }

    private IEnumerator PresentTurnResult(SimulationTurnResult result)
    {
        isAnimating = true;
        DestroyPreview();

        if (result.ActionType == SimulationActionType.Drop && result.Placement != null)
        {
            yield return StartCoroutine(AnimatePlacement(result.Placement));
            OnTilePlaced?.Invoke(result.Placement.Column, result.Placement.Row, result.Placement.Letter, result.Placement.PlayerIndex);
        }

        List<WordResult> aggregateWords = ConvertWords(result);
        if (aggregateWords.Count > 0)
        {
            OnWordsFound?.Invoke(aggregateWords);

            for (int i = 0; i < aggregateWords.Count; i++)
            {
                WordResult word = aggregateWords[i];
                audioManager?.PlayWordComplete(word.word.Length, aggregateWords.Count);
                if (word.positions.Count > 0)
                {
                    Vector2Int origin = word.positions[0];
                    particleManager?.SpawnWordParticles(GetCellWorldPos(origin.x, origin.y));
                }
            }

            if (aggregateWords.Count > 1)
            {
                CameraController.Instance?.PlayComboShot();
                audioManager?.PlayEpicCombo(aggregateWords.Count);
                Vector3 comboPoint = result.Placement != null ? GetCellWorldPos(result.Placement.Column, result.Placement.Row) : boardOrigin.position;
                particleManager?.SpawnEpicParticles(comboPoint);
            }
        }

        if (result.ActionType == SimulationActionType.BombRow || result.ActionType == SimulationActionType.BombColumn)
        {
            audioManager?.PlayPowerUp(PowerUpType.Bomb);
        }
        else if (result.ActionType == SimulationActionType.Swap)
        {
            audioManager?.PlaySwap();
        }

        yield return new WaitForSeconds(0.12f);
        SyncVisualBoardFromSimulation();

        isAnimating = false;

        if (result.GameOver && !string.IsNullOrEmpty(result.GameOverMessage) && result.GameOverMessage.ToLowerInvariant().Contains("board full"))
        {
            OnBoardFull?.Invoke();
        }

        GameManager.Instance?.HandleSimulationTurnResult(result);
    }

    private IEnumerator AnimatePlacement(SimulationTilePlacement placement)
    {
        Vector3 startPos = GetCellWorldPos(placement.Column, rows + 1);
        Vector3 endPos = GetCellWorldPos(placement.Column, placement.Row);

        GameObject tileObj = Instantiate(letterPrefab, startPos, Quaternion.identity, boardOrigin);
        LetterTile tile = tileObj.GetComponent<LetterTile>();
        tile.SetLetter(placement.IsWildcard ? '*' : placement.Letter);
        tile.SetOwner(placement.PlayerIndex, GetPlayerMaterial(placement.PlayerIndex));
        if (placement.IsWildcard)
        {
            tile.SetWildcard(true);
        }

        tile.EnableTrail(true);
        audioManager?.PlayDropStart();

        float elapsed = 0f;
        float duration = Vector3.Distance(startPos, endPos) / dropSpeed;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            tile.transform.position = Vector3.Lerp(startPos, endPos, EaseInQuad(t));
            yield return null;
        }

        tile.transform.position = endPos;
        tile.EnableTrail(false);
        audioManager?.PlayLetterLand(placement.Row / (float)rows);
        Destroy(tileObj);
    }

    private void SyncVisualBoardFromSimulation()
    {
        ClearBoardVisuals();

        if (simulationService == null || simulationService.State == null)
        {
            return;
        }

        SimulationState state = simulationService.State;
        for (int c = 0; c < columns; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                SimulationTile simTile = state.Board[c, r];
                if (simTile == null)
                {
                    continue;
                }

                GameObject tileObj = Instantiate(letterPrefab, GetCellWorldPos(c, r), Quaternion.identity, boardOrigin);
                LetterTile tile = tileObj.GetComponent<LetterTile>();
                tile.SetLetter(simTile.IsWildcard ? '*' : simTile.Letter);
                tile.SetOwner(simTile.OwnerIndex, GetPlayerMaterial(simTile.OwnerIndex));
                if (simTile.IsWildcard)
                {
                    tile.SetWildcard(true);
                }

                board[c, r] = tile;
            }
        }
    }

    private List<WordResult> ConvertWords(SimulationTurnResult result)
    {
        List<WordResult> converted = new List<WordResult>();
        if (result == null)
        {
            return converted;
        }

        for (int i = 0; i < result.Cascades.Count; i++)
        {
            SimulationCascadeResult cascade = result.Cascades[i];
            for (int w = 0; w < cascade.Words.Count; w++)
            {
                SimulationWordResult simWord = cascade.Words[w];
                List<Vector2Int> positions = new List<Vector2Int>();
                for (int p = 0; p < simWord.Positions.Count; p++)
                {
                    positions.Add(new Vector2Int(simWord.Positions[p].Column, simWord.Positions[p].Row));
                }

                converted.Add(new WordResult(simWord.Word, positions, ConvertDirection(simWord.Direction)));
            }
        }

        return converted;
    }

    private WordDirection ConvertDirection(SimulationWordDirection direction)
    {
        switch (direction)
        {
            case SimulationWordDirection.Vertical:
                return WordDirection.Vertical;
            case SimulationWordDirection.DiagonalUp:
                return WordDirection.DiagonalUp;
            case SimulationWordDirection.DiagonalDown:
                return WordDirection.DiagonalDown;
            default:
                return WordDirection.Horizontal;
        }
    }

    private int GetColumnHeight(int column)
    {
        if (simulationService == null || simulationService.State == null || simulationService.State.ColumnHeights == null)
        {
            return 0;
        }

        return simulationService.State.ColumnHeights[column];
    }

    private bool CanDrop(int column)
    {
        return simulationService != null && simulationService.Simulator != null && simulationService.Simulator.CanDrop(column);
    }

    private Material GetPlayerMaterial(int playerIndex)
    {
        if (playerMaterials == null || playerMaterials.Length == 0)
        {
            return null;
        }

        return playerMaterials[Mathf.Clamp(playerIndex, 0, playerMaterials.Length - 1)];
    }

    private float EaseInQuad(float t)
    {
        return t * t;
    }
}
