using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float gravity = -20f;

    [Header("References")]
    [SerializeField] private Animator animator;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        // =========================================
        // KEYBOARD INPUT
        // =========================================
        Vector2 input = ReadKeyboardInput();

        bool running =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed;

        float speed = running ? runSpeed : walkSpeed;

        // =========================================
        // ROTATION
        // A = Rotate Left
        // D = Rotate Right
        // =========================================
        if (Mathf.Abs(input.x) > 0.01f)
        {
            float rotationAmount =
                input.x * turnSpeed * Time.deltaTime;

            transform.Rotate(
                0f,
                rotationAmount,
                0f
            );
        }

        // =========================================
        // FORWARD MOVEMENT
        // W = Move Forward
        // =========================================
        if (input.y > 0f)
        {
            Vector3 movement =
                transform.forward * speed;

            controller.Move(
                movement * Time.deltaTime
            );
        }

        // =========================================
        // GRAVITY
        // =========================================
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime
        );

        // =========================================
        // MOVEMENT ANIMATION
        // =========================================
        if (animator != null)
        {
            float animationSpeed = 0f;

            if (input.y > 0f)
            {
                animationSpeed = running ? 1f : 0.5f;
            }

            animator.SetFloat(
                "Speed",
                animationSpeed,
                0.1f,
                Time.deltaTime
            );
        }

        // =========================================
        // COMBAT INPUT
        // =========================================
        HandleCombatInput();
    }

    // =========================================================
    // READ KEYBOARD INPUT
    // =========================================================
    private Vector2 ReadKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        // W = Move Forward
        if (Keyboard.current.wKey.isPressed)
        {
            input.y += 1f;
        }

        // S intentionally disabled
        // because there is no backward-walk animation.

        // D = Rotate Right
        if (Keyboard.current.dKey.isPressed)
        {
            input.x += 1f;
        }

        // A = Rotate Left
        if (Keyboard.current.aKey.isPressed)
        {
            input.x -= 1f;
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    // =========================================================
    // COMBAT INPUT
    // =========================================================
    private void HandleCombatInput()
    {
        if (Keyboard.current == null || animator == null)
        {
            return;
        }

        // -----------------------------------------
        // J = LIGHT ATTACK
        // -----------------------------------------
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            animator.SetTrigger("LightAttack");
        }

        // -----------------------------------------
        // K = HEAVY ATTACK
        // -----------------------------------------
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            animator.SetTrigger("HeavyAttack");
        }

        // -----------------------------------------
        // SPACE = DODGE BACK
        // -----------------------------------------
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            animator.SetTrigger("DodgeBackward");
        }

        // -----------------------------------------
        // Q = BLOCK
        // Hold Q → Block
        // Release Q → Stop Blocking
        // -----------------------------------------
        bool blocking =
            Keyboard.current.qKey.isPressed;

        animator.SetBool(
            "Block",
            blocking
        );
    }

    // =========================================================
    // FUTURE AI / RL MOVEMENT
    // =========================================================
    public void MoveFromAI(
        Vector2 input,
        bool running)
    {
        input = Vector2.ClampMagnitude(
            input,
            1f
        );

        // -----------------------------------------
        // AI ROTATION
        // -----------------------------------------
        if (Mathf.Abs(input.x) > 0.01f)
        {
            float rotationAmount =
                input.x * turnSpeed * Time.deltaTime;

            transform.Rotate(
                0f,
                rotationAmount,
                0f
            );
        }

        // -----------------------------------------
        // AI FORWARD MOVEMENT
        // -----------------------------------------
        if (input.y > 0f)
        {
            float speed =
                running ? runSpeed : walkSpeed;

            controller.Move(
                transform.forward *
                speed *
                Time.deltaTime
            );
        }
    }
}