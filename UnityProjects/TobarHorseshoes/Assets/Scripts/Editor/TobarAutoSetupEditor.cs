#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TobarAutoSetupEditor
{
    private const string ConfigPath = "Assets/Config/GameConfig.asset";
    private const string PrefabPath = "Assets/Prefabs/HorseshoePrefab.prefab";

    [MenuItem("Tools/Tobar Horseshoes/Auto Setup Prototype Scene")]
    public static void AutoSetupPrototypeScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Tobar Horseshoes", "Open a scene first.", "OK");
            return;
        }

        EnsureFolders();
        EnsureTag("Stake");

        GameConfig config = LoadOrCreateConfig();

        GameObject gameRoot = FindOrCreate("GameRoot");
        GameManager gameManager = AddOrGet<GameManager>(gameRoot);
        ThrowInput throwInput = AddOrGet<ThrowInput>(gameRoot);
        ThrowController throwController = AddOrGet<ThrowController>(gameRoot);

        Transform nearStake = CreateStake("NearStake", new Vector3(0f, 0.5f, 0f));
        Transform farStake = CreateStake("FarStake", new Vector3(0f, 0.5f, config.distance25FeetMeters));
        Transform throwOrigin = CreateThrowOrigin(nearStake, farStake);

        CreateGround();
        CreateSandPit("NearPit", nearStake.position);
        CreateSandPit("FarPit", farStake.position);

        StakeTarget stakeTarget = AddOrGet<StakeTarget>(FindOrCreate("TargetStake"));
        stakeTarget.stakeTransform = farStake;
        stakeTarget.zoneCenter = farStake;

        TobarUI ui = AddOrGet<TobarUI>(FindOrCreate("UIRoot"));

        Transform spawnedParent = FindOrCreate("SpawnedShoes").transform;
        HorseshoeProjectile shoePrefab = LoadOrCreateHorseshoePrefab(config);

        gameManager.config = config;
        gameManager.throwInput = throwInput;
        gameManager.throwController = throwController;
        gameManager.ui = ui;
        gameManager.targetStake = stakeTarget;
        gameManager.nearStake = nearStake;
        gameManager.farStake = farStake;
        gameManager.throwOrigin = throwOrigin;
        gameManager.horseshoePrefab = shoePrefab;
        gameManager.spawnedShoeParent = spawnedParent;

        EditorUtility.SetDirty(gameRoot);
        EditorUtility.SetDirty(stakeTarget.gameObject);
        EditorUtility.SetDirty(ui.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog(
            "Tobar Horseshoes",
            "Auto setup complete. Press Play to test throws. Use the on-screen fallback UI if Canvas widgets are not wired.",
            "OK");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Config"))
        {
            AssetDatabase.CreateFolder("Assets", "Config");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
    }

    private static GameConfig LoadOrCreateConfig()
    {
        GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
        if (config != null)
        {
            return config;
        }

        config = ScriptableObject.CreateInstance<GameConfig>();
        AssetDatabase.CreateAsset(config, ConfigPath);
        AssetDatabase.SaveAssets();
        return config;
    }

    private static HorseshoeProjectile LoadOrCreateHorseshoePrefab(GameConfig config)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            temp.name = "HorseshoePrefab";
            temp.transform.localScale = new Vector3(0.4f, 0.12f, 0.4f);

            Rigidbody rb = AddOrGet<Rigidbody>(temp);
            AudioSource audioSource = AddOrGet<AudioSource>(temp);
            HorseshoeProjectile projectile = AddOrGet<HorseshoeProjectile>(temp);
            projectile.audioSource = audioSource;
            projectile.ApplyConfig(config);

            prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
            Object.DestroyImmediate(temp);
            AssetDatabase.SaveAssets();
        }

        HorseshoeProjectile result = prefab.GetComponent<HorseshoeProjectile>();
        if (result == null)
        {
            result = prefab.AddComponent<HorseshoeProjectile>();
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }

        return result;
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground != null)
        {
            return;
        }

        ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
    }

    private static void EnsureTag(string tag)
    {
        Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        SerializedObject serializedObject = new SerializedObject(tagManager);
        SerializedProperty tags = serializedObject.FindProperty("tags");

        for (int i = 0; i < tags.arraySize; i++)
        {
            SerializedProperty element = tags.GetArrayElementAtIndex(i);
            if (element.stringValue == tag)
            {
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        serializedObject.ApplyModifiedProperties();
    }

    private static Transform CreateStake(string name, Vector3 position)
    {
        GameObject stake = GameObject.Find(name);
        if (stake == null)
        {
            stake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stake.name = name;
        }

        stake.tag = "Stake";
        stake.transform.position = position;
        stake.transform.localScale = new Vector3(0.07f, 0.5f, 0.07f);
        return stake.transform;
    }

    private static Transform CreateThrowOrigin(Transform nearStake, Transform farStake)
    {
        GameObject origin = FindOrCreate("ThrowOrigin");
        origin.transform.position = nearStake.position + new Vector3(0f, 1f, -1.5f);
        Vector3 lookAt = farStake.position;
        lookAt.y = origin.transform.position.y;
        origin.transform.LookAt(lookAt);
        return origin.transform;
    }

    private static void CreateSandPit(string name, Vector3 stakePosition)
    {
        GameObject pit = GameObject.Find(name);
        if (pit == null)
        {
            pit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pit.name = name;
        }

        pit.transform.position = new Vector3(stakePosition.x, 0.03f, stakePosition.z);
        pit.transform.localScale = new Vector3(2f, 0.06f, 1.4f);

        Collider collider = pit.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        AddOrGet<SurfaceDampingZone>(pit);
    }

    private static GameObject FindOrCreate(string name)
    {
        GameObject existing = GameObject.Find(name);
        return existing != null ? existing : new GameObject(name);
    }

    private static T AddOrGet<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }
}
#endif
