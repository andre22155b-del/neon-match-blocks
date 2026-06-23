using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Holds the 7x6 board state, handles player input, animates drops,
/// resolves matched words, and collapses columns for cascades.
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("Board Size")]
    public int columns = 7;
    public int rows = 6;
    public float cellSize = 1.15f;

    [Header("Animation")]
    public float spawnHeightRows = 2f;
    public float dropDuration = 0.28f;
    public float collapseDuration = 0.18f;
    public float previewHeightOffset = 0.08f;
    public float settlePause = 0.08f;
    public float clearPause = 0.2f;

    [Header("References")]
    public Transform boardOrigin;
    public GameObject letterTilePrefab;
    public WordChecker wordChecker;
    public UIManager uiManager;
    public AudioManager audioManager;
    public PowerUpManager powerUpManager;
    public Material[] playerMaterials;
    public LayerMask boardInputMask;

    [Header("Mobile Optimization")]
    public bool reducedAnimationMode;
    public bool allowMouseRaycastInput = true;

    public event Action<int, int, LetterTile> TilePlaced;
    public event Action<List<WordResult>, int, Vector3> WordsCleared;
    public event Action<bool> TurnFinished;
    public event Action BoardFull;

    private LetterTile[,] board;
    private int[] columnHeights;
    private LetterTile previewTile;
    private bool inputEnabled;
    private bool isBusy;
    private int hoveredColumn = -1;

    private void Awake()
    {
        CreateBoardArrays();
    }

    private void Update()
    {
        if (!inputEnabled || isBusy || !allowMouseRaycastInput)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            DestroyPreview();
            return;
        }

        HandlePointerInput();
    }

    public void StartBoard()
    {
        ClearBoardImmediate();
        inputEnabled = true;
        isBusy = false;
        hoveredColumn = -1;
    }

    public void StopBoard()
    {
        inputEnabled = false;
        isBusy = false;
        DestroyPreview();
    }

    public void ClearBoardImmediate()
    {
        CreateBoardArrays();
        DestroyPreview();

        if (boardOrigin == null)
        {
            return;
        }

        for (int i = boardOrigin.childCount - 1; i >= 0; i--)
        {
            Destroy(boardOrigin.GetChild(i).gameObject);
        }
    }

    public bool TryColumnActionByIndex(int column)
    {
        if (!inputEnabled || isBusy)
        {
            return false;
        }

        if (column < 0 || column >= columns)
        {
            return false;
        }

        if (powerUpManager != null && powerUpManager.HasQueuedBomb)
        {
            return TryUseBomb(column);
        }

        return TryDropCurrentLetter(column);
    }

    public void SeedTileAt(int column, int row, char letter, int ownerIndex, bool wildcard)
    {
        if (letterTilePrefab == null || boardOrigin == null)
        {
            return;
        }

        if (!IsCellInBounds(column, row))
        {
            return;
        }

        LetterTile existing = board[column, row];
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        Vector3 position = GetCellWorldPosition(column, row);
        GameObject tileObject = Instantiate(letterTilePrefab, position, Quaternion.identity, boardOrigin);
        LetterTile tile = tileObject.GetComponent<LetterTile>();
        tile.SetLetter(letter);
        tile.SetOwner(ownerIndex, GetPlayerMaterial(ownerIndex));
        tile.SetWildcard(wildcard);

        board[column, row] = tile;
        RecalculateColumnHeights();
    }

    public bool HasAnyTiles()
    {
        for (int c = 0; c < columns; c++)
        {
            if (columnHeights[c] > 0)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsBoardFullNow()
    {
        for (int c = 0; c < columns; c++)
        {
            if (columnHeights[c] < rows)
            {
                return false;
            }
        }

        return true;
    }

    public Vector3 GetCellWorldPosition(int column, int row)
    {
        Vector3 origin = boardOrigin != null ? boardOrigin.position : Vector3.zero;
        float left = origin.x - ((columns - 1) * cellSize * 0.5f);
        float bottom = origin.y;
        return new Vector3(left + column * cellSize, bottom + row * cellSize, origin.z);
    }

    private void CreateBoardArrays()
    {
        board = new LetterTile[columns, rows];
        columnHeights = new int[columns];
    }

    private void HandlePointerInput()
    {
        Camera cameraRef = Camera.main;
        if (cameraRef == null)
        {
            return;
        }

        Ray ray = cameraRef.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f, boardInputMask))
        {
            int column = WorldToColumn(hitInfo.point.x);
            UpdatePreview(column);

            if (Input.GetMouseButtonDown(0))
            {
                TryColumnActionByIndex(column);
            }
        }
        else
        {
            DestroyPreview();
        }
    }

    private bool TryDropCurrentLetter(int column)
    {
        if (columnHeights[column] >= rows)
        {
            uiManager?.ShowMessage("Column full");
            return false;
        }

        StartCoroutine(DropLetterRoutine(column));
        return true;
    }

    private bool TryUseBomb(int column)
    {
        int topRow = columnHeights[column] - 1;
        if (topRow < 0 || board[column, topRow] == null)
        {
            uiManager?.ShowMessage("Pick a column with a tile");
            return false;
        }

        powerUpManager?.ConsumeQueuedBomb();
        StartCoroutine(BombColumnRoutine(column, topRow));
        return true;
    }

    private IEnumerator DropLetterRoutine(int column)
    {
        isBusy = true;
        DestroyPreview();

        int row = columnHeights[column];
        bool isWildcard = powerUpManager != null && powerUpManager.ConsumeQueuedWildcard();
        char chosenLetter = GameManager.Instance != null ? GameManager.Instance.GetCurrentPlayerLetter() : 'A';
        int playerIndex = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerIndex : 0;

        Vector3 start = GetCellWorldPosition(column, rows + Mathf.CeilToInt(spawnHeightRows));
        Vector3 end = GetCellWorldPosition(column, row);

        GameObject tileObject = Instantiate(letterTilePrefab, start, Quaternion.identity, boardOrigin);
        LetterTile tile = tileObject.GetComponent<LetterTile>();
        tile.SetLetter(chosenLetter);
        tile.SetOwner(playerIndex, GetPlayerMaterial(playerIndex));
        tile.SetWildcard(isWildcard);
        tile.EnableTrail(true);

        board[column, row] = tile;
        columnHeights[column]++;

        audioManager?.PlayDrop();

        if (reducedAnimationMode)
        {
            tile.transform.position = end;
        }
        else
        {
            yield return AnimateMove(tile.transform, start, end, dropDuration);
            yield return tile.PlayLandingBounce();
        }

        tile.EnableTrail(false);

        ParticleManager.Instance?.SpawnDropImpact(end);
        CameraController.Instance?.KickForDrop();

        TilePlaced?.Invoke(column, row, tile);

        yield return ResolveBoardRoutine();
    }

    private IEnumerator BombColumnRoutine(int column, int row)
    {
        isBusy = true;
        DestroyPreview();

        LetterTile tile = board[column, row];
        if (tile == null)
        {
            isBusy = false;
            yield break;
        }

        audioManager?.PlayPowerUp(PowerUpType.Bomb);
        ParticleManager.Instance?.SpawnBombBurst(tile.transform.position);
        tile.PlayClearEffect();

        yield return new WaitForSeconds(clearPause * 0.5f);

        board[column, row] = null;
        Destroy(tile.gameObject);
        RecalculateColumnHeights();

        yield return CollapseColumnsRoutine();
        yield return ResolveBoardRoutine();
    }

    private IEnumerator ResolveBoardRoutine()
    {
        bool scoredThisTurn = false;
        int cascadeDepth = 1;

        while (true)
        {
            List<WordResult> words = wordChecker != null ? wordChecker.FindAllWords(board, columns, rows) : new List<WordResult>();
            if (words.Count == 0)
            {
                break;
            }

            scoredThisTurn = true;
            HashSet<Vector2Int> uniqueCells = CollectMatchedCells(words);
            Vector3 centerPoint = CalculateCenterPoint(uniqueCells);

            foreach (Vector2Int position in uniqueCells)
            {
                LetterTile tile = board[position.x, position.y];
                if (tile != null)
                {
                    tile.PlayMatchPulse();
                }
            }

            WordsCleared?.Invoke(words, cascadeDepth, centerPoint);
            yield return new WaitForSeconds(clearPause);

            foreach (Vector2Int position in uniqueCells)
            {
                LetterTile tile = board[position.x, position.y];
                if (tile == null)
                {
                    continue;
                }

                ParticleManager.Instance?.SpawnWordBurst(tile.transform.position);
                tile.PlayClearEffect();
                board[position.x, position.y] = null;
                Destroy(tile.gameObject);
            }

            RecalculateColumnHeights();

            yield return new WaitForSeconds(settlePause);
            yield return CollapseColumnsRoutine();
            cascadeDepth++;
        }

        isBusy = false;
        TurnFinished?.Invoke(scoredThisTurn);

        if (IsBoardFullNow())
        {
            BoardFull?.Invoke();
        }
    }

    private IEnumerator CollapseColumnsRoutine()
    {
        bool movedAnyTile = false;

        for (int column = 0; column < columns; column++)
        {
            int writeRow = 0;
            for (int row = 0; row < rows; row++)
            {
                LetterTile tile = board[column, row];
                if (tile == null)
                {
                    continue;
                }

                if (row != writeRow)
                {
                    board[column, writeRow] = tile;
                    board[column, row] = null;

                    Vector3 target = GetCellWorldPosition(column, writeRow);
                    if (reducedAnimationMode)
                    {
                        tile.transform.position = target;
                    }
                    else
                    {
                        StartCoroutine(AnimateMove(tile.transform, tile.transform.position, target, collapseDuration));
                    }

                    movedAnyTile = true;
                }

                writeRow++;
            }

            columnHeights[column] = writeRow;
        }

        if (movedAnyTile)
        {
            audioManager?.PlayCascade();
            yield return new WaitForSeconds(reducedAnimationMode ? 0.02f : collapseDuration + settlePause);
        }
    }

    private IEnumerator AnimateMove(Transform targetTransform, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            targetTransform.position = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        targetTransform.position = to;
    }

    private void UpdatePreview(int column)
    {
        if (powerUpManager != null && powerUpManager.HasQueuedBomb)
        {
            DestroyPreview();
            hoveredColumn = column;
            return;
        }

        hoveredColumn = column;

        if (columnHeights[column] >= rows)
        {
            DestroyPreview();
            return;
        }

        if (previewTile == null)
        {
            GameObject previewObject = Instantiate(letterTilePrefab, Vector3.zero, Quaternion.identity, boardOrigin);
            previewTile = previewObject.GetComponent<LetterTile>();
        }

        int row = columnHeights[column];
        Vector3 previewPosition = GetCellWorldPosition(column, row) + Vector3.up * previewHeightOffset;
        previewTile.transform.position = previewPosition;
        previewTile.SetLetter(GameManager.Instance != null ? GameManager.Instance.GetCurrentPlayerLetter() : 'A');
        previewTile.SetOwner(GameManager.Instance != null ? GameManager.Instance.CurrentPlayerIndex : 0, GetPlayerMaterial(GameManager.Instance != null ? GameManager.Instance.CurrentPlayerIndex : 0));
        previewTile.SetWildcard(powerUpManager != null && powerUpManager.HasQueuedWildcard);
        previewTile.SetPreviewMode(0.35f);
    }

    private void DestroyPreview()
    {
        hoveredColumn = -1;
        if (previewTile != null)
        {
            Destroy(previewTile.gameObject);
            previewTile = null;
        }
    }

    private int WorldToColumn(float worldX)
    {
        Vector3 origin = boardOrigin != null ? boardOrigin.position : Vector3.zero;
        float left = origin.x - ((columns - 1) * cellSize * 0.5f) - cellSize * 0.5f;
        float normalized = (worldX - left) / cellSize;
        return Mathf.Clamp(Mathf.FloorToInt(normalized), 0, columns - 1);
    }

    private Material GetPlayerMaterial(int playerIndex)
    {
        if (playerMaterials == null || playerMaterials.Length == 0)
        {
            return null;
        }

        return playerMaterials[Mathf.Clamp(playerIndex, 0, playerMaterials.Length - 1)];
    }

    private void RecalculateColumnHeights()
    {
        for (int c = 0; c < columns; c++)
        {
            int height = 0;
            for (int r = 0; r < rows; r++)
            {
                if (board[c, r] != null)
                {
                    height = r + 1;
                }
            }

            columnHeights[c] = height;
        }
    }

    private HashSet<Vector2Int> CollectMatchedCells(List<WordResult> words)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        for (int i = 0; i < words.Count; i++)
        {
            for (int p = 0; p < words[i].positions.Count; p++)
            {
                cells.Add(words[i].positions[p]);
            }
        }

        return cells;
    }

    private Vector3 CalculateCenterPoint(HashSet<Vector2Int> positions)
    {
        if (positions.Count == 0)
        {
            return boardOrigin != null ? boardOrigin.position : Vector3.zero;
        }

        Vector3 total = Vector3.zero;
        foreach (Vector2Int position in positions)
        {
            total += GetCellWorldPosition(position.x, position.y);
        }

        return total / positions.Count;
    }

    private bool IsCellInBounds(int column, int row)
    {
        return column >= 0 && column < columns && row >= 0 && row < rows;
    }
}
