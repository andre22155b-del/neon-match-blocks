using System.Collections.Generic;
using UnityEngine;

public class NetEnergyNodes : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private int nodeCount = 6;
    [SerializeField] private Vector3 localBounds = new Vector3(0.35f, 0.45f, 0.35f);

    [Header("Jitter")]
    [SerializeField] private float jitterAmount = 0.05f;
    [SerializeField] private float jitterSpeed = 3f;

    [Header("Glow")]
    [SerializeField] private Color baseEmission = new Color(0.1f, 0.9f, 1f);
    [SerializeField] private Color scoreEmission = Color.red;
    [SerializeField] private float baseEmissionIntensity = 1.5f;
    [SerializeField] private float scoreEmissionIntensity = 10f;
    [SerializeField] private float scoreFlashDuration = 0.14f;
    [SerializeField] private string emissionProperty = "_EmissionColor";

    private readonly List<Transform> nodes = new List<Transform>();
    private readonly List<Vector3> baseLocalPositions = new List<Vector3>();
    private readonly List<Renderer> nodeRenderers = new List<Renderer>();

    private MaterialPropertyBlock mpb;
    private int emissionId;
    private bool isScoreFlash;
    private float scoreFlashTimer;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        emissionId = Shader.PropertyToID(emissionProperty);
    }

    private void Start()
    {
        SpawnNodes();
        ApplyEmissionToAll(baseEmission, baseEmissionIntensity);
    }

    private void Update()
    {
        if (nodes.Count == 0)
        {
            return;
        }

        float t = Time.time * jitterSpeed;
        for (int i = 0; i < nodes.Count; i++)
        {
            Transform node = nodes[i];
            if (node == null)
            {
                continue;
            }

            Vector3 basePos = baseLocalPositions[i];
            Vector3 jitter = new Vector3(
                Mathf.Sin(t + i * 1.37f),
                Mathf.Cos(t * 1.19f + i * 1.11f),
                Mathf.Sin(t * 0.91f + i * 1.83f)
            ) * jitterAmount;

            node.localPosition = basePos + jitter;
        }

        if (isScoreFlash)
        {
            scoreFlashTimer += Time.deltaTime;
            if (scoreFlashTimer >= scoreFlashDuration)
            {
                isScoreFlash = false;
                scoreFlashTimer = 0f;
                ApplyEmissionToAll(baseEmission, baseEmissionIntensity);
            }
        }
    }

    public void BurstOnScore()
    {
        isScoreFlash = true;
        scoreFlashTimer = 0f;
        ApplyEmissionToAll(scoreEmission, scoreEmissionIntensity);
    }

    private void SpawnNodes()
    {
        for (int i = 0; i < Mathf.Max(0, nodeCount); i++)
        {
            GameObject nodeObj;
            if (nodePrefab != null)
            {
                nodeObj = Instantiate(nodePrefab, transform);
            }
            else
            {
                nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.transform.SetParent(transform, false);
                nodeObj.transform.localScale = Vector3.one * 0.035f;
            }

            nodeObj.name = $"NetNode_{i:00}";
            Vector3 localPos = new Vector3(
                Random.Range(-localBounds.x, localBounds.x),
                Random.Range(-localBounds.y, localBounds.y),
                Random.Range(-localBounds.z, localBounds.z)
            );
            nodeObj.transform.localPosition = localPos;

            Renderer rend = nodeObj.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                nodeRenderers.Add(rend);
            }

            nodes.Add(nodeObj.transform);
            baseLocalPositions.Add(localPos);
        }
    }

    private void ApplyEmissionToAll(Color color, float intensity)
    {
        Color emission = color * Mathf.LinearToGammaSpace(Mathf.Max(0f, intensity));
        for (int i = 0; i < nodeRenderers.Count; i++)
        {
            Renderer rend = nodeRenderers[i];
            if (rend == null)
            {
                continue;
            }

            rend.GetPropertyBlock(mpb);
            mpb.SetColor(emissionId, emission);
            rend.SetPropertyBlock(mpb);
        }
    }
}
