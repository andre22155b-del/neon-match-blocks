using UnityEngine;

public class StakeTarget : MonoBehaviour
{
    [Header("Stake")]
    public Transform stakeTransform;

    [Header("Ringer Zone")]
    public Transform zoneCenter;
    public float zoneRadius = 0.18f;
    public float zoneHeight = 0.32f;
    [Range(0f, 1f)] public float minOrientationAlignment = 0.6f;
    [Range(0f, 1f)] public float minUpDot = 0.45f;

    [Header("Leaner")]
    public float leanerMaxDistance = 0.22f;
    public float leanerMinHeight = 0.06f;

    private Transform ZoneReference => zoneCenter == null ? stakeTransform : zoneCenter;

    private void Reset()
    {
        if (stakeTransform == null)
        {
            stakeTransform = transform;
        }
    }

    public float HorizontalDistanceToStake(Vector3 worldPosition)
    {
        Vector3 a = worldPosition;
        Vector3 b = stakeTransform.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    public bool IsRinger(HorseshoeProjectile shoe)
    {
        if (!IsInsideCylinderZone(shoe.transform.position))
        {
            return false;
        }

        return PassesOrientationCheck(shoe.transform);
    }

    public bool IsLeaner(HorseshoeProjectile shoe)
    {
        if (!shoe.TouchedStake)
        {
            return false;
        }

        float distance = HorizontalDistanceToStake(shoe.transform.position);
        return distance <= leanerMaxDistance && shoe.transform.position.y >= leanerMinHeight;
    }

    private bool IsInsideCylinderZone(Vector3 worldPosition)
    {
        Transform zone = ZoneReference;
        Vector3 local = zone.InverseTransformPoint(worldPosition);
        float radial = new Vector2(local.x, local.z).magnitude;
        bool insideHeight = Mathf.Abs(local.y) <= zoneHeight * 0.5f;
        return radial <= zoneRadius && insideHeight;
    }

    private bool PassesOrientationCheck(Transform shoeTransform)
    {
        Vector3 toStake = stakeTransform.position - shoeTransform.position;
        toStake.y = 0f;
        if (toStake.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        toStake.Normalize();

        Vector3 shoeForward = shoeTransform.forward;
        shoeForward.y = 0f;
        if (shoeForward.sqrMagnitude > 0.0001f)
        {
            shoeForward.Normalize();
        }

        Vector3 shoeRight = shoeTransform.right;
        shoeRight.y = 0f;
        if (shoeRight.sqrMagnitude > 0.0001f)
        {
            shoeRight.Normalize();
        }

        float alignment = Mathf.Max(
            Mathf.Abs(Vector3.Dot(shoeForward, toStake)),
            Mathf.Abs(Vector3.Dot(shoeRight, toStake))
        );

        float upDot = Vector3.Dot(shoeTransform.up, Vector3.up);
        return alignment >= minOrientationAlignment && upDot >= minUpDot;
    }

    private void OnDrawGizmosSelected()
    {
        if (stakeTransform == null)
        {
            return;
        }

        Transform zone = ZoneReference;
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Vector3 center = zone.position;
        Vector3 top = center + Vector3.up * (zoneHeight * 0.5f);
        Vector3 bottom = center - Vector3.up * (zoneHeight * 0.5f);

        Gizmos.DrawWireSphere(top, zoneRadius);
        Gizmos.DrawWireSphere(bottom, zoneRadius);
        Gizmos.DrawLine(top + Vector3.forward * zoneRadius, bottom + Vector3.forward * zoneRadius);
        Gizmos.DrawLine(top - Vector3.forward * zoneRadius, bottom - Vector3.forward * zoneRadius);
        Gizmos.DrawLine(top + Vector3.right * zoneRadius, bottom + Vector3.right * zoneRadius);
        Gizmos.DrawLine(top - Vector3.right * zoneRadius, bottom - Vector3.right * zoneRadius);
    }
}
