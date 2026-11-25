using System;
using UnityEngine;

public static class CombatEventSignals
{
    // =========================================
    // DAMAGE DEALT
    // =========================================

    public static event Action<
        GameObject,
        GameObject,
        AttackData,
        float
    > OnDamageDealt;


    // =========================================
    // ATTACK BLOCKED
    // =========================================

    public static event Action<
        GameObject,
        GameObject,
        AttackData
    > OnAttackBlocked;


    // =========================================
    // ATTACK DODGED
    // =========================================

    public static event Action<
        GameObject,
        GameObject,
        AttackData
    > OnAttackDodged;


    // =========================================
    // RAISE DAMAGE EVENT
    // =========================================

    public static void RaiseDamageDealt(
        GameObject attacker,
        GameObject defender,
        AttackData attack,
        float amount)
    {
        OnDamageDealt?.Invoke(
            attacker,
            defender,
            attack,
            amount
        );
    }


    // =========================================
    // RAISE BLOCK EVENT
    // =========================================

    public static void RaiseAttackBlocked(
        GameObject attacker,
        GameObject defender,
        AttackData attack)
    {
        OnAttackBlocked?.Invoke(
            attacker,
            defender,
            attack
        );
    }


    // =========================================
    // RAISE DODGE EVENT
    // =========================================

    public static void RaiseAttackDodged(
        GameObject attacker,
        GameObject defender,
        AttackData attack)
    {
        OnAttackDodged?.Invoke(
            attacker,
            defender,
            attack
        );
    }
}