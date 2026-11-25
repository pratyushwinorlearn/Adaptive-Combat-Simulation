using UnityEngine;
using UnityEngine.InputSystem;

public class AntagonistTestController : MonoBehaviour
{
    [SerializeField] private CharacterCombatController combatController;

    private void Awake()
    {
        if (combatController == null)
        {
            combatController =
                GetComponent<CharacterCombatController>();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            combatController.StartLightAttack();
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            combatController.StartHeavyAttack();
        }
    }
}