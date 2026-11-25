using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    public CombatActions GetActions()
    {
        CombatActions actions =
            new CombatActions();

        if (Keyboard.current == null)
        {
            return actions;
        }

        // =========================================
        // MOVEMENT
        // =========================================

        // W = Move Forward
        actions.MoveForward =
            Keyboard.current.wKey.isPressed;

        // A = Turn Left
        if (Keyboard.current.aKey.isPressed)
        {
            actions.Turn = -1f;
        }

        // D = Turn Right
        else if (Keyboard.current.dKey.isPressed)
        {
            actions.Turn = 1f;
        }

        // Shift = Run
        actions.Running =
            Keyboard.current.leftShiftKey.isPressed;

        // =========================================
        // COMBAT
        // =========================================

        // J = Light Attack
        actions.LightAttack =
            Keyboard.current.jKey.wasPressedThisFrame;

        // K = Heavy Attack
        actions.HeavyAttack =
            Keyboard.current.kKey.wasPressedThisFrame;

        // Space = Dodge
        actions.Dodge =
            Keyboard.current.spaceKey.wasPressedThisFrame;

        // Q = Block
        actions.Block =
            Keyboard.current.qKey.isPressed;

        return actions;
    }
}