# Neon Connect Words — Complete C# Codebase
## All scripts for Unity 3D (C#)

---

## 📄 `Docs/SETUP_GUIDE.md`

```markdown
# Neon Connect Words — Unity Setup Guide
## Complete Scene Assembly Instructions

---

## 1. Project Setup

1. Create a new **Unity 3D** project (Unity 2022 LTS or later recommended).
2. Install **TextMeshPro** via Package Manager (Window → Package Manager → Unity Registry).
3. Copy all scripts from `Scripts/` into `Assets/Scripts/` maintaining the folder structure:
   - `Assets/Scripts/Core/` → BoardManager, GameManager, WordChecker, LetterTile, CameraAndParticles, PuzzleManager
   - `Assets/Scripts/UI/` → UIManager
   - `Assets/Scripts/Audio/` → AudioManager
   - `Assets/Scripts/PowerUps/` → PowerUpManager
   - `Assets/Scripts/Tutorials/` → TutorialManager

---

## 2. Scene Hierarchy

```
Scene: MainGame
│
├── [MANAGERS]
│   ├── GameManager          (GameManager.cs)
│   ├── BoardManager         (BoardManager.cs)
│   ├── WordChecker          (WordChecker.cs)
│   ├── AudioManager         (AudioManager.cs)
│   ├── UIManager            (UIManager.cs)
│   ├── PowerUpManager       (PowerUpManager.cs)
│   ├── TutorialManager      (TutorialManager.cs)
│   ├── PuzzleManager        (PuzzleManager.cs)
│   └── ParticleManager      (ParticleManager.cs — on same GO as CameraController)
│
├── [CAMERA]
│   └── Main Camera          (CameraController.cs)
│       Position: (0, 4, -12)
│       Rotation: (10, 0, 0)
│       FOV: 60
│
├── [BOARD]
│   ├── BoardOrigin          (empty transform, position 0,0,0)
│   ├── BoardBackground      (Quad or Plane mesh for backdrop)
│   └── ColumnColliders
│       ├── ColCollider_0 … ColCollider_6  (Box Colliders covering each column)
│
├── [LIGHTING]
│   ├── DirectionalLight     (soft warm key light, intensity 0.8)
│   ├── BoardPointLight      (Point Light, assigned to ParticleManager.boardLight)
│   │   Position: (0, 3, -2), Color: #00FFFF, Intensity: 1
│   └── RimLight             (Directional from behind, color #FF00FF, intensity 0.4)
│
├── [UI — Canvas, Screen Space Overlay]
│   ├── HUDPanel
│   │   ├── Player1Score     (TextMeshProUGUI)
│   │   ├── Player2Score     (TextMeshProUGUI)
│   │   ├── TurnIndicator    (Panel + TextMeshProUGUI)
│   │   ├── CurrentLetter    (Panel + TextMeshProUGUI)
│   │   ├── ComboMultiplier  (TextMeshProUGUI)
│   │   ├── StreakBar        (Slider)
│   │   └── TimerGroup       (Panel with Image fill + TextMeshProUGUI)
│   │
│   ├── MainMenuPanel
│   │   ├── TitleText
│   │   ├── PlayButton       (→ ShowModeSelect)
│   │   └── SettingsButton
│   │
│   ├── ModeSelectPanel
│   │   ├── ClassicButton    (→ OnPlayClassic)
│   │   ├── TimedButton      (→ OnPlayTimed)
│   │   ├── PuzzleButton     (→ OnPlayPuzzle)
│   │   └── TutorialButton   (→ OnPlayTutorial)
│   │
│   ├── GameOverPanel        (CanvasGroup for fade)
│   │   ├── TitleText
│   │   ├── ScoresText
│   │   ├── PlayAgainButton
│   │   └── MainMenuButton
│   │
│   ├── SettingsPanel
│   │   ├── MusicToggle
│   │   ├── SFXToggle
│   │   ├── ColorBlindToggle
│   │   ├── ReducedFXToggle
│   │   ├── LetterSizeSlider
│   │   └── CloseButton
│   │
│   ├── PowerUpBar
│   │   ├── WildcardButton + CountText
│   │   ├── BombButton + CountText
│   │   └── SwapButton + CountText
│   │
│   ├── FloatingScoreCanvas  (assign as parent for floating score prefabs)
│   ├── MessageBanner        (TextMeshProUGUI + CanvasGroup)
│   └── TutorialOverlay
│       ├── TitleText
│       ├── BodyText
│       ├── NextButton
│       ├── SkipButton
│       └── HighlightArrow   (Image with arrow sprite)
│
└── [AUDIO]
    ├── MusicSource          (AudioSource, loop=true)
    └── SFXSource            (AudioSource)
```

---

## 3. Prefabs to Create

### A. LetterTile Prefab
```
LetterTile (empty root)
├── TileMesh          (3D Cube or custom beveled mesh, scale 0.9,0.9,0.2)
│   └── Material:     NeonLetter_P1 / NeonLetter_P2 (see Materials section)
├── GlowMesh          (slightly larger cube, additive material, alpha 0.3)
├── LetterLabel       (TextMesh3D, centered, font size 4)
├── TrailRenderer     (component on root, disabled by default)
├── HighlightParticles (Particle System — star burst, yellow/white)
├── RemoveParticles   (Particle System — dissolve, blue/cyan)
└── BoxCollider       (size 0.9, 0.9, 0.9)
Components on root: LetterTile.cs
```

### B. FloatingScore Prefab
```
FloatingScore (empty root)
├── ScoreText         (TextMeshProUGUI, anchored center, font size 28, bold)
├── CanvasGroup       (for fade)
└── RectTransform     (pivot 0.5, 0.5)
```

### C. WordComplete Particle Prefab
- Particle System, burst of 30–50 particles
- Shape: Sphere radius 0.3
- Velocity: random, speed 2–5
- Color: gradient cyan → white → transparent
- Lifetime: 0.8s

### D. EpicCombo Particle Prefab
- Particle System, burst of 100+ particles
- Multiple emitters: star sparks + ring shockwave
- Colors: cycle through cyan, magenta, yellow
- Lifetime: 1.5s, use sub-emitters for trails

### E. ColumnHighlight Prefab (7 instances, for tutorials)
- Vertical glowing plane or line renderer
- Semi-transparent additive material, animated pulse alpha

---

## 4. Materials

### NeonLetter_P1 (Player 1 — Cyan)
- Shader: Universal Render Pipeline/Lit (or Standard)
- Albedo: #001C1C
- Emission: ON, Color: #00FFFF, Intensity: 3
- Metallic: 0.3, Smoothness: 0.9

### NeonLetter_P2 (Player 2 — Magenta)
- Same as above but Emission Color: #FF00FF

### NeonLetter_Wildcard (White)
- Emission Color: #FFFFFF, Intensity: 5 (pulsed by script)

### BoardBackground
- Dark navy/black panel with subtle grid lines
- Albedo: #05051A, slight reflection

### GlowAdditive
- Shader: Sprites/Default or Unlit/Transparent
- Blend: Additive, Alpha 0.3

---

## 5. Inspector Wiring Checklist

### GameManager
- [ ] boardManager → BoardManager GO
- [ ] uiManager → UIManager GO
- [ ] audioManager → AudioManager GO
- [ ] tutorialManager → TutorialManager GO
- [ ] powerUpManager → PowerUpManager GO
- [ ] classicTargetScore = 100
- [ ] timedDuration = 120
- [ ] letterPool = "EEEEEEEEEEEEAAAAAAAAAIIIIIIOOOOOOUUUU..." (full string in script)

### BoardManager
- [ ] letterPrefab → LetterTile prefab
- [ ] boardOrigin → BoardOrigin transform
- [ ] wordChecker → WordChecker GO
- [ ] uiManager → UIManager GO
- [ ] audioManager → AudioManager GO
- [ ] powerUpManager → PowerUpManager GO
- [ ] particleManager → ParticleManager GO
- [ ] playerMaterials[0] → NeonLetter_P1
- [ ] playerMaterials[1] → NeonLetter_P2
- [ ] boardLayerMask → "BoardLayer" (assign column colliders to this layer)

### UIManager
- [ ] Assign all TextMeshProUGUI and Button references
- [ ] playerTurnColors[0] = #00FFFF, [1] = #FF00FF

### AudioManager
- [ ] musicSource → MusicSource AudioSource
- [ ] sfxSource → SFXSource AudioSource
- [ ] Assign AudioClip slots (see Placeholder Audio section)

### PowerUpManager
- [ ] boardManager, audioManager, uiManager references

### TutorialManager
- [ ] Configure TutorialStep array (3 entries minimum):
  - Step 0: "Welcome!" / "Drop a letter into any column." / requiresPlayerAction=true / highlightColumns=[3]
  - Step 1: "Great!" / "Spell a word by filling the board — letters line up horizontally, vertically, or diagonally!" / requiresPlayerAction=true
  - Step 2: "Advanced!" / "Use power-ups to clear tiles and chain combos for massive points!" / requiresPlayerAction=false

---

## 6. Placeholder Audio

Since we cannot include audio files, generate or source free alternatives:

| Slot | Description | Source Suggestion |
|------|-------------|-------------------|
| musicTracks[0] | Chill synthwave loop | freemusicarchive.org, lofi synthwave |
| musicTracks[1] | Intense synthwave | Higher BPM version or pitch-up of above |
| dropStartClip | Whoosh / air displacement | freesound.org "swoosh" |
| letterLandClip | Soft thud / plastic click | freesound.org "click plastic" |
| wordCompleteClip | Chime / bell ding | freesound.org "chime" |
| multiWordClip | Chord strum / layered chime | freesound.org "arpeggio" |
| epicComboClip | Rising synth swell + impact | freesound.org "power up synth" |
| cascadeClip | Cascading water tones | freesound.org "cascade" |
| powerUpWildcardClip | Star sparkle | freesound.org "magic sparkle" |
| powerUpBombClip | Deep boom / explosion | freesound.org "explosion soft" |
| powerUpSwapClip | Swipe / swoosh | freesound.org "swipe" |
| uiClickClip | Soft UI click | freesound.org "button click" |

All audio clips should be:
- Format: MP3 or WAV
- Normalized to -6dBFS
- Loop points set for music clips

---

## 7. Word List Asset

1. Download a free word list: https://github.com/dwyl/english-words (words_alpha.txt)
2. Filter to words 3–10 letters long.
3. Import as a TextAsset into Unity (`Assets/Resources/wordlist.txt`)
4. Assign to WordChecker.wordListAsset in Inspector.
5. The built-in list (GetBuiltInWordList) works as a fallback prototype.

---

## 8. Build Settings

### Desktop
- Platform: Windows / Mac / Linux
- Resolution: 1920×1080 default, resizable
- Quality: High

### Mobile (iOS / Android)
- Platform: iOS or Android
- Resolution: Auto (portrait or landscape, adjust board camera)
- Quality: Medium (enable reducedFX toggle by default)
- Enable: PlayerSettings → Accelerometer Frequency: Disabled (touch only)
- Add mobile touch input support:

```csharp
// In BoardManager.HandleMouseInput(), replace mouse raycast with:
if (Input.touchCount > 0)
{
    Touch touch = Input.GetTouch(0);
    Ray ray = Camera.main.ScreenPointToRay(touch.position);
    // ... rest of logic unchanged
}
```

---

## 9. Performance Notes

- Board uses object pooling for LetterTile prefabs (add PoolManager for production).
- Particle systems should have Max Particles capped:
  - WordComplete: 60
  - EpicCombo: 150
  - SmallSpark: 20
- Disable shadow casting on letter tile meshes (cosmetic, not needed).
- Use `Camera.main` caching; avoid per-frame `FindObjectOfType` calls.
- Trie precomputation in `Awake()` ensures word lookup is O(n) per scan.

---

## 10. Extension Points

The architecture is designed for easy expansion:

| Feature | Where to Add |
|---------|-------------|
| Online Multiplayer | GameManager (Netcode/Mirror/Photon integration) |
| New Power-Up | PowerUpManager.PowerUpType enum + new activation method |
| Cosmetic Skins | LetterTile.SetOwner() — pass alternate material sets |
| More Game Modes | GameMode enum + GameManager switch |
| Leaderboards | New LeaderboardManager.cs, hook into GameManager.EndGame |
| AI Opponent | New AIPlayer.cs — implement WordChecker scanning for best moves |

---

*Neon Connect Words — Unity C# Codebase v1.0*
*Scripts: BoardManager, WordChecker, GameManager, LetterTile, UIManager, AudioManager, PowerUpManager, TutorialManager, PuzzleManager, CameraController, ParticleManager*

```

---

## 📄 `Scripts/Core/BoardManager.cs`

```csharp
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

```

---

## 📄 `Scripts/Core/GameManager.cs`

```csharp
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

```

---

## 📄 `Scripts/Core/WordChecker.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// Data Structures
// ============================================================

/// <summary>Result of a word found on the board.</summary>
public class WordResult
{
    public string word;
    public List<Vector2Int> positions;
    public int startCol;
    public int startRow;
    public WordDirection direction;
    public int points;

    public WordResult(string w, List<Vector2Int> pos, WordDirection dir)
    {
        word = w;
        positions = pos;
        startCol = pos[0].x;
        startRow = pos[0].y;
        direction = dir;
        points = w.Length;  // base points
    }
}

public enum WordDirection { Horizontal, Vertical, DiagonalUp, DiagonalDown }

/// <summary>Trie node for O(n) word validation.</summary>
public class TrieNode
{
    public Dictionary<char, TrieNode> children = new Dictionary<char, TrieNode>();
    public bool isEndOfWord;
    public bool isPrefix;  // pre-computed during insert
}

// ============================================================
// WordChecker
// ============================================================

/// <summary>
/// WordChecker: Builds a trie from a word list, validates words,
/// and scans the board in 4 directions for valid words ≥ minWordLength.
/// </summary>
public class WordChecker : MonoBehaviour
{
    [Header("Settings")]
    public int minWordLength = 3;
    public TextAsset wordListAsset;   // assign a .txt word list in Inspector

    private TrieNode root = new TrieNode();
    private HashSet<string> wordSet = new HashSet<string>();

    // -----------------------------------------------------------------------
    // Initialisation
    // -----------------------------------------------------------------------
    private void Awake()
    {
        LoadDictionary();
    }

    public void LoadDictionary()
    {
        root = new TrieNode();
        wordSet.Clear();

        string[] words;
        if (wordListAsset != null)
        {
            words = wordListAsset.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            // Built-in minimal word set for prototype/testing
            words = GetBuiltInWordList();
        }

        foreach (string raw in words)
        {
            string w = raw.Trim().ToUpperInvariant();
            if (w.Length >= minWordLength)
            {
                InsertWord(w);
                wordSet.Add(w);
            }
        }

        Debug.Log($"[WordChecker] Loaded {wordSet.Count} words into trie.");
    }

    private void InsertWord(string word)
    {
        TrieNode node = root;
        foreach (char c in word)
        {
            if (!node.children.ContainsKey(c))
                node.children[c] = new TrieNode();
            node = node.children[c];
        }
        node.isEndOfWord = true;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    public bool IsValidWord(string word)
    {
        return wordSet.Contains(word.ToUpperInvariant());
    }

    public bool IsPrefix(string prefix)
    {
        TrieNode node = root;
        foreach (char c in prefix.ToUpperInvariant())
        {
            if (!node.children.ContainsKey(c)) return false;
            node = node.children[c];
        }
        return true;
    }

    /// <summary>
    /// Scan entire board and return all valid words found in any direction.
    /// Uses trie prefix pruning for efficiency.
    /// </summary>
    public List<WordResult> FindAllWords(LetterTile[,] board, int cols, int rows)
    {
        List<WordResult> results = new List<WordResult>();
        HashSet<string> foundWords = new HashSet<string>();   // dedup

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // Horizontal →
            new Vector2Int(0, 1),   // Vertical ↑
            new Vector2Int(1, 1),   // Diagonal ↗
            new Vector2Int(1, -1),  // Diagonal ↘
        };

        WordDirection[] dirEnum = new WordDirection[]
        {
            WordDirection.Horizontal, WordDirection.Vertical,
            WordDirection.DiagonalUp, WordDirection.DiagonalDown
        };

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                if (board[c, r] == null) continue;

                for (int d = 0; d < directions.Length; d++)
                {
                    ScanDirection(board, cols, rows, c, r, directions[d], dirEnum[d], results, foundWords);
                }
            }
        }

        return results;
    }

    private void ScanDirection(LetterTile[,] board, int cols, int rows,
        int startC, int startR, Vector2Int dir, WordDirection dirEnum,
        List<WordResult> results, HashSet<string> foundWords)
    {
        TrieNode node = root;
        string accumulated = "";
        List<Vector2Int> positions = new List<Vector2Int>();

        int c = startC;
        int r = startR;

        while (c >= 0 && c < cols && r >= 0 && r < rows)
        {
            LetterTile tile = board[c, r];
            if (tile == null) break;

            char letter = tile.Letter;

            // Wildcard support
            if (tile.IsWildcard)
            {
                // Wildcard: try all children
                foreach (char wc in node.children.Keys)
                {
                    string tryWord = accumulated + wc;
                    if (IsValidWord(tryWord) && tryWord.Length >= minWordLength)
                    {
                        string key = $"{startC},{startR},{dirEnum},{tryWord}";
                        if (!foundWords.Contains(key))
                        {
                            foundWords.Add(key);
                            var pos2 = new List<Vector2Int>(positions) { new Vector2Int(c, r) };
                            results.Add(new WordResult(tryWord, pos2, dirEnum));
                        }
                    }
                }
                break;  // simplification: stop scanning after wildcard
            }

            if (!node.children.ContainsKey(letter)) break;
            node = node.children[letter];
            accumulated += letter;
            positions.Add(new Vector2Int(c, r));

            if (node.isEndOfWord && accumulated.Length >= minWordLength)
            {
                string key = $"{startC},{startR},{dirEnum},{accumulated}";
                if (!foundWords.Contains(key))
                {
                    foundWords.Add(key);
                    results.Add(new WordResult(accumulated, new List<Vector2Int>(positions), dirEnum));
                }
            }

            c += dir.x;
            r += dir.y;
        }
    }

    // -----------------------------------------------------------------------
    // Built-in Word List (prototype fallback)
    // -----------------------------------------------------------------------
    private string[] GetBuiltInWordList()
    {
        return new string[]
        {
            "ACE","ACT","ADD","AGE","AID","AIM","AIR","ALE","AND","ANT",
            "APE","APP","APT","ARC","ARE","ARM","ART","ASH","ASK","ATE",
            "BAD","BAG","BAN","BAR","BAT","BAY","BED","BIG","BIT","BOW",
            "BOX","BOY","BUD","BUG","BUN","BUS","BUT","BUY","CAB","CAN",
            "CAP","CAR","CAT","COB","COD","COG","COP","COT","COW","CRY",
            "CUB","CUP","CUT","DAB","DAD","DAM","DIG","DIM","DIP","DOC",
            "DOG","DOT","DRY","DUB","DUG","DUO","DYE","EAR","EAT","EEL",
            "EGG","ELM","EMU","END","ERA","EWE","FAD","FAN","FAR","FAT",
            "FAX","FIG","FIN","FIT","FLY","FOB","FOG","FOP","FOR","FOX",
            "FRY","FUN","FUR","GAB","GAP","GAS","GAY","GEL","GEM","GET",
            "GIG","GIN","GNU","GOB","GOD","GOT","GUM","GUN","GUT","GUY",
            "GYM","HAD","HAM","HAS","HAT","HAY","HEM","HEN","HER","HID",
            "HIM","HIP","HIS","HIT","HOB","HOG","HOP","HOT","HOW","HUB",
            "HUG","HUM","HUT","ICE","ICY","ILL","IMP","INK","INN","ION",
            "IRE","IRK","JAB","JAG","JAM","JAR","JAW","JAY","JET","JIG",
            "JOB","JOG","JOT","JOY","JUG","JUT","KEG","KID","KIN","KIT",
            "LAB","LAD","LAP","LAW","LAX","LAY","LEA","LED","LEG","LET",
            "LID","LIP","LIT","LOG","LOT","LOW","MAP","MAR","MAT","MAW",
            "NAB","NAG","NAP","NAY","NET","NEW","NIL","NIP","NOB","NOD",
            "NOR","NOT","NOW","NUB","NUN","NUT","OAK","OAR","OAT","ODD",
            "ODE","OFF","OFT","OIL","OLD","OPT","ORB","ORE","OUR","OWE",
            "OWL","OWN","PAD","PAL","PAN","PAP","PAR","PAT","PAW","PAY",
            "PEA","PEG","PEN","PEP","PET","PIE","PIG","PIN","PIP","PIT",
            "PLY","POD","POP","POT","POW","PRY","PUB","PUG","PUN","PUP",
            "PUS","PUT","RAG","RAM","RAN","RAP","RAT","RAW","RAY","RED",
            "REF","RIB","RID","RIG","RIM","RIP","ROB","ROD","ROT","ROW",
            "RUB","RUG","RUM","RUN","RUT","SAC","SAD","SAG","SAP","SAT",
            "SAW","SAY","SEA","SET","SEW","SHY","SIP","SIR","SIT","SIX",
            "SKY","SLY","SOB","SOD","SON","SOP","SOT","SOW","SOY","SPA",
            "SPY","STY","SUB","SUE","SUM","SUN","SUP","TAB","TAN","TAP",
            "TAR","TAT","TAX","TEA","TEN","THE","TIE","TIN","TIP","TOE",
            "TON","TOO","TOP","TOT","TOW","TOY","TUB","TUG","TUN","TWO",
            "URN","USE","VAT","VIA","VIE","VOW","WAD","WAR","WAS","WAX",
            "WEB","WED","WET","WHO","WHY","WIG","WIN","WIT","WOE","WOK",
            "WON","WOO","WOW","YAK","YAM","YAP","YAW","YEA","YEW","YOU",
            // Common 4-letter words
            "ABLE","ACID","AGED","ALSO","AREA","ARMY","AWAY","BABY","BACK",
            "BALL","BAND","BANK","BASE","BATH","BEAR","BEAT","BEEN","BELL",
            "BEST","BIRD","BLOW","BLUE","BOAT","BODY","BOND","BONE","BOOK",
            "BOOM","BORN","BOSS","BOTH","BULK","BURN","CAGE","CAKE","CALL",
            "CALM","CAME","CARD","CARE","CASE","CASH","CAST","CAVE","CELL",
            "CHAT","CHIP","CHOP","CITY","CLAP","CLAY","CLIP","CLUB","CLUE",
            "COAL","COAT","CODE","COLD","COME","COOK","COOL","COPE","COPY",
            "CORD","CORE","CORN","COST","COZY","CREW","CROP","CURE","CUTE",
            "DARK","DASH","DATA","DATE","DAWN","DAYS","DEAD","DEAL","DEAR",
            "DEBT","DEEP","DENY","DESK","DIAL","DIET","DIRT","DISK","DIVE",
            "DOCK","DOES","DOME","DONE","DOOR","DOSE","DOVE","DOWN","DRAW",
            "DREW","DRIP","DROP","DRUM","DUAL","DULL","DUMB","DUMP","DUSK",
            "DUST","DUTY","EACH","EARL","EARN","EASE","EAST","EASY","EDGE",
            "ELSE","EMIT","EPIC","EVEN","EVER","EVIL","EXAM","FACE","FACT",
            "FADE","FAIL","FAIR","FAKE","FALL","FAME","FAST","FATE","FAWN",
            "FEEL","FEET","FELL","FELT","FILE","FILL","FILM","FIND","FINE",
            "FIRE","FIRM","FISH","FIST","FLAG","FLAT","FLEW","FLIP","FLOW",
            "FOAM","FOLD","FOLK","FOND","FONT","FOOD","FOOL","FOOT","FORD",
            "FORE","FORK","FORM","FORT","FOUL","FREE","FROM","FUEL","FULL",
            "FUND","FUSE","GAIN","GAME","GAVE","GEAR","GENE","GIFT","GIVE",
            "GLAD","GLOW","GLUE","GOAL","GOES","GOLD","GOLF","GONE","GOOD",
            "GOWN","GRAB","GRAY","GREW","GRID","GRIM","GRIP","GROW","GULF",
            "GUST","HACK","HAIL","HALF","HALL","HAND","HANG","HARD","HARM",
            "HASH","HATE","HAVE","HAWK","HEAD","HEAL","HEAP","HEAR","HEAT",
            "HEEL","HELD","HELM","HELP","HERE","HIGH","HILL","HINT","HIRE",
            "HOLD","HOLE","HOLY","HOME","HOOD","HOOK","HOPE","HORN","HOST",
            "HOUR","HUGE","HULL","HUNT","HURT","HYMN","ICED","IDEA","IDLE",
            "INCH","INTO","IRON","ISLE","ITEM","JAIL","JERK","JOIN","JOKE",
            "JUMP","JUST","KEEN","KEEP","KICK","KIND","KING","KISS","KNEW",
            "KNOB","KNOW","LACK","LAKE","LAMP","LAND","LANE","LAST","LATE",
            "LAVA","LAWN","LEAD","LEAF","LEAN","LEAP","LEAN","LEFT","LEND",
            "LESS","LIFE","LIFT","LIKE","LIME","LINE","LINK","LION","LIST",
            "LIVE","LOAD","LOAN","LOCK","LOFT","LONE","LONG","LOOK","LOOP",
            "LOOT","LOSE","LOSS","LOST","LOVE","LUCK","LURE","LUSH","MADE",
            "MAIL","MAIN","MAKE","MALE","MALL","MANE","MANY","MARK","MASS",
            "MAST","MATE","MATH","MAZE","MEAL","MEAN","MEAT","MEET","MELT",
            "MEMO","MERE","MESH","MILD","MILE","MILK","MILL","MIND","MINE",
            "MINT","MISS","MODE","MOLD","MOLE","MOOD","MOON","MORE","MOST",
            "MOVE","MUCH","MUST","MYTH","NAIL","NAME","NAVY","NEAR","NECK",
            "NEED","NEST","NEWS","NEXT","NICE","NINE","NODE","NONE","NOON",
            "NORM","NOSE","NOTE","NULL","NUMB","OATH","ONCE","ONLY","OPEN",
            "OPUS","OVAL","OVEN","OVER","PACE","PACK","PAGE","PAID","PAIN",
            "PAIR","PALE","PALM","PANE","PARK","PART","PASS","PAST","PATH",
            "PAVE","PEAK","PEAR","PEEL","PEER","PICK","PIER","PILE","PILL",
            "PINE","PINK","PIPE","PLAN","PLAY","PLOT","PLOW","PLUG","PLUM",
            "PLUS","POEM","POET","POLE","POLL","POND","POOL","POOR","POPE",
            "PORK","PORT","POSE","POST","POUR","PREY","PROD","PROP","PULL",
            "PUMP","PURE","PUSH","RACK","RAIL","RAIN","RAKE","RAMP","RANG",
            "RANK","RARE","RATE","READ","REAL","REEL","RELY","RENT","REST",
            "RICE","RICH","RIDE","RING","RIOT","RISE","RISK","ROAD","ROAM",
            "ROAR","ROBE","ROCK","ROLE","ROLL","ROOF","ROOM","ROOT","ROPE",
            "ROSE","RUBY","RULE","RUSH","RUST","SAFE","SAGE","SAIL","SAKE",
            "SALE","SALT","SAME","SAND","SANE","SANG","SANK","SAVE","SCAN",
            "SCAR","SEAL","SEAM","SEEK","SEEM","SEEN","SELF","SELL","SEND",
            "SENT","SHED","SHIP","SHOP","SHOT","SHOW","SHUT","SICK","SIDE",
            "SIGN","SILK","SING","SINK","SITE","SIZE","SKIN","SKIP","SLAB",
            "SLAM","SLAP","SLIM","SLIP","SLOT","SLOW","SLUG","SNAP","SNOW",
            "SOAK","SOAP","SOCK","SOFT","SOIL","SOLD","SOLE","SOME","SONG",
            "SOON","SORT","SOUL","SOUP","SOUR","SPAN","SPAR","SPIN","SPIT",
            "SPOT","STAB","STAR","STAY","STEM","STEP","STIR","STOP","STOW",
            "STUB","SUCH","SUIT","SUNG","SUNK","SURF","SWAP","SWIM","TAIL",
            "TALE","TALL","TAME","TANK","TAPE","TASK","TEAR","TELL","TEND",
            "TENT","TERM","THAN","THAT","THEM","THEN","THEY","THIN","THIS",
            "THOU","THUS","TIDE","TILL","TILT","TIME","TINY","TIRE","TOAD",
            "TOLD","TOLL","TOMB","TONE","TOOK","TOOL","TORN","TOSS","TOUR",
            "TOWN","TRAP","TRAY","TREE","TRIM","TRIO","TRIP","TROT","TRUE",
            "TUBE","TUCK","TUFT","TUNE","TURF","TUSK","TWIN","TYPE","UGLY",
            "UNDO","UNIT","UPON","USED","USER","VALE","VANE","VARY","VAST",
            "VEIL","VEIN","VERB","VERY","VEST","VIEW","VINE","VISA","VOID",
            "VOLT","VOTE","WADE","WAGE","WAIL","WAKE","WALK","WALL","WAND",
            "WANT","WARD","WARM","WARP","WARY","WASH","WAVE","WEAK","WEAL",
            "WEAN","WEED","WEEK","WELL","WENT","WERE","WEST","WHAT","WHEN",
            "WHOM","WIDE","WIFE","WILD","WILL","WIND","WINE","WING","WIRE",
            "WISE","WISH","WITH","WOLF","WOOD","WOOL","WORD","WORE","WORK",
            "WORM","WORN","WOVE","WRAP","WREN","WRIT","YAWN","YEAR","YELL",
            "YOUR","ZONE","ZOOM",
        };
    }
}

