using UnityEngine;

public class AIActionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterCombatController combatController;
    [SerializeField] private CombatNavigator combatNavigator;

    [Header("PPO Combat Actions")]
    private bool blockActive;

    [Header("Debug")]
    [SerializeField] private bool logActionPipeline = false;
    [SerializeField] private bool logCombatRequests = true;
    [SerializeField, Min(0.1f)] private float pipelineLogInterval = 0.5f;
    private float nextPipelineLogTime;

    public CombatActions LastAction { get; private set; }

    private void Awake()
    {
        if (combatController == null)
            combatController = GetComponent<CharacterCombatController>();

        if (combatNavigator == null)
            combatNavigator = GetComponent<CombatNavigator>();

        if (combatController == null)
            Debug.LogError($"{name} AIActionController: CharacterCombatController is missing.", this);

        if (combatNavigator == null)
            Debug.LogError($"{name} AIActionController: CombatNavigator is missing.", this);

        Debug.Log(
            $"[AIActionController] {name} initialized | " +
            $"combat={(combatController != null ? combatController.name : "NULL")}, " +
            $"navigator={(combatNavigator != null ? combatNavigator.name : "NULL")}", this);
    }

    private void Update()
    {
        if (combatController == null)
            return;

        // Movement/turning are updated every rendered frame. One-shot PPO
        // combat actions are executed immediately inside ApplyAction(), so
        // requests cannot accumulate and collide in a set of pending booleans.
        CombatActions frameActions = new CombatActions();

        if (combatNavigator != null && combatNavigator.isActiveAndEnabled)
        {
            CombatActions navigationActions = combatNavigator.GetNavigationActions();
            frameActions.MoveForward = navigationActions.MoveForward;
            frameActions.Turn = navigationActions.Turn;
            frameActions.Running = navigationActions.Running;
        }

        frameActions.Block = blockActive;
        LastAction = frameActions;
        combatController.ExecuteActions(frameActions);
        LogPipelineIfNeeded(frameActions);
    }

    public void ApplyAction(CombatActions actions)
    {
        if (combatController == null)
            return;

        // Block is persistent until the next agent decision changes it.
        blockActive = actions.Block;

        // The action space is one discrete branch, so only one combat action
        // should be requested per decision. Execute one-shots immediately.
        CombatActions combatRequest = new CombatActions
        {
            LightAttack = actions.LightAttack,
            HeavyAttack = actions.HeavyAttack,
            Dodge = actions.Dodge
        };

        if (combatRequest.LightAttack || combatRequest.HeavyAttack || combatRequest.Dodge)
        {
            if (logCombatRequests)
            {
                Debug.Log(
                    $"[AIActionController] {name}: EXECUTE REQUEST | " +
                    $"light={combatRequest.LightAttack} " +
                    $"heavy={combatRequest.HeavyAttack} " +
                    $"dodge={combatRequest.Dodge}", this);
            }

            combatController.ExecuteCombatActions(combatRequest);
        }
    }

    public void ResetAction()
    {
        blockActive = false;
        LastAction = new CombatActions();

        if (combatController != null)
        {
            BlockController block = GetComponent<BlockController>();
            if (block != null)
                block.SetBlocking(false);
        }
    }

    private void LogPipelineIfNeeded(CombatActions actions)
    {
        if (!logActionPipeline || Time.unscaledTime < nextPipelineLogTime)
            return;

        nextPipelineLogTime = Time.unscaledTime + pipelineLogInterval;

        float targetAngle = 0f;
        string targetName = "NULL";
        if (combatNavigator != null && combatNavigator.Target != null)
        {
            Transform target = combatNavigator.Target;
            targetName = target.name;
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                targetAngle = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up);
        }

        Debug.Log(
            $"[AIActionController] {name} pipeline | target={targetName} | " +
            $"turn={actions.Turn:F2} | move={actions.MoveForward} | run={actions.Running} | " +
            $"angle={targetAngle:F1} | yaw={transform.eulerAngles.y:F1} | " +
            $"navigatorEnabled={(combatNavigator != null && combatNavigator.isActiveAndEnabled)} | " +
            $"controller={(combatController != null ? "OK" : "NULL")}", this);
    }
}
