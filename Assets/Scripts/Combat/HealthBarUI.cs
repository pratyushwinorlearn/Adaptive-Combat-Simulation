using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Health target;

    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text healthText;

    [Header("Look")]
    [SerializeField] private float fillSpeed = 1.5f; // fraction of the bar per second

    private float targetFill = 1f;
    private float shownFill = 1f;

    private void OnEnable()
    {
        if (target != null)
            target.OnHealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        Refresh();
        shownFill = targetFill;
        ApplyFill();
    }

    private void OnDisable()
    {
        if (target != null)
            target.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(Health _)
    {
        Refresh();

        // Healing / episode reset snaps instantly; damage drains smoothly.
        if (targetFill > shownFill)
        {
            shownFill = targetFill;
            ApplyFill();
        }
    }

    private void Refresh()
    {
        if (target == null) return;

        targetFill = target.MaxHealth > 0f
            ? Mathf.Clamp01(target.CurrentHealth / target.MaxHealth)
            : 0f;

        if (healthText != null)
        {
            healthText.text =
                $"{Mathf.CeilToInt(target.CurrentHealth)} / {Mathf.CeilToInt(target.MaxHealth)}";
        }
    }

    private void Update()
    {
        if (Mathf.Approximately(shownFill, targetFill)) return;

        shownFill = Mathf.MoveTowards(shownFill, targetFill, fillSpeed * Time.unscaledDeltaTime);
        ApplyFill();
    }

    private void ApplyFill()
    {
        if (fillImage != null)
            fillImage.fillAmount = shownFill;
    }
}