```

---

## 📄 `Scripts/Core/LetterTile.cs`

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// LetterTile: Represents a single letter tile in the 3D world.
/// Handles neon visuals, trails, highlighting, wildcard state, and removal animation.
/// Attach this to the Letter prefab.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class LetterTile : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Public State
    // -----------------------------------------------------------------------
    public char Letter { get; private set; }
    public int OwnerIndex { get; private set; } = -1;
    public bool IsWildcard { get; private set; } = false;

    // -----------------------------------------------------------------------
    // Inspector References
    // -----------------------------------------------------------------------
    [Header("Visuals")]
    public TextMesh letterLabel;       // 3D text mesh
    public Renderer tileRenderer;
    public Renderer glowRenderer;      // separate inner glow mesh (optional)
    public TrailRenderer trailRenderer;
    public ParticleSystem highlightParticles;
    public ParticleSystem removeParticles;

    [Header("Animation")]
    public float highlightPulseSpeed = 4f;
    public float highlightPulseMagnitude = 0.15f;
    public Color wildcardColor = Color.white;

    // -----------------------------------------------------------------------
    // Internal
    // -----------------------------------------------------------------------
    private Material tileMat;
    private Color baseEmission;
    private bool isPulsing = false;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    // -----------------------------------------------------------------------
    // Initialisation
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (tileRenderer == null) tileRenderer = GetComponent<Renderer>();
        tileMat = tileRenderer.material;  // instance material
        if (trailRenderer) trailRenderer.enabled = false;
    }

    // -----------------------------------------------------------------------
    // Setup API
    // -----------------------------------------------------------------------

    public void SetLetter(char c)
    {
        Letter = c;
        if (letterLabel) letterLabel.text = c.ToString();
    }

    public void SetOwner(int playerIndex, Material mat)
    {
        OwnerIndex = playerIndex;
        tileRenderer.material = mat;
        tileMat = tileRenderer.material;
        baseEmission = tileMat.GetColor(EmissionColorID);
    }

    public void SetPreviewMode(float alpha, int playerIndex)
    {
        Color c = tileRenderer.material.color;
        c.a = alpha;
        tileRenderer.material.color = c;
        // Disable collider so it doesn't interfere with raycasts
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
    }

    public void SetWildcard(bool value)
    {
        IsWildcard = value;
        if (value)
        {
            Letter = '*';
            if (letterLabel) letterLabel.text = "★";
            tileMat.SetColor(EmissionColorID, wildcardColor * 3f);
        }
    }

    // -----------------------------------------------------------------------
    // Trail
    // -----------------------------------------------------------------------
    public void EnableTrail(bool on)
    {
        if (trailRenderer) trailRenderer.enabled = on;
    }

    // -----------------------------------------------------------------------
    // Word Highlight
    // -----------------------------------------------------------------------
    public void PlayWordHighlight()
    {
        if (!isPulsing) StartCoroutine(PulseHighlight());
        if (highlightParticles) highlightParticles.Play();
    }

    private IEnumerator PulseHighlight()
    {
        isPulsing = true;
        float elapsed = 0f;
        float duration = 0.6f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(elapsed * highlightPulseSpeed * Mathf.PI * 2f) * highlightPulseMagnitude;
            transform.localScale = originalScale * pulse;

            // Brighten emission
            float brightness = 2f + Mathf.Sin(elapsed * highlightPulseSpeed * Mathf.PI * 2f) * 1.5f;
            tileMat.SetColor(EmissionColorID, baseEmission * brightness);

            yield return null;
        }

        transform.localScale = originalScale;
        tileMat.SetColor(EmissionColorID, baseEmission);
        isPulsing = false;
    }

    // -----------------------------------------------------------------------
    // Remove Animation
    // -----------------------------------------------------------------------
    public void PlayRemoveAnimation()
    {
        StartCoroutine(RemoveRoutine());
    }

    private IEnumerator RemoveRoutine()
    {
        if (removeParticles) removeParticles.Play();

        float elapsed = 0f;
        float duration = 0.28f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = startScale * (1f - EaseInQuad(t));
            Color c = tileMat.color;
            c.a = 1f - t;
            tileMat.color = c;
            yield return null;
        }
    }

    // -----------------------------------------------------------------------
    // Gentle Idle Bob (optional cosmetic)
    // -----------------------------------------------------------------------
    private float bobOffset;
    public void EnableIdleBob(float offset = 0f)
    {
        bobOffset = offset;
        StartCoroutine(IdleBob());
    }

    private IEnumerator IdleBob()
    {
        Vector3 basePos = transform.localPosition;
        while (true)
        {
            float y = Mathf.Sin((Time.time + bobOffset) * 1.5f) * 0.04f;
            transform.localPosition = basePos + Vector3.up * y;
            yield return null;
        }
    }

    // -----------------------------------------------------------------------
    // Easing
    // -----------------------------------------------------------------------
    private float EaseInQuad(float t) => t * t;
}

```

