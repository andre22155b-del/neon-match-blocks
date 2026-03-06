using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Neon Match Blocks - Competitive game flow manager.
/// Setup (Inspector):
/// 1) Assign Block prefab, Board Root, and Sports Face sprites.
/// 2) Assign UIManager / SoundManager / ParticleManager objects in scene (singletons).
/// 3) Block prefab must contain Block.cs + Collider.
/// 4) Sports face sprite list should include soccer/basketball/football/baseball/boxing variants.
/// 5) If you use AI, set Play Vs AI true.
/// </summary>
public class GameManagerCompetitive : MonoBehaviour
{
    public static GameManagerCompetitive Instance { get; private set; }

    [Header("Mode")]
    [SerializeField] private bool playVsAI = true;
    [SerializeField] private int maxLevels = 20;

    [Header("Board")]
    [SerializeField] private Block blockPrefab;
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float blockSpacing = 1.45f;

    [Header("Faces")]
    [SerializeField] private List<Sprite> sportsFaces = new List<Sprite>();

    [Header("Gameplay Timing")]
    [SerializeField] private float revealPause = 0.20f;
    [SerializeField] private float mismatchHideDelay = 0.55f;
    [SerializeField] private float aiPickDelay = 0.40f;

    [Header("Scoring")]
    [SerializeField] private int baseMatchScore = 100;
    [SerializeField] private float comboStepMultiplier = 0.25f;

    [Header("Floating Text Colors")]
    [SerializeField] private Color player1Color = new Color(0.2f, 0.95f, 1f);
    [SerializeField] private Color player2Color = new Color(1f, 0.45f, 0.2f);

    private readonly Vector2Int[] levelGrids =
    {
        new Vector2Int(2, 2),  // 1
        new Vector2Int(2, 3),  // 2
        new Vector2Int(2, 4),  // 3
        new Vector2Int(3, 4),  // 4
        new Vector2Int(4, 4),  // 5
        new Vector2Int(4, 5),  // 6
        new Vector2Int(4, 5),  // 7
        new Vector2Int(4, 6),  // 8
        new Vector2Int(4, 6),  // 9
        new Vector2Int(5, 6),  // 10
        new Vector2Int(5, 6),  // 11
        new Vector2Int(5, 6),  // 12
        new Vector2Int(6, 6),  // 13
        new Vector2Int(6, 6),  // 14
        new Vector2Int(6, 6),  // 15
        new Vector2Int(6, 6),  // 16
        new Vector2Int(6, 6),  // 17
        new Vector2Int(6, 6),  // 18
        new Vector2Int(6, 6),  // 19
        new Vector2Int(6, 6)   // 20
    };

    private readonly int[] scores = new int[2];
    private readonly int[] combos = new int[2];
    private readonly int[] maxCombosPerLevel = new int[2];
    private readonly int[] levelsWon = new int[2];

    private readonly Dictionary<int, List<Block>> aiMemory = new Dictionary<int, List<Block>>();
    private readonly List<Block> activeBlocks = new List<Block>();

    private int currentLevel = 1;
    private int currentPlayer; // 0 = P1, 1 = P2/AI
    private int totalPairs;
    private int matchedPairs;

    private float levelTimer;
    private bool levelRunning;
    private bool levelFinishing;
    private bool inputLocked;
    private bool isEvaluatingPair;
    private bool aiTurnRoutineRunning;

    private Block firstPick;
    private Block secondPick;

    private sealed class SpawnData
    {
        public int faceId;
        public BlockRarity rarity;

        public SpawnData(int faceId, BlockRarity rarity)
        {
            this.faceId = faceId;
            this.rarity = rarity;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (boardRoot == null)
        {
            boardRoot = transform;
        }
    }

    private void Start()
    {
        BeginGame();
    }

    public void BeginGame()
    {
        StopAllCoroutines();
        currentLevel = 1;
        levelsWon[0] = 0;
        levelsWon[1] = 0;
        StartCoroutine(GameLoop());
    }

