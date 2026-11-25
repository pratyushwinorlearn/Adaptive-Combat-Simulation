
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AdaptiveCombatAgent : Agent
{
    [Header("References")]
    [SerializeField] private CombatObserver combatObserver;
    [SerializeField] private CombatOpponentObserver opponentObserver;
    [SerializeField] private AIActionController aiActionController;
    [SerializeField] private CombatRewardController rewardController;
    [SerializeField] private CombatEpisodeManager episodeManager;

    [Header("Debug")]
    [SerializeField] private bool logActions = false;
    [SerializeField] private bool logRewards = false;

    [Header("Action Masking")]
    [SerializeField] private bool maskUnavailableActions = true;
    [SerializeField] private bool logActionMask = false;
    [SerializeField, Min(0f)] private float lightAttackMaxRange = 2.8f;
    [SerializeField, Min(0f)] private float heavyAttackMaxRange = 4.2f;

    [Header("Heavy Attack Rate Limit")]
    [SerializeField, Min(0f)] private float heavyAttackCooldown = 1.5f;
    [SerializeField] private bool logHeavyAttackCooldown = false;

    private float nextHeavyAttackAllowedTime;

    [Header("Defensive Action Masking")]
    [SerializeField] private bool maskDefenseWithoutThreat = true;
    [SerializeField, Min(0f)] private float lightDefenseMaskMaxRange = 3.0f;
    [SerializeField, Min(0f)] private float heavyDefenseMaskMaxRange = 4.5f;
    [SerializeField, Range(-1f, 1f)]
    private float minimumOpponentFacingDot = 0.35f;
    [SerializeField] private bool requireOpponentFacingMe = true;

    private bool episodeEnding;

    private CombatPerception combatPerception;
    private HitStunController hitStunController;
    private DodgeController dodgeController;

    public override void Initialize()
    {
        if (combatObserver == null)
            combatObserver = GetComponent<CombatObserver>();

        if (opponentObserver == null)
            opponentObserver = GetComponent<CombatOpponentObserver>();

        if (combatPerception == null)
            combatPerception = GetComponent<CombatPerception>();

        if (hitStunController == null)
            hitStunController = GetComponent<HitStunController>();

        if (dodgeController == null)
            dodgeController = GetComponent<DodgeController>();

        if (aiActionController == null)
            aiActionController = GetComponent<AIActionController>();

        if (rewardController == null)
            rewardController = GetComponent<CombatRewardController>();

        if (episodeManager == null)
            episodeManager = FindAnyObjectByType<CombatEpisodeManager>();

        Debug.Log(name + " ADAPTIVE COMBAT AGENT INITIALIZED", this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (episodeManager != null)
        {
            episodeManager.OnEpisodeEnded += HandleEpisodeEnded;
            episodeManager.OnEpisodeReset += HandleEpisodeReset;
        }
    }

    protected override void OnDisable()
    {
        if (episodeManager != null)
        {
            episodeManager.OnEpisodeEnded -= HandleEpisodeEnded;
            episodeManager.OnEpisodeReset -= HandleEpisodeReset;
        }

        base.OnDisable();
    }

    private void Update()
    {
        if (rewardController == null)
            return;

        float reward = rewardController.ConsumeReward();

        if (Mathf.Abs(reward) > 0.000001f)
        {
            AddReward(reward);

            if (logRewards)
            {
                Debug.Log(
                    name + " ML-AGENT REWARD: " +
                    reward.ToString("F3"),
                    this
                );
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (combatObserver == null)
            return;

        float[] observations = combatObserver.GetObservationVector();

        for (int i = 0; i < observations.Length; i++)
            sensor.AddObservation(observations[i]);
    }

    // Action mapping:
    // 0 = Wait
    // 1 = Light Attack
    // 2 = Heavy Attack
    // 3 = Block
    // 4 = Dodge

    public override void WriteDiscreteActionMask(
        IDiscreteActionMask actionMask)
    {
        if (!maskUnavailableActions)
            return;

        bool isAttacking =
            combatPerception != null &&
            combatPerception.IsAttacking;

        bool isDodging =
            (combatPerception != null &&
             combatPerception.IsDodging) ||
            (dodgeController != null &&
             dodgeController.IsDodging);

        bool isStunned =
            hitStunController != null &&
            hitStunController.IsStunned;

        bool combatLocked =
            episodeEnding ||
            isAttacking ||
            isDodging ||
            isStunned;

        if (combatLocked)
        {
            actionMask.SetActionEnabled(0, 1, false);
            actionMask.SetActionEnabled(0, 2, false);
            actionMask.SetActionEnabled(0, 3, false);
            actionMask.SetActionEnabled(0, 4, false);

            if (logActionMask)
            {
                Debug.Log(
                    name +
                    " ACTION MASK | combat locked" +
                    " | attacking=" + isAttacking +
                    " | dodging=" + isDodging +
                    " | stunned=" + isStunned +
                    " | episodeEnding=" + episodeEnding,
                    this
                );
            }

            return;
        }

        float distance = opponentObserver != null
            ? opponentObserver.GetDistanceToOpponent()
            : Mathf.Infinity;

        if (distance > lightAttackMaxRange)
            actionMask.SetActionEnabled(0, 1, false);

        if (distance > heavyAttackMaxRange)
            actionMask.SetActionEnabled(0, 2, false);

        if (Time.time < nextHeavyAttackAllowedTime)
            actionMask.SetActionEnabled(0, 2, false);

        bool dodgeReady =
            dodgeController != null &&
            dodgeController.CanDodge;

        if (!dodgeReady)
            actionMask.SetActionEnabled(0, 4, false);

        bool hasTelegraph =
            opponentObserver != null &&
            opponentObserver.IsOpponentTelegraphing &&
            opponentObserver.OpponentTelegraphedAttack != null;

        bool isHeavyTelegraph =
            hasTelegraph &&
            opponentObserver.OpponentTelegraphedAttack.attackType ==
            AttackData.AttackType.Heavy;

        // Use a separate defensive range for each attack type.
        float allowedDefenseRange = isHeavyTelegraph
            ? heavyDefenseMaskMaxRange
            : lightDefenseMaskMaxRange;

        bool withinDefenseRange =
            hasTelegraph &&
            distance <= allowedDefenseRange;

        float opponentFacingDot =
            opponentObserver != null
                ? opponentObserver.GetOpponentFacingDot()
                : -1f;

        bool opponentFacingMe =
            !requireOpponentFacingMe ||
            opponentFacingDot >= minimumOpponentFacingDot;

        bool threateningTelegraph =
            hasTelegraph &&
            withinDefenseRange &&
            opponentFacingMe;

        if (maskDefenseWithoutThreat)
        {
            bool alreadyBlocking =
                combatPerception != null &&
                combatPerception.IsBlocking;

            if (!threateningTelegraph && !alreadyBlocking)
                actionMask.SetActionEnabled(0, 3, false);

            if (!threateningTelegraph || !dodgeReady)
                actionMask.SetActionEnabled(0, 4, false);
        }

        if (logActionMask)
        {
            Debug.Log(
                name +
                " ACTION MASK" +
                " | distance=" + distance.ToString("F2") +
                " | telegraph=" + hasTelegraph +
                " | attackType=" +
                (hasTelegraph
                    ? opponentObserver.OpponentTelegraphedAttack.attackType.ToString()
                    : "None") +
                " | defenseRange=" + allowedDefenseRange.ToString("F2") +
                " | facingDot=" + opponentFacingDot.ToString("F2") +
                " | threatening=" + threateningTelegraph +
                " | dodgeReady=" + dodgeReady +
                " | heavyCooldownRemaining=" +
                Mathf.Max(
                    0f,
                    nextHeavyAttackAllowedTime - Time.time
                ).ToString("F2"),
                this
            );
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (episodeEnding)
        {
            if (aiActionController != null)
                aiActionController.ResetAction();

            return;
        }

        CombatActions combatActions = new CombatActions();

        if (actions.DiscreteActions.Length > 0)
        {
            int combatAction = actions.DiscreteActions[0];

            switch (combatAction)
            {
                case 0:
                    break;

                case 1:
                    combatActions.LightAttack = true;
                    break;

                case 2:
                    combatActions.HeavyAttack = true;

                    nextHeavyAttackAllowedTime =
                        Time.time + heavyAttackCooldown;

                    if (logHeavyAttackCooldown)
                    {
                        Debug.Log(
                            name +
                            " HEAVY ATTACK COOLDOWN STARTED | duration=" +
                            heavyAttackCooldown.ToString("F2") + "s",
                            this
                        );
                    }

                    break;

                case 3:
                    combatActions.Block = true;
                    break;

                case 4:
                    combatActions.Dodge = true;
                    break;

                default:
                    Debug.LogWarning(
                        name + " received invalid combat action: " +
                        combatAction,
                        this
                    );
                    break;
            }
        }

        if (aiActionController != null)
            aiActionController.ApplyAction(combatActions);

        if (logActions)
        {
            Debug.Log(
                name + " PPO COMBAT ACTION: " +
                GetCombatActionName(actions),
                this
            );
        }
    }

    private string GetCombatActionName(ActionBuffers actions)
    {
        if (actions.DiscreteActions.Length == 0)
            return "NoAction";

        switch (actions.DiscreteActions[0])
        {
            case 0: return "Wait";
            case 1: return "LightAttack";
            case 2: return "HeavyAttack";
            case 3: return "Block";
            case 4: return "Dodge";
            default: return "Unknown";
        }
    }

    public override void OnEpisodeBegin()
    {
        episodeEnding = false;
        nextHeavyAttackAllowedTime = 0f;

        if (aiActionController != null)
            aiActionController.ResetAction();

        if (rewardController != null)
            rewardController.ResetReward();

        if (logActions)
            Debug.Log(name + " ML-AGENT EPISODE BEGIN", this);
    }

    private void HandleEpisodeEnded(GameObject winner, GameObject loser)
    {
        if (episodeEnding)
            return;

        episodeEnding = true;

        if (aiActionController != null)
            aiActionController.ResetAction();

        if (logActions)
        {
            Debug.Log(
                name +
                " ML-AGENT EPISODE END SIGNAL | Winner=" +
                (winner != null ? winner.name : "Unknown") +
                " | Loser=" +
                (loser != null ? loser.name : "Unknown"),
                this
            );
        }
    }

    private void HandleEpisodeReset()
    {
        if (logActions)
            Debug.Log(name + " ML-AGENT ENVIRONMENT RESET", this);

        EndEpisode();
    }

    // N = Light Attack
    // M = Heavy Attack
    // Right Alt = Block
    // Right Ctrl = Dodge

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;

        if (discrete.Length == 0)
            return;

        int combatAction = 0;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.nKey.wasPressedThisFrame)
                combatAction = 1;
            else if (Keyboard.current.mKey.wasPressedThisFrame)
                combatAction = 2;
            else if (Keyboard.current.rightCtrlKey.wasPressedThisFrame)
                combatAction = 4;
            else if (Keyboard.current.rightAltKey.isPressed)
                combatAction = 3;
        }

        discrete[0] = combatAction;
    }
}
