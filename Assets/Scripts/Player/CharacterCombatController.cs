using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterCombatController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private bool usePlayerInput = true;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float gravity = -20f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private BlockController blockController;
    [SerializeField] private DodgeController dodgeController;
    [SerializeField] private HitStunController hitStunController;
    [SerializeField] private CombatAttackState attackState;

    private CharacterController controller;
    private AIActionController aiActionController;
    private float verticalVelocity;

    [Header("Action Debug")]
    [SerializeField] private bool logCombatExecution = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        aiActionController = GetComponent<AIActionController>();

        if (inputController == null)
            inputController = GetComponent<PlayerInputController>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (blockController == null)
            blockController = GetComponent<BlockController>();
        if (dodgeController == null)
            dodgeController = GetComponent<DodgeController>();
        if (hitStunController == null)
            hitStunController = GetComponent<HitStunController>();
        if (attackState == null)
            attackState = GetComponent<CombatAttackState>();
    }

    private void Update()
    {
        // AIActionController owns actions on AI-controlled fighters.
        if (aiActionController != null &&
            aiActionController.isActiveAndEnabled)
        {
            return;
        }

        if (!usePlayerInput || inputController == null)
            return;

        ExecuteActions(inputController.GetActions());
    }

    public void SetPlayerInputEnabled(bool enabled)
    {
        usePlayerInput = enabled;
        if (!enabled && blockController != null)
            blockController.SetBlocking(false);
    }

    // Executes continuous movement and persistent block. For keyboard-controlled
    // fighters this also executes any combat requests present in the input action.
    // The AIActionController sends one-shot PPO requests through ExecuteCombatActions
    // directly from ApplyAction(), aligned with the ML-Agents decision callback.
    public void ExecuteActions(CombatActions actions)
    {
        float speed = actions.Running ? runSpeed : walkSpeed;

        if (Mathf.Abs(actions.Turn) > 0.01f)
        {
            float rotationAmount = actions.Turn * turnSpeed * Time.deltaTime;
            transform.Rotate(0f, rotationAmount, 0f);
        }

        if (actions.MoveForward)
        {
            Vector3 movement = transform.forward * speed;
            controller.Move(movement * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        if (hitStunController != null && hitStunController.IsStunned)
        {
            if (logCombatExecution && (actions.LightAttack || actions.HeavyAttack || actions.Dodge))
                Debug.Log($"[CombatExecutor] {name}: combat request suppressed by hit-stun | light={actions.LightAttack} heavy={actions.HeavyAttack} dodge={actions.Dodge}", this);

            if (animator != null)
                animator.SetFloat("Speed", 0f);
            if (blockController != null)
                blockController.SetBlocking(false);
            return;
        }

        if (animator != null)
        {
            float animationSpeed = 0f;
            if (actions.MoveForward)
                animationSpeed = actions.Running ? 1f : 0.5f;
            animator.SetFloat("Speed", animationSpeed, 0.1f, Time.deltaTime);
        }

        ExecuteCombatActions(actions);

        if (blockController != null)
            blockController.SetBlocking(actions.Block);
    }

    // Executes one-shot combat commands without advancing movement/gravity.
    // Calling this directly from AdaptiveCombatAgent's action callback prevents
    // multiple policy decisions from accumulating into Light+Heavy+Dodge=true.
    public void ExecuteCombatActions(CombatActions actions)
    {
        if (!actions.Dodge && !actions.LightAttack && !actions.HeavyAttack)
            return;

        if (hitStunController != null && hitStunController.IsStunned)
        {
            if (logCombatExecution)
                Debug.Log($"[CombatExecutor] {name}: combat request suppressed by hit-stun | light={actions.LightAttack} heavy={actions.HeavyAttack} dodge={actions.Dodge}", this);
            return;
        }

        if (actions.Dodge)
        {
            if (dodgeController == null)
            {
                Debug.LogWarning($"[CombatExecutor] {name}: Dodge requested, but DodgeController is NULL.", this);
            }
            else
            {
                bool wasDodging = dodgeController.IsDodging;
                if (logCombatExecution)
                    Debug.Log($"[CombatExecutor] {name}: Dodge request | wasDodging={wasDodging}", this);

                dodgeController.TryDodge();

                if (logCombatExecution)
                    Debug.Log($"[CombatExecutor] {name}: Dodge result | isDodging={dodgeController.IsDodging}", this);
            }
        }

        bool dodgeActive = dodgeController != null && dodgeController.IsDodging;
        if (dodgeActive && (actions.LightAttack || actions.HeavyAttack) && logCombatExecution)
            Debug.Log($"[CombatExecutor] {name}: attack request skipped because dodge is active.", this);

        if (dodgeActive)
            return;

        // These checks are separate for keyboard input; PPO's discrete branch
        // only supplies one of these requests for any one decision.
        if (actions.LightAttack)
        {
            if (attackState == null)
            {
                Debug.LogWarning($"[CombatExecutor] {name}: LightAttack requested, but CombatAttackState is NULL.", this);
            }
            else
            {
                bool started = attackState.StartLightAttack();
                if (logCombatExecution)
                    Debug.Log($"[CombatExecutor] {name}: LightAttack accepted={started}", this);

                if (started && animator != null)
                {
                    animator.SetTrigger("LightAttack");
                    if (logCombatExecution)
                        Debug.Log($"[CombatExecutor] {name}: LightAttack Animator trigger sent.", this);
                }
                else if (started)
                {
                    Debug.LogWarning($"[CombatExecutor] {name}: LightAttack accepted but Animator is NULL.", this);
                }
            }
        }

        if (actions.HeavyAttack)
        {
            if (attackState == null)
            {
                Debug.LogWarning($"[CombatExecutor] {name}: HeavyAttack requested, but CombatAttackState is NULL.", this);
            }
            else
            {
                bool started = attackState.StartHeavyAttack();
                if (logCombatExecution)
                    Debug.Log($"[CombatExecutor] {name}: HeavyAttack accepted={started}", this);

                if (started && animator != null)
                {
                    animator.SetTrigger("HeavyAttack");
                    if (logCombatExecution)
                        Debug.Log($"[CombatExecutor] {name}: HeavyAttack Animator trigger sent.", this);
                }
                else if (started)
                {
                    Debug.LogWarning($"[CombatExecutor] {name}: HeavyAttack accepted but Animator is NULL.", this);
                }
            }
        }
    }

    public void StartLightAttack()
    {
        CombatActions actions = new CombatActions { LightAttack = true };
        ExecuteCombatActions(actions);
    }

    public void StartHeavyAttack()
    {
        CombatActions actions = new CombatActions { HeavyAttack = true };
        ExecuteCombatActions(actions);
    }
}
