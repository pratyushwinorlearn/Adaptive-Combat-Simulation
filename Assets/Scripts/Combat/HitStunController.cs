using UnityEngine;

/// <summary>
/// Applies a timed hit-stun and safely interrupts any action that would
/// otherwise remain locked if its animation is interrupted by HitReaction.
/// </summary>
public class HitStunController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CombatAttackState attackState;
    [SerializeField] private AttackTelegraph attackTelegraph;
    [SerializeField] private SwordHitbox swordHitbox;
    [SerializeField] private BlockController blockController;
    [SerializeField] private DodgeController dodgeController;

    [Header("Animator")]
    [SerializeField] private string hitReactionTrigger = "HitReaction";
    [SerializeField] private string lightAttackTrigger = "LightAttack";
    [SerializeField] private string heavyAttackTrigger = "HeavyAttack";

    [Header("Debug")]
    [SerializeField] private bool logHitStun = false;

    public bool IsStunned { get; private set; }
    private float stunEndTime;

    private int hitReactionTriggerHash;
    private int lightAttackTriggerHash;
    private int heavyAttackTriggerHash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (attackState == null)
            attackState = GetComponent<CombatAttackState>();
        if (attackTelegraph == null)
            attackTelegraph = GetComponent<AttackTelegraph>();
        if (swordHitbox == null)
            swordHitbox = GetComponentInChildren<SwordHitbox>(true);
        if (blockController == null)
            blockController = GetComponent<BlockController>();
        if (dodgeController == null)
            dodgeController = GetComponent<DodgeController>();

        hitReactionTriggerHash = Animator.StringToHash(hitReactionTrigger);
        lightAttackTriggerHash = Animator.StringToHash(lightAttackTrigger);
        heavyAttackTriggerHash = Animator.StringToHash(heavyAttackTrigger);
    }

    private void Update()
    {
        if (IsStunned && Time.time >= stunEndTime)
        {
            IsStunned = false;
            if (logHitStun)
                Debug.Log($"[HitStun] {name} stun ended; attack state is {(attackState != null ? attackState.Phase.ToString() : "missing")}.", this);
        }
    }

    public void ApplyHitStun(float duration)
    {
        if (duration <= 0f)
            return;

        stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);
        IsStunned = true;

        // A hit reaction can interrupt an attack animation before its EndAttack
        // animation event fires. Reset all associated combat state here so the
        // fighter does not remain permanently marked as attacking.
        if (swordHitbox != null)
            swordHitbox.DeactivateHitbox();

        if (attackState != null && attackState.IsAttacking)
        {
            if (logHitStun)
                Debug.Log($"[HitStun] {name} cancelling interrupted attack {attackState.CurrentAttack?.attackName ?? "unknown"} ({attackState.Phase}).", this);
            attackState.ResetAttackState();
        }

        // ResetTelegraph notifies observers if the attack was interrupted
        // during windup, avoiding a stale opponent-telegraph flag.
        if (attackTelegraph != null)
            attackTelegraph.ResetTelegraph();

        if (blockController != null)
            blockController.SetBlocking(false);

        // If HitReaction interrupts a dodge animation, its EndDodge animation
        // event may never fire. Clear the dodge flags but preserve its cooldown.
        if (dodgeController != null && dodgeController.IsDodging)
            dodgeController.InterruptForHitStun();

        if (animator != null)
        {
            animator.ResetTrigger(lightAttackTriggerHash);
            animator.ResetTrigger(heavyAttackTriggerHash);
            animator.ResetTrigger(hitReactionTriggerHash);
            animator.SetTrigger(hitReactionTriggerHash);
        }

        if (logHitStun)
            Debug.Log($"[HitStun] {name} stunned for at least {stunEndTime - Time.time:F2}s.", this);
    }

    public void ResetHitStun()
    {
        IsStunned = false;
        stunEndTime = 0f;
    }
}
