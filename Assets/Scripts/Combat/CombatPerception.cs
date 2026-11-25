using UnityEngine;

public class CombatPerception : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private CombatAttackState attackState;
    [SerializeField] private BlockController blockController;
    [SerializeField] private DodgeController dodgeController;

    // =========================================
    // BASIC INFORMATION
    // =========================================

    public Vector3 Position
    {
        get
        {
            return transform.position;
        }
    }

    public Vector3 Forward
    {
        get
        {
            return transform.forward;
        }
    }

    // =========================================
    // HEALTH
    // =========================================

    public float CurrentHealth
    {
        get
        {
            if (health == null)
                return 0f;

            return health.CurrentHealth;
        }
    }

    public float MaxHealth
    {
        get
        {
            if (health == null)
                return 0f;

            return health.MaxHealth;
        }
    }

    public float NormalizedHealth
    {
        get
        {
            if (health == null ||
                health.MaxHealth <= 0f)
            {
                return 0f;
            }

            return health.CurrentHealth /
                   health.MaxHealth;
        }
    }

    public bool IsDead
    {
        get
        {
            return health != null &&
                   health.IsDead;
        }
    }

    // =========================================
    // ATTACK STATE
    // =========================================

    public CombatAttackState.AttackPhase AttackPhase
    {
        get
        {
            if (attackState == null)
            {
                return CombatAttackState.AttackPhase.Idle;
            }

            return attackState.Phase;
        }
    }

    public bool IsAttacking
    {
        get
        {
            return attackState != null &&
                   attackState.IsAttacking;
        }
    }

    public bool IsInWindup
    {
        get
        {
            return attackState != null &&
                   attackState.IsInWindup;
        }
    }

    public bool IsAttackActive
    {
        get
        {
            return attackState != null &&
                   attackState.IsAttackActive;
        }
    }

    public bool IsRecovering
    {
        get
        {
            return attackState != null &&
                   attackState.IsRecovering;
        }
    }

    // =========================================
    // BLOCK
    // =========================================

    public bool IsBlocking
    {
        get
        {
            return blockController != null &&
                   blockController.IsBlocking;
        }
    }

    // =========================================
    // DODGE / INVULNERABILITY
    // =========================================

    public bool IsInvulnerable
    {
        get
        {
            return dodgeController != null &&
                   dodgeController.IsInvulnerable;
        }
    }

    public bool IsDodging
    {
        get
        {
            return dodgeController != null &&
                   dodgeController.IsDodging;
        }
    }

    // =========================================
    // INITIALIZATION
    // =========================================

    private void Awake()
    {
        if (health == null)
        {
            health =
                GetComponent<Health>();
        }

        if (attackState == null)
        {
            attackState =
                GetComponent<CombatAttackState>();
        }

        if (blockController == null)
        {
            blockController =
                GetComponent<BlockController>();
        }

        if (dodgeController == null)
        {
            dodgeController =
                GetComponent<DodgeController>();
        }
    }
}