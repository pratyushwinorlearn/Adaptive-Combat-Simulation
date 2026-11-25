using System;
using System.Collections;
using UnityEngine;

public class CombatEpisodeManager : MonoBehaviour
{
    [Header("Fighters")]
    [SerializeField] private GameObject nightshade;
    [SerializeField] private GameObject antagonist;

    [Header("Reset Settings")]
    [SerializeField] private float resetDelay = 1.5f;

    [Header("Episode Timeout")]
    [SerializeField, Min(10f)]
    private float maxEpisodeDuration = 45f;

    [Header("Optional Spawn Points")]
    [SerializeField] private Transform nightshadeSpawnPoint;
    [SerializeField] private Transform antagonistSpawnPoint;

    // =========================================
    // PUBLIC EVENTS
    // =========================================

    public event Action<GameObject, GameObject>
        OnEpisodeEnded;

    public event Action
        OnEpisodeReset;

    // =========================================
    // COMPONENT REFERENCES
    // =========================================

    private Health nightshadeHealth;
    private Health antagonistHealth;

    private CombatAttackState nightshadeAttackState;
    private CombatAttackState antagonistAttackState;

    private DodgeController nightshadeDodge;
    private DodgeController antagonistDodge;

    private HitStunController nightshadeHitStun;
    private HitStunController antagonistHitStun;

    private BlockController nightshadeBlock;
    private BlockController antagonistBlock;

    private Animator nightshadeAnimator;
    private Animator antagonistAnimator;

    private AttackTelegraph nightshadeTelegraph;
    private AttackTelegraph antagonistTelegraph;

    private AIActionController nightshadeAIAction;
    private AIActionController antagonistAIAction;

    // =========================================
    // INITIAL POSITIONS
    // =========================================

    private Vector3 nightshadeStartPosition;
    private Quaternion nightshadeStartRotation;

    private Vector3 antagonistStartPosition;
    private Quaternion antagonistStartRotation;

    // =========================================
    // EPISODE STATE
    // =========================================

    public int EpisodeNumber { get; private set; }

    public bool EpisodeEnding { get; private set; }

    private float episodeStartTime;

    // =========================================
    // AWAKE
    // =========================================

    private void Awake()
    {
        if (nightshade == null || antagonist == null)
        {
            Debug.LogError(
                "CombatEpisodeManager: " +
                "Nightshade and Antagonist must be assigned."
            );

            enabled = false;
            return;
        }

        // Find components.

        nightshadeHealth =
            nightshade.GetComponent<Health>();

        antagonistHealth =
            antagonist.GetComponent<Health>();

        nightshadeAttackState =
            nightshade.GetComponent<CombatAttackState>();

        antagonistAttackState =
            antagonist.GetComponent<CombatAttackState>();

        nightshadeDodge =
            nightshade.GetComponent<DodgeController>();

        antagonistDodge =
            antagonist.GetComponent<DodgeController>();

        nightshadeHitStun =
            nightshade.GetComponent<HitStunController>();

        antagonistHitStun =
            antagonist.GetComponent<HitStunController>();

        nightshadeBlock =
            nightshade.GetComponent<BlockController>();

        antagonistBlock =
            antagonist.GetComponent<BlockController>();

        nightshadeAnimator =
            nightshade.GetComponent<Animator>();

        antagonistAnimator =
            antagonist.GetComponent<Animator>();

        nightshadeTelegraph =
            nightshade.GetComponent<AttackTelegraph>();

        antagonistTelegraph =
            antagonist.GetComponent<AttackTelegraph>();

        nightshadeAIAction =
            nightshade.GetComponent<AIActionController>();

        antagonistAIAction =
            antagonist.GetComponent<AIActionController>();

        // Capture starting transforms.

        if (nightshadeSpawnPoint != null)
        {
            nightshadeStartPosition =
                nightshadeSpawnPoint.position;

            nightshadeStartRotation =
                nightshadeSpawnPoint.rotation;
        }
        else
        {
            nightshadeStartPosition =
                nightshade.transform.position;

            nightshadeStartRotation =
                nightshade.transform.rotation;
        }

        if (antagonistSpawnPoint != null)
        {
            antagonistStartPosition =
                antagonistSpawnPoint.position;

            antagonistStartRotation =
                antagonistSpawnPoint.rotation;
        }
        else
        {
            antagonistStartPosition =
                antagonist.transform.position;

            antagonistStartRotation =
                antagonist.transform.rotation;
        }

        EpisodeNumber = 0;
        EpisodeEnding = false;

        episodeStartTime = Time.time;
    }

    // =========================================
    // UPDATE — EPISODE TIMEOUT
    // =========================================

    private void Update()
    {
        if (EpisodeEnding)
            return;

        if (maxEpisodeDuration <= 0f)
            return;

        float elapsedTime =
            Time.time - episodeStartTime;

        if (elapsedTime >= maxEpisodeDuration)
        {
            HandleEpisodeTimeout();
        }
    }

    // =========================================
    // ENABLE
    // =========================================

    private void OnEnable()
    {
        if (nightshadeHealth != null)
        {
            nightshadeHealth.OnDied +=
                HandleFighterDied;
        }

        if (antagonistHealth != null)
        {
            antagonistHealth.OnDied +=
                HandleFighterDied;
        }
    }

    // =========================================
    // DISABLE
    // =========================================

    private void OnDisable()
    {
        if (nightshadeHealth != null)
        {
            nightshadeHealth.OnDied -=
                HandleFighterDied;
        }

        if (antagonistHealth != null)
        {
            antagonistHealth.OnDied -=
                HandleFighterDied;
        }
    }

