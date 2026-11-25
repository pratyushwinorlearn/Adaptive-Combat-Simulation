using UnityEngine;

/// <summary>
/// Scripted sparring opponent. It only reacts to plausible threats, prioritizes
/// defense during a dangerous telegraph, and falls back to blocking if a dodge
/// is unavailable. It may cancel its own attack during windup/recovery to defend.
/// </summary>
public class TrainingOpponentController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterCombatController combatController;
    [SerializeField] private AIActionController aiActionController;
    [SerializeField] private CombatPerception ownPerception;
    [SerializeField] private CombatAttackState ownAttackState;
    [SerializeField] private AttackTelegraph ownTelegraph;
    [SerializeField] private SwordHitbox ownSwordHitbox;
    [SerializeField] private Animator animator;
    [SerializeField] private CombatOpponentObserver opponentObserver;
    [SerializeField] private CombatEpisodeManager episodeManager;
    [SerializeField] private HitStunController hitStunController;
    [SerializeField] private DodgeController dodgeController;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float preferredDistance = 2.5f;
    [SerializeField, Min(0f)] private float attackRange = 2.8f;

    [Header("Decision")]
    [SerializeField, Min(0.02f)] private float decisionInterval = 0.12f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.9f;

    [Header("Attack Selection")]
    [Range(0f, 1f)]
    [SerializeField] private float heavyAttackChance = 0.30f;

    [Header("Defense Threat Check")]
    [Tooltip("Maximum distance at which a Light Attack telegraph is considered a threat.")]
    [SerializeField, Min(0f)] private float defenseReactionRange = 3.0f;

    [Tooltip("Heavy attacks may lunge farther, so give their telegraph a separate reaction range. Tune to the actual lunge reach.")]
    [SerializeField, Min(0f)] private float heavyDefenseReactionRange = 4.5f;

    [Tooltip("Require the opponent to be facing reasonably toward this fighter. 0 = sideways is acceptable; 1 = directly facing.")]
    [SerializeField, Range(-1f, 1f)] private float minimumOpponentFacingDot = 0.35f;
    [SerializeField] private bool requireOpponentFacingMe = true;

    [Header("Defense")]
    [SerializeField] private bool blockLightAttacks = true;
    [SerializeField] private bool dodgeHeavyAttacks = true;
    [Tooltip("If a heavy-attack dodge is unavailable, block instead of standing idle.")]
    [SerializeField] private bool blockHeavyAttacksWhenDodgeUnavailable = true;

    [Header("Animation Parameters")]
    [SerializeField] private string lightAttackTrigger = "LightAttack";
    [SerializeField] private string heavyAttackTrigger = "HeavyAttack";

    [Header("Debug")]
    [SerializeField] private bool logDecisions = false;
    [SerializeField] private bool logDecisionTrace = false;
    [SerializeField, Min(0.1f)] private float decisionTraceInterval = 0.5f;

    private float nextDecisionTime;
    private float nextAttackTime;
    private float nextDecisionTraceTime;
    private bool dodgeRequestedForCurrentTelegraph;
    private bool wasOpponentTelegraphing;

    private void Awake()
    {
        if (combatController == null) combatController = GetComponent<CharacterCombatController>();
        if (aiActionController == null) aiActionController = GetComponent<AIActionController>();
        if (ownPerception == null) ownPerception = GetComponent<CombatPerception>();
        if (ownAttackState == null) ownAttackState = GetComponent<CombatAttackState>();
        if (ownTelegraph == null) ownTelegraph = GetComponent<AttackTelegraph>();
        if (ownSwordHitbox == null) ownSwordHitbox = GetComponentInChildren<SwordHitbox>(true);
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (opponentObserver == null) opponentObserver = GetComponent<CombatOpponentObserver>();
        if (hitStunController == null) hitStunController = GetComponent<HitStunController>();
        if (dodgeController == null) dodgeController = GetComponent<DodgeController>();
        if (episodeManager == null) episodeManager = FindAnyObjectByType<CombatEpisodeManager>();

        // This scripted opponent must not also read keyboard controls.
        if (combatController != null)
            combatController.SetPlayerInputEnabled(false);

        if (aiActionController == null)
            Debug.LogError($"{name}: TrainingOpponentController needs an AIActionController.", this);
        if (opponentObserver == null)
            Debug.LogError($"{name}: TrainingOpponentController needs a CombatOpponentObserver.", this);
        if (dodgeHeavyAttacks && dodgeController == null)
            Debug.LogWarning($"{name}: dodgeHeavyAttacks is enabled but no DodgeController was found.", this);
    }

    private void OnEnable()
    {
        if (episodeManager != null)
            episodeManager.OnEpisodeReset += HandleEpisodeReset;
    }

    private void OnDisable()
    {
        if (episodeManager != null)
            episodeManager.OnEpisodeReset -= HandleEpisodeReset;
    }

    private void Start() => ResetDecisionTimers();

    private void Update()
    {
        if (aiActionController == null || opponentObserver == null)
            return;

        bool telegraphing = opponentObserver.IsOpponentTelegraphing;
        if (!telegraphing || !wasOpponentTelegraphing)
            dodgeRequestedForCurrentTelegraph = false;
        wasOpponentTelegraphing = telegraphing;

        if (Time.time >= nextDecisionTraceTime)
        {
            if (logDecisionTrace)
                PrintDecisionTrace();
            nextDecisionTraceTime = Time.time + decisionTraceInterval;
        }

        if (Time.time < nextDecisionTime)
            return;

        nextDecisionTime = Time.time + decisionInterval;
        aiActionController.ApplyAction(DecideAction());
    }

    private void PrintDecisionTrace()
    {
        float distance = opponentObserver != null ? opponentObserver.GetDistanceToOpponent() : -1f;
        float facingDot = opponentObserver != null ? opponentObserver.GetOpponentFacingDot() : -1f;
        bool stunned = hitStunController != null && hitStunController.IsStunned;
        bool dodging = ownPerception != null && ownPerception.IsDodging;
        bool attacking = ownAttackState != null ? ownAttackState.IsAttacking : ownPerception != null && ownPerception.IsAttacking;
        bool telegraphing = opponentObserver != null && opponentObserver.IsOpponentTelegraphing;
        bool cooldownReady = Time.time >= nextAttackTime;
        bool threat = IsTelegraphThreatening(distance);

        Debug.Log($"{name} TRAINING TRACE | Distance={distance:F2} | OpponentFacingDot={facingDot:F2} | " +
                  $"Stunned={stunned} | Dodging={dodging} | Attacking={attacking} | " +
                  $"Telegraphing={telegraphing} | Threat={threat} | AttackCooldownReady={cooldownReady}", this);
    }

    private CombatActions DecideAction()
    {
        CombatActions actions = new CombatActions();
        Vector3 directionToOpponent = opponentObserver.GetDirectionToOpponent();
        float distance = opponentObserver.GetDistanceToOpponent();
        actions.Turn = CalculateTurn(directionToOpponent);

        if (hitStunController != null && hitStunController.IsStunned)
            return actions;

        if (ownPerception != null && ownPerception.IsDodging)
            return actions;

        // A dangerous incoming telegraph gets priority over starting a new attack.
        // If our own attack is still in windup or recovery, cancel it so defense
        // can run. Do not cancel in Active phase, where the swing is already live.
        if (opponentObserver.IsOpponentTelegraphing && IsTelegraphThreatening(distance))
        {
            if (IsOwnAttacking())
            {
                if (ownAttackState != null && ownAttackState.Phase != CombatAttackState.AttackPhase.Active)
                {
                    InterruptOwnNonActiveAttack();
                }
                else
                {
                    return actions;
                }
            }

            return DecideDefense(actions);
        }

        // Finish an attack already in its active phase before issuing another one.
        if (IsOwnAttacking())
            return actions;

        if (distance > preferredDistance)
        {
            actions.MoveForward = true;
            return actions;
        }

        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            bool useHeavy = Random.value < heavyAttackChance;
            actions.LightAttack = !useHeavy;
            actions.HeavyAttack = useHeavy;
            nextAttackTime = Time.time + attackCooldown;

            if (logDecisions)
                Debug.Log($"{name} TRAINING OPPONENT ATTACK | {(useHeavy ? "HeavyAttack" : "LightAttack")}", this);
            return actions;
        }

        if (distance > attackRange)
            actions.MoveForward = true;

        return actions;
    }

    private bool IsOwnAttacking()
    {
        if (ownAttackState != null)
            return ownAttackState.IsAttacking;
        return ownPerception != null && ownPerception.IsAttacking;
    }

    private void InterruptOwnNonActiveAttack()
    {
        if (ownSwordHitbox != null)
            ownSwordHitbox.DeactivateHitbox();

        if (ownAttackState != null && ownAttackState.IsAttacking)
            ownAttackState.ResetAttackState();

        if (ownTelegraph != null)
            ownTelegraph.ResetTelegraph();

        if (animator != null)
        {
            animator.ResetTrigger(lightAttackTrigger);
            animator.ResetTrigger(heavyAttackTrigger);
        }

        if (logDecisions)
            Debug.Log($"{name} interrupted its non-active attack to defend.", this);
    }

    private bool IsTelegraphThreatening(float distance)
    {
        if (opponentObserver == null || !opponentObserver.IsOpponentTelegraphing)
            return false;

        AttackData attack = opponentObserver.OpponentTelegraphedAttack;
        if (attack == null)
            return false;

        float allowedRange = attack.attackType == AttackData.AttackType.Heavy
            ? heavyDefenseReactionRange
            : defenseReactionRange;

        if (distance > allowedRange)
            return false;

        if (requireOpponentFacingMe && opponentObserver.GetOpponentFacingDot() < minimumOpponentFacingDot)
            return false;

        return true;
    }

    private CombatActions DecideDefense(CombatActions actions)
    {
        if (opponentObserver.IsOpponentTelegraphingHeavyAttack())
        {
            bool canDodge = dodgeHeavyAttacks &&
                            !dodgeRequestedForCurrentTelegraph &&
                            dodgeController != null &&
                            dodgeController.CanDodge;

            if (canDodge)
            {
                actions.Dodge = true;
                dodgeRequestedForCurrentTelegraph = true;
                if (logDecisions)
                    Debug.Log($"{name} TRAINING OPPONENT DODGE | Threatening heavy telegraph", this);
            }
            else if (blockHeavyAttacksWhenDodgeUnavailable)
            {
                // Previously, a heavy telegraph with dodge on cooldown returned
                // an empty action, making the opponent appear to do nothing.
                actions.Block = true;
                if (logDecisions)
                    Debug.Log($"{name} TRAINING OPPONENT BLOCK | Dodge unavailable during heavy telegraph", this);
            }

            return actions;
        }

        if (opponentObserver.IsOpponentTelegraphingLightAttack() && blockLightAttacks)
        {
            actions.Block = true;
            if (logDecisions)
                Debug.Log($"{name} TRAINING OPPONENT BLOCK | Threatening light telegraph", this);
        }

        return actions;
    }

    private float CalculateTurn(Vector3 directionToOpponent)
    {
        if (directionToOpponent.sqrMagnitude < 0.0001f)
            return 0f;

        float signedAngle = Vector3.SignedAngle(transform.forward, directionToOpponent, Vector3.up);
        return Mathf.Clamp(signedAngle / 45f, -1f, 1f);
    }

    private void HandleEpisodeReset()
    {
        ResetDecisionTimers();
        dodgeRequestedForCurrentTelegraph = false;
        wasOpponentTelegraphing = false;
        if (aiActionController != null)
            aiActionController.ResetAction();
        if (logDecisions)
            Debug.Log($"{name} TRAINING OPPONENT RESET", this);
    }

    private void ResetDecisionTimers()
    {
        nextDecisionTime = Time.time + 0.1f;
        nextAttackTime = Time.time + attackCooldown;
        nextDecisionTraceTime = Time.time;
    }
}
