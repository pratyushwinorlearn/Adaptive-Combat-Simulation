using UnityEngine;

public class DodgeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Dodge Settings")]
    [SerializeField, Min(0f)] private float cooldown = 0.8f;

    [Header("Animator")]
    [SerializeField] private string dodgeTrigger = "DodgeBackward";

    public bool IsDodging { get; private set; }
    public bool IsInvulnerable { get; private set; }

    /// <summary>True when a new dodge can begin.</summary>
    public bool CanDodge =>
        animator != null &&
        !IsDodging &&
        Time.time >= lastDodgeTime + cooldown;

    private float lastDodgeTime = -Mathf.Infinity;
    private int dodgeTriggerHash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        dodgeTriggerHash = Animator.StringToHash(dodgeTrigger);

        if (animator == null)
            Debug.LogWarning($"{name} DodgeController has no Animator assigned.", this);
    }

    public bool TryDodge()
    {
        if (!CanDodge)
            return false;

        lastDodgeTime = Time.time;
        IsDodging = true;
        IsInvulnerable = false;

        animator.ResetTrigger(dodgeTriggerHash);
        animator.SetTrigger(dodgeTriggerHash);
        return true;
    }

    // Called by an Animation Event at the beginning of the invulnerable window.
    public void StartDodgeInvulnerability()
    {
        if (IsDodging)
            IsInvulnerable = true;
    }

    // Called by an Animation Event at the end of the invulnerable window.
    public void EndDodgeInvulnerability()
    {
        IsInvulnerable = false;
    }

    // Called by an Animation Event at the end of the dodge animation.
    public void EndDodge()
    {
        IsInvulnerable = false;
        IsDodging = false;
    }

    /// <summary>
    /// Cancels a dodge when a hit reaction interrupts its animation.
    /// Preserves the already-earned cooldown instead of resetting it.
    /// </summary>
    public void InterruptForHitStun()
    {
        IsInvulnerable = false;
        IsDodging = false;

        if (animator != null)
            animator.ResetTrigger(dodgeTriggerHash);
    }

    public void ResetDodge()
    {
        IsDodging = false;
        IsInvulnerable = false;
        lastDodgeTime = -Mathf.Infinity;

        if (animator != null)
            animator.ResetTrigger(dodgeTriggerHash);
    }
}
