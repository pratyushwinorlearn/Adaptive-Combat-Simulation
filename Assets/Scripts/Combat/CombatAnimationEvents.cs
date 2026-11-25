using UnityEngine;

public class CombatAnimationEvents : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SwordHitbox swordHitbox;
    [SerializeField] private CombatAttackState attackState;

    private static int nextDebugId = 0;
    private int debugId;

    private void Awake()
    {
        debugId = ++nextDebugId;

        if (swordHitbox == null)
        {
            swordHitbox =
                GetComponentInChildren<SwordHitbox>();
        }

        if (attackState == null)
        {
            attackState =
                GetComponent<CombatAttackState>();
        }
    }

    // =========================================
    // ATTACK BECOMES ACTIVE
    // =========================================

    public void StartSwordHitbox()
    {
        if (attackState != null)
        {
            attackState.BeginActivePhase();
        }

        if (swordHitbox != null)
        {
            swordHitbox.ActivateHitbox();
        }
    }

    // =========================================
    // ATTACK LEAVES ACTIVE PHASE
    // =========================================

    public void StopSwordHitbox()
    {
        if (swordHitbox != null)
        {
            swordHitbox.DeactivateHitbox();
        }

        if (attackState != null)
        {
            attackState.BeginRecoveryPhase();
        }
    }

    // =========================================
    // ATTACK COMPLETELY FINISHED
    // =========================================

    public void EndAttack()
    {
        Debug.Log(
            "END ATTACK EVENT FIRED | " +
            "Object=" + gameObject.name +
            " | DebugID=" + debugId +
            " | Frame=" + Time.frameCount
        );

        if (attackState != null)
        {
            attackState.FinishAttack();
        }
    }
}