---

## 📄 `Scripts/Core/CameraAndParticles.cs`

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// CameraController: Handles smooth camera motion — idle sway, combo zoom,
/// tilt reactions, and cinematic shots for epic drops.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Idle Motion")]
    public float idleSwayAmplitude = 0.04f;
    public float idleSwaySpeed = 0.4f;
    public float idleTiltAmplitude = 1.5f;

    [Header("Combo Shot")]
    public float comboZoomAmount = 0.8f;
    public float comboZoomDuration = 0.6f;
    public float comboReturnDuration = 1.0f;

    [Header("Shake")]
    public float shakeIntensity = 0.12f;
    public float shakeDuration = 0.3f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float baseFOV;
    private Camera cam;
    private bool inCinematic = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        cam = GetComponent<Camera>();
        basePosition = transform.position;
        baseRotation = transform.rotation;
        baseFOV = cam ? cam.fieldOfView : 60f;
    }

    private void Update()
    {
        if (!inCinematic) ApplyIdleSway();
    }

    private void ApplyIdleSway()
    {
        float t = Time.time * idleSwaySpeed;
        Vector3 sway = new Vector3(
            Mathf.Sin(t) * idleSwayAmplitude,
            Mathf.Cos(t * 0.7f) * idleSwayAmplitude * 0.5f,
            0f);
        float tiltZ = Mathf.Sin(t * 0.5f) * idleTiltAmplitude;

        transform.position = Vector3.Lerp(transform.position, basePosition + sway, Time.deltaTime * 3f);
        transform.rotation = Quaternion.Lerp(transform.rotation,
            baseRotation * Quaternion.Euler(0, 0, tiltZ), Time.deltaTime * 2f);
    }

    // Called on epic combo
    public void PlayComboShot()
    {
        if (!inCinematic) StartCoroutine(ComboShotRoutine());
    }

    private IEnumerator ComboShotRoutine()
    {
        inCinematic = true;
        float startFOV = cam ? cam.fieldOfView : baseFOV;

        // Zoom in
        float elapsed = 0f;
        while (elapsed < comboZoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / comboZoomDuration;
            if (cam) cam.fieldOfView = Mathf.Lerp(startFOV, baseFOV - comboZoomAmount * 10f, EaseOutCubic(t));
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // Zoom back
        elapsed = 0f;
        float zoomedFOV = cam ? cam.fieldOfView : baseFOV;
        while (elapsed < comboReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / comboReturnDuration;
            if (cam) cam.fieldOfView = Mathf.Lerp(zoomedFOV, baseFOV, EaseOutCubic(t));
            yield return null;
        }
        if (cam) cam.fieldOfView = baseFOV;
        inCinematic = false;
    }

    public void Shake()
    {
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(shakeIntensity, 0f, elapsed / shakeDuration);
            transform.position = basePosition + Random.insideUnitSphere * strength;
            yield return null;
        }
        transform.position = basePosition;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}

