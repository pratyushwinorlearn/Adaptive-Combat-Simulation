using UnityEngine;

public class CombatRewardController : MonoBehaviour
{
    [Header("Episode Manager")]
    [SerializeField] private CombatEpisodeManager episodeManager;

    [Header("Opponent Observation")]
    [SerializeField] private CombatOpponentObserver opponentObserver;

    [Header("Combat Rewards")]
    [SerializeField] private float rewardPerDamageDealt = 0.01f;
    [SerializeField] private float penaltyPerDamageTaken = 0.01f;
    [SerializeField] private float rewardForSuccessfulBlock = 0.10f;
    [SerializeField] private float rewardForSuccessfulDodge = 0.15f;
    [SerializeField] private float penaltyForBeingBlocked = 0.10f;
    [SerializeField] private float penaltyForBeingDodged = 0.10f;

    [Header("Episode Rewards")]
    [SerializeField] private float rewardForWinning = 1.0f;
    [SerializeField] private float penaltyForLosing = -1.0f;

    [Header("Dense Shaping Rewards")]
    [SerializeField] private float rewardForFacingOpponent = 0.002f;
    [SerializeField] private float penaltyForFacingAway = 0.003f;
    [SerializeField] private float rewardForDistanceImprovement = 0.02f;

    [Header("Facing Settings")]
    [SerializeField] private float facingRewardInterval = 0.12f;
    [SerializeField] private float facingRewardThreshold = 0.5f;
    [SerializeField] private float facingPenaltyThreshold = 0.0f;

    [Header("Desired Combat Distance")]
    [SerializeField] private float desiredCombatDistance = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool logRewards = true;
    [SerializeField] private bool logShapingRewards = false;

    public float PendingReward
    {
        get;
        private set;
    }

    private float previousDistanceError;
    private bool shapingInitialized;

    private float nextFacingRewardTime;

    private bool episodeEnded;

    // =========================================
    // INITIALIZATION
    // =========================================

    private void Awake()
    {
        if (episodeManager == null)
        {
            episodeManager =
                FindAnyObjectByType<CombatEpisodeManager>();
        }

        if (opponentObserver == null)
        {
            opponentObserver =
                GetComponent<CombatOpponentObserver>();
        }
    }

    // =========================================
    // ENABLE
    // =========================================

    private void OnEnable()
    {
        CombatEventSignals.OnDamageDealt +=
            HandleDamageDealt;

        CombatEventSignals.OnAttackBlocked +=
            HandleAttackBlocked;

        CombatEventSignals.OnAttackDodged +=
            HandleAttackDodged;

        if (episodeManager != null)
        {
            episodeManager.OnEpisodeEnded +=
                HandleEpisodeEnded;

            episodeManager.OnEpisodeReset +=
                HandleEpisodeReset;
        }
    }

    // =========================================
    // DISABLE
    // =========================================

    private void OnDisable()
    {
        CombatEventSignals.OnDamageDealt -=
            HandleDamageDealt;

        CombatEventSignals.OnAttackBlocked -=
            HandleAttackBlocked;

        CombatEventSignals.OnAttackDodged -=
            HandleAttackDodged;

        if (episodeManager != null)
        {
            episodeManager.OnEpisodeEnded -=
                HandleEpisodeEnded;

            episodeManager.OnEpisodeReset -=
                HandleEpisodeReset;
        }
    }

    // =========================================
    // UPDATE
    // =========================================

    private void Update()
    {
        if (episodeEnded)
            return;

        UpdateFacingReward();
        UpdateDistanceReward();
    }

    // =========================================
    // FACING REWARD
    // =========================================

    private void UpdateFacingReward()
    {
        if (opponentObserver == null)
            return;

        if (Time.time < nextFacingRewardTime)
            return;

        nextFacingRewardTime =
            Time.time + facingRewardInterval;

        Vector3 directionToOpponent =
            opponentObserver.GetDirectionToOpponent();

        if (directionToOpponent.sqrMagnitude < 0.0001f)
            return;

        Vector3 flatDirection =
            Vector3.ProjectOnPlane(
                directionToOpponent,
                Vector3.up
            ).normalized;

        Vector3 flatForward =
            Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up
            ).normalized;

        float facingAlignment =
            Vector3.Dot(
                flatForward,
                flatDirection
            );

        // -----------------------------------------
        // Facing opponent correctly
        // -----------------------------------------

        if (facingAlignment >= facingRewardThreshold)
        {
            AddShapingReward(
                rewardForFacingOpponent,
                "Facing Opponent"
            );
        }

        // -----------------------------------------
        // Facing away from opponent
        // -----------------------------------------

