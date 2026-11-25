using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Position")]
    [SerializeField] private float distance = 4.5f;
    [SerializeField] private float height = 2.2f;

    [Header("Follow")]
    [SerializeField] private float positionSmooth = 10f;
    [SerializeField] private float rotationSmooth = 12f;

    [Header("Look")]
    [SerializeField] private float lookHeight = 1.2f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Position the camera behind the character.
        Vector3 desiredPosition =
            target.position
            - target.forward * distance
            + Vector3.up * height;

        // Smoothly follow the character.
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            positionSmooth * Time.deltaTime
        );

        // Look toward the character's upper body.
        Vector3 lookTarget =
            target.position +
            Vector3.up * lookHeight;

        Vector3 direction =
            lookTarget - transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationSmooth * Time.deltaTime
            );
        }
    }
}