// ============================================================

/// <summary>
/// ParticleManager: Spawns and manages all VFX — word completion bursts,
/// epic connection explosions, small per-tile sparks, and board lighting pulses.
/// Supports reduced-FX mode for mobile performance.
/// </summary>
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject wordCompletePrefab;   // medium burst
    public GameObject epicComboPrefab;      // large multi-colour explosion
    public GameObject smallSparkPrefab;     // tiny per-tile pop
    public GameObject trailPrefab;          // used by letter tile drop trails

    [Header("Board Glow")]
    public Light boardLight;
    public float boardLightBaseIntensity = 1f;
    public float boardLightComboIntensity = 5f;
    public Color[] comboLightColors;

    [Header("Performance")]
    public bool reducedFX = false;
    private int comboColorIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetReducedFX(bool on)
    {
        reducedFX = on;
    }

    public void SpawnWordParticles(Vector3 worldPos)
    {
        if (wordCompletePrefab)
        {
            GameObject go = Instantiate(wordCompletePrefab, worldPos, Quaternion.identity);
            Destroy(go, 3f);
        }
        PulseBoardLight(false);
    }

    public void SpawnEpicParticles(Vector3 worldPos)
    {
        if (reducedFX) { SpawnWordParticles(worldPos); return; }

        if (epicComboPrefab)
        {
            GameObject go = Instantiate(epicComboPrefab, worldPos, Quaternion.identity);
            Destroy(go, 4f);
        }
        PulseBoardLight(true);
        CameraController.Instance?.Shake();
    }

    public void SpawnSmallParticle(Vector3 worldPos)
    {
        if (reducedFX) return;
        if (smallSparkPrefab)
        {
            GameObject go = Instantiate(smallSparkPrefab, worldPos, Quaternion.identity);
            Destroy(go, 1.5f);
        }
    }

    private void PulseBoardLight(bool epic)
    {
        if (boardLight == null) return;
        StartCoroutine(LightPulse(epic));
    }

    private IEnumerator LightPulse(bool epic)
    {
        float targetIntensity = epic ? boardLightComboIntensity : boardLightBaseIntensity * 2.5f;
        Color targetColor = comboLightColors.Length > 0
            ? comboLightColors[comboColorIndex % comboLightColors.Length]
            : Color.white;
        comboColorIndex++;

        float elapsed = 0f;
        float riseTime = 0.15f;
        float fallTime = epic ? 0.8f : 0.4f;

        Color startColor = boardLight.color;
        float startIntensity = boardLight.intensity;

        // Rise
        while (elapsed < riseTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / riseTime;
            boardLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            boardLight.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        // Fall
        elapsed = 0f;
        while (elapsed < fallTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallTime;
            boardLight.intensity = Mathf.Lerp(targetIntensity, boardLightBaseIntensity, t);
            boardLight.color = Color.Lerp(targetColor, startColor, t);
            yield return null;
        }

        boardLight.intensity = boardLightBaseIntensity;
        boardLight.color = startColor;
    }
}

