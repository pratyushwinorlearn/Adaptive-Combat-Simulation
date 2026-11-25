using UnityEngine;

public class BlockController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator")]
    [SerializeField] private string blockParameter = "Block";

    [Header("Block Settings")]
    [Tooltip("Half-angle of the block cone in degrees.")]
    [SerializeField, Range(1f, 89f)]
    private float blockAngle = 60f;

    [Header("Debug")]
    [SerializeField] private bool startBlocking;

    public bool IsBlocking { get; private set; }

    private int blockParameterHash;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        blockParameterHash =
            Animator.StringToHash(blockParameter);

        // Debug/testing option.
        if (startBlocking)
        {
            SetBlocking(true);
        }
    }

    public void SetBlocking(bool blocking)
    {
        IsBlocking = blocking;

        if (animator != null)
        {
            animator.SetBool(
                blockParameterHash,
                blocking
            );
        }
    }

    public bool CanBlockAttack(Vector3 attackerPosition)
    {
        if (!IsBlocking)
            return false;

        Vector3 directionToAttacker =
            attackerPosition - transform.position;

        directionToAttacker.y = 0f;

        if (directionToAttacker.sqrMagnitude <
            0.0001f)
        {
            return true;
        }

        directionToAttacker.Normalize();

        float dot =
            Vector3.Dot(
                transform.forward,
                directionToAttacker
            );

        float requiredDot =
            Mathf.Cos(
                blockAngle * Mathf.Deg2Rad
            );

        return dot >= requiredDot;
    }

    public bool CanBlockAttack(Transform attacker)
    {
        if (attacker == null)
            return false;

        return CanBlockAttack(
            attacker.position
        );
    }

    public float GetBlockDot(Vector3 attackerPosition)
    {
        Vector3 directionToAttacker =
            attackerPosition - transform.position;

        directionToAttacker.y = 0f;

        if (directionToAttacker.sqrMagnitude <
            0.0001f)
        {
            return 1f;
        }

        directionToAttacker.Normalize();

        return Vector3.Dot(
            transform.forward,
            directionToAttacker
        );
    }

    public bool IsAttackWithinBlockCone(
        Vector3 attackerPosition)
    {
        return CanBlockAttack(attackerPosition);
    }
}