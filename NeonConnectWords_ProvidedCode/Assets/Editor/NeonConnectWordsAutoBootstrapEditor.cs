using TMPro;
using NeonConnectWords.Simulation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NeonConnectWordsAutoBootstrapEditor
{
    private const string ScenePath = "Assets/Scenes/NeonConnectWords.unity";
    private const string LetterPrefabPath = "Assets/Prefabs/LetterTile.prefab";
    private const string PlayerOneMaterialPath = "Assets/Materials/PlayerOneNeon.mat";
    private const string PlayerTwoMaterialPath = "Assets/Materials/PlayerTwoNeon.mat";
    private const string BoardMaterialPath = "Assets/Materials/BoardSurface.mat";
    private const string AccentMaterialPath = "Assets/Materials/AccentNeon.mat";
    private const string GlassMaterialPath = "Assets/Materials/DarkGlass.mat";
    private const string CyanBeamMaterialPath = "Assets/Materials/CyanBeam.mat";
    private const string MagentaBeamMaterialPath = "Assets/Materials/MagentaBeam.mat";
    private const string VolumeProfilePath = "Assets/Settings/NeonArenaVolumeProfile.asset";
    private const string DictionaryPath = "Assets/Data/NeonDictionary.txt";
    private const string TmpSettingsSourcePath = "Packages/com.unity.render-pipelines.core/Samples~/Common/TextMesh Pro/Resources/TMP Settings.asset";
    private const string TmpFontSourcePath = "Packages/com.unity.render-pipelines.core/Samples~/Common/TextMesh Pro/Resources/Fonts & Materials/Inter-Regular SDF.asset";
    private const string TmpSettingsTargetPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string TmpFontTargetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Inter-Regular SDF.asset";

    private sealed class SceneRefs
    {
        public UIManager uiManager;
        public GameManager gameManager;
        public BoardManager boardManager;
        public PowerUpManager powerUpManager;
        public AudioManager audioManager;
        public TutorialManager tutorialManager;
        public PuzzleManager puzzleManager;
        public SimulationService simulationService;
        public ParticleManager particleManager;
        public WordChecker wordChecker;
        public GameObject mainMenuPanel;
        public GameObject gameHudPanel;
        public GameObject gameOverPanel;
        public GameObject tutorialOverlayPanel;
        public Button classicButton;
        public Button timedButton;
        public Button puzzleButton;
        public Button tutorialButton;
        public Button playAgainButton;
        public Button backToMenuButton;
        public Button puzzleRetryButton;
        public Button puzzleNextButton;
        public Button tutorialNextButton;
        public Button tutorialSkipButton;
        public TextMeshProUGUI puzzleObjectiveText;
        public TextMeshProUGUI puzzleMovesText;
        public GameObject puzzleCompletePanel;
        public GameObject puzzleFailPanel;
        public TextMeshProUGUI puzzleCompleteText;
        public GameObject swapSelectionPanel;
        public TextMeshProUGUI swapInstructionText;
        public Button wildcardButton;
        public Button bombButton;
        public Button swapButton;
        public TextMeshProUGUI wildcardCountText;
        public TextMeshProUGUI bombCountText;
        public TextMeshProUGUI swapCountText;
    }

    [MenuItem("Tools/Neon Connect Words/Build Playable Scene")]
    public static void BuildPlayableSceneMenu()
    {
        BuildPlayableScene();
    }

    public static void BuildPlayableScene()
    {
        EnsureFolder("Assets/Editor");
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Settings");
        EnsureTmpResources();

        Material playerOneMaterial = CreateNeonMaterial(PlayerOneMaterialPath, new Color(0.05f, 0.95f, 1f), new Color(0.05f, 1f, 1f) * 3f);
        Material playerTwoMaterial = CreateNeonMaterial(PlayerTwoMaterialPath, new Color(1f, 0.2f, 0.7f), new Color(1f, 0.1f, 0.8f) * 3f);
        Material boardMaterial = CreateBoardMaterial(BoardMaterialPath, new Color(0.03f, 0.05f, 0.09f, 0.98f));
        Material accentMaterial = CreateNeonMaterial(AccentMaterialPath, new Color(0.08f, 0.18f, 0.75f), new Color(0.12f, 0.45f, 1f) * 2.4f);
        Material glassMaterial = CreateGlassMaterial(GlassMaterialPath, new Color(0.04f, 0.08f, 0.15f, 0.82f), new Color(0.06f, 0.16f, 0.3f) * 1.5f);
        Material cyanBeamMaterial = CreateNeonMaterial(CyanBeamMaterialPath, new Color(0.04f, 0.85f, 1f), new Color(0.04f, 0.95f, 1f) * 4f);
        Material magentaBeamMaterial = CreateNeonMaterial(MagentaBeamMaterialPath, new Color(1f, 0.18f, 0.78f), new Color(1f, 0.18f, 0.78f) * 4f);
        VolumeProfile volumeProfile = CreateVolumeProfile(VolumeProfilePath);
        GameObject letterPrefab = CreateLetterPrefab(LetterPrefabPath, playerOneMaterial);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupWorld(boardMaterial, accentMaterial, glassMaterial, cyanBeamMaterial, magentaBeamMaterial, volumeProfile);
        SceneRefs refs = BuildUiAndSystems(letterPrefab, playerOneMaterial, playerTwoMaterial);
        WireScene(refs, playerOneMaterial, playerTwoMaterial, letterPrefab);

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[NeonConnectWords] Playable scene generated at " + ScenePath);
    }

    private static void SetupWorld(
        Material boardMaterial,
        Material accentMaterial,
        Material glassMaterial,
        Material cyanBeamMaterial,
        Material magentaBeamMaterial,
        VolumeProfile volumeProfile)
    {
        GameObject cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraController), typeof(UniversalAdditionalCameraData));
        cameraGo.tag = "MainCamera";
        cameraGo.transform.position = new Vector3(0f, 4.4f, 12.3f);
        cameraGo.transform.rotation = Quaternion.Euler(16f, 180f, 0f);
        Camera camera = cameraGo.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.01f, 0.015f, 0.045f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.fieldOfView = 46f;
        UniversalAdditionalCameraData cameraData = cameraGo.GetComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;

        GameObject directionalLightGo = new GameObject("Directional Light", typeof(Light));
        directionalLightGo.transform.rotation = Quaternion.Euler(35f, -28f, 0f);
        Light directionalLight = directionalLightGo.GetComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 0.38f;
        directionalLight.color = new Color(0.58f, 0.68f, 1f);

        GameObject pulseLightGo = new GameObject("Board Pulse Light", typeof(Light));
        pulseLightGo.transform.position = new Vector3(0f, 4.7f, 2.2f);
        Light pulseLight = pulseLightGo.GetComponent<Light>();
        pulseLight.type = LightType.Point;
        pulseLight.range = 24f;
        pulseLight.intensity = 1.45f;
        pulseLight.color = new Color(0.1f, 0.9f, 1f);

        GameObject leftSpotGo = new GameObject("Left Rim Light", typeof(Light));
        leftSpotGo.transform.position = new Vector3(-6.8f, 5.1f, 5.3f);
        leftSpotGo.transform.rotation = Quaternion.Euler(30f, 122f, 0f);
        Light leftSpot = leftSpotGo.GetComponent<Light>();
        leftSpot.type = LightType.Spot;
        leftSpot.range = 24f;
        leftSpot.spotAngle = 42f;
        leftSpot.intensity = 6f;
        leftSpot.color = new Color(0.05f, 0.95f, 1f);

        GameObject rightSpotGo = new GameObject("Right Rim Light", typeof(Light));
        rightSpotGo.transform.position = new Vector3(6.8f, 5.1f, 5.3f);
        rightSpotGo.transform.rotation = Quaternion.Euler(30f, -122f, 0f);
        Light rightSpot = rightSpotGo.GetComponent<Light>();
        rightSpot.type = LightType.Spot;
        rightSpot.range = 24f;
        rightSpot.spotAngle = 42f;
        rightSpot.intensity = 6f;
        rightSpot.color = new Color(1f, 0.2f, 0.78f);

        GameObject volumeGo = new GameObject("Global Volume", typeof(Volume));
        Volume volume = volumeGo.GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = volumeProfile;

        GameObject boardOrigin = new GameObject("BoardOrigin");
        boardOrigin.transform.position = Vector3.zero;

        GameObject backdrop = CreateWorldPrimitive("SkyBackdrop", PrimitiveType.Cube, null, new Vector3(0f, 5f, -18f), new Vector3(34f, 18f, 0.3f), glassMaterial);
        RemoveCollider(backdrop);
        GameObject halo = CreateWorldPrimitive("NeonHalo", PrimitiveType.Cylinder, null, new Vector3(0f, 4.2f, -8.6f), new Vector3(8.8f, 0.06f, 8.8f), cyanBeamMaterial);
        halo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        RemoveCollider(halo);
        GameObject haloInner = CreateWorldPrimitive("NeonHaloInner", PrimitiveType.Cylinder, null, new Vector3(0f, 3.5f, -6.5f), new Vector3(5.8f, 0.04f, 5.8f), magentaBeamMaterial);
        haloInner.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        RemoveCollider(haloInner);

        GameObject stage = CreateWorldPrimitive("StageBase", PrimitiveType.Cube, null, new Vector3(0f, -0.95f, -0.9f), new Vector3(12.5f, 0.7f, 4.4f), boardMaterial);
        GameObject stageLip = CreateWorldPrimitive("StageLip", PrimitiveType.Cube, null, new Vector3(0f, -0.58f, 1.1f), new Vector3(10.2f, 0.16f, 0.42f), cyanBeamMaterial);
        RemoveCollider(stageLip);
        CreateWorldPrimitive("BoardBackdrop", PrimitiveType.Cube, boardOrigin.transform, new Vector3(0f, 3f, -1f), new Vector3(10.4f, 8.9f, 0.5f), glassMaterial);
        CreateWorldPrimitive("BoardCrown", PrimitiveType.Cube, boardOrigin.transform, new Vector3(0f, 7.15f, -0.66f), new Vector3(9.6f, 0.22f, 0.28f), cyanBeamMaterial);
        CreateWorldPrimitive("BoardBase", PrimitiveType.Cube, boardOrigin.transform, new Vector3(0f, -0.15f, -0.66f), new Vector3(9.6f, 0.24f, 0.28f), magentaBeamMaterial);
        CreateWorldPrimitive("BoardLeftRail", PrimitiveType.Cube, boardOrigin.transform, new Vector3(-4.92f, 3f, -0.66f), new Vector3(0.22f, 7.2f, 0.28f), cyanBeamMaterial);
        CreateWorldPrimitive("BoardRightRail", PrimitiveType.Cube, boardOrigin.transform, new Vector3(4.92f, 3f, -0.66f), new Vector3(0.22f, 7.2f, 0.28f), magentaBeamMaterial);

        GameObject inputPlane = CreateWorldPrimitive("BoardInputPlane", PrimitiveType.Cube, boardOrigin.transform, new Vector3(0f, 3f, -0.4f), new Vector3(8.55f, 7.35f, 0.18f), boardMaterial);
        Collider inputCollider = inputPlane.GetComponent<Collider>();
        if (inputCollider != null)
        {
            inputCollider.isTrigger = false;
        }

        for (int i = 0; i < 8; i++)
        {
            Material dividerMaterial = i % 2 == 0 ? cyanBeamMaterial : magentaBeamMaterial;
            GameObject line = CreateWorldPrimitive(
                "ColumnDivider_" + i,
                PrimitiveType.Cube,
                boardOrigin.transform,
                new Vector3(-4.2f + (i * 1.2f), 3f, -0.28f),
                new Vector3(0.05f, 7.1f, 0.12f),
                dividerMaterial);
            RemoveCollider(line);
        }

        for (int row = 0; row <= 6; row++)
        {
            Material rowMaterial = row % 2 == 0 ? accentMaterial : glassMaterial;
            GameObject rowBar = CreateWorldPrimitive(
                "RowDivider_" + row,
                PrimitiveType.Cube,
                boardOrigin.transform,
                new Vector3(0f, row * 1.2f, -0.26f),
                new Vector3(8.4f, 0.04f, 0.12f),
                rowMaterial);
            RemoveCollider(rowBar);
        }

        GameObject leftTower = CreateWorldPrimitive("LeftTower", PrimitiveType.Cube, null, new Vector3(-8.8f, 3.2f, -1.4f), new Vector3(1.1f, 8.6f, 1.1f), glassMaterial);
        CreateWorldPrimitive("LeftTowerBeam", PrimitiveType.Cube, leftTower.transform, new Vector3(0f, 0f, 0.62f), new Vector3(0.26f, 8.1f, 0.18f), cyanBeamMaterial);
        GameObject rightTower = CreateWorldPrimitive("RightTower", PrimitiveType.Cube, null, new Vector3(8.8f, 3.2f, -1.4f), new Vector3(1.1f, 8.6f, 1.1f), glassMaterial);
        CreateWorldPrimitive("RightTowerBeam", PrimitiveType.Cube, rightTower.transform, new Vector3(0f, 0f, 0.62f), new Vector3(0.26f, 8.1f, 0.18f), magentaBeamMaterial);

        GameObject floor = CreateWorldPrimitive("NeonFloor", PrimitiveType.Cube, null, new Vector3(0f, -1.35f, -2.4f), new Vector3(24f, 0.2f, 10f), boardMaterial);
        RemoveCollider(floor);
        GameObject floorStripLeft = CreateWorldPrimitive("FloorStripLeft", PrimitiveType.Cube, null, new Vector3(-4.8f, -1.22f, 1.75f), new Vector3(4.6f, 0.06f, 0.18f), cyanBeamMaterial);
        RemoveCollider(floorStripLeft);
        GameObject floorStripRight = CreateWorldPrimitive("FloorStripRight", PrimitiveType.Cube, null, new Vector3(4.8f, -1.22f, 1.75f), new Vector3(4.6f, 0.06f, 0.18f), magentaBeamMaterial);
        RemoveCollider(floorStripRight);
    }

    private static SceneRefs BuildUiAndSystems(GameObject letterPrefab, Material playerOneMaterial, Material playerTwoMaterial)
    {
        SceneRefs refs = new SceneRefs();

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        TextAsset dictionaryAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DictionaryPath);

        GameObject simulationGo = new GameObject("SimulationService", typeof(SimulationService));
        refs.simulationService = simulationGo.GetComponent<SimulationService>();
        refs.simulationService.wordListAsset = dictionaryAsset;

        GameObject audioGo = new GameObject("AudioManager", typeof(AudioManager));
        refs.audioManager = audioGo.GetComponent<AudioManager>();
        refs.audioManager.musicSource = audioGo.AddComponent<AudioSource>();
        refs.audioManager.sfxSource = audioGo.AddComponent<AudioSource>();
        refs.audioManager.uiSource = audioGo.AddComponent<AudioSource>();

        GameObject particleGo = new GameObject("ParticleManager", typeof(ParticleManager));
        refs.particleManager = particleGo.GetComponent<ParticleManager>();
        refs.particleManager.boardLight = GameObject.Find("Board Pulse Light").GetComponent<Light>();
        refs.particleManager.comboLightColors = new[]
        {
            new Color(0.1f, 0.95f, 1f),
            new Color(1f, 0.25f, 0.75f),
            new Color(1f, 0.75f, 0.15f)
        };

        new GameObject("MobileRuntimeSettings", typeof(MobileRuntimeSettings));

        GameObject wordCheckerGo = new GameObject("WordChecker", typeof(WordChecker));
        refs.wordChecker = wordCheckerGo.GetComponent<WordChecker>();
        refs.wordChecker.wordListAsset = dictionaryAsset;

        GameObject boardGo = new GameObject("BoardManager", typeof(BoardManager));
        refs.boardManager = boardGo.GetComponent<BoardManager>();
        refs.boardManager.letterPrefab = letterPrefab;
        refs.boardManager.boardOrigin = GameObject.Find("BoardOrigin").transform;
        refs.boardManager.wordChecker = refs.wordChecker;
        refs.boardManager.audioManager = refs.audioManager;
        refs.boardManager.particleManager = refs.particleManager;
        refs.boardManager.simulationService = refs.simulationService;
        refs.boardManager.playerMaterials = new[] { playerOneMaterial, playerTwoMaterial };
        refs.boardManager.boardLayerMask = LayerMask.GetMask("Default");

        GameObject powerUpGo = new GameObject("PowerUpManager", typeof(PowerUpManager));
        refs.powerUpManager = powerUpGo.GetComponent<PowerUpManager>();
        refs.powerUpManager.boardManager = refs.boardManager;
        refs.powerUpManager.audioManager = refs.audioManager;
        refs.powerUpManager.simulationService = refs.simulationService;

        GameObject tutorialGo = new GameObject("TutorialManager", typeof(TutorialManager));
        refs.tutorialManager = tutorialGo.GetComponent<TutorialManager>();
        refs.tutorialManager.columnHighlightObjects = new GameObject[0];
        refs.tutorialManager.steps = new[]
        {
            new TutorialStep
            {
                title = "Drop A Letter",
                body = "Move over the board and tap or click a column to drop the current neon letter.",
                requiresPlayerAction = true,
                highlightColumns = new[] { 3 },
                arrowTargetColumn = 3
            },
            new TutorialStep
            {
                title = "Make A Word",
                body = "Form words horizontally, vertically, or diagonally. Words clear and score immediately.",
                requiresPlayerAction = true,
                highlightColumns = new[] { 2, 3, 4 },
                arrowTargetColumn = 4
            },
            new TutorialStep
            {
                title = "Use Power-Ups",
                body = "As you score, wildcard, bomb, and swap charges unlock on the right side of the HUD.",
                requiresPlayerAction = false,
                highlightColumns = new int[0],
                arrowTargetColumn = -1
            }
        };

        GameObject puzzleGo = new GameObject("PuzzleManager", typeof(PuzzleManager));
        refs.puzzleManager = puzzleGo.GetComponent<PuzzleManager>();
        refs.puzzleManager.puzzles = new[]
        {
            new PuzzleDefinition
            {
                puzzleName = "Starter Stack",
                description = "Clear one word in a forgiving opener.",
                movesAllowed = 8,
                wordsRequired = 1,
                presetTiles = new[]
                {
                    new PresetTile { column = 1, letter = 'C' },
                    new PresetTile { column = 2, letter = 'A' },
                    new PresetTile { column = 4, letter = 'T' }
                }
            },
            new PuzzleDefinition
            {
                puzzleName = "Diagonal Spark",
                description = "Build through the center and trigger a diagonal clear.",
                movesAllowed = 10,
                wordsRequired = 1,
                presetTiles = new[]
                {
                    new PresetTile { column = 0, letter = 'D' },
                    new PresetTile { column = 1, letter = 'O' },
                    new PresetTile { column = 3, letter = 'G' }
                }
            }
        };

        GameObject canvasGo = CreateCanvas("UICanvas");
        refs.uiManager = canvasGo.AddComponent<UIManager>();
        BuildUi(canvasGo.transform as RectTransform, refs);

        GameObject gameManagerGo = new GameObject("GameManager", typeof(GameManager));
        refs.gameManager = gameManagerGo.GetComponent<GameManager>();
        refs.gameManager.boardManager = refs.boardManager;
        refs.gameManager.uiManager = refs.uiManager;
        refs.gameManager.audioManager = refs.audioManager;
        refs.gameManager.tutorialManager = refs.tutorialManager;
        refs.gameManager.powerUpManager = refs.powerUpManager;
        refs.gameManager.simulationService = refs.simulationService;

        return refs;
    }

    private static void WireScene(SceneRefs refs, Material playerOneMaterial, Material playerTwoMaterial, GameObject letterPrefab)
    {
        refs.uiManager.playerTurnColors = new[]
        {
            new Color(0.08f, 0.95f, 1f),
            new Color(1f, 0.2f, 0.75f)
        };

        refs.boardManager.uiManager = refs.uiManager;
        refs.boardManager.powerUpManager = refs.powerUpManager;

        refs.powerUpManager.uiManager = refs.uiManager;

        refs.tutorialManager.tutorialOverlay = refs.tutorialOverlayPanel;
        refs.tutorialManager.nextButton = refs.tutorialNextButton;
        refs.tutorialManager.skipButton = refs.tutorialSkipButton;

        refs.puzzleManager.objectiveText = refs.puzzleObjectiveText;
        refs.puzzleManager.movesRemainingText = refs.puzzleMovesText;
        refs.puzzleManager.puzzleCompletePanel = refs.puzzleCompletePanel;
        refs.puzzleManager.puzzleFailPanel = refs.puzzleFailPanel;
        refs.puzzleManager.puzzleCompleteText = refs.puzzleCompleteText;
        refs.puzzleManager.nextPuzzleButton = refs.puzzleNextButton;
        refs.puzzleManager.retryButton = refs.puzzleRetryButton;

        refs.uiManager.mainMenuPanel = refs.mainMenuPanel;
        refs.uiManager.gameHUDPanel = refs.gameHudPanel;
        refs.uiManager.gameOverPanel = refs.gameOverPanel;
        refs.uiManager.tutorialOverlayPanel = refs.tutorialOverlayPanel;
        refs.uiManager.playAgainButton = refs.playAgainButton;
        refs.uiManager.mainMenuButton = refs.backToMenuButton;

        refs.powerUpManager.wildcardButton = refs.wildcardButton;
        refs.powerUpManager.bombButton = refs.bombButton;
        refs.powerUpManager.swapButton = refs.swapButton;
        refs.powerUpManager.wildcardCountText = refs.wildcardCountText;
        refs.powerUpManager.bombCountText = refs.bombCountText;
        refs.powerUpManager.swapCountText = refs.swapCountText;
        refs.powerUpManager.swapSelectionPanel = refs.swapSelectionPanel;
        refs.powerUpManager.swapInstructionText = refs.swapInstructionText;

        UnityEventTools.AddPersistentListener(refs.classicButton.onClick, refs.uiManager.OnPlayClassic);
        UnityEventTools.AddPersistentListener(refs.timedButton.onClick, refs.uiManager.OnPlayTimed);
        UnityEventTools.AddPersistentListener(refs.puzzleButton.onClick, refs.uiManager.OnPlayPuzzle);
        UnityEventTools.AddPersistentListener(refs.tutorialButton.onClick, refs.uiManager.OnPlayTutorial);
        UnityEventTools.AddPersistentListener(refs.playAgainButton.onClick, refs.uiManager.OnPlayAgain);
        UnityEventTools.AddPersistentListener(refs.backToMenuButton.onClick, refs.uiManager.OnMainMenu);
    }

    private static void BuildUi(RectTransform canvas, SceneRefs refs)
    {
        TMP_FontAsset font = FindAnyFontAsset();
        GameObject safeAreaRoot = CreatePanel(canvas, "SafeAreaRoot", new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image safeAreaImage = safeAreaRoot.GetComponent<Image>();
        safeAreaImage.raycastTarget = false;
        safeAreaRoot.AddComponent<MobileSafeArea>();
        canvas = safeAreaRoot.transform as RectTransform;

        refs.mainMenuPanel = CreatePanel(canvas, "MainMenuPanel", new Color(0f, 0f, 0f, 0.72f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateText(refs.mainMenuPanel.transform as RectTransform, "Title", font, "NEON CONNECT WORDS", 62, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0f, -20f), new Vector2(900f, 100f));
        CreateText(refs.mainMenuPanel.transform as RectTransform, "Subtitle", font, "Drop letters. Build words. Chain neon combos.", 24, TextAlignmentOptions.Center, new Color(0.75f, 0.9f, 1f), new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(820f, 50f));
        refs.classicButton = CreateButton(refs.mainMenuPanel.transform as RectTransform, "ClassicButton", font, "Classic", new Vector2(0.5f, 0.55f), new Vector2(300f, 56f), new Color(0.05f, 0.55f, 0.8f, 0.95f));
        refs.timedButton = CreateButton(refs.mainMenuPanel.transform as RectTransform, "TimedButton", font, "Timed", new Vector2(0.5f, 0.47f), new Vector2(300f, 56f), new Color(0.1f, 0.8f, 0.9f, 0.95f));
        refs.puzzleButton = CreateButton(refs.mainMenuPanel.transform as RectTransform, "PuzzleButton", font, "Puzzle", new Vector2(0.5f, 0.39f), new Vector2(300f, 56f), new Color(0.85f, 0.2f, 0.7f, 0.95f));
        refs.tutorialButton = CreateButton(refs.mainMenuPanel.transform as RectTransform, "TutorialButton", font, "Tutorial", new Vector2(0.5f, 0.31f), new Vector2(300f, 56f), new Color(1f, 0.55f, 0.2f, 0.95f));

        refs.gameHudPanel = CreatePanel(canvas, "GameHUDPanel", new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        refs.uiManager.playerScoreTexts = new TextMeshProUGUI[2];
        refs.uiManager.playerScorePanels = new Image[2];
        for (int i = 0; i < 2; i++)
        {
            Color panelColor = i == 0 ? new Color(0.05f, 0.85f, 1f, 0.22f) : new Color(1f, 0.2f, 0.75f, 0.22f);
            GameObject panel = CreatePanel(refs.gameHudPanel.transform as RectTransform, "ScorePanel" + i, panelColor, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20 + (i * 180), -20), new Vector2(160f, 70f));
            refs.uiManager.playerScorePanels[i] = panel.GetComponent<Image>();
            CreateText(panel.transform as RectTransform, "Label", font, "P" + (i + 1), 20, TextAlignmentOptions.Center, Color.white, new Vector2(0.2f, 0.6f), new Vector2(0.2f, 0.6f), Vector2.zero, new Vector2(36f, 24f));
            refs.uiManager.playerScoreTexts[i] = CreateText(panel.transform as RectTransform, "Score", font, "0", 28, TextAlignmentOptions.Center, Color.white, new Vector2(0.65f, 0.5f), new Vector2(0.65f, 0.5f), Vector2.zero, new Vector2(72f, 32f));
        }

        GameObject turnPanel = CreatePanel(refs.gameHudPanel.transform as RectTransform, "TurnPanel", new Color(0.05f, 0.1f, 0.18f, 0.85f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(340f, 56f));
        refs.uiManager.turnIndicatorPanel = turnPanel.GetComponent<Image>();
        refs.uiManager.turnIndicatorText = CreateText(turnPanel.transform as RectTransform, "TurnText", font, "Player 1's Turn", 24, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 32f));

        GameObject comboPanel = CreatePanel(refs.gameHudPanel.transform as RectTransform, "ComboPanel", new Color(0.05f, 0.1f, 0.18f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(220f, 48f));
        refs.uiManager.comboMultiplierText = CreateText(comboPanel.transform as RectTransform, "ComboText", font, "x1.0", 24, TextAlignmentOptions.Center, Color.yellow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 28f));

        GameObject streakPanel = CreatePanel(refs.gameHudPanel.transform as RectTransform, "StreakPanel", new Color(0.05f, 0.1f, 0.18f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -144f), new Vector2(300f, 58f));
        refs.uiManager.streakLabel = CreateText(streakPanel.transform as RectTransform, "StreakLabel", font, "", 18, TextAlignmentOptions.Left, Color.white, new Vector2(0f, 0.75f), new Vector2(1f, 0.75f), new Vector2(10f, 0f), new Vector2(-10f, 18f));
        refs.uiManager.streakBar = CreateSlider(streakPanel.transform as RectTransform, "StreakBar", new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.44f));
        refs.uiManager.streakBar.minValue = 0f;
        refs.uiManager.streakBar.maxValue = 1f;
        refs.uiManager.streakBar.value = 0f;
        refs.uiManager.streakBar.interactable = false;

        GameObject letterPanel = CreatePanel(refs.gameHudPanel.transform as RectTransform, "CurrentLetterPanel", new Color(0.05f, 0.1f, 0.18f, 0.9f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -20f), new Vector2(180f, 120f));
        refs.uiManager.currentLetterPanel = letterPanel.GetComponent<Image>();
        CreateText(letterPanel.transform as RectTransform, "CurrentLabel", font, "Current", 20, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(120f, 24f));
        refs.uiManager.currentLetterText = CreateText(letterPanel.transform as RectTransform, "CurrentLetterText", font, "A", 54, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(120f, 64f));

        GameObject timerGroup = CreatePanel(refs.gameHudPanel.transform as RectTransform, "TimerGroup", new Color(0.05f, 0.1f, 0.18f, 0.85f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -160f), new Vector2(180f, 74f));
        refs.uiManager.timerGroup = timerGroup;
        refs.uiManager.timerText = CreateText(timerGroup.transform as RectTransform, "TimerText", font, "02:00", 28, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(140f, 26f));
        Image timerBackground = CreatePanel(timerGroup.transform as RectTransform, "TimerFillBackground", new Color(0f, 0f, 0f, 0.35f), new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.34f), Vector2.zero, Vector2.zero).GetComponent<Image>();
        timerBackground.type = Image.Type.Sliced;
        Image timerFill = CreatePanel(timerBackground.transform as RectTransform, "TimerFill", new Color(0.1f, 0.9f, 1f, 0.95f), new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero).GetComponent<Image>();
        timerFill.type = Image.Type.Filled;
        timerFill.fillMethod = Image.FillMethod.Horizontal;
        timerFill.fillOrigin = 0;
        refs.uiManager.timerFill = timerFill;

        refs.puzzleObjectiveText = CreateText(refs.gameHudPanel.transform as RectTransform, "PuzzleObjectiveText", font, "Objective", 20, TextAlignmentOptions.Left, Color.white, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 54f), new Vector2(420f, 26f));
        refs.puzzleMovesText = CreateText(refs.gameHudPanel.transform as RectTransform, "PuzzleMovesText", font, "Moves: 0", 18, TextAlignmentOptions.Left, new Color(0.75f, 0.85f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 24f), new Vector2(220f, 24f));

        GameObject messageBanner = CreatePanel(refs.gameHudPanel.transform as RectTransform, "MessageBanner", new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(520f, 52f));
        refs.uiManager.messageBannerGroup = messageBanner.AddComponent<CanvasGroup>();
        refs.uiManager.messageBannerGroup.alpha = 0f;
        refs.uiManager.messageBannerText = CreateText(messageBanner.transform as RectTransform, "MessageText", font, "", 22, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 28f));

        GameObject powerUpDock = CreatePanel(refs.gameHudPanel.transform as RectTransform, "PowerUpDock", new Color(0.04f, 0.08f, 0.16f, 0.86f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-112f, -10f), new Vector2(200f, 240f));
        CreateText(powerUpDock.transform as RectTransform, "PowerUpLabel", font, "POWER-UPS", 20, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(160f, 24f));
        refs.wildcardButton = CreateButton(powerUpDock.transform as RectTransform, "WildcardButton", font, "Wildcard", new Vector2(0.5f, 0.66f), new Vector2(150f, 42f), new Color(0.1f, 0.85f, 1f, 0.95f));
        refs.bombButton = CreateButton(powerUpDock.transform as RectTransform, "BombButton", font, "Bomb", new Vector2(0.5f, 0.46f), new Vector2(150f, 42f), new Color(1f, 0.35f, 0.25f, 0.95f));
        refs.swapButton = CreateButton(powerUpDock.transform as RectTransform, "SwapButton", font, "Swap", new Vector2(0.5f, 0.26f), new Vector2(150f, 42f), new Color(1f, 0.2f, 0.75f, 0.95f));
        refs.wildcardCountText = CreateText(powerUpDock.transform as RectTransform, "WildcardCount", font, "LOCK", 18, TextAlignmentOptions.Center, Color.white, new Vector2(0.84f, 0.66f), new Vector2(0.84f, 0.66f), Vector2.zero, new Vector2(46f, 22f));
        refs.bombCountText = CreateText(powerUpDock.transform as RectTransform, "BombCount", font, "LOCK", 18, TextAlignmentOptions.Center, Color.white, new Vector2(0.84f, 0.46f), new Vector2(0.84f, 0.46f), Vector2.zero, new Vector2(46f, 22f));
        refs.swapCountText = CreateText(powerUpDock.transform as RectTransform, "SwapCount", font, "LOCK", 18, TextAlignmentOptions.Center, Color.white, new Vector2(0.84f, 0.26f), new Vector2(0.84f, 0.26f), Vector2.zero, new Vector2(46f, 22f));

        refs.swapSelectionPanel = CreatePanel(canvas, "SwapSelectionPanel", new Color(0f, 0f, 0f, 0.68f), new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        refs.swapInstructionText = CreateText(refs.swapSelectionPanel.transform as RectTransform, "SwapInstruction", font, "Select FIRST tile to swap", 28, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(500f, 36f));
        refs.swapSelectionPanel.SetActive(false);

        refs.gameOverPanel = CreatePanel(canvas, "GameOverPanel", new Color(0f, 0f, 0f, 0.82f), new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        refs.gameOverPanel.AddComponent<CanvasGroup>().alpha = 1f;
        refs.uiManager.gameOverTitleText = CreateText(refs.gameOverPanel.transform as RectTransform, "GameOverTitle", font, "Game Over", 52, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(620f, 60f));
        refs.uiManager.gameOverScoresText = CreateText(refs.gameOverPanel.transform as RectTransform, "GameOverScores", font, "", 28, TextAlignmentOptions.Center, new Color(0.8f, 0.9f, 1f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(560f, 150f));
        refs.playAgainButton = CreateButton(refs.gameOverPanel.transform as RectTransform, "PlayAgainButton", font, "Play Again", new Vector2(0.5f, 0.35f), new Vector2(220f, 52f), new Color(0.1f, 0.85f, 1f, 0.95f));
        refs.backToMenuButton = CreateButton(refs.gameOverPanel.transform as RectTransform, "MainMenuButton", font, "Main Menu", new Vector2(0.5f, 0.26f), new Vector2(220f, 52f), new Color(0.85f, 0.2f, 0.7f, 0.95f));
        refs.gameOverPanel.SetActive(false);

        refs.tutorialOverlayPanel = CreatePanel(canvas, "TutorialOverlayPanel", new Color(0f, 0f, 0f, 0.75f), new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        refs.tutorialManager.tutorialTitleText = CreateText(refs.tutorialOverlayPanel.transform as RectTransform, "TutorialTitle", font, "Tutorial", 40, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), Vector2.zero, new Vector2(600f, 50f));
        refs.tutorialManager.tutorialBodyText = CreateText(refs.tutorialOverlayPanel.transform as RectTransform, "TutorialBody", font, "", 26, TextAlignmentOptions.Center, new Color(0.8f, 0.9f, 1f), new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(720f, 180f));
        refs.tutorialNextButton = CreateButton(refs.tutorialOverlayPanel.transform as RectTransform, "TutorialNextButton", font, "Next", new Vector2(0.5f, 0.28f), new Vector2(200f, 50f), new Color(0.1f, 0.85f, 1f, 0.95f));
        refs.tutorialSkipButton = CreateButton(refs.tutorialOverlayPanel.transform as RectTransform, "TutorialSkipButton", font, "Skip", new Vector2(0.5f, 0.2f), new Vector2(200f, 50f), new Color(0.9f, 0.4f, 0.2f, 0.95f));
        refs.tutorialOverlayPanel.SetActive(false);

        refs.puzzleCompletePanel = CreatePanel(canvas, "PuzzleCompletePanel", new Color(0f, 0f, 0f, 0.78f), new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        refs.puzzleCompleteText = CreateText(refs.puzzleCompletePanel.transform as RectTransform, "PuzzleCompleteText", font, "Puzzle Complete!", 48, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(600f, 52f));
        refs.puzzleNextButton = CreateButton(refs.puzzleCompletePanel.transform as RectTransform, "PuzzleNextButton", font, "Next Puzzle", new Vector2(0.5f, 0.42f), new Vector2(220f, 50f), new Color(0.1f, 0.85f, 1f, 0.95f));
        refs.puzzleCompletePanel.SetActive(false);

        refs.puzzleFailPanel = CreatePanel(canvas, "PuzzleFailPanel", new Color(0f, 0f, 0f, 0.78f), new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        CreateText(refs.puzzleFailPanel.transform as RectTransform, "PuzzleFailText", font, "Puzzle Failed", 48, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(600f, 52f));
        refs.puzzleRetryButton = CreateButton(refs.puzzleFailPanel.transform as RectTransform, "PuzzleRetryButton", font, "Retry", new Vector2(0.5f, 0.42f), new Vector2(220f, 50f), new Color(0.9f, 0.45f, 0.2f, 0.95f));
        refs.puzzleFailPanel.SetActive(false);
    }

    private static GameObject CreateCanvas(string name)
    {
        GameObject canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.7f;
        return canvasGo;
    }

    private static GameObject CreatePanel(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject panelGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = panelGo.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        panelGo.GetComponent<Image>().color = color;
        return panelGo;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, TMP_FontAsset font, string value, float fontSize, TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }
        tmp.text = value;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static Button CreateButton(RectTransform parent, string name, TMP_FontAsset font, string label, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;

        Image image = buttonGo.GetComponent<Image>();
        image.color = color;
        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = color * 1.08f;
        colors.pressedColor = color * 0.9f;
        colors.selectedColor = color * 1.04f;
        button.colors = colors;

        CreateText(rect, "Label", font, label, 24f, TextAlignmentOptions.Center, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static Slider CreateSlider(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.SetParent(rect, false);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.SetParent(rect, false);
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.SetParent(fillAreaRect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.1f, 0.92f, 1f, 0.95f);

        Slider slider = root.GetComponent<Slider>();
        slider.targetGraphic = background.GetComponent<Image>();
        slider.fillRect = fillRect;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Material CreateNeonMaterial(string path, Color baseColor, Color emissionColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", baseColor);
        }
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateBoardMaterial(string path, Color color)
    {
        Material material = CreateNeonMaterial(path, color, new Color(0.08f, 0.18f, 0.35f));
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.15f);
        }
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.82f);
        }
        return material;
    }

    private static Material CreateGlassMaterial(string path, Color baseColor, Color emissionColor)
    {
        Material material = CreateNeonMaterial(path, baseColor, emissionColor);
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }
        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", 2f);
        }
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.95f);
        }
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.05f);
        }
        return material;
    }

    private static GameObject CreateLetterPrefab(string path, Material defaultMaterial)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "LetterTile";
        root.transform.localScale = new Vector3(1f, 1f, 1f);

        MeshRenderer renderer = root.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = defaultMaterial;

        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.22f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0.02f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.enabled = false;

        GameObject textGo = new GameObject("LetterLabel", typeof(TextMesh));
        textGo.transform.SetParent(root.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 0f, 0.52f);
        TextMesh textMesh = textGo.GetComponent<TextMesh>();
        textMesh.text = "A";
        textMesh.fontSize = 80;
        textMesh.characterSize = 0.18f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        LetterTile letterTile = root.AddComponent<LetterTile>();
        letterTile.tileRenderer = renderer;
        letterTile.letterLabel = textMesh;
        letterTile.trailRenderer = trail;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void ApplyMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static GameObject CreateWorldPrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
        }
        else
        {
            go.transform.position = position;
            go.transform.localScale = scale;
        }
        ApplyMaterial(go, material);
        return go;
    }

    private static void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static TMP_FontAsset FindAnyFontAsset()
    {
        TMP_FontAsset projectFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontTargetPath);
        if (projectFont != null)
        {
            return projectFont;
        }

        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (font != null)
            {
                return font;
            }
        }

        return null;
    }

    private static void EnsureTmpResources()
    {
        EnsureFolder("Assets/TextMesh Pro");
        EnsureFolder("Assets/TextMesh Pro/Resources");
        EnsureFolder("Assets/TextMesh Pro/Resources/Fonts & Materials");

        if (AssetDatabase.LoadAssetAtPath<Object>(TmpSettingsTargetPath) == null)
        {
            AssetDatabase.CopyAsset(TmpSettingsSourcePath, TmpSettingsTargetPath);
        }

        if (AssetDatabase.LoadAssetAtPath<Object>(TmpFontTargetPath) == null)
        {
            AssetDatabase.CopyAsset(TmpFontSourcePath, TmpFontTargetPath);
        }
    }

    private static VolumeProfile CreateVolumeProfile(string path)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.name = "NeonArenaVolumeProfile";
        EnsurePostProcess(profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void EnsurePostProcess(VolumeProfile profile)
    {
        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(true);
        }
        bloom.active = true;
        bloom.threshold.Override(0.72f);
        bloom.intensity.Override(1.65f);
        bloom.scatter.Override(0.78f);

        if (!profile.TryGet(out Tonemapping tonemapping))
        {
            tonemapping = profile.Add<Tonemapping>(true);
        }
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        if (!profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        }
        colorAdjustments.active = true;
        colorAdjustments.postExposure.Override(0.15f);
        colorAdjustments.contrast.Override(18f);
        colorAdjustments.saturation.Override(10f);

        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>(true);
        }
        vignette.active = true;
        vignette.intensity.Override(0.24f);
        vignette.smoothness.Override(0.72f);
    }
}
