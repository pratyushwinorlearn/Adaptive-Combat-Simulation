using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Death")]
    [SerializeField] private string deathTrigger = "Death";

    [Header("Debug")]
    [SerializeField] private bool logEvents = true;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public bool IsDead => CurrentHealth <= 0f;

    public event Action<Health> OnDied;

    // Raised whenever current or max health changes (damage, reset, max change).
    public event Action<Health> OnHealthChanged;

    private void Awake()
    {
        CurrentHealth = maxHealth;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        if (damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0f);

        if (logEvents)
        {
            Debug.Log($"{gameObject.name} took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
        }

        OnHealthChanged?.Invoke(this);

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (animator != null)
        {
            animator.SetTrigger(deathTrigger);
        }

        if (logEvents)
        {
            Debug.Log(gameObject.name + " died.");
        }

        OnDied?.Invoke(this);
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(this);
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);

        if (CurrentHealth > maxHealth)
        {
            CurrentHealth = maxHealth;
        }

        OnHealthChanged?.Invoke(this);
    }
}