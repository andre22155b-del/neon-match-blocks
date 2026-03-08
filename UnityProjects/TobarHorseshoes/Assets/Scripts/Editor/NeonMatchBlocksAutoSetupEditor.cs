#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class NeonMatchBlocksAutoSetupEditor
{
    private const string RootFolder = "Assets/NeonMatchBlocks";
    private const string PrefabsFolder = RootFolder + "/Prefabs";
    private const string ParticlesFolder = PrefabsFolder + "/Particles";
    private const string ArtFolder = RootFolder + "/Art";
    private const string MaterialsFolder = RootFolder + "/Materials";
    private const string ScenesFolder = "Assets/Scenes";

    private const string BlockPrefabPath = PrefabsFolder + "/NeonCube.prefab";
    private const string ScenePath = ScenesFolder + "/NeonMatchBlocks.unity";
    private const string CubeMaterialPath = MaterialsFolder + "/NeonCube.mat";
    private const string CubeGlowMaterialPath = MaterialsFolder + "/NeonCubeGlow.mat";
    private const string FloorBaseMaterialPath = MaterialsFolder + "/NeonFloorBase.mat";
    private const string FloorGridMaterialPath = MaterialsFolder + "/NeonFloorGrid.mat";
    private const string BackdropMaterialPath = MaterialsFolder + "/NeonBackdrop.mat";
    private const string AccentBlueMaterialPath = MaterialsFolder + "/NeonAccentBlue.mat";
    private const string AccentPinkMaterialPath = MaterialsFolder + "/NeonAccentPink.mat";

    [MenuItem("Tools/Neon Match Blocks/Auto Setup Complete Scene")]
    public static void AutoSetupCompleteScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AutoSetupCore(showDialog: true);
    }

    [MenuItem("Tools/Neon Match Blocks/Import TMP Essentials (Silent)")]
    public static void ImportTmpEssentialsSilent()
    {
        TMP_PackageResourceImporter.ImportResources(importEssentials: true, importExamples: false, interactive: false);
        AssetDatabase.Refresh();
        CloseTmpImporterWindows();
        Debug.Log("TMP Essential Resources imported silently.");
    }

    // Batch-mode entry point (use with -executeMethod).
    public static void AutoSetupCompleteSceneBatch()
    {
        AutoSetupCore(showDialog: false);
    }

    private static void AutoSetupCore(bool showDialog)
    {
        EnsureFolder(ScenesFolder);
        EnsureFolder(RootFolder);
        EnsureFolder(PrefabsFolder);
        EnsureFolder(ParticlesFolder);
        EnsureFolder(ArtFolder);
        EnsureFolder(MaterialsFolder);
        AssetDatabase.Refresh();
        ImportTmpEssentialsSilent();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        SetupCamera();
        
        GameObject boardRoot = new GameObject("BoardRoot");
        BuildEnvironment(boardRoot.transform);

        GameObject managers = new GameObject("Managers");
        GameObject gameManagerGo = new GameObject("GameManagerCompetitive");
        GameObject uiManagerGo = new GameObject("UIManager");
        GameObject soundManagerGo = new GameObject("SoundManager");
        GameObject particleManagerGo = new GameObject("ParticleManager");

        gameManagerGo.transform.SetParent(managers.transform);
        uiManagerGo.transform.SetParent(managers.transform);
        soundManagerGo.transform.SetParent(managers.transform);
        particleManagerGo.transform.SetParent(managers.transform);

        GameManagerCompetitive gameManager = gameManagerGo.AddComponent<GameManagerCompetitive>();
        UIManager uiManager = uiManagerGo.AddComponent<UIManager>();
        SoundManager soundManager = soundManagerGo.AddComponent<SoundManager>();
        ParticleManager particleManager = particleManagerGo.AddComponent<ParticleManager>();

        AudioSource sfxSource = soundManagerGo.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        EnsureEventSystem();

        UiRefs ui = BuildUi();
        Material cubeMat = LoadOrCreateCubeMaterial();
        Material cubeGlowMat = LoadOrCreateGlowMaterial();
        Block blockPrefab = LoadOrCreateBlockPrefab(cubeMat, cubeGlowMat);
        List<Sprite> sportsSprites = LoadOrCreateSportsSprites();

        ParticleSystem normalFx = LoadOrCreateParticlePrefab("PS_Match_Normal", new Color(0.20f, 0.95f, 1.00f), 1.0f, 20);
        ParticleSystem rareFx = LoadOrCreateParticlePrefab("PS_Match_Rare", new Color(0.35f, 1.00f, 0.45f), 1.2f, 28);
        ParticleSystem legendaryFx = LoadOrCreateParticlePrefab("PS_Match_Legendary", new Color(1.00f, 0.60f, 0.20f), 1.45f, 38);
        ParticleSystem ringFx = LoadOrCreateParticlePrefab("PS_Ring", new Color(0.50f, 0.90f, 1.00f), 1.3f, 24);
        ParticleSystem wrongFx = LoadOrCreateParticlePrefab("PS_Wrong", new Color(1.00f, 0.12f, 0.45f), 0.9f, 14);
        ParticleSystem comboFx = LoadOrCreateParticlePrefab("PS_Combo", new Color(1.00f, 1.00f, 0.25f), 1.0f, 16);
        ParticleSystem completeFx = LoadOrCreateParticlePrefab("PS_LevelComplete", new Color(0.95f, 0.45f, 1.00f), 1.7f, 60);

        ConfigureGameManager(gameManager, blockPrefab, boardRoot.transform, sportsSprites);
        ConfigureUiManager(uiManager, ui);
        ConfigureSoundManager(soundManager, sfxSource);
        ConfigureParticleManager(particleManager, normalFx, rareFx, legendaryFx, ringFx, wrongFx, comboFx, completeFx);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Neon Match Blocks",
                "Auto setup complete.\n\nScene created: Assets/Scenes/NeonMatchBlocks.unity\nYou can press Play now (with placeholder art/audio), then swap in your final assets.",
                "OK");
        }
        else
        {
            Debug.Log("Neon Match Blocks auto setup complete (batch mode).");
        }
    }

    private static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }

        cam.transform.position = new Vector3(0f, 9.5f, -10.5f);
        cam.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
        cam.fieldOfView = 48f;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void BuildEnvironment(Transform boardRoot)
    {
        Material floorBase = LoadOrCreateNeonMaterial(FloorBaseMaterialPath, new Color(0.02f, 0.02f, 0.08f), new Color(0.03f, 0.05f, 0.18f) * 1.2f, 0.2f, 0.95f);
        Material floorGrid = LoadOrCreateNeonMaterial(FloorGridMaterialPath, new Color(0.12f, 0.06f, 0.24f), new Color(0.96f, 0.22f, 1.0f) * 2.5f, 0.05f, 0.9f);
        Material backdrop = LoadOrCreateNeonMaterial(BackdropMaterialPath, new Color(0.03f, 0.04f, 0.12f), new Color(0.06f, 0.16f, 0.32f) * 1.3f, 0.1f, 0.8f);
        Material accentBlue = LoadOrCreateNeonMaterial(AccentBlueMaterialPath, new Color(0.08f, 0.20f, 0.36f), new Color(0.16f, 0.95f, 1.0f) * 3.0f, 0.0f, 0.85f);
        Material accentPink = LoadOrCreateNeonMaterial(AccentPinkMaterialPath, new Color(0.24f, 0.08f, 0.32f), new Color(1.0f, 0.25f, 0.92f) * 3.0f, 0.0f, 0.85f);

        GameObject environment = new GameObject("Environment");

        GameObject backdropWall = CreateSceneBlock("BackdropWall", environment.transform, new Vector3(0f, 5.2f, 18.5f), new Vector3(30f, 10f, 0.35f), backdrop);
        CreateSceneBlock("HorizonGlow", environment.transform, new Vector3(0f, 0.15f, 12f), new Vector3(24f, 0.07f, 0.2f), accentPink);
        CreateSceneBlock("FloorBase", environment.transform, new Vector3(0f, -0.82f, 7f), new Vector3(28f, 0.1f, 28f), floorBase);

        for (int i = -14; i <= 14; i++)
        {
            float x = i * 1.3f;
            float z = 7f + i * 0.65f;
            CreateSceneBlock("FloorLineX_" + i, environment.transform, new Vector3(x, -0.77f, 7f), new Vector3(0.02f, 0.012f, 24f), i % 4 == 0 ? accentBlue : floorGrid);
            CreateSceneBlock("FloorLineZ_" + i, environment.transform, new Vector3(0f, -0.77f, z), new Vector3(24f, 0.012f, 0.02f), i % 4 == 0 ? accentBlue : floorGrid);
        }

        CreateSceneBlock("LeftTower", environment.transform, new Vector3(-10f, 3.4f, 15f), new Vector3(0.25f, 6.8f, 0.25f), accentBlue);
        CreateSceneBlock("RightTower", environment.transform, new Vector3(10f, 3.4f, 15f), new Vector3(0.25f, 6.8f, 0.25f), accentPink);
        CreateSceneBlock("LeftTowerCross", environment.transform, new Vector3(-10f, 5.1f, 15f), new Vector3(2.4f, 0.08f, 0.08f), accentBlue);
        CreateSceneBlock("RightTowerCross", environment.transform, new Vector3(10f, 4.4f, 15f), new Vector3(2.8f, 0.08f, 0.08f), accentPink);
        CreateSceneBlock("BoardAnchor", boardRoot, Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f), accentBlue).SetActive(false);

        CreateSceneLight("BlueWash", environment.transform, new Vector3(-4.5f, 5.5f, -1.5f), new Color(0.15f, 0.85f, 1f), 18f, 1.6f);
        CreateSceneLight("PinkWash", environment.transform, new Vector3(4.5f, 5f, 1.5f), new Color(1f, 0.28f, 0.82f), 18f, 1.45f);
        CreateSceneLight("WarmCore", environment.transform, new Vector3(0f, 3.2f, 3.5f), new Color(1f, 0.64f, 0.22f), 14f, 0.75f);

        Light[] sceneLights = Object.FindObjectsOfType<Light>();
        for (int i = 0; i < sceneLights.Length; i++)
        {
            Light mainLight = sceneLights[i];
            if (mainLight == null || mainLight.type != LightType.Directional)
            {
                continue;
            }

            mainLight.intensity = 0.9f;
            mainLight.color = new Color(0.72f, 0.82f, 1f);
            mainLight.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            break;
        }

        if (backdropWall != null)
        {
            backdropWall.transform.position += new Vector3(0f, 0f, 0f);
        }
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindObjectOfType<EventSystem>();
        if (existing == null)
        {
            GameObject es = new GameObject("EventSystem");
            existing = es.AddComponent<EventSystem>();
        }

        ConfigureInputModule(existing);
    }

    private static void ConfigureInputModule(EventSystem eventSystem)
    {
        if (eventSystem == null)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacy != null)
        {
            legacy.enabled = false;
        }

        TouchInputModule touch = eventSystem.GetComponent<TouchInputModule>();
        if (touch != null)
        {
            touch.enabled = false;
        }

        InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        inputSystemModule.enabled = true;
