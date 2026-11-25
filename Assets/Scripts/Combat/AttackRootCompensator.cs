using UnityEngine;

public class AttackRootCompensator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform hips;

    [Header("Attack States")]
    [SerializeField] private string lightAttackState = "Armature|LightAttack";
    [SerializeField] private string heavyAttackState = "Armature|HeavyAttack";

    private Vector3 originalRootPosition;
    private Vector3 originalHipsLocalPosition;
    private bool compensating;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (hips == null)
        {
            hips = FindDeepChild(transform, "mixamorig:Hips");
        }

        if (hips == null)
        {
            Debug.LogError("AttackRootCompensator: Could not find mixamorig:Hips.");
        }
    }

    private void LateUpdate()
    {
        if (animator == null || hips == null)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        bool attackPlaying =
            state.IsName(lightAttackState) ||
            state.IsName(heavyAttackState);

        if (attackPlaying)
        {
            if (!compensating)
            {
                originalRootPosition = transform.position;
                originalHipsLocalPosition = hips.localPosition;
                compensating = true;
            }

            // How far the animation has translated the Hips.
            Vector3 localDelta =
                hips.localPosition - originalHipsLocalPosition;

            // Convert the Hips movement into world-space.
            Vector3 worldDelta =
                hips.parent.TransformVector(localDelta);

            // Only compensate horizontal movement.
            worldDelta.y = 0f;

            // Move the character root in the opposite direction.
            transform.position =
                originalRootPosition - worldDelta;
        }
        else if (compensating)
        {
            // Attack is over.
            transform.position = originalRootPosition;
            compensating = false;
        }
    }

    private Transform FindDeepChild(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            Transform result = FindDeepChild(child, targetName);

            if (result != null)
                return result;
        }

        return null;
    }
}