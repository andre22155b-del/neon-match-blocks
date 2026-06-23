using System.Collections;
using UnityEngine;

public class OrbitalRings : MonoBehaviour
{
    [Header("Ring References")]
    [SerializeField] private Transform topRing;
    [SerializeField] private Transform bottomRing;

    [Header("Motion")]
    [SerializeField] private float rotateSpeed = 50f;
    [SerializeField] private float burstMultiplier = 2.2f;
    [SerializeField] private float burstDuration = 0.35f;

    private Coroutine burstRoutine;
    private float currentSpeed;

    private void Awake()
    {
        currentSpeed = rotateSpeed;
    }

    private void Update()
    {
        float delta = currentSpeed * Time.deltaTime;
        if (topRing != null)
        {
            topRing.Rotate(Vector3.up * delta, Space.Self);
        }

        if (bottomRing != null)
        {
            bottomRing.Rotate(Vector3.down * delta, Space.Self);
        }
    }

    public void BoostOnScore()
    {
        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
        }

        burstRoutine = StartCoroutine(SpeedBurst());
    }

    private IEnumerator SpeedBurst()
    {
        float t = 0f;
        float start = currentSpeed;
        float peak = rotateSpeed * Mathf.Max(1f, burstMultiplier);

        while (t < burstDuration)
        {
            t += Time.deltaTime;
            float a = t / burstDuration;
            currentSpeed = Mathf.Lerp(start, peak, a);
            yield return null;
        }

        t = 0f;
        start = currentSpeed;
        while (t < burstDuration)
        {
            t += Time.deltaTime;
            float a = t / burstDuration;
            currentSpeed = Mathf.Lerp(start, rotateSpeed, a);
            yield return null;
        }

        currentSpeed = rotateSpeed;
        burstRoutine = null;
    }
}
