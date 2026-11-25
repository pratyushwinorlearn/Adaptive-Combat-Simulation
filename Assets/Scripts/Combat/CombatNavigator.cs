using UnityEngine;

public class CombatNavigator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("References")]
    [SerializeField] private CombatPerception ownPerception;
    [SerializeField] private HitStunController hitStunController;

    [Header("Combat Distance")]
    [SerializeField] private float minCombatDistance = 1.8f;
    [SerializeField] private float maxCombatDistance = 3.0f;

    [Header("Turning")]
    [SerializeField, Min(1f)] private float fullTurnAngle = 45f;
    [SerializeField, Min(0f)] private float movementAngleLimit = 60f;
    [SerializeField, Min(0f)] private float turnDeadZone = 1.5f;

    [Header("Running")]
    [SerializeField] private bool allowRunning = true;
    [SerializeField] private float runDistance = 6f;

    [Header("Combat Behaviour")]
    [Tooltip("Allow forward movement while an attack animation is active.")]
    [SerializeField] private bool allowNavigationWhileAttacking = true;

    [Header("Debug")]
    [SerializeField] private bool logNavigation = false;
    [SerializeField, Min(0.1f)] private float logInterval = 0.5f;

    private float nextLogTime;

    public Transform Target => target;

    public float DistanceToTarget
    {
        get
        {
            if (target == null)
                return Mathf.Infinity;

            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }
    }

    private void Awake()
    {
        if (ownPerception == null)
            ownPerception = GetComponent<CombatPerception>();

        if (hitStunController == null)
            hitStunController = GetComponent<HitStunController>();

        if (target == null)
            Debug.LogError($"[{nameof(CombatNavigator)}] {name}: Target is not assigned.", this);

        if (ownPerception == null)
            Debug.LogWarning($"[{nameof(CombatNavigator)}] {name}: CombatPerception not found.", this);

        if (hitStunController == null)
            Debug.LogWarning($"[{nameof(CombatNavigator)}] {name}: HitStunController not found.", this);
    }

    public CombatActions GetNavigationActions()
    {
        CombatActions actions = new CombatActions();

        if (target == null)
        {
            LogNavigation("NO TARGET", 0f, 0f, false, false, false, false);
            return actions;
        }

        // Work on the ground plane so different target heights don't corrupt
        // the heading or distance calculation.
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;

        if (distance < 0.0001f)
        {
            LogNavigation("TARGET TOO CLOSE TO CALCULATE DIRECTION", distance, 0f, false, false, false, false);
            return actions;
        }

        Vector3 directionToTarget = offset / distance;
        float signedAngle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

        bool isAttacking = ownPerception != null && ownPerception.IsAttacking;
        bool isDodging = ownPerception != null && ownPerception.IsDodging;
        bool isBlocking = ownPerception != null && ownPerception.IsBlocking;
        bool isStunned = hitStunController != null && hitStunController.IsStunned;

        // IMPORTANT: turning is calculated BEFORE any combat-state movement gate.
        // This lets the fighter keep facing the opponent even if an attack, dodge,
        // or hit-stun flag is active. CharacterCombatController applies rotation
        // before its hit-stun early return.
        if (Mathf.Abs(signedAngle) > turnDeadZone)
        {
            actions.Turn = Mathf.Clamp(
                signedAngle / Mathf.Max(fullTurnAngle, 1f),
                -1f,
                1f
            );
        }

        // Combat states may suppress translation, but never the turning command.
        bool canTranslate = !isDodging && !isStunned && !isBlocking;
        if (isAttacking && !allowNavigationWhileAttacking)
            canTranslate = false;

        bool facingEnoughToMove = Mathf.Abs(signedAngle) <= movementAngleLimit;

        if (canTranslate && distance > maxCombatDistance && facingEnoughToMove)
            actions.MoveForward = true;

        if (canTranslate && allowRunning && distance > runDistance && facingEnoughToMove)
            actions.Running = true;

        if (distance < minCombatDistance)
        {
            actions.MoveForward = false;
            actions.Running = false;
        }

        string state = isStunned ? "STUNNED" : isDodging ? "DODGING" : isAttacking ? "ATTACKING" : "NORMAL";
        LogNavigation(state, distance, signedAngle, isAttacking, isDodging, isBlocking, isStunned, actions);

        return actions;
    }

    private void LogNavigation(
        string state,
        float distance,
        float angle,
        bool attacking,
        bool dodging,
        bool blocking,
        bool stunned,
        CombatActions actions = default)
    {
        if (!logNavigation || Time.unscaledTime < nextLogTime)
            return;

        nextLogTime = Time.unscaledTime + logInterval;
        Debug.Log(
            $"[{nameof(CombatNavigator)}] {name} -> target={(target != null ? target.name : "NULL")}" +
            $" | state={state} | distance={distance:F2} | angle={angle:F1}" +
            $" | turn={actions.Turn:F2} | move={actions.MoveForward} | run={actions.Running}" +
            $" | attacking={attacking} | dodging={dodging} | blocking={blocking} | stunned={stunned}" +
            $" | enabled={enabled} | active={gameObject.activeInHierarchy}", this
        );
    }
}
