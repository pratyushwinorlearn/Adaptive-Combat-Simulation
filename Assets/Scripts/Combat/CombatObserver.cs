using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatObserver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatPerception ownPerception;
    [SerializeField] private CombatOpponentObserver opponentObserver;

    [Header("Observation Settings")]
    [SerializeField] private float maxObservationDistance = 10f;

    // =========================================
    // INITIALIZATION
    // =========================================

    private void Awake()
    {
        if (ownPerception == null)
        {
            ownPerception =
                GetComponent<CombatPerception>();
        }

        if (opponentObserver == null)
        {
            opponentObserver =
                GetComponent<CombatOpponentObserver>();
        }

        if (maxObservationDistance <= 0f)
        {
            maxObservationDistance = 10f;
        }
    }

    // =========================================
    // UPDATE
    // Temporary debug input
    // O = print observation vector
    // =========================================

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            PrintObservationVector();
        }
    }

    // =========================================
    // OWN HEALTH
    // =========================================

    public float GetOwnHealthNormalized()
    {
        if (ownPerception == null)
            return 0f;

        return Mathf.Clamp01(
            ownPerception.NormalizedHealth
        );
    }

    // =========================================
    // OWN ATTACK STATE
    // =========================================

    public CombatAttackState.AttackPhase
        GetOwnAttackPhase()
    {
        if (ownPerception == null)
        {
            return CombatAttackState.AttackPhase.Idle;
        }

        return ownPerception.AttackPhase;
    }

    // =========================================
    // OPPONENT DISTANCE
    // =========================================

    public float GetNormalizedOpponentDistance()
    {
        if (opponentObserver == null)
            return 0f;

        float distance =
            opponentObserver.GetDistanceToOpponent();

        return Mathf.Clamp01(
            distance /
            maxObservationDistance
        );
    }

    // =========================================
    // OBSERVATION VECTOR
    // =========================================
    //
    // 0  Own HP
    // 1  Distance to opponent
    // 2  Opponent relative X
    // 3  Opponent relative Z
    // 4  Opponent facing toward self
    // 5  Own Idle
    // 6  Own Windup
    // 7  Own Active
    // 8  Own Recovery
    // 9  Own Blocking
    // 10 Own Dodging
    // 11 Own Invulnerable
    // 12 Opponent Telegraphing
    // 13 Opponent Light Telegraph
    // 14 Opponent Heavy Telegraph
    //
    // Total = 15 observations
    // =========================================

    public float[] GetObservationVector()
    {
        List<float> observations =
            new List<float>();

        // =====================================
        // 0 - OWN HEALTH
        // =====================================

        observations.Add(
            GetOwnHealthNormalized()
        );

        // =====================================
        // OPPONENT POSITION
        // =====================================

        Vector3 directionToOpponent =
            Vector3.zero;

        float normalizedDistance = 0f;

        if (opponentObserver != null)
        {
            directionToOpponent =
                opponentObserver.GetDirectionToOpponent();

            normalizedDistance =
                GetNormalizedOpponentDistance();
        }

        // =====================================
        // 1 - NORMALIZED DISTANCE
        // =====================================

        observations.Add(
            normalizedDistance
        );

        // =====================================
        // 2 / 3 - RELATIVE POSITION
        // =====================================

        Vector3 localDirection =
            transform.InverseTransformDirection(
                directionToOpponent
            );

        float relativeX =
            localDirection.x;

        float relativeZ =
            localDirection.z;

        observations.Add(
            Mathf.Clamp(
                relativeX,
                -1f,
                1f
            )
        );

        observations.Add(
            Mathf.Clamp(
                relativeZ,
                -1f,
                1f
            )
        );

        // =====================================
        // 4 - OPPONENT FACING TOWARD SELF
        // =====================================

        float opponentFacingDot = 0f;

        if (opponentObserver != null)
        {
            opponentFacingDot =
                opponentObserver
                    .GetOpponentFacingDot();
        }

        observations.Add(
            Mathf.Clamp(
                opponentFacingDot,
                -1f,
                1f
            )
        );

        // =====================================
        // OWN ATTACK PHASE
        // =====================================

        CombatAttackState.AttackPhase phase =
            GetOwnAttackPhase();

        // =====================================
        // 5 - OWN IDLE
        // =====================================

        observations.Add(
            phase ==
            CombatAttackState.AttackPhase.Idle
                ? 1f
                : 0f
        );

        // =====================================
        // 6 - OWN WINDUP
        // =====================================

        observations.Add(
            phase ==
            CombatAttackState.AttackPhase.Windup
                ? 1f
                : 0f
        );

        // =====================================
        // 7 - OWN ACTIVE
        // =====================================

        observations.Add(
            phase ==
            CombatAttackState.AttackPhase.Active
                ? 1f
                : 0f
        );

        // =====================================
        // 8 - OWN RECOVERY
        // =====================================

        observations.Add(
            phase ==
            CombatAttackState.AttackPhase.Recovery
                ? 1f
                : 0f
        );

        // =====================================
        // 9 - OWN BLOCKING
        // =====================================

        observations.Add(
            ownPerception != null &&
            ownPerception.IsBlocking
                ? 1f
                : 0f
        );

        // =====================================
        // 10 - OWN DODGING
        // =====================================

        observations.Add(
            ownPerception != null &&
            ownPerception.IsDodging
                ? 1f
                : 0f
        );

        // =====================================
        // 11 - OWN INVULNERABLE
        // =====================================

        observations.Add(
            ownPerception != null &&
            ownPerception.IsInvulnerable
                ? 1f
                : 0f
        );

        // =====================================
        // 12 - OPPONENT TELEGRAPHING
        // =====================================

        observations.Add(
            opponentObserver != null &&
            opponentObserver.IsOpponentTelegraphing
                ? 1f
                : 0f
        );

        // =====================================
        // 13 - OPPONENT LIGHT TELEGRAPH
        // =====================================

        observations.Add(
            opponentObserver != null &&
            opponentObserver
                .IsOpponentTelegraphingLightAttack()
                ? 1f
                : 0f
        );

        // =====================================
        // 14 - OPPONENT HEAVY TELEGRAPH
        // =====================================

        observations.Add(
            opponentObserver != null &&
            opponentObserver
                .IsOpponentTelegraphingHeavyAttack()
                ? 1f
                : 0f
        );

        return observations.ToArray();
    }

    // =========================================
    // DEBUG PRINT
    // =========================================

    public void PrintObservationVector()
    {
        float[] observations =
            GetObservationVector();

        string output =
            gameObject.name +
            " OBSERVATION VECTOR: ";

        for (int i = 0;
             i < observations.Length;
             i++)
        {
            output +=
                "[" +
                i +
                "]=" +
                observations[i].ToString("F2") +
                " ";
        }

        Debug.Log(output);
    }
}