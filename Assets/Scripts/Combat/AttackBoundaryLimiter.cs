using UnityEngine;

public class AttackBoundaryLimiter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform hips;

    [Header("Arena Boundary")]
    [SerializeField] private LayerMask boundaryMask;
    [SerializeField] private float probeRadius = 0.35f;
    [SerializeField] private float safetyMargin = 0.03f;

    private Vector3 previousHipsPosition;
    private bool actionActive;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (hips == null)
            hips = FindDeepChild(transform, "mixamorig:Hips");

        if (hips == null)
        {
            Debug.LogError(
                "AttackBoundaryLimiter: mixamorig:Hips not found."
            );

            enabled = false;
            return;
        }

        previousHipsPosition = hips.position;
    }

    private void LateUpdate()
    {
        if (animator == null || hips == null)
            return;

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        bool boundarySensitive =
            state.IsTag("BoundarySensitive");

        if (!boundarySensitive)
        {
            actionActive = false;
            previousHipsPosition = hips.position;
            return;
        }

        if (!actionActive)
        {
            actionActive = true;
            previousHipsPosition = hips.position;
            return;
        }

        Vector3 currentHipsPosition = hips.position;

        Vector3 movement =
            currentHipsPosition - previousHipsPosition;

        movement.y = 0f;

        float distance = movement.magnitude;

        if (distance > 0.0001f)
        {
            Vector3 direction = movement.normalized;

            if (Physics.SphereCast(
                previousHipsPosition,
                probeRadius,
                direction,
                out RaycastHit hit,
                distance + safetyMargin,
                boundaryMask,
                QueryTriggerInteraction.Ignore))
            {
                Vector3 safePosition =
                    hit.point +
                    hit.normal *
                    (probeRadius + safetyMargin);

                Vector3 correction =
                    safePosition -
                    currentHipsPosition;

                correction.y = 0f;

                if (correction.sqrMagnitude > 0.000001f)
                {
                    transform.position += correction;
                    currentHipsPosition = hips.position;
                }
            }
        }

        previousHipsPosition = currentHipsPosition;
    }

    private Transform FindDeepChild(
        Transform parent,
        string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            Transform result =
                FindDeepChild(child, targetName);

            if (result != null)
                return result;
        }

        return null;
    }
}