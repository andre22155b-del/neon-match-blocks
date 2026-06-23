using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SurfaceDampingZone : MonoBehaviour
{
    public float extraLinearDamping = 2f;
    public float extraAngularDamping = 0.75f;

    private readonly Dictionary<Rigidbody, Vector2> originalValues = new Dictionary<Rigidbody, Vector2>();

    private void OnValidate()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        HorseshoeProjectile shoe = other.GetComponentInParent<HorseshoeProjectile>();
        if (shoe == null)
        {
            return;
        }

        Rigidbody rb = shoe.Body;
        if (rb == null || originalValues.ContainsKey(rb))
        {
            return;
        }

#if UNITY_6000_0_OR_NEWER
        originalValues.Add(rb, new Vector2(rb.linearDamping, rb.angularDamping));
        rb.linearDamping += extraLinearDamping;
        rb.angularDamping += extraAngularDamping;
#else
        originalValues.Add(rb, new Vector2(rb.drag, rb.angularDrag));
        rb.drag += extraLinearDamping;
        rb.angularDrag += extraAngularDamping;
#endif
    }

    private void OnTriggerExit(Collider other)
    {
        HorseshoeProjectile shoe = other.GetComponentInParent<HorseshoeProjectile>();
        if (shoe == null)
        {
            return;
        }

        Rigidbody rb = shoe.Body;
        if (rb == null)
        {
            return;
        }

        if (!originalValues.TryGetValue(rb, out Vector2 original))
        {
            return;
        }

#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = original.x;
        rb.angularDamping = original.y;
#else
        rb.drag = original.x;
        rb.angularDrag = original.y;
#endif
        originalValues.Remove(rb);
    }
}
