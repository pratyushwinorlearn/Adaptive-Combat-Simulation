
using UnityEngine;

public class CombatOpponentObserver : MonoBehaviour
{
    [Header("Opponent")]
    [SerializeField] private Transform opponent;

    [Header("Opponent Systems")]
    [SerializeField] private CombatPerception opponentPerception;
    [SerializeField] private AttackTelegraph opponentTelegraph;

    public bool IsOpponentTelegraphing { get; private set; }

    public AttackData OpponentTelegraphedAttack { get; private set; }

    public Vector3 OpponentTelegraphPosition { get; private set; }

    public Vector3 OpponentTelegraphDirection { get; private set; }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (opponentTelegraph == null)
            return;

        opponentTelegraph.OnTelegraphStarted += HandleTelegraphStarted;
        opponentTelegraph.OnTelegraphEnded += HandleTelegraphEnded;

        SyncCurrentTelegraph();
    }

    private void OnDisable()
    {
        if (opponentTelegraph == null)
            return;

        opponentTelegraph.OnTelegraphStarted -= HandleTelegraphStarted;
        opponentTelegraph.OnTelegraphEnded -= HandleTelegraphEnded;
    }

    private void ResolveReferences()
    {
        if (opponent == null)
            return;

        if (opponentPerception == null)
            opponentPerception = opponent.GetComponent<CombatPerception>();

        if (opponentTelegraph == null)
            opponentTelegraph = opponent.GetComponent<AttackTelegraph>();
    }

    private void SyncCurrentTelegraph()
    {
        if (opponentTelegraph == null ||
            !opponentTelegraph.IsTelegraphing ||
            opponentTelegraph.TelegraphedAttack == null)
        {
            ClearObservedTelegraph();
            return;
        }

        IsOpponentTelegraphing = true;
        OpponentTelegraphedAttack = opponentTelegraph.TelegraphedAttack;

        if (opponent != null)
        {
            OpponentTelegraphPosition = opponent.position;
            OpponentTelegraphDirection = opponent.forward;
        }
    }

    private void HandleTelegraphStarted(
        AttackTelegraph.TelegraphSignal signal)
    {
        IsOpponentTelegraphing = true;
        OpponentTelegraphedAttack = signal.attack;
        OpponentTelegraphPosition = signal.position;
        OpponentTelegraphDirection = signal.direction;

        if (signal.attack != null)
        {
            Debug.Log(
                gameObject.name +
                " OBSERVED OPPONENT TELEGRAPH: " +
                signal.attack.attackName +
                " | Type: " +
                signal.attack.attackType,
                this
            );
        }
    }

    private void HandleTelegraphEnded(AttackData attack)
    {
        ClearObservedTelegraph();

        if (attack != null)
        {
            Debug.Log(
                gameObject.name +
                " OPPONENT TELEGRAPH ENDED: " +
                attack.attackName,
                this
            );
        }
    }

    private void ClearObservedTelegraph()
    {
        IsOpponentTelegraphing = false;
        OpponentTelegraphedAttack = null;
        OpponentTelegraphPosition = Vector3.zero;
        OpponentTelegraphDirection = Vector3.zero;
    }

    public Vector3 GetOpponentPosition()
    {
        return opponent != null ? opponent.position : Vector3.zero;
    }

    public float GetDistanceToOpponent()
    {
        if (opponent == null)
            return 0f;

        return Vector3.Distance(transform.position, opponent.position);
    }

    public Vector3 GetDirectionToOpponent()
    {
        if (opponent == null)
            return Vector3.zero;

        Vector3 direction = opponent.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return direction.normalized;
    }

    public float GetOpponentDirectionDot()
    {
        if (opponent == null)
            return 0f;

        Vector3 directionToOpponent = GetDirectionToOpponent();

        if (directionToOpponent == Vector3.zero)
            return 1f;

        return Vector3.Dot(transform.forward, directionToOpponent);
    }

    public float GetOpponentFacingDot()
    {
        if (opponent == null)
            return 0f;

        Vector3 directionToMe = transform.position - opponent.position;
        directionToMe.y = 0f;

        if (directionToMe.sqrMagnitude < 0.0001f)
            return 1f;

        directionToMe.Normalize();

        return Vector3.Dot(opponent.forward, directionToMe);
    }

    public bool IsOpponentTelegraphingLightAttack()
    {
        return IsOpponentTelegraphing &&
               OpponentTelegraphedAttack != null &&
               OpponentTelegraphedAttack.attackType == AttackData.AttackType.Light;
    }

    public bool IsOpponentTelegraphingHeavyAttack()
    {
        return IsOpponentTelegraphing &&
               OpponentTelegraphedAttack != null &&
               OpponentTelegraphedAttack.attackType == AttackData.AttackType.Heavy;
    }

    public void PrintOpponentObservation()
    {
        string attackName = OpponentTelegraphedAttack != null
            ? OpponentTelegraphedAttack.attackName
            : "None";

        string attackType = OpponentTelegraphedAttack != null
            ? OpponentTelegraphedAttack.attackType.ToString()
            : "None";

        Debug.Log(
            gameObject.name +
            " OPPONENT OBSERVATION | " +
            "Distance: " + GetDistanceToOpponent() +
            " | FacingDot: " + GetOpponentFacingDot() +
            " | Telegraphing: " + IsOpponentTelegraphing +
            " | Attack: " + attackName +
            " | Type: " + attackType,
            this
        );
    }
}
