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
        Block blockPrefab = LoadOrCreateBlockPrefab(cubeMat);
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

        cam.transform.position = new Vector3(0f, 7.0f, -18f);
        cam.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        cam.fieldOfView = 58f;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
        cam.clearFlags = CameraClearFlags.SolidColor;
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
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        refs.canvas = canvas;

        GameObject hud = CreateUiPanel("HUD", canvasGo.transform, new Color(0f, 0f, 0f, 0.25f));
        StretchRect(hud.GetComponent<RectTransform>());

        refs.p1TurnGlow = CreateImage("P1TurnGlow", hud.transform, new Color(0.2f, 0.95f, 1f, 0.95f));
        SetRect(refs.p1TurnGlow.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, 44f), new Vector2(120f, -42f));

        refs.p2TurnGlow = CreateImage("P2TurnGlow", hud.transform, new Color(1f, 0.45f, 0.2f, 0.45f));
        SetRect(refs.p2TurnGlow.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(160f, 44f), new Vector2(-120f, -42f));

        refs.p1ScoreText = CreateTmp("P1ScoreText", hud.transform, "P1: 0", 38, TextAlignmentOptions.MidlineLeft);
        SetRect(refs.p1ScoreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, 50f), new Vector2(28f, -30f));

        refs.p2ScoreText = CreateTmp("P2ScoreText", hud.transform, "P2: 0", 38, TextAlignmentOptions.MidlineRight);
        SetRect(refs.p2ScoreText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(280f, 50f), new Vector2(-28f, -30f));

        refs.p1ComboText = CreateTmp("P1ComboText", hud.transform, "Combo: x1", 28, TextAlignmentOptions.MidlineLeft);
        SetRect(refs.p1ComboText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(260f, 40f), new Vector2(28f, -72f));

        refs.p2ComboText = CreateTmp("P2ComboText", hud.transform, "Combo: x1", 28, TextAlignmentOptions.MidlineRight);
        SetRect(refs.p2ComboText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(260f, 40f), new Vector2(-28f, -72f));

        refs.turnText = CreateTmp("TurnText", hud.transform, "Turn: PLAYER 1", 34, TextAlignmentOptions.Center);
        SetRect(refs.turnText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(440f, 50f), new Vector2(0f, -30f));

        refs.levelText = CreateTmp("LevelText", hud.transform, "Level 1 / 20", 30, TextAlignmentOptions.Center);
        SetRect(refs.levelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(280f, 46f), new Vector2(0f, -74f));

        refs.timerText = CreateTmp("TimerText", hud.transform, "Time: 30", 30, TextAlignmentOptions.Center);
        SetRect(refs.timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(240f, 46f), new Vector2(0f, -112f));

        refs.reflexPanel = CreateUiPanel("ReflexPanel", canvasGo.transform, new Color(0.02f, 0.06f, 0.12f, 0.88f));
        SetRect(refs.reflexPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800f, 420f), new Vector2(0f, 0f));

        refs.reflexCountdownText = CreateTmp("ReflexCountdownText", refs.reflexPanel.transform, "3", 120, TextAlignmentOptions.Center);
        SetRect(refs.reflexCountdownText.rectTransform, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(300f, 140f), new Vector2(0f, 0f));

        refs.reflexResultText = CreateTmp("ReflexResultText", refs.reflexPanel.transform, "", 42, TextAlignmentOptions.Center);
        SetRect(refs.reflexResultText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(520f, 90f), new Vector2(0f, 0f));

        refs.reflexLeftButton = CreateButton("ReflexLeftButton", refs.reflexPanel.transform, "PLAYER 1 TAP", new Vector2(220f, 90f), new Vector2(-200f, -90f));
        refs.reflexRightButton = CreateButton("ReflexRightButton", refs.reflexPanel.transform, "PLAYER 2 TAP", new Vector2(220f, 90f), new Vector2(200f, -90f));

        refs.reflexLeftFlash = CreateImage("LeftFlash", refs.reflexLeftButton.transform, new Color(0.1f, 0.95f, 1f, 0.25f));
        StretchRect(refs.reflexLeftFlash.rectTransform);
        refs.reflexRightFlash = CreateImage("RightFlash", refs.reflexRightButton.transform, new Color(1f, 0.55f, 0.25f, 0.25f));
        StretchRect(refs.reflexRightFlash.rectTransform);

        refs.levelResultPanel = CreateUiPanel("LevelResultPanel", canvasGo.transform, new Color(0.02f, 0.06f, 0.12f, 0.90f));
        SetRect(refs.levelResultPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(880f, 480f), new Vector2(0f, 0f));

        refs.resultTitle = CreateTmp("ResultTitleText", refs.levelResultPanel.transform, "LEVEL COMPLETE", 64, TextAlignmentOptions.Center);
        SetRect(refs.resultTitle.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(760f, 120f), new Vector2(0f, 0f));

        refs.resultBody = CreateTmp("ResultBodyText", refs.levelResultPanel.transform, "Winner: PLAYER 1", 38, TextAlignmentOptions.Center);
        SetRect(refs.resultBody.rectTransform, new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f), new Vector2(760f, 260f), new Vector2(0f, 0f));

        refs.floatingText = CreateTmp("FloatingTextPrefab", canvasGo.transform, "COMBO x2", 40, TextAlignmentOptions.Center);
        SetRect(refs.floatingText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 80f), new Vector2(0f, 0f));
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
        SetMaterialColor(mat, "_BaseColor", new Color(0.06f, 0.08f, 0.16f));
        SetMaterialColor(mat, "_Color", new Color(0.06f, 0.08f, 0.16f));
        SetMaterialColor(mat, "_EmissionColor", new Color(0.0f, 0.7f, 1.0f) * 1.8f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(mat, CubeMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static Block LoadOrCreateBlockPrefab(Material cubeMat)
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

        GameObject faceRoot = new GameObject("FaceRoot");
        faceRoot.transform.SetParent(visualRoot.transform);
        faceRoot.transform.localPosition = new Vector3(0f, 0f, 0.501f);
        faceRoot.transform.localRotation = Quaternion.identity;
        faceRoot.transform.localScale = Vector3.one * 0.8f;

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
        SetField(gameManager, "playVsAI", true);
        SetField(gameManager, "maxLevels", 20);
        SetField(gameManager, "blockPrefab", blockPrefab);
        SetField(gameManager, "boardRoot", boardRoot);
        SetField(gameManager, "boardCenter", Vector3.zero);
        SetField(gameManager, "blockSpacing", 1.45f);
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
        image.color = new Color(0.10f, 0.12f, 0.20f, 0.95f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.20f, 0.25f, 0.35f, 1f);
        colors.pressedColor = new Color(0.05f, 0.08f, 0.14f, 1f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;

        RectTransform rect = go.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, anchoredPos);

        TextMeshProUGUI text = CreateTmp("Label", go.transform, label, 28, TextAlignmentOptions.Center);
        StretchRect(text.rectTransform);
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
