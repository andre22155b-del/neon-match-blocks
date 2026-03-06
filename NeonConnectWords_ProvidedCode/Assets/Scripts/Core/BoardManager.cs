using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoardManager: Manages the 7x6 grid, letter dropping, gravity simulation,
/// word detection triggers, and board state. Central hub for game logic.
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

    [Header("Visual Settings")]
    public Material[] playerMaterials;   // [0] = Player1 neon, [1] = Player2 neon
    public float dropPreviewAlpha = 0.35f;
    public LayerMask boardLayerMask;

    // Board state: null = empty cell
    private LetterTile[,] board;
    private int[] columnHeights;          // tracks how many tiles are in each column
    private bool isAnimating = false;
    private bool gameActive = false;

    // Drop preview ghost tile
    private GameObject previewTile;
    private int lastPreviewColumn = -1;

    // Events
    public System.Action<int, int, char, int> OnTilePlaced;   // col, row, letter, playerIndex
    public System.Action<List<WordResult>> OnWordsFound;
    public System.Action OnBoardFull;

    // -----------------------------------------------------------------------
    // Unity Lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InitBoard();
    }

    private void Update()
    {
        if (!gameActive || isAnimating) return;
        HandleMouseInput();
    }

    // -----------------------------------------------------------------------
    // Initialisation
    // -----------------------------------------------------------------------
    public void InitBoard()
    {
        board = new LetterTile[columns, rows];
        columnHeights = new int[columns];
        ClearBoardVisuals();
    }

    public void StartGame()
    {
        InitBoard();
        gameActive = true;
    }

    public void StopGame()
    {
        gameActive = false;
        DestroyPreview();
    }

    private void ClearBoardVisuals()
    {
        // Destroy any existing tile GameObjects
        foreach (Transform child in boardOrigin)
            Destroy(child.gameObject);
    }

    // -----------------------------------------------------------------------
    // Input Handling
    // -----------------------------------------------------------------------
    private void HandleMouseInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, boardLayerMask))
        {
            int col = WorldToColumn(hit.point.x);
            if (col >= 0 && col < columns)
            {
                UpdatePreview(col);
                if (Input.GetMouseButtonDown(0))
                    TryDropLetter(col);
            }
        }
        else
        {
            DestroyPreview();
        }
    }

    private int WorldToColumn(float worldX)
    {
        float boardLeft = boardOrigin.position.x - (columns * cellSize * 0.5f);
        int col = Mathf.FloorToInt((worldX - boardLeft) / cellSize);
        return Mathf.Clamp(col, 0, columns - 1);
    }

    private Vector3 GetCellWorldPos(int col, int row)
    {
        float x = boardOrigin.position.x - (columns * cellSize * 0.5f) + col * cellSize + cellSize * 0.5f;
        float y = boardOrigin.position.y + row * cellSize;
        float z = boardOrigin.position.z;
        return new Vector3(x, y, z);
    }

    // -----------------------------------------------------------------------
    // Preview Ghost Tile
    // -----------------------------------------------------------------------
    private void UpdatePreview(int col)
    {
        if (col == lastPreviewColumn) return;
        lastPreviewColumn = col;
        DestroyPreview();

        if (columnHeights[col] >= rows) return;  // column full

        char currentLetter = GameManager.Instance.GetCurrentPlayerLetter();
        int targetRow = columnHeights[col];
        Vector3 pos = GetCellWorldPos(col, targetRow);

        previewTile = Instantiate(letterPrefab, pos + Vector3.up * (rows * cellSize), Quaternion.identity, boardOrigin);
        LetterTile tile = previewTile.GetComponent<LetterTile>();
        tile.SetLetter(currentLetter);
        tile.SetPreviewMode(dropPreviewAlpha, GameManager.Instance.CurrentPlayerIndex);
    }

    private void DestroyPreview()
    {
        if (previewTile != null) Destroy(previewTile);
        previewTile = null;
        lastPreviewColumn = -1;
    }

    // -----------------------------------------------------------------------
    // Letter Dropping
    // -----------------------------------------------------------------------
    public bool TryDropLetter(int col)
    {
        if (isAnimating || !gameActive) return false;
        if (columnHeights[col] >= rows)
        {
            uiManager.ShowMessage("Column full!");
            return false;
        }

        char letter = GameManager.Instance.GetCurrentPlayerLetter();
        int playerIndex = GameManager.Instance.CurrentPlayerIndex;
        DropLetter(col, letter, playerIndex);
        return true;
    }

    public void DropLetter(int col, char letter, int playerIndex)
    {
        DestroyPreview();
        int targetRow = columnHeights[col];
        Vector3 startPos = GetCellWorldPos(col, rows + 1);   // spawn above board
        Vector3 endPos = GetCellWorldPos(col, targetRow);

        GameObject tileObj = Instantiate(letterPrefab, startPos, Quaternion.identity, boardOrigin);
        LetterTile tile = tileObj.GetComponent<LetterTile>();
        tile.SetLetter(letter);
        tile.SetOwner(playerIndex, playerMaterials[playerIndex]);

        board[col, targetRow] = tile;
        columnHeights[col]++;

        audioManager.PlayDropStart();
        StartCoroutine(AnimateDrop(tile, startPos, endPos, col, targetRow, playerIndex));
    }

    private IEnumerator AnimateDrop(LetterTile tile, Vector3 from, Vector3 to, int col, int row, int playerIndex)
    {
        isAnimating = true;

        // Spawn trail
        tile.EnableTrail(true);

        float elapsed = 0f;
        float duration = Vector3.Distance(from, to) / dropSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            tile.transform.position = Vector3.Lerp(from, to, EaseInQuad(t));
            yield return null;
        }

        tile.transform.position = to;
        tile.EnableTrail(false);

        audioManager.PlayLetterLand(row / (float)rows);
        yield return StartCoroutine(AnimateBounce(tile, to));

        OnTilePlaced?.Invoke(col, row, tile.Letter, playerIndex);
        yield return StartCoroutine(CheckAndProcessWords(col, row));

        isAnimating = false;

        if (IsBoardFull()) OnBoardFull?.Invoke();
        else GameManager.Instance.EndTurn();
    }

    private IEnumerator AnimateBounce(LetterTile tile, Vector3 basePos)
    {
        float elapsed = 0f;
        float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float bounce = Mathf.Sin(t * Mathf.PI) * bounceForce * (1f - t);
            tile.transform.position = basePos + Vector3.up * bounce;
            yield return null;
        }
        tile.transform.position = basePos;
    }

    // -----------------------------------------------------------------------
    // Word Processing
    // -----------------------------------------------------------------------
    private IEnumerator CheckAndProcessWords(int col, int row)
    {
        List<WordResult> results = wordChecker.FindAllWords(board, columns, rows);

        if (results.Count == 0) yield break;

        OnWordsFound?.Invoke(results);

        // Highlight each word found
        foreach (WordResult wr in results)
        {
            HighlightWord(wr);
            audioManager.PlayWordComplete(wr.word.Length, results.Count);
            particleManager.SpawnWordParticles(GetCellWorldPos(wr.startCol, wr.startRow));
        }

        // Epic connection: multiple words from single drop
        if (results.Count > 1)
        {
            CameraController.Instance.PlayComboShot();
            audioManager.PlayEpicCombo(results.Count);
            particleManager.SpawnEpicParticles(GetCellWorldPos(col, row));
            yield return new WaitForSeconds(0.6f);
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        // Remove completed word tiles (optional cascade)
        yield return StartCoroutine(RemoveWordTiles(results));
    }

    private void HighlightWord(WordResult wr)
    {
        foreach (Vector2Int pos in wr.positions)
        {
            if (board[pos.x, pos.y] != null)
                board[pos.x, pos.y].PlayWordHighlight();
        }
    }

    private IEnumerator RemoveWordTiles(List<WordResult> results)
    {
        HashSet<Vector2Int> toRemove = new HashSet<Vector2Int>();
        foreach (WordResult wr in results)
            foreach (Vector2Int pos in wr.positions)
                toRemove.Add(pos);

        foreach (Vector2Int pos in toRemove)
        {
            if (board[pos.x, pos.y] != null)
            {
                board[pos.x, pos.y].PlayRemoveAnimation();
            }
        }

        yield return new WaitForSeconds(0.3f);

        foreach (Vector2Int pos in toRemove)
        {
            if (board[pos.x, pos.y] != null)
            {
                Destroy(board[pos.x, pos.y].gameObject);
                board[pos.x, pos.y] = null;
            }
        }

        // Apply gravity to cascade tiles downward
        yield return StartCoroutine(ApplyGravityCascade());
    }

    // -----------------------------------------------------------------------
    // Gravity Cascade
    // -----------------------------------------------------------------------
    private IEnumerator ApplyGravityCascade()
    {
        bool moved = true;
        while (moved)
        {
            moved = false;
            for (int c = 0; c < columns; c++)
            {
                for (int r = 1; r < rows; r++)
                {
                    if (board[c, r] != null && board[c, r - 1] == null)
                    {
                        // Slide tile down
                        board[c, r - 1] = board[c, r];
                        board[c, r] = null;
                        Vector3 targetPos = GetCellWorldPos(c, r - 1);
                        StartCoroutine(SlideTile(board[c, r - 1], targetPos));
                        moved = true;
                    }
                }
            }
            if (moved) yield return new WaitForSeconds(0.12f);
        }

        // Recalculate column heights
        RecalcColumnHeights();

        // Check for cascade word matches
        List<WordResult> cascade = wordChecker.FindAllWords(board, columns, rows);
        if (cascade.Count > 0)
        {
            audioManager.PlayCascade();
            yield return StartCoroutine(CheckAndProcessWords(-1, -1));
        }
    }

    private IEnumerator SlideTile(LetterTile tile, Vector3 target)
    {
        float elapsed = 0f;
        float dur = 0.15f;
        Vector3 start = tile.transform.position;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            tile.transform.position = Vector3.Lerp(start, target, elapsed / dur);
            yield return null;
        }
        tile.transform.position = target;
    }

    private void RecalcColumnHeights()
    {
        for (int c = 0; c < columns; c++)
        {
            columnHeights[c] = 0;
            for (int r = 0; r < rows; r++)
                if (board[c, r] != null) columnHeights[c] = r + 1;
        }
    }

    // -----------------------------------------------------------------------
    // Board State Queries
    // -----------------------------------------------------------------------
    public LetterTile GetTile(int col, int row) => board[col, row];
    public bool IsBoardFull()
    {
        for (int c = 0; c < columns; c++)
            if (columnHeights[c] < rows) return false;
        return true;
    }

    public bool IsColumnFull(int col) => columnHeights[col] >= rows;

    // -----------------------------------------------------------------------
    // Power-Up Board Operations
    // -----------------------------------------------------------------------
    public void ClearRow(int row)
    {
        StartCoroutine(ClearRowRoutine(row));
    }

    private IEnumerator ClearRowRoutine(int row)
    {
        for (int c = 0; c < columns; c++)
        {
            if (board[c, row] != null)
            {
                board[c, row].PlayRemoveAnimation();
                particleManager.SpawnSmallParticle(GetCellWorldPos(c, row));
            }
        }
        yield return new WaitForSeconds(0.25f);
        for (int c = 0; c < columns; c++)
        {
            if (board[c, row] != null)
            {
                Destroy(board[c, row].gameObject);
                board[c, row] = null;
            }
        }
        yield return StartCoroutine(ApplyGravityCascade());
    }

    public void ClearColumn(int col)
    {
        StartCoroutine(ClearColumnRoutine(col));
    }

    private IEnumerator ClearColumnRoutine(int col)
    {
        for (int r = 0; r < rows; r++)
        {
            if (board[col, r] != null)
            {
                board[col, r].PlayRemoveAnimation();
                particleManager.SpawnSmallParticle(GetCellWorldPos(col, r));
            }
        }
        yield return new WaitForSeconds(0.25f);
        for (int r = 0; r < rows; r++)
        {
            if (board[col, r] != null)
            {
                Destroy(board[col, r].gameObject);
                board[col, r] = null;
            }
        }
        columnHeights[col] = 0;
    }

    public bool SwapTiles(Vector2Int posA, Vector2Int posB)
    {
        LetterTile tA = board[posA.x, posA.y];
        LetterTile tB = board[posB.x, posB.y];
        if (tA == null || tB == null) return false;

        board[posA.x, posA.y] = tB;
        board[posB.x, posB.y] = tA;

        Vector3 worldA = GetCellWorldPos(posA.x, posA.y);
        Vector3 worldB = GetCellWorldPos(posB.x, posB.y);

        StartCoroutine(SlideTile(tA, worldB));
        StartCoroutine(SlideTile(tB, worldA));

        audioManager.PlaySwap();
        return true;
    }

    // -----------------------------------------------------------------------
    // Easing
    // -----------------------------------------------------------------------
    private float EaseInQuad(float t) => t * t;
    private float EaseOutBounce(float t)
    {
        if (t < 1 / 2.75f) return 7.5625f * t * t;
        else if (t < 2 / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + 0.75f; }
        else if (t < 2.5 / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + 0.9375f; }
        else { t -= 2.625f / 2.75f; return 7.5625f * t * t + 0.984375f; }
    }
}