    public void TrySelectBlock(Block block)
    {
        if (!levelRunning || inputLocked || block == null)
        {
            return;
        }

        if (playVsAI && currentPlayer == 1)
        {
            return;
        }

        SelectBlockInternal(block);
    }

    public void TrySelectBlockFromAI(Block block)
    {
        if (!levelRunning || inputLocked || block == null)
        {
            return;
        }

        if (!playVsAI || currentPlayer != 1)
        {
            return;
        }

        SelectBlockInternal(block);
    }

    private IEnumerator GameLoop()
    {
        while (currentLevel <= maxLevels)
        {
            yield return StartCoroutine(RunLevel(currentLevel));
            currentLevel++;
        }

        int finalWinner = levelsWon[0] == levelsWon[1] ? -1 : (levelsWon[0] > levelsWon[1] ? 0 : 1);
        string finalWinnerName = finalWinner < 0 ? "TIE" : GetPlayerName(finalWinner);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLevelResult(
                currentLevel - 1,
                "Championship Complete",
                finalWinnerName,
                levelsWon[0],
                levelsWon[1],
                0,
                0,
                true
            );
        }
    }

    private IEnumerator RunLevel(int level)
    {
        PrepareLevel(level);
        yield return StartCoroutine(RunReflexMiniGame());

        inputLocked = false;
        levelRunning = true;
        levelFinishing = false;

        while (levelRunning)
        {
            levelTimer -= Time.deltaTime;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimer(levelTimer);
            }

            if (!levelFinishing && levelTimer <= 0f)
            {
                yield return StartCoroutine(FinishLevel("Time Up"));
                break;
            }

            if (!levelFinishing && matchedPairs >= totalPairs)
            {
                yield return StartCoroutine(FinishLevel("Board Cleared"));
                break;
            }

            if (playVsAI && currentPlayer == 1 && !inputLocked && !isEvaluatingPair && !aiTurnRoutineRunning && firstPick == null)
            {
                StartCoroutine(AITakeTurn());
            }

            yield return null;
        }
    }

    private void PrepareLevel(int level)
    {
        ClearBoard();
        ResetLevelState();

        BuildBoard(level);

        levelTimer = GetLevelDuration(level);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideLevelResult();
            UIManager.Instance.UpdateLevel(level, maxLevels);
            UIManager.Instance.UpdateScores(scores[0], scores[1]);
            UIManager.Instance.UpdateCombos(combos[0], combos[1]);
            UIManager.Instance.UpdateTurn(currentPlayer, playVsAI);
            UIManager.Instance.UpdateTimer(levelTimer);
        }
    }

    private void ResetLevelState()
    {
        scores[0] = 0;
        scores[1] = 0;
        combos[0] = 0;
        combos[1] = 0;
        maxCombosPerLevel[0] = 0;
        maxCombosPerLevel[1] = 0;

        currentPlayer = 0;
        totalPairs = 0;
        matchedPairs = 0;

        levelRunning = false;
        levelFinishing = false;
        inputLocked = true;
        isEvaluatingPair = false;
        aiTurnRoutineRunning = false;

        firstPick = null;
        secondPick = null;
        aiMemory.Clear();
    }

    private void BuildBoard(int level)
    {
        if (blockPrefab == null)
        {
            Debug.LogError("GameManagerCompetitive: Block prefab is not assigned.");
            return;
        }

        if (sportsFaces == null || sportsFaces.Count < 2)
        {
            Debug.LogError("GameManagerCompetitive: Assign at least 2 sports face sprites.");
            return;
        }

        Vector2Int grid = GetGridForLevel(level);
        int cellCount = grid.x * grid.y;
        if (cellCount % 2 != 0)
        {
            cellCount--;
        }

        totalPairs = cellCount / 2;

        List<SpawnData> deck = new List<SpawnData>(cellCount);
        int unlockedFaces = GetUnlockedFaceCount(level);

        for (int i = 0; i < totalPairs; i++)
        {
            int faceId = UnityEngine.Random.Range(0, unlockedFaces);
            BlockRarity rarity = RollRarity(level);
            deck.Add(new SpawnData(faceId, rarity));
            deck.Add(new SpawnData(faceId, rarity));
        }

        Shuffle(deck);

        float startX = -((grid.y - 1) * blockSpacing) * 0.5f;
        float startZ = -((grid.x - 1) * blockSpacing) * 0.5f;

        int index = 0;
        for (int row = 0; row < grid.x; row++)
        {
            for (int col = 0; col < grid.y; col++)
            {
                if (index >= deck.Count)
                {
                    break;
                }

                Vector3 pos = boardCenter + new Vector3(startX + col * blockSpacing, 0f, startZ + row * blockSpacing);
                Block block = Instantiate(blockPrefab, pos, Quaternion.identity, boardRoot);

                SpawnData data = deck[index];
                block.Setup(data.faceId, sportsFaces[data.faceId], data.rarity, this);

                activeBlocks.Add(block);
                index++;
            }
        }
    }

    private IEnumerator RunReflexMiniGame()
    {
        if (UIManager.Instance == null)
        {
            currentPlayer = UnityEngine.Random.Range(0, 2);
            yield break;
        }

        float p1Tap = float.MaxValue;
        float p2Tap = float.MaxValue;
        float tapStartTime = -1f;
        bool tapWindowOpen = false;

        Action onP1Tap = delegate
        {
            if (!tapWindowOpen || p1Tap < float.MaxValue)
            {
                return;
            }

            p1Tap = Time.time - tapStartTime;
            UIManager.Instance.PulseReflexSide(true);
        };

        Action onP2Tap = delegate
        {
            if (!tapWindowOpen || p2Tap < float.MaxValue)
            {
                return;
            }

            p2Tap = Time.time - tapStartTime;
            UIManager.Instance.PulseReflexSide(false);
        };

        UIManager.Instance.ShowReflexPanel(true);
        UIManager.Instance.SetReflexResult(string.Empty);
        UIManager.Instance.ConfigureReflexButtons(onP1Tap, onP2Tap);

        for (int n = 3; n >= 1; n--)
        {
            UIManager.Instance.SetReflexCountdown(n.ToString());
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.Play("Countdown");
            }

            yield return new WaitForSeconds(0.65f);
        }

        UIManager.Instance.SetReflexCountdown("TAP!");
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play("Tap");
        }

        tapWindowOpen = true;
        tapStartTime = Time.time;
        float aiReaction = playVsAI ? UnityEngine.Random.Range(0.18f, 0.45f) : float.MaxValue;
        float tapWindowDuration = 1.5f;

        while (Time.time - tapStartTime < tapWindowDuration)
        {
            if (WasPlayer1TapPressed())
            {
                onP1Tap();
            }

            if (!playVsAI && WasPlayer2TapPressed())
            {
                onP2Tap();
            }

            if (playVsAI && p2Tap == float.MaxValue && Time.time - tapStartTime >= aiReaction)
            {
                onP2Tap();
            }

            if (p1Tap < float.MaxValue && p2Tap < float.MaxValue)
            {
                break;
            }

            yield return null;
        }

        if (p1Tap == float.MaxValue)
        {
            p1Tap = 999f;
        }

        if (p2Tap == float.MaxValue)
        {
            p2Tap = 999f;
        }

        if (Mathf.Approximately(p1Tap, p2Tap))
        {
            currentPlayer = UnityEngine.Random.Range(0, 2);
        }
        else
        {
            currentPlayer = p1Tap < p2Tap ? 0 : 1;
        }

        UIManager.Instance.SetReflexResult(GetPlayerName(currentPlayer) + " starts!");
        UIManager.Instance.UpdateTurn(currentPlayer, playVsAI);

        yield return new WaitForSeconds(0.8f);
        UIManager.Instance.ShowReflexPanel(false);
    }

    private bool WasPlayer1TapPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.A);
