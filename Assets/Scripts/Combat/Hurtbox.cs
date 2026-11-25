using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [Header("Owner")]
    [SerializeField] private Health ownerHealth;

    public Health OwnerHealth => ownerHealth;

    private void Awake()
    {
        if (ownerHealth == null)
        {
            ownerHealth = GetComponentInParent<Health>();
        }
    }
}