using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GushyBallController : MonoBehaviour
{
    private const string SceneName = "NeonConnectWords";
    private const string BallName = "GushyBall";
    private const float BaseEmission = 2.4f;
    private const float ClickEmission = 5f;
    private const float WobbleDuration = 0.5f;
    private const float HoverAmplitude = 0.08f;
    private const float HoverSpeed = 1.7f;

    private static readonly Vector3 SpawnPosition = new Vector3(0f, 0.18f, 1.38f);
    private static readonly Vector3 SpawnScale = new Vector3(1.35f, 1.12f, 1.35f);
    private static readonly Color[] Palette =
    {
        new Color(0.66f, 0.31f, 1f),
        new Color(0.84f, 0.41f, 1f),
        new Color(0.96f, 0.39f, 0.96f),
        new Color(0.56f, 0.48f, 1f),
        new Color(0.72f, 0.56f, 1f)
    };

    private Material shellMaterial;
    private Material coreMaterial;
    private Light glowLight;
    private Vector3 restScale;
    private Vector3 restPosition;
    private float restRadius;
    private float wobbleTimeRemaining;
    private float glowPulse;
    private int colorIndex;
    private Color currentColor;
    private Color targetColor;
    private float hoverOffset;

    public int CurrentColorIndex => colorIndex;
    public Color CurrentVisualColor => currentColor;
    public Color TargetVisualColor => targetColor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSpawner()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TrySpawnInScene(SceneManager.GetActiveScene());
    }

    private void Awake()
    {
        restScale = transform.localScale;
        restPosition = transform.position;
        restRadius = restScale.y * 0.5f;

        currentColor = Palette[0];
        targetColor = currentColor;

        ConfigureShell();
        CreateCore();
        CreateGlowLight();
        ApplyPalette(currentColor, BaseEmission);
    }

    private void Update()
    {
        HandlePointerInput();
        AnimateSquish();
        AnimateGlow();
    }

    private void OnDestroy()
    {
        DisposeMaterial(shellMaterial);
        DisposeMaterial(coreMaterial);
    }

    private void HandlePointerInput()
    {
        if (Input.touchCount <= 0)
        {
            return;
        }

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Ended)
        {
            TryInteractAtScreenPosition(touch.position, touch.fingerId);
        }
    }

    private void CycleColor()
    {
        colorIndex = (colorIndex + 1) % Palette.Length;
        targetColor = Palette[colorIndex];
        wobbleTimeRemaining = WobbleDuration;
        glowPulse = 1f;
    }

    public bool TryInteractAtScreenPosition(Vector2 screenPosition, int pointerId = -1)
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return false;
        }

        Ray ray = activeCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f) && hitInfo.collider != null && hitInfo.collider.gameObject == gameObject)
        {
            CycleColor();
            return true;
        }

        return false;
    }

    private void OnMouseDown()
    {
        CycleColor();
    }

    private void AnimateSquish()
    {
        wobbleTimeRemaining = Mathf.Max(0f, wobbleTimeRemaining - Time.deltaTime);

        float idlePulse = Mathf.Sin(Time.time * 2.2f) * 0.028f;
        float wobbleStrength = wobbleTimeRemaining / WobbleDuration;
        float wobbleWave = wobbleStrength > 0f
            ? Mathf.Sin((1f - wobbleStrength) * Mathf.PI * 4f) * wobbleStrength * 0.16f
            : 0f;

        float widthScale = Mathf.Clamp(1f + idlePulse + wobbleWave, 0.84f, 1.24f);
        float heightScale = Mathf.Clamp(1f - (idlePulse * 0.55f) - (wobbleWave * 1.35f), 0.78f, 1.28f);
        hoverOffset = Mathf.Sin(Time.time * HoverSpeed) * HoverAmplitude;

        transform.localScale = new Vector3(
            restScale.x * widthScale,
            restScale.y * heightScale,
            restScale.z * widthScale);

        // Keep the bottom of the ball planted while it squishes and stretches.
        float currentRadius = transform.localScale.y * 0.5f;
        transform.position = new Vector3(
            restPosition.x,
            restPosition.y - (restRadius - currentRadius) + hoverOffset,
            restPosition.z);

        transform.rotation = Quaternion.Euler(
            Mathf.Sin(Time.time * 1.5f) * 2.2f,
            Time.time * 18f,
            Mathf.Sin(Time.time * 1.2f) * 3f);
    }

    private void AnimateGlow()
    {
        currentColor = Color.Lerp(currentColor, targetColor, 1f - Mathf.Exp(-7f * Time.deltaTime));
        glowPulse = Mathf.MoveTowards(glowPulse, 0f, Time.deltaTime * 2.6f);

        float emission = Mathf.Lerp(BaseEmission, ClickEmission, glowPulse);
        ApplyPalette(currentColor, emission);

        if (glowLight != null)
        {
            glowLight.color = currentColor;
            glowLight.intensity = Mathf.Lerp(1.6f, 3.2f, glowPulse);
        }
    }

    private void ConfigureShell()
    {
        MeshRenderer shellRenderer = GetComponent<MeshRenderer>();
        if (shellRenderer == null)
        {
            return;
        }

        shellMaterial = CreateLitMaterial();
        if (shellMaterial != null)
        {
            shellRenderer.material = shellMaterial;
        }
    }

    private void CreateCore()
    {
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "GushyBallCore";
        core.transform.SetParent(transform, false);
        core.transform.localPosition = new Vector3(-0.1f, 0.12f, 0.18f);
        core.transform.localScale = new Vector3(0.42f, 0.34f, 0.42f);

        Collider coreCollider = core.GetComponent<Collider>();
        if (coreCollider != null)
        {
            coreCollider.enabled = false;
            Destroy(coreCollider);
        }

        MeshRenderer coreRenderer = core.GetComponent<MeshRenderer>();
        if (coreRenderer == null)
        {
            return;
        }

        coreMaterial = CreateLitMaterial();
        if (coreMaterial != null)
        {
            coreRenderer.material = coreMaterial;
        }
    }

    private void CreateGlowLight()
    {
        GameObject glow = new GameObject("GushyBallGlow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        glowLight = glow.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.range = 6.25f;
        glowLight.intensity = 1.8f;
        glowLight.shadows = LightShadows.None;
    }

    private void ApplyPalette(Color color, float emissionStrength)
    {
        ApplyMaterialColor(shellMaterial, new Color(color.r, color.g, color.b, 0.82f), emissionStrength);
        ApplyMaterialColor(coreMaterial, Color.Lerp(color, Color.white, 0.45f), emissionStrength * 0.95f);
    }

    private static Material CreateLitMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);
        ConfigureGellySurface(material);

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

    private static void ConfigureGellySurface(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.shader.name == "Universal Render Pipeline/Lit")
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.SetFloat("_ZWrite", 0f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }

    private static void ApplyMaterialColor(Material material, Color color, float emissionStrength)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emissionStrength);
        }
    }

    private static void DisposeMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySpawnInScene(scene);
    }

    private static void TrySpawnInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName)
        {
            return;
        }

        if (GameObject.Find(BallName) != null)
        {
            return;
        }

        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        SceneManager.MoveGameObjectToScene(ball, scene);
        ball.name = BallName;
        ball.transform.position = SpawnPosition;
        ball.transform.localScale = SpawnScale;
        ball.AddComponent<GushyBallController>();
    }
}