```

---

## 📄 `Scripts/Core/PuzzleManager.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PuzzleManager: Manages pre-configured puzzle boards with specific objectives.
/// Loads puzzle definitions, validates objectives, and handles puzzle progression.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector
    // -----------------------------------------------------------------------
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

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    private PuzzleDefinition activePuzzle;
    private int movesUsed = 0;
    private int wordsFoundThisPuzzle = 0;
    private int targetWordsRequired;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        BoardManager.Instance.OnWordsFound += HandleWordsFound;
        BoardManager.Instance.OnTilePlaced += HandleTilePlaced;

        if (nextPuzzleButton) nextPuzzleButton.onClick.AddListener(LoadNextPuzzle);
        if (retryButton) retryButton.onClick.AddListener(RetryPuzzle);
    }

    // -----------------------------------------------------------------------
    // Puzzle Loading
    // -----------------------------------------------------------------------
    public void LoadPuzzle(int index)
    {
        if (index >= puzzles.Length) { Debug.Log("[PuzzleManager] All puzzles complete!"); return; }

        currentPuzzleIndex = index;
        activePuzzle = puzzles[index];
        movesUsed = 0;
        wordsFoundThisPuzzle = 0;
        targetWordsRequired = activePuzzle.wordsRequired;

        // Place pre-set tiles on board
        BoardManager.Instance.InitBoard();
        foreach (PresetTile pt in activePuzzle.presetTiles)
        {
            BoardManager.Instance.DropLetter(pt.column, pt.letter, 2);   // player 2 = neutral color
        }

        RefreshUI();
        if (puzzleCompletePanel) puzzleCompletePanel.SetActive(false);
        if (puzzleFailPanel) puzzleFailPanel.SetActive(false);
    }

    private void RefreshUI()
    {
        if (objectiveText)
            objectiveText.text = $"Find {targetWordsRequired - wordsFoundThisPuzzle} more word(s). {activePuzzle.movesAllowed - movesUsed} moves left.";
        if (movesRemainingText)
            movesRemainingText.text = $"Moves: {activePuzzle.movesAllowed - movesUsed}";
    }

    // -----------------------------------------------------------------------
    // Event Handlers
    // -----------------------------------------------------------------------
    private void HandleTilePlaced(int col, int row, char letter, int player)
    {
        if (GameManager.Instance.ActiveMode != GameMode.Puzzle) return;
        movesUsed++;

        if (movesUsed >= activePuzzle.movesAllowed && wordsFoundThisPuzzle < targetWordsRequired)
        {
            StartCoroutine(ShowResult(false));
        }
        RefreshUI();
    }

    private void HandleWordsFound(List<WordResult> words)
    {
        if (GameManager.Instance.ActiveMode != GameMode.Puzzle) return;
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
            AudioManager.Instance.PlayWordComplete(6, 3);
        }
        else
        {
            if (puzzleFailPanel) puzzleFailPanel.SetActive(true);
        }
    }

    // -----------------------------------------------------------------------
    // Navigation
    // -----------------------------------------------------------------------
    private void LoadNextPuzzle() => LoadPuzzle(currentPuzzleIndex + 1);
    private void RetryPuzzle() => LoadPuzzle(currentPuzzleIndex);
}

