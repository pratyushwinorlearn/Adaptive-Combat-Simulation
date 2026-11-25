using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CombatRootMotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform hips;

    [Header("Animation Tag")]
    [SerializeField] private string rootMotionTag = "RootMotion";

    [Header("Heavy Attack Only")]
    [Tooltip("Exact Animator state name for the forward-lunge Heavy Attack.")]
    [SerializeField] private string heavyAttackStateName = "Armature|HeavyAttack";
    [Tooltip("Preserve the furthest forward root position during Heavy Attack. Other root-motion actions, including backward dodge, keep the original hips-following behavior.")]
    [SerializeField] private bool preserveHeavyAttackLunge = true;

    [Header("Debug")]
    [SerializeField] private bool logRootMotion = false;

    private CharacterController controller;
    private bool actionActive;
    private bool currentActionIsHeavyAttack;

    private Vector3 startRootPosition;
    private Vector3 startHipsLocalPosition;
    private Vector3 startForwardDirection;
    private float furthestForwardDistance;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (hips == null)
            hips = FindDeepChild(transform, "mixamorig:Hips");

        if (animator == null)
        {
            Debug.LogError("CombatRootMotion: Could not find Animator. Assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        if (hips == null)
        {
            Debug.LogError("CombatRootMotion: Could not find mixamorig:Hips. Assign it in the Inspector.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (!enabled || animator == null || hips == null || controller == null)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        bool rootMotionAction = state.IsTag(rootMotionTag);
        bool heavyAttackState = state.IsName(heavyAttackStateName);

        // Preserve the existing behavior: stop transferring root motion as soon
        // as a RootMotion-tagged action begins transitioning to a non-tagged state.
        if (rootMotionAction && animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            if (!nextState.IsTag(rootMotionTag))
                rootMotionAction = false;
        }

        if (!rootMotionAction)
        {
            actionActive = false;
            currentActionIsHeavyAttack = false;
            return;
        }

        if (!actionActive)
        {
            actionActive = true;
            startRootPosition = transform.position;
            startHipsLocalPosition = hips.localPosition;

            startForwardDirection = transform.forward;
            startForwardDirection.y = 0f;
            if (startForwardDirection.sqrMagnitude > 0.0001f)
                startForwardDirection.Normalize();
            else
                startForwardDirection = Vector3.forward;

            furthestForwardDistance = 0f;
            currentActionIsHeavyAttack = heavyAttackState;

            if (logRootMotion)
            {
                Debug.Log(
                    $"[CombatRootMotion] {name} begin | state={state.IsName(heavyAttackStateName)} " +
                    $"heavyPreserve={currentActionIsHeavyAttack && preserveHeavyAttackLunge}", this);
            }

            return;
        }

        // Same hips-derived horizontal displacement used by the previous script.
        Vector3 animatedLocalDelta = hips.localPosition - startHipsLocalPosition;
        animatedLocalDelta.y = 0f;

        Vector3 desiredWorldDelta = hips.parent != null
            ? hips.parent.TransformVector(animatedLocalDelta)
            : transform.TransformVector(animatedLocalDelta);
        desiredWorldDelta.y = 0f;

        if (preserveHeavyAttackLunge && currentActionIsHeavyAttack)
        {
            // HeavyAttack is the only behavior changed here. The hips animation
            // may retreat during the swing/recovery; never undo forward distance
            // that this HeavyAttack has already transferred to the character root.
            float forwardDistance = Vector3.Dot(desiredWorldDelta, startForwardDirection);
            if (forwardDistance > furthestForwardDistance)
            {
                float additionalDistance = forwardDistance - furthestForwardDistance;
                Vector3 forwardStep = startForwardDirection * additionalDistance;
                CollisionFlags flags = controller.Move(forwardStep);
                furthestForwardDistance = forwardDistance;

                if (logRootMotion && additionalDistance > 0.005f)
                {
                    Debug.Log(
                        $"[CombatRootMotion] {name} HeavyAttack +{additionalDistance:F3}m " +
                        $"total={furthestForwardDistance:F3}m flags={flags}", this);
                }
            }
        }
        else
        {
            // Original behavior retained for other RootMotion-tagged actions,
            // especially Nightshade's backward dodge.
            Vector3 desiredRootPosition = startRootPosition + desiredWorldDelta;
            Vector3 rootDelta = desiredRootPosition - transform.position;
            rootDelta.y = 0f;

            if (rootDelta.sqrMagnitude > 0.000001f)
            {
                CollisionFlags flags = controller.Move(rootDelta);
                if (logRootMotion && rootDelta.sqrMagnitude > 0.0001f)
                {
                    Debug.Log(
                        $"[CombatRootMotion] {name} state={state.fullPathHash} " +
                        $"delta={rootDelta} flags={flags}", this);
                }
            }
        }

        // Retain the original horizontal hips compensation. Vertical pose remains
        // driven by the clip, which is important for the jump in HeavyAttack.
        Vector3 correctedHipsPosition = startHipsLocalPosition;
        correctedHipsPosition.y = hips.localPosition.y;
        hips.localPosition = correctedHipsPosition;
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