    // =========================================
    // FIGHTER DIED
    // =========================================

    private void HandleFighterDied(
        Health deadFighter)
    {
        if (EpisodeEnding)
            return;

        EpisodeEnding = true;

        GameObject loser =
            deadFighter.gameObject;

        GameObject winner = null;

        if (loser == nightshade)
        {
            winner = antagonist;
        }
        else if (loser == antagonist)
        {
            winner = nightshade;
        }

        Debug.Log(
            "COMBAT EPISODE ENDED | " +
            "Winner=" +
            (winner != null
                ? winner.name
                : "Unknown") +
            " | Loser=" +
            loser.name
        );

        OnEpisodeEnded?.Invoke(
            winner,
            loser
        );

        StartCoroutine(
            ResetEpisodeAfterDelay()
        );
    }

    // =========================================
    // EPISODE TIMEOUT
    // =========================================

    private void HandleEpisodeTimeout()
    {
        if (EpisodeEnding)
            return;

        EpisodeEnding = true;

        GameObject winner = null;
        GameObject loser = null;

        // Decide the winner using remaining health.

        if (nightshadeHealth != null &&
            antagonistHealth != null)
        {
            if (nightshadeHealth.CurrentHealth >
                antagonistHealth.CurrentHealth)
            {
                winner = nightshade;
                loser = antagonist;
            }
            else if (antagonistHealth.CurrentHealth >
                     nightshadeHealth.CurrentHealth)
            {
                winner = antagonist;
                loser = nightshade;
            }
        }

        string outcome;

        if (winner != null)
        {
            outcome =
                winner.name + " wins on health";
        }
        else
        {
            outcome = "Draw";
        }

        Debug.Log(
            "COMBAT EPISODE TIMED OUT | " +
            "Episode=" + (EpisodeNumber + 1) +
            " | Outcome=" + outcome +
            " | Nightshade HP=" +
            (nightshadeHealth != null
                ? nightshadeHealth.CurrentHealth
                : -1f) +
            " | Antagonist HP=" +
            (antagonistHealth != null
                ? antagonistHealth.CurrentHealth
                : -1f)
        );

        // Notify reward, agent and telemetry systems.
        // A draw sends null for both winner and loser.

        OnEpisodeEnded?.Invoke(
            winner,
            loser
        );

        StartCoroutine(
            ResetEpisodeAfterDelay()
        );
    }

    // =========================================
    // WAIT BEFORE RESET
    // =========================================

    private IEnumerator ResetEpisodeAfterDelay()
    {
        yield return new WaitForSeconds(
            resetDelay
        );

        ResetEpisode();
    }

    // =========================================
    // RESET EPISODE
    // =========================================

    public void ResetEpisode()
    {
        EpisodeNumber++;

        Debug.Log(
            "RESETTING COMBAT EPISODE #" +
            EpisodeNumber
        );

        // Reset Nightshade.

        ResetFighter(
            nightshade,
            nightshadeStartPosition,
            nightshadeStartRotation,
            nightshadeHealth,
            nightshadeAttackState,
            nightshadeDodge,
            nightshadeHitStun,
            nightshadeBlock,
            nightshadeAnimator,
            nightshadeTelegraph,
            nightshadeAIAction
        );

        // Reset Antagonist.

        ResetFighter(
            antagonist,
            antagonistStartPosition,
            antagonistStartRotation,
            antagonistHealth,
            antagonistAttackState,
            antagonistDodge,
            antagonistHitStun,
            antagonistBlock,
            antagonistAnimator,
            antagonistTelegraph,
            antagonistAIAction
        );

        // Start a fresh episode timer.

        episodeStartTime = Time.time;

        EpisodeEnding = false;

        // Tell AI and environment systems that
        // the physical reset is complete.

        OnEpisodeReset?.Invoke();

        Debug.Log(
            "COMBAT EPISODE RESET COMPLETE | " +
            "Episode=" + EpisodeNumber
        );
    }

    // =========================================
    // RESET ONE FIGHTER
    // =========================================

    private void ResetFighter(
        GameObject fighter,
        Vector3 startPosition,
        Quaternion startRotation,
        Health health,
        CombatAttackState attackState,
        DodgeController dodge,
        HitStunController hitStun,
        BlockController block,
        Animator animator,
        AttackTelegraph telegraph,
        AIActionController aiAction)
    {
        if (fighter == null)
            return;

        // Reset transform.

        CharacterController characterController =
            fighter.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        fighter.transform.position =
            startPosition;

        fighter.transform.rotation =
            startRotation;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // Reset health.

        if (health != null)
        {
            health.ResetHealth();
        }

        // Reset attack state.

        if (attackState != null)
        {
            attackState.ResetAttackState();
        }

        // Reset dodge.

        if (dodge != null)
        {
            dodge.ResetDodge();
        }

        // Reset hit stun.

        if (hitStun != null)
        {
            hitStun.ResetHitStun();
        }

        // Reset blocking.

        if (block != null)
        {
            block.SetBlocking(false);
        }

        // Reset opponent telegraph.

        if (telegraph != null)
        {
            telegraph.ResetTelegraph();
        }

        // Reset queued AI actions.

        if (aiAction != null)
        {
            aiAction.ResetAction();
        }

        // Reset animation state.

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

            animator.SetFloat(
                "Speed",
                0f
            );

            animator.ResetTrigger(
                "LightAttack"
            );

            animator.ResetTrigger(
                "HeavyAttack"
            );

            animator.ResetTrigger(
                "DodgeBackward"
            );

            animator.ResetTrigger(
                "HitReaction"
            );

            animator.ResetTrigger(
                "Death"
            );
        }
    }
}