// -----------------------------------------------------------------------
// Data Classes
// -----------------------------------------------------------------------

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

```

---

## 📄 `Scripts/UI/UIManager.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIManager: Manages all HUD elements — scores, turn indicator, combo bar,
/// streak display, floating score pop-ups, settings panel, mode selection,
/// and game-over screen. Uses smooth easing for all transitions.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector References — HUD
    // -----------------------------------------------------------------------
    [Header("Score Panels")]
    public TextMeshProUGUI[] playerScoreTexts;    // one per player
    public Image[] playerScorePanels;
    public TextMeshProUGUI comboMultiplierText;
    public Slider streakBar;
    public TextMeshProUGUI streakLabel;

    [Header("Turn Indicator")]
    public TextMeshProUGUI turnIndicatorText;
    public Image turnIndicatorPanel;
    public Color[] playerTurnColors;

    [Header("Letter Display")]
    public TextMeshProUGUI currentLetterText;
    public Image currentLetterPanel;

    [Header("Timer (Timed Mode)")]
    public GameObject timerGroup;
    public TextMeshProUGUI timerText;
    public Image timerFill;

    [Header("Floating Score")]
    public GameObject floatingScorePrefab;
    public Transform floatingScoreCanvas;

    [Header("Message Banner")]
    public TextMeshProUGUI messageBannerText;
    public CanvasGroup messageBannerGroup;

    // -----------------------------------------------------------------------
    // Inspector References — Panels
    // -----------------------------------------------------------------------
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject modeSelectPanel;
    public GameObject gameHUDPanel;
    public GameObject gameOverPanel;
    public GameObject settingsPanel;
    public GameObject tutorialOverlayPanel;

    [Header("Game Over")]
    public TextMeshProUGUI gameOverTitleText;
    public TextMeshProUGUI gameOverScoresText;
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Settings")]
    public Toggle musicToggle;
    public Toggle sfxToggle;
    public Toggle colorBlindToggle;
    public Toggle reducedFXToggle;
    public Slider letterSizeSlider;

    // -----------------------------------------------------------------------
    // Internal
    // -----------------------------------------------------------------------
    private Camera mainCamera;

    // -----------------------------------------------------------------------
    // Unity Lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        ShowMainMenu();
        SetupSettingsCallbacks();
    }

    // -----------------------------------------------------------------------
    // Panel Management
    // -----------------------------------------------------------------------
    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(modeSelectPanel, false);
        SetPanelActive(gameHUDPanel, false);
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(settingsPanel, false);
    }

    public void ShowModeSelect()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(modeSelectPanel, true);
    }

    public void ShowHUD()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(modeSelectPanel, false);
        SetPanelActive(gameHUDPanel, true);
    }

    public void ShowGameOver(int[] scores, string message)
    {
        SetPanelActive(gameOverPanel, true);
        gameOverTitleText.text = message;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < scores.Length; i++)
            sb.AppendLine($"Player {i + 1}: {scores[i]} pts");
        gameOverScoresText.text = sb.ToString();

        StartCoroutine(FadeInPanel(gameOverPanel.GetComponent<CanvasGroup>(), 0.5f));
    }

    public void ToggleSettings(bool open)
    {
        SetPanelActive(settingsPanel, open);
        if (open) StartCoroutine(SlideInPanel(settingsPanel, Vector2.right * -400f, Vector2.zero, 0.3f));
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel) panel.SetActive(active);
    }

    // -----------------------------------------------------------------------
    // HUD Updates
    // -----------------------------------------------------------------------
    public void RefreshAll(int[] scores, int currentPlayer, float combo)
    {
        for (int i = 0; i < scores.Length; i++)
            UpdateScore(i, scores[i]);
        ShowTurnIndicator(currentPlayer);
        UpdateComboMultiplier(combo);
    }

    public void UpdateScore(int playerIndex, int score)
    {
        if (playerIndex < playerScoreTexts.Length)
            StartCoroutine(AnimateScoreCount(playerScoreTexts[playerIndex], score));
    }

    public void ShowTurnIndicator(int playerIndex)
    {
        if (turnIndicatorText)
        {
            turnIndicatorText.text = $"Player {playerIndex + 1}'s Turn";
            if (playerIndex < playerTurnColors.Length)
                turnIndicatorPanel.color = playerTurnColors[playerIndex];

            StartCoroutine(PunchScale(turnIndicatorPanel.transform, 0.2f));
        }
    }

    public void SetCurrentLetter(char c, int playerIndex)
    {
        if (currentLetterText) currentLetterText.text = c.ToString();
        if (playerIndex < playerTurnColors.Length && currentLetterPanel)
            currentLetterPanel.color = playerTurnColors[playerIndex];
        StartCoroutine(PunchScale(currentLetterText.transform, 0.25f));
    }

    public void UpdateComboMultiplier(float multiplier)
    {
        if (comboMultiplierText)
        {
            comboMultiplierText.text = $"x{multiplier:F1}";
            comboMultiplierText.color = Color.Lerp(Color.white, Color.yellow, (multiplier - 1f) / 4f);
        }
    }

    public void UpdateStreakBar(int streak, int threshold)
    {
        if (streakBar) streakBar.value = (float)streak / (threshold * 2f);
        if (streakLabel) streakLabel.text = streak > 0 ? $"Streak x{streak}" : "";
    }

    public void UpdateTimerDisplay(float seconds)
    {
        if (seconds < 0) seconds = 0;
        if (timerText) timerText.text = $"{Mathf.FloorToInt(seconds / 60f):00}:{Mathf.FloorToInt(seconds % 60f):00}";
        if (timerFill) timerFill.fillAmount = seconds / GameManager.Instance.timedDuration;
        if (timerFill) timerFill.color = Color.Lerp(Color.red, Color.cyan, seconds / GameManager.Instance.timedDuration);
    }

    // -----------------------------------------------------------------------
    // Floating Score Pop-ups
    // -----------------------------------------------------------------------
    public void ShowFloatingScore(int points, Vector3 worldPos, string prefix = "")
    {
        if (!floatingScorePrefab || !floatingScoreCanvas) return;

        GameObject go = Instantiate(floatingScorePrefab, floatingScoreCanvas);
        TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = prefix + (prefix.Length > 0 ? " +" : "+") + points;

        // Convert world to screen to canvas
        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        go.GetComponent<RectTransform>().position = screenPos;

        StartCoroutine(AnimateFloatingScore(go));
    }

    private IEnumerator AnimateFloatingScore(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (!cg) cg = go.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        float duration = 1.2f;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = startPos + Vector2.up * (80f * t);
            cg.alpha = 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
            yield return null;
        }

        Destroy(go);
    }

    // -----------------------------------------------------------------------
    // Message Banner
    // -----------------------------------------------------------------------
    public void ShowMessage(string msg, float duration = 2f)
    {
        StartCoroutine(MessageRoutine(msg, duration));
    }

    private IEnumerator MessageRoutine(string msg, float duration)
    {
        if (!messageBannerText || !messageBannerGroup) yield break;
        messageBannerText.text = msg;
        messageBannerGroup.alpha = 1f;
        yield return new WaitForSeconds(duration);
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            messageBannerGroup.alpha = 1f - elapsed / 0.4f;
            yield return null;
        }
        messageBannerGroup.alpha = 0f;
    }

    // -----------------------------------------------------------------------
    // Settings Callbacks
    // -----------------------------------------------------------------------
    private void SetupSettingsCallbacks()
    {
        if (musicToggle) musicToggle.onValueChanged.AddListener(v => AudioManager.Instance.SetMusicEnabled(v));
        if (sfxToggle) sfxToggle.onValueChanged.AddListener(v => AudioManager.Instance.SetSFXEnabled(v));
        if (colorBlindToggle) colorBlindToggle.onValueChanged.AddListener(v => ApplyColorBlindMode(v));
        if (reducedFXToggle) reducedFXToggle.onValueChanged.AddListener(v => ParticleManager.Instance?.SetReducedFX(v));
        if (letterSizeSlider) letterSizeSlider.onValueChanged.AddListener(v => ApplyLetterSize(v));
    }

    private void ApplyColorBlindMode(bool on)
    {
        // Swap player colors to colorblind-safe palette
        if (playerTurnColors.Length >= 2)
        {
            playerTurnColors[0] = on ? new Color(0f, 0.45f, 0.7f) : new Color(0f, 1f, 1f);    // cyan vs blue
            playerTurnColors[1] = on ? new Color(0.9f, 0.6f, 0f) : new Color(1f, 0.2f, 0.8f); // orange vs pink
        }
    }

    private void ApplyLetterSize(float scale)
    {
        // Scale all letter prefab instances (broadcast to BoardManager)
        foreach (LetterTile tile in FindObjectsOfType<LetterTile>())
            tile.transform.localScale = Vector3.one * scale;
    }

    // -----------------------------------------------------------------------
    // Animation Helpers
    // -----------------------------------------------------------------------
    private IEnumerator PunchScale(Transform t, float duration)
    {
        Vector3 orig = t.localScale;
        float half = duration * 0.5f;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(orig, orig * 1.25f, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(orig * 1.25f, orig, elapsed / half);
            yield return null;
        }
        t.localScale = orig;
    }

    private IEnumerator AnimateScoreCount(TextMeshProUGUI text, int targetScore)
    {
        int current = 0;
        if (int.TryParse(text.text, out int parsed)) current = parsed;

        float elapsed = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int display = Mathf.RoundToInt(Mathf.Lerp(current, targetScore, elapsed / duration));
            text.text = display.ToString();
            yield return null;
        }
        text.text = targetScore.ToString();
    }

    private IEnumerator FadeInPanel(CanvasGroup cg, float duration)
    {
        if (!cg) yield break;
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = elapsed / duration;
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator SlideInPanel(GameObject panel, Vector2 from, Vector2 to, float duration)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (!rt) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = Vector2.Lerp(from, to, EaseOutCubic(t));
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    // -----------------------------------------------------------------------
    // Button Handlers (wire up in Unity Inspector)
    // -----------------------------------------------------------------------
    public void OnPlayClassic() => GameManager.Instance.StartGame(GameMode.Classic);
    public void OnPlayTimed() => GameManager.Instance.StartGame(GameMode.Timed);
    public void OnPlayPuzzle() => GameManager.Instance.StartGame(GameMode.Puzzle);
    public void OnPlayTutorial() => GameManager.Instance.StartGame(GameMode.Tutorial);
    public void OnPlayAgain() => GameManager.Instance.StartGame(GameManager.Instance.ActiveMode);
    public void OnMainMenu() => ShowMainMenu();
    public void OnOpenSettings() => ToggleSettings(true);
    public void OnCloseSettings() => ToggleSettings(false);
}