#endif
    }

    private bool WasPlayer2TapPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.L);
#endif
    }

    private void SelectBlockInternal(Block block)
    {
        if (block.IsMatched || block.IsAnimating || block.IsRevealed)
        {
            return;
        }

        if (secondPick != null)
        {
            return;
        }

        if (firstPick == null)
        {
            inputLocked = true;
            firstPick = block;
            block.Reveal(delegate
            {
                RememberBlockForAI(block);
                inputLocked = false;
            });
            return;
        }

        if (block == firstPick)
        {
            return;
        }

        inputLocked = true;
        secondPick = block;
        block.Reveal(delegate
        {
            RememberBlockForAI(block);
            StartCoroutine(EvaluatePair());
        });
    }

    private IEnumerator EvaluatePair()
    {
        isEvaluatingPair = true;
        yield return new WaitForSeconds(revealPause);

        if (firstPick == null || secondPick == null)
        {
            isEvaluatingPair = false;
            inputLocked = false;
            yield break;
        }

        bool isMatch = firstPick.FaceId == secondPick.FaceId;

        if (isMatch)
        {
            HandleMatch();
        }
        else
        {
            yield return StartCoroutine(HandleMismatch());
        }

        firstPick = null;
        secondPick = null;

        isEvaluatingPair = false;
        inputLocked = false;
    }

    private void HandleMatch()
    {
        firstPick.SetMatched();
        secondPick.SetMatched();

        RemoveBlockFromAIMemory(firstPick);
        RemoveBlockFromAIMemory(secondPick);

        matchedPairs++;

        int player = currentPlayer;
        combos[player]++;
        maxCombosPerLevel[player] = Mathf.Max(maxCombosPerLevel[player], combos[player]);

        int scoreAdd = CalculateScore(firstPick.Rarity, combos[player]);
        scores[player] += scoreAdd;

        Vector3 center = (firstPick.transform.position + secondPick.transform.position) * 0.5f;

        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayMatch(center, firstPick.Rarity);
            if (combos[player] > 1)
            {
                ParticleManager.Instance.PlayCombo(center + Vector3.up * 0.4f, Mathf.Clamp(1f + combos[player] * 0.05f, 1f, 1.7f));
            }
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play("Match");
            if (combos[player] > 1)
            {
                SoundManager.Instance.Play("Combo", 1f + (combos[player] * 0.03f));
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScores(scores[0], scores[1]);
            UIManager.Instance.UpdateCombos(combos[0], combos[1]);
            UIManager.Instance.ShowFloatingText(
                "+" + scoreAdd + "  x" + GetMultiplier(combos[player]).ToString("0.0"),
                center + Vector3.up * 0.35f,
                GetPlayerColor(player),
                1f
            );

            if (combos[player] > 1)
            {
                UIManager.Instance.ShowFloatingText(
                    "COMBO x" + combos[player],
                    center + Vector3.up * 0.8f,
                    GetPlayerColor(player),
                    1.15f
                );
            }
        }
    }

    private IEnumerator HandleMismatch()
    {
        combos[currentPlayer] = 0;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCombos(combos[0], combos[1]);
        }

        firstPick.PlayWrongFeedback();
        secondPick.PlayWrongFeedback();

        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayWrong(firstPick.transform.position);
            ParticleManager.Instance.PlayWrong(secondPick.transform.position);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play("Wrong");
        }

        yield return new WaitForSeconds(mismatchHideDelay);

        bool firstDone = false;
        bool secondDone = false;

        firstPick.Hide(delegate { firstDone = true; });
        secondPick.Hide(delegate { secondDone = true; });

        while (!firstDone || !secondDone)
        {
            yield return null;
        }

        SwitchTurn();
    }

    private IEnumerator FinishLevel(string reason)
    {
        if (levelFinishing)
        {
            yield break;
        }

        levelFinishing = true;
        levelRunning = false;
        inputLocked = true;

        int winner = DetermineWinner();
        string winnerName = winner < 0 ? "TIE" : GetPlayerName(winner);

        if (winner >= 0)
        {
            levelsWon[winner]++;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play("LevelComplete");
        }

        if (ParticleManager.Instance != null)
        {
            ParticleManager.Instance.PlayLevelComplete(boardCenter + Vector3.up * 0.35f);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurn(currentPlayer, playVsAI);
            UIManager.Instance.ShowLevelResult(
                currentLevel,
                reason,
                winnerName,
                scores[0],
                scores[1],
                maxCombosPerLevel[0],
                maxCombosPerLevel[1],
                currentLevel >= maxLevels
            );
        }

        yield return new WaitForSeconds(2.5f);
    }

    private IEnumerator AITakeTurn()
    {
        aiTurnRoutineRunning = true;

        while (levelRunning && playVsAI && currentPlayer == 1 && !inputLocked && !isEvaluatingPair)
        {
            if (firstPick == null)
            {
                Block first = ChooseAIFirstBlock();
                if (first == null)
                {
                    break;
                }

                yield return new WaitForSeconds(aiPickDelay);
                TrySelectBlockFromAI(first);
                yield return new WaitUntil(delegate { return !inputLocked || !levelRunning; });
            }

            if (!levelRunning || currentPlayer != 1)
            {
                break;
            }

            if (firstPick == null || secondPick != null || inputLocked)
            {
                break;
            }

            Block second = ChooseAISecondBlock(firstPick);
            if (second == null)
            {
                break;
            }

            yield return new WaitForSeconds(aiPickDelay);
            TrySelectBlockFromAI(second);
            break;
        }

        aiTurnRoutineRunning = false;
    }

    private Block ChooseAIFirstBlock()
    {
        CleanupAIMemory();

        foreach (KeyValuePair<int, List<Block>> kv in aiMemory)
        {
            Block candidateA = null;
            Block candidateB = null;
            List<Block> list = kv.Value;

            for (int i = 0; i < list.Count; i++)
            {
                Block b = list[i];
                if (IsBlockSelectableForAI(b))
                {
                    if (candidateA == null)
                    {
                        candidateA = b;
                    }
                    else
                    {
                        candidateB = b;
                        break;
                    }
                }
            }

            if (candidateA != null && candidateB != null)
            {
                return candidateA;
            }
        }

        return GetRandomHiddenBlock(null);
    }

    private Block ChooseAISecondBlock(Block first)
    {
        CleanupAIMemory();

        if (aiMemory.TryGetValue(first.FaceId, out List<Block> list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                Block candidate = list[i];
                if (candidate != first && IsBlockSelectableForAI(candidate))
                {
                    return candidate;
                }
            }
        }

        // AI gets stronger in higher levels.
        float perfectChance = Mathf.Lerp(0.30f, 0.88f, (currentLevel - 1f) / Mathf.Max(1f, maxLevels - 1f));
        if (UnityEngine.Random.value <= perfectChance)
        {
            for (int i = 0; i < activeBlocks.Count; i++)
            {
                Block b = activeBlocks[i];
                if (b != first && IsBlockSelectableForAI(b) && b.FaceId == first.FaceId)
                {
                    return b;
                }
            }
        }

        return GetRandomHiddenBlock(first);
    }

    private Block GetRandomHiddenBlock(Block exclude)
    {
        List<Block> candidates = new List<Block>();
        for (int i = 0; i < activeBlocks.Count; i++)
        {
            Block b = activeBlocks[i];
            if (b == null || b == exclude)
            {
                continue;
            }

            if (!b.IsMatched && !b.IsRevealed && !b.IsAnimating)
            {
                candidates.Add(b);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private bool IsBlockSelectableForAI(Block block)
    {
        return block != null && !block.IsMatched && !block.IsRevealed && !block.IsAnimating;
    }

    private void RememberBlockForAI(Block block)
    {
        if (block == null || block.IsMatched)
        {
            return;
        }

        if (!aiMemory.TryGetValue(block.FaceId, out List<Block> list))
        {
            list = new List<Block>();
            aiMemory[block.FaceId] = list;
        }

        if (!list.Contains(block))
        {
            list.Add(block);
        }
    }

    private void RemoveBlockFromAIMemory(Block block)
    {
        if (block == null)
        {
            return;
        }

        if (aiMemory.TryGetValue(block.FaceId, out List<Block> list))
        {
            list.Remove(block);
            if (list.Count == 0)
            {
                aiMemory.Remove(block.FaceId);
            }
        }
    }

    private void CleanupAIMemory()
    {
        List<int> removeKeys = new List<int>();
        foreach (KeyValuePair<int, List<Block>> kv in aiMemory)
        {
            kv.Value.RemoveAll(delegate(Block b) { return b == null || b.IsMatched; });
            if (kv.Value.Count == 0)
            {
                removeKeys.Add(kv.Key);
            }
        }

        for (int i = 0; i < removeKeys.Count; i++)
        {
            aiMemory.Remove(removeKeys[i]);
        }
    }

    private void SwitchTurn()
    {
        currentPlayer = 1 - currentPlayer;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurn(currentPlayer, playVsAI);
        }
    }

    private int DetermineWinner()
    {
        if (scores[0] > scores[1])
        {
            return 0;
        }

        if (scores[1] > scores[0])
        {
            return 1;
        }

        if (maxCombosPerLevel[0] > maxCombosPerLevel[1])
        {
            return 0;
        }

        if (maxCombosPerLevel[1] > maxCombosPerLevel[0])
        {
            return 1;
        }

        return -1;
    }

    private Vector2Int GetGridForLevel(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, levelGrids.Length - 1);
        return levelGrids[idx];
    }

    private float GetLevelDuration(int level)
    {
        return Mathf.Lerp(10f, 30f, (level - 1f) / Mathf.Max(1f, maxLevels - 1f));
    }

    private int GetUnlockedFaceCount(int level)
    {
        int unlocked = 2 + ((level - 1) / 3);
        return Mathf.Clamp(unlocked, 2, sportsFaces.Count);
    }

    private BlockRarity RollRarity(int level)
    {
        float rareChance = Mathf.Lerp(0f, 0.22f, Mathf.InverseLerp(6f, 20f, level));
        float legendaryChance = Mathf.Lerp(0f, 0.08f, Mathf.InverseLerp(12f, 20f, level));

        float roll = UnityEngine.Random.value;
        if (roll <= legendaryChance)
        {
            return BlockRarity.Legendary;
        }

        if (roll <= legendaryChance + rareChance)
        {
            return BlockRarity.Rare;
        }

        return BlockRarity.Normal;
    }

    private int CalculateScore(BlockRarity rarity, int comboCount)
    {
        int rarityBonus = 0;
        if (rarity == BlockRarity.Rare)
        {
            rarityBonus = 75;
        }

        if (rarity == BlockRarity.Legendary)
        {
            rarityBonus = 200;
        }

        float multiplier = GetMultiplier(comboCount);
        return Mathf.RoundToInt((baseMatchScore + rarityBonus) * multiplier);
    }

    private float GetMultiplier(int comboCount)
    {
        return 1f + Mathf.Max(0, comboCount - 1) * comboStepMultiplier;
    }

    private Color GetPlayerColor(int player)
    {
        return player == 0 ? player1Color : player2Color;
    }

    private string GetPlayerName(int player)
    {
        if (player == 0)
        {
            return "PLAYER 1";
        }

        return playVsAI ? "AI" : "PLAYER 2";
    }

    private void ClearBoard()
    {
        for (int i = 0; i < activeBlocks.Count; i++)
        {
            if (activeBlocks[i] != null)
            {
                Destroy(activeBlocks[i].gameObject);
            }
        }

        activeBlocks.Clear();
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
