using System;
using System.IO;
using UnityEngine;

public class EpisodeStats : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatEpisodeManager episodeManager;
    [SerializeField] private Health ownHealth;
    [SerializeField] private Health opponentHealth;
    [SerializeField] private CombatAttackState attackState;
    [SerializeField] private Transform opponent;

    [Header("Telemetry")]
    [SerializeField] private string runId = "adaptive_combat";
    [SerializeField] private bool writeToFile = true;
    [SerializeField] private bool logEpisodeSummary = true;

    [Header("Debug")]
    [SerializeField] private bool logEvents = false;

    // =========================================
    // EPISODE STATE
    // =========================================

    private int episodeIndex = 1;
    private float episodeStartTime;

    private bool episodeCompleted;
    private bool attackInProgress;
    private bool currentAttackHadContact;

    private AttackData currentAttack;

    // =========================================
    // COMBAT STATISTICS
    // =========================================

    private float damageDealt;
    private float damageTaken;

    private int lightAttacks;
    private int heavyAttacks;

    private int successfulBlocks;
    private int successfulDodges;

    private int attacksBlocked;
    private int attacksDodged;

    private int whiffs;

    // =========================================
    // FILE
    // =========================================

    private string telemetryDirectory;
    private string telemetryFilePath;

    // =========================================
    // AWAKE
    // =========================================

    private void Awake()
    {
        if (episodeManager == null)
        {
            episodeManager =
                FindAnyObjectByType<CombatEpisodeManager>();
        }

        if (ownHealth == null)
        {
            ownHealth =
                GetComponent<Health>();
        }

        if (attackState == null)
        {
            attackState =
                GetComponent<CombatAttackState>();
        }

        if (opponent == null)
        {
            CombatOpponentObserver observer =
                GetComponent<CombatOpponentObserver>();

            if (observer != null)
            {
                // We don't assume a public opponent
                // property exists, so opponent can
                // simply be assigned in Inspector.
            }
        }

        telemetryDirectory =
            Path.Combine(
                Application.persistentDataPath,
                "CombatTelemetry"
            );

        telemetryFilePath =
            Path.Combine(
                telemetryDirectory,
                "episodes.jsonl"
            );

        if (writeToFile)
        {
            try
            {
                Directory.CreateDirectory(
                    telemetryDirectory
                );

                Debug.Log(
                    "EpisodeStats telemetry path: " +
                    telemetryFilePath
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "EpisodeStats could not create telemetry directory: " +
                    exception.Message
                );
            }
        }
    }

    // =========================================
    // START
    // =========================================

    private void Start()
    {
        StartNewEpisode();
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

        if (attackState != null)
        {
            attackState.OnAttackStarted +=
                HandleAttackStarted;

            attackState.OnAttackEnded +=
                HandleAttackEnded;
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

        if (attackState != null)
        {
            attackState.OnAttackStarted -=
                HandleAttackStarted;

            attackState.OnAttackEnded -=
                HandleAttackEnded;
        }
    }

    // =========================================
    // START NEW EPISODE
    // =========================================

    private void StartNewEpisode()
    {
        episodeStartTime =
            Time.time;

        episodeCompleted = false;

        attackInProgress = false;
        currentAttackHadContact = false;
        currentAttack = null;

        damageDealt = 0f;
        damageTaken = 0f;

        lightAttacks = 0;
        heavyAttacks = 0;

        successfulBlocks = 0;
        successfulDodges = 0;

        attacksBlocked = 0;
        attacksDodged = 0;

        whiffs = 0;

        if (logEvents)
        {
            Debug.Log(
                gameObject.name +
                " EpisodeStats started episode " +
                episodeIndex
            );
        }
    }

    // =========================================
    // ATTACK STARTED
    // =========================================

    private void HandleAttackStarted(
        AttackData attack)
    {
        if (attack == null)
            return;

        currentAttack =
            attack;

        attackInProgress = true;
        currentAttackHadContact = false;

        if (attack.attackType ==
            AttackData.AttackType.Light)
        {
            lightAttacks++;
        }
        else if (
            attack.attackType ==
            AttackData.AttackType.Heavy)
        {
            heavyAttacks++;
        }

        if (logEvents)
        {
            Debug.Log(
                gameObject.name +
                " EpisodeStats attack started: " +
                attack.attackName
            );
        }
    }

    // =========================================
    // ATTACK ENDED
    // =========================================

    private void HandleAttackEnded(
        AttackData attack)
    {
        if (!attackInProgress)
            return;

        // If the attack neither damaged,
        // got blocked, nor got dodged, it
        // produced no combat contact.
        if (!currentAttackHadContact)
        {
            whiffs++;

            if (logEvents)
            {
                Debug.Log(
                    gameObject.name +
                    " EpisodeStats WHIFF | " +
                    (attack != null
                        ? attack.attackName
                        : "Unknown Attack")
                );
            }
        }

        attackInProgress = false;
        currentAttack = null;
        currentAttackHadContact = false;
    }

    // =========================================
    // DAMAGE DEALT / TAKEN
    // =========================================

    private void HandleDamageDealt(
        GameObject attacker,
        GameObject defender,
        AttackData attack,
        float amount)
    {
        if (attacker == gameObject)
        {
            damageDealt += amount;

            if (attackInProgress)
            {
                currentAttackHadContact = true;
            }

            if (logEvents)
            {
                Debug.Log(
                    gameObject.name +
                    " EpisodeStats damage dealt: " +
                    amount.ToString("F2")
                );
            }
        }

        if (defender == gameObject)
        {
            damageTaken += amount;

            if (logEvents)
            {
                Debug.Log(
                    gameObject.name +
                    " EpisodeStats damage taken: " +
                    amount.ToString("F2")
                );
            }
        }
    }

    // =========================================
    // BLOCKED ATTACK
    // =========================================

    private void HandleAttackBlocked(
        GameObject attacker,
        GameObject defender,
        AttackData attack)
    {
        // We successfully blocked an attack.
        if (defender == gameObject)
        {
            successfulBlocks++;
        }

        // Our attack was blocked.
        if (attacker == gameObject)
        {
            attacksBlocked++;

            if (attackInProgress)
            {
                currentAttackHadContact = true;
            }
        }

        if (logEvents)
        {
            Debug.Log(
                gameObject.name +
                " EpisodeStats BLOCK EVENT"
            );
        }
    }

    // =========================================
    // DODGED ATTACK
    // =========================================

    private void HandleAttackDodged(
        GameObject attacker,
        GameObject defender,
        AttackData attack)
    {
        // We successfully dodged.
        if (defender == gameObject)
        {
            successfulDodges++;
        }

        // Our attack was dodged.
        if (attacker == gameObject)
        {
            attacksDodged++;

            if (attackInProgress)
            {
                currentAttackHadContact = true;
            }
        }

        if (logEvents)
        {
            Debug.Log(
                gameObject.name +
                " EpisodeStats DODGE EVENT"
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
        if (episodeCompleted)
            return;

        episodeCompleted = true;

        string outcome =
            "unknown";

        if (winner == gameObject)
        {
            outcome = "win";
        }
        else if (loser == gameObject)
        {
            outcome = "loss";
        }

        float duration =
            Time.time -
            episodeStartTime;

        WriteEpisodeRecord(
            outcome,
            duration,
            winner,
            loser
        );
    }

    // =========================================
    // EPISODE RESET
    // =========================================

    private void HandleEpisodeReset()
    {
        episodeIndex++;

        StartNewEpisode();
    }

    // =========================================
    // WRITE EPISODE RECORD
    // =========================================

    private void WriteEpisodeRecord(
        string outcome,
        float duration,
        GameObject winner,
        GameObject loser)
    {
        EpisodeRecord record =
            new EpisodeRecord();

        record.timestampUtc =
            DateTime.UtcNow.ToString(
                "o"
            );

        record.runId =
            runId;

        record.fighter =
            gameObject.name;

        record.episodeIndex =
            episodeIndex;

        record.durationSeconds =
            duration;

        record.outcome =
            outcome;

        record.winner =
            winner != null
                ? winner.name
                : "";

        record.loser =
            loser != null
                ? loser.name
                : "";

        record.damageDealt =
            damageDealt;

        record.damageTaken =
            damageTaken;

        record.lightAttacks =
            lightAttacks;

        record.heavyAttacks =
            heavyAttacks;

        record.successfulBlocks =
            successfulBlocks;

        record.successfulDodges =
            successfulDodges;

        record.attacksBlocked =
            attacksBlocked;

        record.attacksDodged =
            attacksDodged;

        record.whiffs =
            whiffs;

        record.finalOwnHealth =
            ownHealth != null
                ? ownHealth.CurrentHealth
                : -1f;

        record.finalOpponentHealth =
            opponentHealth != null
                ? opponentHealth.CurrentHealth
                : -1f;

        string json =
            JsonUtility.ToJson(
                record
            );

        if (writeToFile)
        {
            try
            {
                File.AppendAllText(
                    telemetryFilePath,
                    json +
                    Environment.NewLine
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    gameObject.name +
                    " EpisodeStats failed to write episode: " +
                    exception.Message
                );
            }
        }

        if (logEpisodeSummary)
        {
            Debug.Log(
                "===== EPISODE TELEMETRY =====\n" +
                "Episode: " +
                episodeIndex +
                "\n" +
                "Outcome: " +
                outcome +
                "\n" +
                "Duration: " +
                duration.ToString("F2") +
                "s\n" +
                "Damage Dealt: " +
                damageDealt.ToString("F2") +
                "\n" +
                "Damage Taken: " +
                damageTaken.ToString("F2") +
                "\n" +
                "Light Attacks: " +
                lightAttacks +
                "\n" +
                "Heavy Attacks: " +
                heavyAttacks +
                "\n" +
                "Successful Blocks: " +
                successfulBlocks +
                "\n" +
                "Successful Dodges: " +
                successfulDodges +
                "\n" +
                "Attacks Blocked: " +
                attacksBlocked +
                "\n" +
                "Attacks Dodged: " +
                attacksDodged +
                "\n" +
                "Whiffs: " +
                whiffs +
                "\n" +
                "=============================="
            );
        }
    }

    // =========================================
    // PUBLIC FILE PATH
    // =========================================

    public string GetTelemetryFilePath()
    {
        return telemetryFilePath;
    }

    // =========================================
    // EPISODE DATA
    // =========================================

    [Serializable]
    private class EpisodeRecord
    {
        public string timestampUtc;
        public string runId;
        public string fighter;

        public int episodeIndex;

        public float durationSeconds;

        public string outcome;
        public string winner;
        public string loser;

        public float damageDealt;
        public float damageTaken;

        public int lightAttacks;
        public int heavyAttacks;

        public int successfulBlocks;
        public int successfulDodges;

        public int attacksBlocked;
        public int attacksDodged;

        public int whiffs;

        public float finalOwnHealth;
        public float finalOpponentHealth;
    }
}