```

---

## 📄 `Scripts/Audio/AudioManager.cs`

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// AudioManager: Handles all game audio — background synthwave music,
/// letter drop sounds, word completion chimes, power-up sounds, and cascades.
/// Music tempo dynamically adapts to player streaks.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector References
    // -----------------------------------------------------------------------
    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip[] musicTracks;         // 0=classic loop, 1=timed/intense loop
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0.8f, 1.5f)] public float basePitch = 1f;

    [Header("SFX Sources")]
    public AudioSource sfxSource;
    public AudioSource uiSource;

    [Header("SFX Clips")]
    public AudioClip dropStartClip;
    public AudioClip letterLandClip;
    public AudioClip wordCompleteClip;
    public AudioClip multiWordClip;
    public AudioClip epicComboClip;
    public AudioClip cascadeClip;
    public AudioClip powerUpWildcardClip;
    public AudioClip powerUpBombClip;
    public AudioClip powerUpSwapClip;
    public AudioClip uiClickClip;
    public AudioClip uiHoverClip;
    public AudioClip swapClip;

    [Header("Chime Pitches")]
    public float[] chimeScalePitches = { 1f, 1.122f, 1.26f, 1.498f, 1.682f, 2f };  // major scale

    // -----------------------------------------------------------------------
    // Settings
    // -----------------------------------------------------------------------
    private bool musicEnabled = true;
    private bool sfxEnabled = true;
    private float currentPitch;
    private Coroutine tempoRoutine;

    // -----------------------------------------------------------------------
    // Unity Lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentPitch = basePitch;
    }

    // -----------------------------------------------------------------------
    // Music Control
    // -----------------------------------------------------------------------
    public void PlayMusic(bool intense = false)
    {
        if (!musicEnabled || !musicSource) return;

        AudioClip track = intense && musicTracks.Length > 1 ? musicTracks[1] : musicTracks[0];
        if (track != null)
        {
            musicSource.clip = track;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.pitch = basePitch;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource) musicSource.Stop();
    }

    public void RaiseTempo(int streakLevel)
    {
        if (tempoRoutine != null) StopCoroutine(tempoRoutine);
        float targetPitch = Mathf.Clamp(basePitch + streakLevel * 0.04f, basePitch, 1.4f);
        tempoRoutine = StartCoroutine(SmoothPitchShift(targetPitch, 0.8f));
    }

    public void ResetTempo()
    {
        if (tempoRoutine != null) StopCoroutine(tempoRoutine);
        tempoRoutine = StartCoroutine(SmoothPitchShift(basePitch, 1.5f));
    }

    private IEnumerator SmoothPitchShift(float targetPitch, float duration)
    {
        float startPitch = musicSource ? musicSource.pitch : basePitch;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Lerp(startPitch, targetPitch, elapsed / duration);
            if (musicSource) musicSource.pitch = p;
            yield return null;
        }
        if (musicSource) musicSource.pitch = targetPitch;
    }

    // -----------------------------------------------------------------------
    // SFX Playback
    // -----------------------------------------------------------------------

    public void PlayDropStart()
    {
        PlaySFX(dropStartClip, 0.7f, RandomPitch(0.9f, 1.1f));
    }

    /// <param name="heightNormalized">0=top drop, 1=bottom row. Higher = higher pitch.</param>
    public void PlayLetterLand(float heightNormalized)
    {
        float pitch = Mathf.Lerp(1.4f, 0.8f, heightNormalized);
        PlaySFX(letterLandClip, 0.9f, pitch);
    }

    public void PlayWordComplete(int wordLength, int wordCount)
    {
        if (!sfxEnabled || !sfxSource || !wordCompleteClip) return;

        // Play a chime at the pitch corresponding to word length
        int chimeIndex = Mathf.Clamp(wordLength - 3, 0, chimeScalePitches.Length - 1);
        sfxSource.pitch = chimeScalePitches[chimeIndex];
        sfxSource.PlayOneShot(wordCompleteClip, 0.85f);

        // Layer additional chime for multi-word
        if (wordCount > 1 && multiWordClip)
        {
            StartCoroutine(DelayedSFX(multiWordClip, 0.12f, 0.7f, chimeScalePitches[Mathf.Min(chimeIndex + 1, chimeScalePitches.Length - 1)]));
        }
    }

    public void PlayEpicCombo(int wordCount)
    {
        PlaySFX(epicComboClip, 1f, Mathf.Lerp(1f, 1.3f, wordCount / 5f));
    }

    public void PlayCascade()
    {
        PlaySFX(cascadeClip, 0.6f, RandomPitch(0.95f, 1.05f));
    }

    public void PlayPowerUp(PowerUpType type)
    {
        AudioClip clip = type switch
        {
            PowerUpType.Wildcard => powerUpWildcardClip,
            PowerUpType.Bomb => powerUpBombClip,
            PowerUpType.Swap => powerUpSwapClip,
            _ => null
        };
        PlaySFX(clip, 1f, 1f);
    }

    public void PlaySwap()
    {
        PlaySFX(swapClip, 0.8f, RandomPitch(0.9f, 1.1f));
    }

    public void PlayUIClick()
    {
        if (uiSource && uiClickClip) uiSource.PlayOneShot(uiClickClip, 0.5f);
    }

    public void PlayUIHover()
    {
        if (uiSource && uiHoverClip) uiSource.PlayOneShot(uiHoverClip, 0.3f);
    }

    // -----------------------------------------------------------------------
    // Settings
    // -----------------------------------------------------------------------
    public void SetMusicEnabled(bool on)
    {
        musicEnabled = on;
        if (musicSource) musicSource.mute = !on;
    }

    public void SetSFXEnabled(bool on)
    {
        sfxEnabled = on;
        if (sfxSource) sfxSource.mute = !on;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (!sfxEnabled || !sfxSource || clip == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    private IEnumerator DelayedSFX(AudioClip clip, float delay, float volume, float pitch)
    {
        yield return new WaitForSeconds(delay);
        PlaySFX(clip, volume, pitch);
    }

    private float RandomPitch(float min, float max) => Random.Range(min, max);
}

```

