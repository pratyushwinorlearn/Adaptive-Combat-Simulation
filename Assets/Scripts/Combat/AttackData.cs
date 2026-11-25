using UnityEngine;

[CreateAssetMenu(
    fileName = "NewAttackData",
    menuName = "Combat/Attack Data"
)]
public class AttackData : ScriptableObject
{
    public enum AttackType
    {
        Light,
        Heavy
    }

    [Header("Basic Information")]
    public string attackName;

    public AttackType attackType;

    [Header("Damage")]
    public float damage = 20f;

    [Header("Timing")]
    public float windupTime = 0.25f;
    public float activeTime = 0.20f;
    public float recoveryTime = 0.40f;

    [Header("Hit Effects")]
    public float hitStun = 0.25f;

    [Header("Combat Properties")]
    public bool canBeBlocked = true;
    public bool canBeDodged = true;
}