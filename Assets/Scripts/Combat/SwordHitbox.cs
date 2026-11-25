using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    [Header("Hitbox")]
    [SerializeField] private CapsuleCollider hitCollider;

    [Header("Detection")]
    [SerializeField] private LayerMask targetLayers = ~0;

    [Header("Attack Data")]
    [SerializeField] private AttackData lightAttack;
    [SerializeField] private AttackData heavyAttack;

    [Header("References")]
    [SerializeField] private Animator animator;

    private bool isActive;

    // Prevent the same target from being hit
    // multiple times during one attack.
    private readonly HashSet<Hurtbox> hitTargets =
        new HashSet<Hurtbox>();

    private void Awake()
    {
        if (hitCollider == null)
        {
            hitCollider =
                GetComponent<CapsuleCollider>();
        }

        if (animator == null)
        {
            animator =
                GetComponentInParent<Animator>();
        }

        // Keep the physical collider enabled.
        // isActive controls whether hits count.
        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }

        isActive = false;
    }

    private void LateUpdate()
    {
        if (!isActive || hitCollider == null)
            return;

        // Make Physics see the latest animated
        // sword position.
        Physics.SyncTransforms();

        DetectHits();
    }

    // --------------------------------------------------
    // CALLED BY ANIMATION EVENT
    // --------------------------------------------------

    public void ActivateHitbox()
    {
        // New attack = clear previous targets.
        hitTargets.Clear();

        isActive = true;

        Physics.SyncTransforms();

        // Check immediately when the hit window starts.
        DetectHits();
    }

    // --------------------------------------------------
    // CALLED BY ANIMATION EVENT
    // --------------------------------------------------

    public void DeactivateHitbox()
    {
        isActive = false;
    }

    // --------------------------------------------------
    // FIND CURRENT ATTACK
    // --------------------------------------------------

    private AttackData GetCurrentAttackData()
    {
        if (animator == null)
            return null;

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Armature|LightAttack"))
        {
            return lightAttack;
        }

        if (state.IsName("Armature|HeavyAttack"))
        {
            return heavyAttack;
        }

        return null;
    }

    // --------------------------------------------------
    // HIT DETECTION
    // --------------------------------------------------

    private void DetectHits()
    {
        if (!isActive || hitCollider == null)
            return;

        // --------------------------------------------------
        // SWORD COLLIDER WORLD SPACE
        // --------------------------------------------------

        Vector3 direction;

        switch (hitCollider.direction)
        {
            case 0:
                direction =
                    hitCollider.transform.right;
                break;

            case 1:
                direction =
                    hitCollider.transform.up;
                break;

            default:
                direction =
                    hitCollider.transform.forward;
                break;
        }

        float scale =
            Mathf.Max(
                Mathf.Abs(
                    hitCollider.transform.lossyScale.x
                ),
                Mathf.Abs(
                    hitCollider.transform.lossyScale.y
                ),
                Mathf.Abs(
                    hitCollider.transform.lossyScale.z
                )
            );

        float radius =
            hitCollider.radius * scale;

        float height =
            hitCollider.height * scale;

        Vector3 center =
            hitCollider.transform.TransformPoint(
                hitCollider.center
            );

        float halfLength =
            Mathf.Max(
                0f,
                height * 0.5f - radius
            );

        Vector3 point1 =
            center - direction * halfLength;

        Vector3 point2 =
            center + direction * halfLength;

        // --------------------------------------------------
        // PHYSICS QUERY
        // --------------------------------------------------

        Collider[] results =
            Physics.OverlapCapsule(
                point1,
                point2,
                radius,
                targetLayers,
                QueryTriggerInteraction.Collide
            );

        // --------------------------------------------------
        // PROCESS RESULTS
        // --------------------------------------------------

        foreach (Collider other in results)
        {
            Hurtbox hurtbox =
                other.GetComponentInParent<Hurtbox>();

            if (hurtbox == null)
                continue;

            // Ignore our own character.
            if (hurtbox.transform.root ==
                transform.root)
            {
                continue;
            }

            // One hit per target per attack.
            if (hitTargets.Contains(hurtbox))
                continue;

            AttackData attack =
                GetCurrentAttackData();

            if (attack == null)
                continue;

            // Register target as hit.
            hitTargets.Add(hurtbox);

            // Apply damage through the central
            // damage system.
            DamageSystem.ApplyDamage(
                hurtbox,
                attack,
                transform.root.gameObject
            );
        }
    }
}