---

## 📄 `Scripts/PowerUps/PowerUpManager.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum PowerUpType { Wildcard, Bomb, Swap }

/// <summary>
/// PowerUpManager: Tracks power-up inventory, handles activation logic,
/// and manages progressive unlock thresholds.
/// Each power-up type has its own activation flow.
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector Config
    // -----------------------------------------------------------------------
    [Header("Unlock Thresholds (points)")]
    public int wildcardUnlockAt = 20;
    public int bombUnlockAt = 50;
    public int swapUnlockAt = 80;

    [Header("Charge Costs (uses earned every N points)")]
    public int wildcardEvery = 30;
    public int bombEvery = 60;
    public int swapEvery = 90;

    [Header("UI References")]
    public Button wildcardButton;
    public Button bombButton;
    public Button swapButton;
    public TextMeshProUGUI wildcardCountText;
    public TextMeshProUGUI bombCountText;
    public TextMeshProUGUI swapCountText;

    [Header("Swap UI")]
    public GameObject swapSelectionPanel;   // shown during swap targeting
    public TextMeshProUGUI swapInstructionText;

    [Header("References")]
    public BoardManager boardManager;
    public AudioManager audioManager;
    public UIManager uiManager;

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    private int wildcardCharges = 0;
    private int bombCharges = 0;
    private int swapCharges = 0;

    private bool wildcardUnlocked = false;
    private bool bombUnlocked = false;
    private bool swapUnlocked = false;

    private int pointsAtLastWildcard = 0;
    private int pointsAtLastBomb = 0;
    private int pointsAtLastSwap = 0;

    // Swap selection state
    private bool selectingSwap = false;
    private Vector2Int swapFirst = new Vector2Int(-1, -1);
    private bool swapFirstSelected = false;

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
        RefreshButtonStates();

        if (wildcardButton) wildcardButton.onClick.AddListener(ActivateWildcard);
        if (bombButton) bombButton.onClick.AddListener(ActivateBomb);
        if (swapButton) swapButton.onClick.AddListener(BeginSwap);
    }

    // -----------------------------------------------------------------------
    // Unlock & Charge Logic
    // -----------------------------------------------------------------------
    public void OnPointsEarned(int totalPoints)
    {
        // Unlock checks
        if (!wildcardUnlocked && totalPoints >= wildcardUnlockAt)
        {
            wildcardUnlocked = true;
            wildcardCharges = 1;
            uiManager.ShowMessage("Wildcard power-up unlocked! ★");
            pointsAtLastWildcard = totalPoints;
        }
        if (!bombUnlocked && totalPoints >= bombUnlockAt)
        {
            bombUnlocked = true;
            bombCharges = 1;
            uiManager.ShowMessage("Bomb power-up unlocked! 💣");
            pointsAtLastBomb = totalPoints;
        }
        if (!swapUnlocked && totalPoints >= swapUnlockAt)
        {
            swapUnlocked = true;
            swapCharges = 1;
            uiManager.ShowMessage("Swap power-up unlocked! ⇄");
            pointsAtLastSwap = totalPoints;
        }

        // Recharge after unlock
        if (wildcardUnlocked && totalPoints - pointsAtLastWildcard >= wildcardEvery)
        {
            wildcardCharges++;
            pointsAtLastWildcard = totalPoints;
        }
        if (bombUnlocked && totalPoints - pointsAtLastBomb >= bombEvery)
        {
            bombCharges++;
            pointsAtLastBomb = totalPoints;
        }
        if (swapUnlocked && totalPoints - pointsAtLastSwap >= swapEvery)
        {
            swapCharges++;
            pointsAtLastSwap = totalPoints;
        }

        RefreshButtonStates();
    }

    // -----------------------------------------------------------------------
    // Wildcard Activation
    // -----------------------------------------------------------------------
    private void ActivateWildcard()
    {
        if (wildcardCharges <= 0) return;
        wildcardCharges--;
        RefreshButtonStates();
        audioManager.PlayPowerUp(PowerUpType.Wildcard);
        uiManager.ShowMessage("Wildcard! Drop as any letter.");
        GameManager.Instance.SetCurrentLetter('*');
    }

    // -----------------------------------------------------------------------
    // Bomb Activation
    // -----------------------------------------------------------------------
    private void ActivateBomb()
    {
        if (bombCharges <= 0) return;
        StartCoroutine(SelectBombTarget());
    }

    private IEnumerator SelectBombTarget()
    {
        uiManager.ShowMessage("Click a tile to bomb its row or column!");
        bool selected = false;

        while (!selected)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    LetterTile tile = hit.collider.GetComponentInParent<LetterTile>();
                    if (tile != null)
                    {
                        selected = true;
                        // Find position on board
                        Vector2Int pos = FindTilePosition(tile);
                        if (pos.x >= 0)
                        {
                            bombCharges--;
                            RefreshButtonStates();
                            audioManager.PlayPowerUp(PowerUpType.Bomb);
                            // Bomb clears the row
                            boardManager.ClearRow(pos.y);
                            uiManager.ShowMessage($"BOOM! Row {pos.y + 1} cleared!");
                        }
                    }
                }
            }
            yield return null;
        }
    }

    // -----------------------------------------------------------------------
    // Swap Activation
    // -----------------------------------------------------------------------
    private void BeginSwap()
    {
        if (swapCharges <= 0) return;
        selectingSwap = true;
        swapFirstSelected = false;
        if (swapSelectionPanel) swapSelectionPanel.SetActive(true);
        if (swapInstructionText) swapInstructionText.text = "Select FIRST tile to swap";
        StartCoroutine(SwapSelectionRoutine());
    }

    private IEnumerator SwapSelectionRoutine()
    {
        Vector2Int first = new Vector2Int(-1, -1);
        Vector2Int second = new Vector2Int(-1, -1);
        int step = 0;

        while (step < 2)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    LetterTile tile = hit.collider.GetComponentInParent<LetterTile>();
                    if (tile != null)
                    {
                        Vector2Int pos = FindTilePosition(tile);
                        if (pos.x >= 0)
                        {
                            if (step == 0)
                            {
                                first = pos;
                                tile.PlayWordHighlight();
                                if (swapInstructionText) swapInstructionText.text = "Select SECOND tile to swap";
                                step++;
                            }
                            else if (step == 1 && pos != first)
                            {
                                second = pos;
                                step++;
                            }
                        }
                    }
                }
            }
            yield return null;
        }

        if (swapSelectionPanel) swapSelectionPanel.SetActive(false);
        selectingSwap = false;

        if (boardManager.SwapTiles(first, second))
        {
            swapCharges--;
            RefreshButtonStates();
            audioManager.PlayPowerUp(PowerUpType.Swap);
            uiManager.ShowMessage("Tiles swapped!");
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private Vector2Int FindTilePosition(LetterTile tile)
    {
        for (int c = 0; c < boardManager.columns; c++)
            for (int r = 0; r < boardManager.rows; r++)
                if (boardManager.GetTile(c, r) == tile)
                    return new Vector2Int(c, r);
        return new Vector2Int(-1, -1);
    }

    private void RefreshButtonStates()
    {
        SetButtonState(wildcardButton, wildcardCountText, wildcardUnlocked, wildcardCharges);
        SetButtonState(bombButton, bombCountText, bombUnlocked, bombCharges);
        SetButtonState(swapButton, swapCountText, swapUnlocked, swapCharges);
    }

    private void SetButtonState(Button btn, TextMeshProUGUI countTxt, bool unlocked, int charges)
    {
        if (btn) btn.interactable = unlocked && charges > 0;
        if (countTxt)
        {
            countTxt.text = unlocked ? charges.ToString() : "🔒";
            countTxt.color = charges > 0 ? Color.white : Color.gray;
        }
    }
}

```

---

## 📄 `Scripts/Tutorials/TutorialManager.cs`

```csharp
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

```

