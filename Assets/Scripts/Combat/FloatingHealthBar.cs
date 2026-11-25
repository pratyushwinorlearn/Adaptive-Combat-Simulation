
using UnityEngine;
using UnityEngine.UI;

public class FloatingHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Image fillImage;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 5f;

    private float targetFill;

    private void Start()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (health != null)
            targetFill = health.CurrentHealth / health.MaxHealth;
    }

    private void Update()
    {
        if (health == null || fillImage == null)
            return;

        targetFill = health.CurrentHealth / health.MaxHealth;

        fillImage.fillAmount = Mathf.Lerp(
            fillImage.fillAmount,
            targetFill,
            Time.deltaTime * smoothSpeed
        );
    }
}
