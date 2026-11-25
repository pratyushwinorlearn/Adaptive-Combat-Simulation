using System;
using UnityEngine;

public class CombatAttackState : MonoBehaviour
{
    public enum AttackPhase
    {
        Idle,
        Windup,
        Active,
        Recovery
    }

    [Header("Attack Data")]
    [SerializeField] private AttackData lightAttack;
    [SerializeField] private AttackData heavyAttack;

    public AttackData CurrentAttack { get; private set; }

    public AttackPhase Phase { get; private set; }
        = AttackPhase.Idle;

    public bool IsAttacking =>
        Phase != AttackPhase.Idle;

    public bool IsInWindup =>
        Phase == AttackPhase.Windup;

    public bool IsAttackActive =>
        Phase == AttackPhase.Active;

    public bool IsRecovering =>
        Phase == AttackPhase.Recovery;

    public event Action<AttackData> OnAttackStarted;
    public event Action<AttackData> OnAttackBecameActive;
    public event Action<AttackData> OnAttackEnteredRecovery;
    public event Action<AttackData> OnAttackEnded;


    // =========================================
    // START LIGHT ATTACK
    // =========================================

    public bool StartLightAttack()
    {
        return StartAttack(lightAttack);
    }


    // =========================================
    // START HEAVY ATTACK
    // =========================================

    public bool StartHeavyAttack()
    {
        return StartAttack(heavyAttack);
    }


    // =========================================
    // START ATTACK
    // =========================================

    private bool StartAttack(AttackData attack)
    {
        if (attack == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " tried to start an attack, but AttackData is missing."
            );

            return false;
        }

        if (IsAttacking)
        {
            Debug.Log(
                gameObject.name +
                " cannot start " +
                attack.attackName +
                " because another attack is already active."
            );

            return false;
        }

        CurrentAttack = attack;

        Phase =
            AttackPhase.Windup;

        Debug.Log(
            gameObject.name +
            " ATTACK STARTED: " +
            attack.attackName +
            " | Phase: " +
            Phase
        );

        OnAttackStarted?.Invoke(
            CurrentAttack
        );

        return true;
    }


    // =========================================
    // ACTIVE PHASE
    // =========================================

    public void BeginActivePhase()
    {
        if (CurrentAttack == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " tried to enter Active phase, but there is no current attack."
            );

            return;
        }

        if (Phase != AttackPhase.Windup)
        {
            Debug.LogWarning(
                gameObject.name +
                " tried to enter Active phase from phase: " +
                Phase
            );

            return;
        }

        Phase =
            AttackPhase.Active;

        Debug.Log(
            gameObject.name +
            " ATTACK ACTIVE: " +
            CurrentAttack.attackName +
            " | Phase: " +
            Phase
        );

        OnAttackBecameActive?.Invoke(
            CurrentAttack
        );
    }


    // =========================================
    // RECOVERY PHASE
    // =========================================

    public void BeginRecoveryPhase()
    {
        if (CurrentAttack == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " tried to enter Recovery phase, but there is no current attack."
            );

            return;
        }

        if (Phase != AttackPhase.Active)
        {
            Debug.LogWarning(
                gameObject.name +
                " tried to enter Recovery phase from phase: " +
                Phase
            );

            return;
        }

        Phase =
            AttackPhase.Recovery;

        Debug.Log(
            gameObject.name +
            " ATTACK RECOVERY: " +
            CurrentAttack.attackName +
            " | Phase: " +
            Phase
        );

        OnAttackEnteredRecovery?.Invoke(
            CurrentAttack
        );
    }


    // =========================================
    // FINISH ATTACK
    // =========================================

    public void FinishAttack()
    {
        if (CurrentAttack == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " tried to finish attack, but there is no current attack."
            );

            return;
        }

        AttackData finishedAttack =
            CurrentAttack;

        Debug.Log(
            gameObject.name +
            " ATTACK ENDED: " +
            finishedAttack.attackName +
            " | Phase: Idle"
        );

        CurrentAttack = null;

        Phase =
            AttackPhase.Idle;

        OnAttackEnded?.Invoke(
            finishedAttack
        );
    }


    // =========================================
    // RESET
    // =========================================

    public void ResetAttackState()
    {
        CurrentAttack = null;

        Phase =
            AttackPhase.Idle;

        Debug.Log(
            gameObject.name +
            " CombatAttackState RESET | Phase: Idle"
        );
    }


    // =========================================
    // SET LIGHT ATTACK DATA
    // =========================================

    public void SetLightAttackData(
        AttackData attack)
    {
        lightAttack = attack;
    }


    // =========================================
    // SET HEAVY ATTACK DATA
    // =========================================

    public void SetHeavyAttackData(
        AttackData attack)
    {
        heavyAttack = attack;
    }
}