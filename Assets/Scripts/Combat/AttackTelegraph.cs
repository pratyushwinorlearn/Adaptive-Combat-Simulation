using System;
using UnityEngine;

public class AttackTelegraph : MonoBehaviour
{
    public struct TelegraphSignal
    {
        public GameObject attacker;
        public AttackData attack;
        public Vector3 position;
        public Vector3 direction;
    }

    [Header("References")]
    [SerializeField] private CombatAttackState attackState;

    [Header("Visual Telegraph")]
    [SerializeField] private GameObject telegraphIndicator;

    [Header("Head Tracking")]
    [SerializeField] private Transform head;

    public bool IsTelegraphing { get; private set; }
    public AttackData TelegraphedAttack { get; private set; }

    public event Action<TelegraphSignal> OnTelegraphStarted;
    public event Action<AttackData> OnTelegraphEnded;

    private Vector3 headOffset;

    private void Awake()
    {
        if (attackState == null)
            attackState = GetComponent<CombatAttackState>();

        if (head == null)
            head = FindDeepChild(transform, "mixamorig:Head");
        if (head == null)
            head = FindDeepChild(transform, "Head");

        if (head == null)
            Debug.LogWarning(name + " AttackTelegraph could not find the Head bone.", this);

        if (telegraphIndicator != null)
        {
            if (head != null)
                headOffset = telegraphIndicator.transform.position - head.position;
            telegraphIndicator.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (attackState == null)
            return;

        attackState.OnAttackStarted += HandleAttackStarted;
        attackState.OnAttackBecameActive += HandleAttackBecameActive;
        attackState.OnAttackEnded += HandleAttackEnded;
    }

    private void OnDisable()
    {
        if (attackState == null)
            return;

        attackState.OnAttackStarted -= HandleAttackStarted;
        attackState.OnAttackBecameActive -= HandleAttackBecameActive;
        attackState.OnAttackEnded -= HandleAttackEnded;
    }

    private void LateUpdate()
    {
        if (!IsTelegraphing || telegraphIndicator == null || head == null)
            return;

        telegraphIndicator.transform.position = head.position + headOffset;
    }

    private void HandleAttackStarted(AttackData attack)
    {
        if (attack == null)
            return;

        TelegraphedAttack = attack;
        IsTelegraphing = true;

        if (telegraphIndicator != null)
        {
            telegraphIndicator.SetActive(true);
            if (head != null)
                telegraphIndicator.transform.position = head.position + headOffset;
        }

        OnTelegraphStarted?.Invoke(new TelegraphSignal
        {
            attacker = gameObject,
            attack = attack,
            position = transform.position,
            direction = transform.forward
        });

        Debug.Log(name + " TELEGRAPHING: " + attack.attackName, this);
    }

    private void HandleAttackBecameActive(AttackData attack)
    {
        bool wasTelegraphing = IsTelegraphing;
        ClearTelegraph(attack, notifyObservers: wasTelegraphing);
        Debug.Log(name + " TELEGRAPH ENDED: " + (attack != null ? attack.attackName : "Unknown"), this);
    }

    private void HandleAttackEnded(AttackData attack)
    {
        // Normally the telegraph has already ended at the Active phase. This
        // also safely clears it if an attack ends abnormally during windup.
        bool wasTelegraphing = IsTelegraphing;
        ClearTelegraph(attack, notifyObservers: wasTelegraphing);
    }

    /// <summary>
    /// Clears the local indicator/state and notifies observers when a telegraph
    /// is cancelled (for example, by hit-stun interrupting an attack in windup).
    /// </summary>
    public void ResetTelegraph()
    {
        AttackData endingAttack = TelegraphedAttack;
        bool wasTelegraphing = IsTelegraphing;
        ClearTelegraph(endingAttack, notifyObservers: wasTelegraphing);
    }

    private void ClearTelegraph(AttackData attack, bool notifyObservers)
    {
        IsTelegraphing = false;
        TelegraphedAttack = null;

        if (telegraphIndicator != null)
            telegraphIndicator.SetActive(false);

        if (notifyObservers)
            OnTelegraphEnded?.Invoke(attack);
    }

    private Transform FindDeepChild(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            Transform result = FindDeepChild(child, targetName);
            if (result != null)
                return result;
        }

        return null;
    }
}