#else
        StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacy == null)
        {
            legacy = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        legacy.enabled = true;
#endif
    }

    private static void CloseTmpImporterWindows()
    {
        EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        for (int i = 0; i < windows.Length; i++)
        {
            EditorWindow win = windows[i];
            if (win == null || win.titleContent == null)
            {
                continue;
            }

            if (win.titleContent.text == "TMP Importer")
            {
                win.Close();
            }
        }
    }

    private static UiRefs BuildUi()
    {
        UiRefs refs = new UiRefs();

        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440f, 2560f);
        scaler.matchWidthOrHeight = 1f;

        refs.canvas = canvas;

        GameObject hud = new GameObject("HUD", typeof(RectTransform));
        hud.transform.SetParent(canvasGo.transform, false);
        StretchRect(hud.GetComponent<RectTransform>());

        Color cyan = new Color(0.25f, 0.95f, 1f, 1f);
        Color pink = new Color(1f, 0.34f, 0.88f, 1f);
        Color panelColor = new Color(0.03f, 0.05f, 0.12f, 0.74f);

        GameObject titleGroup = new GameObject("TitleGroup", typeof(RectTransform));
        titleGroup.transform.SetParent(hud.transform, false);
        SetRect(titleGroup.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 260f), new Vector2(0f, -132f));

        TextMeshProUGUI neonText = CreateTmp("TitleNeon", titleGroup.transform, "NEON", 104, TextAlignmentOptions.Top);
        SetRect(neonText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 88f), new Vector2(0f, 0f));
        neonText.color = cyan;
        neonText.fontStyle = FontStyles.Bold;
        neonText.characterSpacing = 4f;

        TextMeshProUGUI matchText = CreateTmp("TitleMatch", titleGroup.transform, "MATCH", 88, TextAlignmentOptions.Top);
        SetRect(matchText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 82f), new Vector2(0f, -82f));
        matchText.color = pink;
        matchText.fontStyle = FontStyles.Bold;
        matchText.characterSpacing = 3f;

        TextMeshProUGUI blocksText = CreateTmp("TitleBlocks", titleGroup.transform, "BLOCKS", 74, TextAlignmentOptions.Top);
        SetRect(blocksText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 70f), new Vector2(0f, -156f));
        blocksText.color = new Color(0.98f, 0.76f, 1f, 1f);
        blocksText.fontStyle = FontStyles.Bold;
        blocksText.characterSpacing = 5f;

        TextMeshProUGUI subtitleText = CreateTmp("TitleSubtitle", titleGroup.transform, "3D ARCADE MEMORY DUEL", 26, TextAlignmentOptions.Top);
        SetRect(subtitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 34f), new Vector2(0f, -224f));
        subtitleText.color = new Color(0.72f, 0.90f, 1f, 0.82f);
        subtitleText.characterSpacing = 4f;

        GameObject p1Panel = CreateUiPanel("P1Panel", hud.transform, panelColor);
        SetRect(p1Panel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(298f, 118f), new Vector2(164f, -372f));
        refs.p1TurnGlow = CreateImage("P1TurnGlow", p1Panel.transform, new Color(0.2f, 0.95f, 1f, 0.95f));
        StretchRect(refs.p1TurnGlow.rectTransform);
        refs.p1TurnGlow.rectTransform.offsetMin = new Vector2(8f, 8f);
        refs.p1TurnGlow.rectTransform.offsetMax = new Vector2(-8f, -8f);
        refs.p1ScoreText = CreateTmp("P1ScoreText", p1Panel.transform, "PLAYER 1\n0", 38, TextAlignmentOptions.TopLeft);
        SetRect(refs.p1ScoreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(240f, 72f), new Vector2(22f, -12f));
        refs.p1ScoreText.color = cyan;
        refs.p1ScoreText.fontStyle = FontStyles.Bold;
        refs.p1ComboText = CreateTmp("P1ComboText", p1Panel.transform, "COMBO x1", 26, TextAlignmentOptions.BottomLeft);
        SetRect(refs.p1ComboText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220f, 36f), new Vector2(24f, 18f));
        refs.p1ComboText.color = new Color(0.78f, 0.96f, 1f, 0.9f);

        GameObject p2Panel = CreateUiPanel("P2Panel", hud.transform, panelColor);
        SetRect(p2Panel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(298f, 118f), new Vector2(-164f, -372f));
        refs.p2TurnGlow = CreateImage("P2TurnGlow", p2Panel.transform, new Color(1f, 0.45f, 0.2f, 0.45f));
        StretchRect(refs.p2TurnGlow.rectTransform);
        refs.p2TurnGlow.rectTransform.offsetMin = new Vector2(8f, 8f);
        refs.p2TurnGlow.rectTransform.offsetMax = new Vector2(-8f, -8f);
        refs.p2ScoreText = CreateTmp("P2ScoreText", p2Panel.transform, "PLAYER 2\n0", 38, TextAlignmentOptions.TopRight);
        SetRect(refs.p2ScoreText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(240f, 72f), new Vector2(-22f, -12f));
        refs.p2ScoreText.color = new Color(1f, 0.58f, 0.28f, 1f);
        refs.p2ScoreText.fontStyle = FontStyles.Bold;
        refs.p2ComboText = CreateTmp("P2ComboText", p2Panel.transform, "COMBO x1", 26, TextAlignmentOptions.BottomRight);
        SetRect(refs.p2ComboText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(220f, 36f), new Vector2(-24f, 18f));
        refs.p2ComboText.color = new Color(1f, 0.86f, 0.72f, 0.92f);

        GameObject centerPanel = CreateUiPanel("CenterPanel", hud.transform, new Color(0.04f, 0.06f, 0.14f, 0.82f));
        SetRect(centerPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(380f, 120f), new Vector2(0f, -322f));
        refs.turnText = CreateTmp("TurnText", centerPanel.transform, "TURN: PLAYER 1", 30, TextAlignmentOptions.Top);
        SetRect(refs.turnText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(320f, 34f), new Vector2(0f, -14f));
        refs.turnText.color = new Color(0.95f, 0.98f, 1f, 1f);
        refs.turnText.fontStyle = FontStyles.Bold;
        refs.levelText = CreateTmp("LevelText", centerPanel.transform, "LEVEL 1 / 20", 26, TextAlignmentOptions.Center);
        SetRect(refs.levelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 34f), new Vector2(0f, 4f));
        refs.levelText.color = pink;
        refs.levelText.fontStyle = FontStyles.Bold;
        refs.timerText = CreateTmp("TimerText", centerPanel.transform, "TIME 30", 24, TextAlignmentOptions.Bottom);
        SetRect(refs.timerText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 30f), new Vector2(0f, 16f));
        refs.timerText.color = cyan;

        refs.reflexPanel = CreateUiPanel("ReflexGlow", canvasGo.transform, new Color(0.04f, 0.05f, 0.16f, 0.92f));
        SetRect(refs.reflexPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 480f), Vector2.zero);
        GameObject reflexContent = CreateUiPanel("ReflexPanel", refs.reflexPanel.transform, new Color(0.02f, 0.06f, 0.12f, 0.96f));
        StretchRect(reflexContent.GetComponent<RectTransform>());
        refs.reflexCountdownText = CreateTmp("ReflexCountdownText", reflexContent.transform, "3", 132, TextAlignmentOptions.Center);
        SetRect(refs.reflexCountdownText.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(340f, 140f), Vector2.zero);
        refs.reflexCountdownText.color = cyan;
        refs.reflexCountdownText.fontStyle = FontStyles.Bold;
        refs.reflexResultText = CreateTmp("ReflexResultText", reflexContent.transform, "", 40, TextAlignmentOptions.Center);
        SetRect(refs.reflexResultText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(620f, 84f), Vector2.zero);
        refs.reflexResultText.color = new Color(1f, 0.82f, 0.96f, 1f);
        refs.reflexLeftButton = CreateButton("ReflexLeftButton", reflexContent.transform, "PLAYER 1 TAP", new Vector2(250f, 96f), new Vector2(-220f, -94f));
        refs.reflexRightButton = CreateButton("ReflexRightButton", reflexContent.transform, "PLAYER 2 TAP", new Vector2(250f, 96f), new Vector2(220f, -94f));
        refs.reflexLeftFlash = CreateImage("LeftFlash", refs.reflexLeftButton.transform, new Color(0.1f, 0.95f, 1f, 0.25f));
        StretchRect(refs.reflexLeftFlash.rectTransform);
        refs.reflexRightFlash = CreateImage("RightFlash", refs.reflexRightButton.transform, new Color(1f, 0.55f, 0.25f, 0.25f));
        StretchRect(refs.reflexRightFlash.rectTransform);

        refs.levelResultPanel = CreateUiPanel("ResultGlow", canvasGo.transform, new Color(0.04f, 0.05f, 0.16f, 0.92f));
        SetRect(refs.levelResultPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(980f, 560f), Vector2.zero);
        GameObject resultContent = CreateUiPanel("LevelResultPanel", refs.levelResultPanel.transform, new Color(0.02f, 0.06f, 0.12f, 0.96f));
        StretchRect(resultContent.GetComponent<RectTransform>());
        refs.resultTitle = CreateTmp("ResultTitleText", resultContent.transform, "LEVEL COMPLETE", 72, TextAlignmentOptions.Center);
        SetRect(refs.resultTitle.rectTransform, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(820f, 110f), Vector2.zero);
        refs.resultTitle.color = cyan;
        refs.resultTitle.fontStyle = FontStyles.Bold;
        refs.resultBody = CreateTmp("ResultBodyText", resultContent.transform, "Winner: PLAYER 1", 40, TextAlignmentOptions.Center);
        SetRect(refs.resultBody.rectTransform, new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f), new Vector2(820f, 270f), Vector2.zero);
        refs.resultBody.color = new Color(1f, 0.88f, 0.96f, 1f);

        refs.floatingText = CreateTmp("FloatingTextPrefab", canvasGo.transform, "COMBO x2", 46, TextAlignmentOptions.Center);
        SetRect(refs.floatingText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 80f), new Vector2(0f, 0f));
        refs.floatingText.color = pink;
        refs.floatingText.fontStyle = FontStyles.Bold;
        refs.floatingText.gameObject.SetActive(false);

        refs.reflexPanel.SetActive(false);
        refs.levelResultPanel.SetActive(false);

        return refs;
    }

    private static Material LoadOrCreateCubeMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(CubeMaterialPath);
        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        mat = new Material(shader);
        SetMaterialColor(mat, "_BaseColor", new Color(0.04f, 0.06f, 0.14f));
        SetMaterialColor(mat, "_Color", new Color(0.04f, 0.06f, 0.14f));
        SetMaterialColor(mat, "_EmissionColor", new Color(0.05f, 0.35f, 0.65f) * 1.65f);
        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", 0.2f);
        }

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", 0.92f);
        }

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(mat, CubeMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static Material LoadOrCreateGlowMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(CubeGlowMaterialPath);
        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        mat = new Material(shader);
        SetMaterialColor(mat, "_BaseColor", new Color(0.0f, 0.72f, 1.0f));
        SetMaterialColor(mat, "_Color", new Color(0.0f, 0.72f, 1.0f));
        SetMaterialColor(mat, "_EmissionColor", new Color(0.15f, 0.95f, 1.0f) * 3.5f);
        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", 0.05f);
        }

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", 0.95f);
        }

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(mat, CubeGlowMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static Material LoadOrCreateNeonMaterial(string path, Color baseColor, Color emissionColor, float metallic, float smoothness)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        mat = new Material(shader);
        SetMaterialColor(mat, "_BaseColor", baseColor);
        SetMaterialColor(mat, "_Color", baseColor);
        SetMaterialColor(mat, "_EmissionColor", emissionColor);
        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", metallic);
        }

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", smoothness);
        }

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static Block LoadOrCreateBlockPrefab(Material cubeMat, Material cubeGlowMat)
    {
        GameObject root = new GameObject("NeonCube");
        root.AddComponent<BoxCollider>();
        Block block = root.AddComponent<Block>();

        GameObject visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(root.transform);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "CubeMesh";
        cube.transform.SetParent(visualRoot.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = Vector3.one;

        Collider cubeCollider = cube.GetComponent<Collider>();
        if (cubeCollider != null)
        {
            Object.DestroyImmediate(cubeCollider);
        }

        MeshRenderer cubeRenderer = cube.GetComponent<MeshRenderer>();
        if (cubeRenderer != null && cubeMat != null)
        {
            cubeRenderer.sharedMaterial = cubeMat;
        }

        GameObject glowShell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glowShell.name = "GlowShell";
        glowShell.transform.SetParent(visualRoot.transform);
        glowShell.transform.localPosition = Vector3.zero;
        glowShell.transform.localRotation = Quaternion.identity;
        glowShell.transform.localScale = Vector3.one * 1.08f;
        Collider glowCollider = glowShell.GetComponent<Collider>();
        if (glowCollider != null)
        {
            Object.DestroyImmediate(glowCollider);
        }

        MeshRenderer glowRenderer = glowShell.GetComponent<MeshRenderer>();
        if (glowRenderer != null && cubeGlowMat != null)
        {
            glowRenderer.sharedMaterial = cubeGlowMat;
            glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;
        }

        GameObject faceRoot = new GameObject("FaceRoot");
        faceRoot.transform.SetParent(visualRoot.transform);
        faceRoot.transform.localPosition = new Vector3(0f, 0f, 0.501f);
        faceRoot.transform.localRotation = Quaternion.identity;
        faceRoot.transform.localScale = Vector3.one * 0.86f;

        SpriteRenderer faceSprite = faceRoot.AddComponent<SpriteRenderer>();
        faceSprite.color = Color.white;
        faceSprite.sortingOrder = 1;
        faceRoot.SetActive(false);

        GameObject glow = new GameObject("GlowLight");
        glow.transform.SetParent(visualRoot.transform);
        glow.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        Light glowLight = glow.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.range = 2.5f;
        glowLight.intensity = 1.2f;
        glowLight.color = new Color(0.15f, 0.9f, 1f);

        SetField(block, "visualRoot", visualRoot.transform);
        SetField(block, "faceRoot", faceRoot);
        SetField(block, "faceSpriteRenderer", faceSprite);
        SetField(block, "faceMeshRenderer", null);
        SetField(block, "edgeGlowRenderer", cubeRenderer);
        SetField(block, "glowLight", glowLight);

        GameObject prefabObj = PrefabUtility.SaveAsPrefabAsset(root, BlockPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        return prefabObj != null ? prefabObj.GetComponent<Block>() : null;
    }

    private static List<Sprite> LoadOrCreateSportsSprites()
    {
        List<Sprite> sprites = new List<Sprite>
        {
            CreateSportsSprite("Soccer", new Color(0.95f, 0.95f, 0.95f), new Color(0.1f, 0.1f, 0.12f)),
            CreateSportsSprite("Basketball", new Color(0.95f, 0.45f, 0.15f), new Color(0.22f, 0.12f, 0.05f)),
            CreateSportsSprite("Football", new Color(0.55f, 0.26f, 0.12f), new Color(0.92f, 0.92f, 0.92f)),
            CreateSportsSprite("Baseball", new Color(0.98f, 0.98f, 0.98f), new Color(0.9f, 0.16f, 0.16f)),
            CreateSportsSprite("Boxing", new Color(0.86f, 0.08f, 0.18f), new Color(0.96f, 0.82f, 0.10f)),
            CreateSportsSprite("Tennis", new Color(0.66f, 0.94f, 0.20f), new Color(0.06f, 0.25f, 0.08f)),
            CreateSportsSprite("Hockey", new Color(0.12f, 0.12f, 0.15f), new Color(0.6f, 0.94f, 1.0f)),
            CreateSportsSprite("Rugby", new Color(0.18f, 0.48f, 0.20f), new Color(0.98f, 0.98f, 0.98f))
        };

        sprites.RemoveAll(s => s == null);
        return sprites;
    }

    private static Sprite CreateSportsSprite(string name, Color baseColor, Color accentColor)
    {
        string filePath = ArtFolder + "/" + name + ".png";
        string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        const int size = 128;

        if (!File.Exists(absolutePath))
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color bg = new Color(0.03f, 0.06f, 0.1f, 1f);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.42f;
            float innerRadius = radius * 0.78f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    Color c = bg;

                    if (d <= radius)
                    {
                        c = baseColor;
                        if (d <= innerRadius)
                        {
                            float t = Mathf.InverseLerp(innerRadius, 0f, d);
                            c = Color.Lerp(baseColor, Color.white, t * 0.15f);
                        }
                    }

                    float line = Mathf.Abs((x - y)) < 2 || Mathf.Abs((x + y) - size) < 2 ? 1f : 0f;
                    if (line > 0f && d <= radius * 0.95f)
                    {
                        c = Color.Lerp(c, accentColor, 0.85f);
                    }

                    if (x < 3 || y < 3 || x > size - 4 || y > size - 4)
                    {
                        c = new Color(0f, 0.95f, 1f, 1f);
                    }

                    texture.SetPixel(x, y, c);
                }
            }

            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            File.WriteAllBytes(absolutePath, png);
        }

        AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
    }

    private static ParticleSystem LoadOrCreateParticlePrefab(string name, Color color, float scale, int burstCount)
    {
        string path = ParticlesFolder + "/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing.GetComponent<ParticleSystem>();
        }

        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null)
        {
            particleShader = Shader.Find("Sprites/Default");
        }

        renderer.material = new Material(particleShader);

        var main = ps.main;
        main.duration = 0.65f;
        main.startLifetime = 0.45f;
        main.startSpeed = 3.5f;
        main.startSize = 0.09f * scale;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f * scale;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(Color.white, 0.7f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        return prefab != null ? prefab.GetComponent<ParticleSystem>() : null;
    }

    private static void ConfigureGameManager(
        GameManagerCompetitive gameManager,
        Block blockPrefab,
        Transform boardRoot,
        List<Sprite> sportsFaces)
    {
        SetSerializedBool(gameManager, "playVsAI", true);
        SetSerializedInt(gameManager, "maxLevels", 20);
        SetSerializedObjectReference(gameManager, "blockPrefab", blockPrefab);
        SetSerializedObjectReference(gameManager, "boardRoot", boardRoot);
        SetSerializedVector3(gameManager, "boardCenter", new Vector3(0f, -0.3f, 0f));
        SetSerializedFloat(gameManager, "blockSpacing", 1.52f);
        SetObjectReferenceListField(gameManager, "sportsFaces", sportsFaces);
    }

    private static void ConfigureUiManager(UIManager uiManager, UiRefs ui)
    {
        SetField(uiManager, "canvas", ui.canvas);
        SetField(uiManager, "worldCamera", Camera.main);
        SetField(uiManager, "p1ScoreText", ui.p1ScoreText);
        SetField(uiManager, "p2ScoreText", ui.p2ScoreText);
        SetField(uiManager, "p1ComboText", ui.p1ComboText);
        SetField(uiManager, "p2ComboText", ui.p2ComboText);
        SetField(uiManager, "turnText", ui.turnText);
        SetField(uiManager, "levelText", ui.levelText);
        SetField(uiManager, "timerText", ui.timerText);
        SetField(uiManager, "p1TurnGlow", ui.p1TurnGlow);
        SetField(uiManager, "p2TurnGlow", ui.p2TurnGlow);
        SetField(uiManager, "reflexPanel", ui.reflexPanel);
        SetField(uiManager, "reflexCountdownText", ui.reflexCountdownText);
        SetField(uiManager, "reflexResultText", ui.reflexResultText);
        SetField(uiManager, "reflexLeftButton", ui.reflexLeftButton);
        SetField(uiManager, "reflexRightButton", ui.reflexRightButton);
        SetField(uiManager, "reflexLeftFlash", ui.reflexLeftFlash);
        SetField(uiManager, "reflexRightFlash", ui.reflexRightFlash);
        SetField(uiManager, "floatingTextPrefab", ui.floatingText);
        SetField(uiManager, "levelResultPanel", ui.levelResultPanel);
        SetField(uiManager, "levelResultTitleText", ui.resultTitle);
        SetField(uiManager, "levelResultBodyText", ui.resultBody);
    }

    private static void ConfigureSoundManager(SoundManager soundManager, AudioSource audioSource)
    {
        SetField(soundManager, "sfxSource", audioSource);

        List<SoundManager.ClipEntry> entries = new List<SoundManager.ClipEntry>
        {
            new SoundManager.ClipEntry { id = "Flip", volume = 1f },
            new SoundManager.ClipEntry { id = "Match", volume = 1f },
            new SoundManager.ClipEntry { id = "Wrong", volume = 1f },
            new SoundManager.ClipEntry { id = "Combo", volume = 1f },
            new SoundManager.ClipEntry { id = "LevelComplete", volume = 1f },
            new SoundManager.ClipEntry { id = "Countdown", volume = 1f },
            new SoundManager.ClipEntry { id = "Tap", volume = 1f }
        };

        SetField(soundManager, "clips", entries);
    }

    private static void ConfigureParticleManager(
        ParticleManager particleManager,
        ParticleSystem normal,
        ParticleSystem rare,
        ParticleSystem legendary,
        ParticleSystem ring,
        ParticleSystem wrong,
        ParticleSystem combo,
        ParticleSystem complete)
    {
        SetField(particleManager, "normalMatchFx", normal);
        SetField(particleManager, "rareMatchFx", rare);
        SetField(particleManager, "legendaryMatchFx", legendary);
        SetField(particleManager, "ringFx", ring);
        SetField(particleManager, "wrongFx", wrong);
        SetField(particleManager, "comboFx", combo);
        SetField(particleManager, "levelCompleteFx", complete);
    }

    private static GameObject CreateSceneBlock(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            Object.DestroyImmediate(col);
        }

        return go;
    }

    private static Light CreateSceneLight(string name, Transform parent, Vector3 position, Color color, float range, float intensity)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        return light;
    }

    private static GameObject CreateUiPanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateTmp(
        string name,
        Transform parent,
        string text,
        int fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = new Color(0.94f, 0.98f, 1f);
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.10f, 0.12f, 0.22f, 0.96f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.18f, 0.24f, 0.40f, 1f);
        colors.pressedColor = new Color(0.06f, 0.08f, 0.16f, 1f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;

        RectTransform rect = go.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, anchoredPos);

        TextMeshProUGUI text = CreateTmp("Label", go.transform, label, 28, TextAlignmentOptions.Center);
        StretchRect(text.rectTransform);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 2f;
        text.color = new Color(0.86f, 0.96f, 1f, 1f);
        return button;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size,
        Vector2 anchoredPosition)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
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

    private static void SetMaterialColor(Material mat, string property, Color value)
    {
        if (mat != null && mat.HasProperty(property))
        {
            mat.SetColor(property, value);
        }
    }

    private static void SetField(object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            Debug.LogWarning("NeonMatchBlocksAutoSetupEditor: Could not find field " + fieldName + " on " + target.GetType().Name);
            return;
        }

        field.SetValue(target, value);
        if (target is Object unityObj)
        {
            EditorUtility.SetDirty(unityObj);
        }
    }

    private static void SetObjectReferenceListField(Object target, string fieldName, IList<Sprite> values)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null || !property.isArray)
        {
            Debug.LogWarning("NeonMatchBlocksAutoSetupEditor: Could not find array field " + fieldName + " on " + target.GetType().Name);
            return;
        }

        property.arraySize = values != null ? values.Count : 0;
        for (int i = 0; i < property.arraySize; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedBool(Object target, string fieldName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning("NeonMatchBlocksAutoSetupEditor: Could not find bool field " + fieldName + " on " + target.GetType().Name);
            return;
        }

        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedInt(Object target, string fieldName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning("NeonMatchBlocksAutoSetupEditor: Could not find int field " + fieldName + " on " + target.GetType().Name);
            return;
        }

        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedFloat(Object target, string fieldName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning("NeonMatchBlocksAutoSetupEditor: Could not find float field " + fieldName + " on " + target.GetType().Name);
            return;
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedVector3(Object target, string fieldName, Vector3 value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning("NeonMatchBlocksAutoSetupEditor: Could not find Vector3 field " + fieldName + " on " + target.GetType().Name);
            return;
        }

        property.vector3Value = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedObjectReference(Object target, string fieldName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning("NeonMatchBlocksAutoSetupEditor: Could not find object field " + fieldName + " on " + target.GetType().Name);
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private struct UiRefs
    {
        public Canvas canvas;
        public TextMeshProUGUI p1ScoreText;
        public TextMeshProUGUI p2ScoreText;
        public TextMeshProUGUI p1ComboText;
        public TextMeshProUGUI p2ComboText;
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI timerText;
        public Image p1TurnGlow;
        public Image p2TurnGlow;
        public GameObject reflexPanel;
        public TextMeshProUGUI reflexCountdownText;
        public TextMeshProUGUI reflexResultText;
        public Button reflexLeftButton;
        public Button reflexRightButton;
        public Image reflexLeftFlash;
        public Image reflexRightFlash;
        public TextMeshProUGUI floatingText;
        public GameObject levelResultPanel;
        public TextMeshProUGUI resultTitle;
        public TextMeshProUGUI resultBody;
    }
}
#endif
