using UnityEngine;

public static class DamageSystem
{
    public static void ApplyDamage(
        Hurtbox target,
        AttackData attack,
        GameObject attacker)
    {
        if (target == null)
            return;

        if (target.OwnerHealth == null)
            return;

        if (attack == null)
            return;

        if (attacker == null)
            return;

        GameObject attackerRoot =
            attacker.transform.root.gameObject;

        GameObject defender =
            target.OwnerHealth.gameObject;


        // =========================================
        // SELF-HIT CHECK
        // =========================================

        if (defender == attackerRoot)
        {
            return;
        }


        // =========================================
        // DODGE CHECK
        // =========================================

        DodgeController dodgeController =
            target.OwnerHealth.GetComponent<DodgeController>();

        if (dodgeController != null &&
            attack.canBeDodged &&
            dodgeController.IsInvulnerable)
        {
            Debug.Log(
                defender.name +
                " DODGED " +
                attack.attackName +
                " from " +
                attackerRoot.name
            );

            CombatEventSignals.RaiseAttackDodged(
                attackerRoot,
                defender,
                attack
            );

            return;
        }


        // =========================================
        // BLOCK CHECK
        // =========================================

        BlockController blockController =
            target.OwnerHealth.GetComponent<BlockController>();

        if (blockController != null)
        {
            bool canBlock =
                blockController.CanBlockAttack(
                    attacker.transform
                );

            if (attack.canBeBlocked &&
                canBlock)
            {
                Debug.Log(
                    defender.name +
                    " BLOCKED " +
                    attack.attackName +
                    " from " +
                    attackerRoot.name
                );

                CombatEventSignals.RaiseAttackBlocked(
                    attackerRoot,
                    defender,
                    attack
                );

                return;
            }
        }


        // =========================================
        // RECORD HEALTH BEFORE DAMAGE
        // =========================================

        float healthBefore =
            target.OwnerHealth.CurrentHealth;


        // =========================================
        // APPLY DAMAGE
        // =========================================

        target.OwnerHealth.TakeDamage(
            attack.damage
        );


        // =========================================
        // CALCULATE ACTUAL DAMAGE
        // =========================================

        float actualDamage =
            healthBefore -
            target.OwnerHealth.CurrentHealth;


        // =========================================
        // DAMAGE EVENT
        // =========================================

        if (actualDamage > 0f)
        {
            CombatEventSignals.RaiseDamageDealt(
                attackerRoot,
                defender,
                attack,
                actualDamage
            );
        }


        // =========================================
        // DEATH CHECK
        // =========================================

        if (target.OwnerHealth.IsDead)
        {
            return;
        }


        // =========================================
        // HIT STUN + HIT REACTION
        // =========================================

        HitStunController hitStunController =
            target.OwnerHealth.GetComponent<HitStunController>();

        if (hitStunController != null)
        {
            hitStunController.ApplyHitStun(
                attack.hitStun
            );
        }
    }
}