        else if (facingAlignment < facingPenaltyThreshold)
        {
            AddShapingReward(
                -penaltyForFacingAway,
                "Facing Away"
            );
        }
    }

    // =========================================
    // DISTANCE REWARD
    // =========================================

    private void UpdateDistanceReward()
    {
        if (opponentObserver == null)
            return;

        float distance =
            opponentObserver.GetDistanceToOpponent();

        float distanceError =
            Mathf.Abs(
                distance -
                desiredCombatDistance
            );

        if (!shapingInitialized)
        {
            previousDistanceError =
                distanceError;

            shapingInitialized = true;

            return;
        }

        float distanceImprovement =
            previousDistanceError -
            distanceError;

        if (distanceImprovement > 0f)
        {
            float reward =
                distanceImprovement *
                rewardForDistanceImprovement;

            AddShapingReward(
                reward,
                "Distance Improvement"
            );
        }

        previousDistanceError =
            distanceError;
    }

    // =========================================
    // DAMAGE DEALT
    // =========================================

    private void HandleDamageDealt(
        GameObject attacker,
        GameObject defender,
        AttackData attack,
        float amount)
    {
        if (attacker == gameObject)
        {
            float reward =
                amount *
                rewardPerDamageDealt;

            AddReward(
                reward,
                "Damage Dealt"
            );
        }

        if (defender == gameObject)
        {
            float penalty =
                amount *
                penaltyPerDamageTaken;

            AddReward(
                -penalty,
                "Damage Taken"
            );
        }
    }

    // =========================================
    // ATTACK BLOCKED
    // =========================================

    private void HandleAttackBlocked(
        GameObject attacker,
        GameObject defender,
        AttackData attack)
    {
        if (defender == gameObject)
        {
            AddReward(
                rewardForSuccessfulBlock,
                "Successful Block"
            );
        }

        if (attacker == gameObject)
        {
            AddReward(
                -penaltyForBeingBlocked,
                "Attack Blocked"
            );
        }
    }

    // =========================================
    // ATTACK DODGED
    // =========================================

    private void HandleAttackDodged(
        GameObject attacker,
        GameObject defender,
        AttackData attack)
    {
        if (defender == gameObject)
        {
            AddReward(
                rewardForSuccessfulDodge,
                "Successful Dodge"
            );
        }

        if (attacker == gameObject)
        {
            AddReward(
                -penaltyForBeingDodged,
                "Attack Dodged"
            );
        }
    }

    // =========================================
    // EPISODE ENDED
    // =========================================

    private void HandleEpisodeEnded(
        GameObject winner,
        GameObject loser)
    {
        episodeEnded = true;

        if (winner == gameObject)
        {
            AddReward(
                rewardForWinning,
                "Episode Won"
            );
        }
        else if (loser == gameObject)
        {
            AddReward(
                penaltyForLosing,
                "Episode Lost"
            );
        }
    }

    // =========================================
    // EPISODE RESET
    // =========================================

    private void HandleEpisodeReset()
    {
        PendingReward = 0f;

        previousDistanceError = 0f;

        shapingInitialized = false;
        episodeEnded = false;

        nextFacingRewardTime =
            Time.time + 0.2f;

        if (logRewards)
        {
            Debug.Log(
                gameObject.name +
                " reward reset."
            );
        }
    }

    // =========================================
    // NORMAL REWARD
    // =========================================

    private void AddReward(
        float amount,
        string reason)
    {
        PendingReward += amount;

        if (logRewards)
        {
            Debug.Log(
                gameObject.name +
                " REWARD: " +
                amount.ToString("F3") +
                " | " +
                reason +
                " | Pending=" +
                PendingReward.ToString("F3")
            );
        }
    }

    // =========================================
    // SHAPING REWARD
    // =========================================

    private void AddShapingReward(
        float amount,
        string reason)
    {
        PendingReward += amount;

        if (logShapingRewards)
        {
            Debug.Log(
                gameObject.name +
                " SHAPING REWARD: " +
                amount.ToString("F4") +
                " | " +
                reason +
                " | Pending=" +
                PendingReward.ToString("F4")
            );
        }
    }

    // =========================================
    // CONSUME REWARD
    // =========================================

    public float ConsumeReward()
    {
        float reward =
            PendingReward;

        PendingReward = 0f;

        return reward;
    }

    // =========================================
    // RESET REWARD
    // =========================================

    public void ResetReward()
    {
        PendingReward = 0f;

        previousDistanceError = 0f;

        shapingInitialized = false;
        episodeEnded = false;

        nextFacingRewardTime =
            Time.time + 0.2f;
    }
}