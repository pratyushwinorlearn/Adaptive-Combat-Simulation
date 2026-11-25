public struct CombatActions
{
    // ==============================
    // MOVEMENT
    // ==============================

    // Move forward this frame.
    public bool MoveForward;

    // Turn value:
    // -1 = left
    //  0 = no turn
    // +1 = right
    public float Turn;

    // Run instead of walk.
    public bool Running;


    // ==============================
    // COMBAT ACTIONS
    // ==============================

    // One-shot actions.
    public bool LightAttack;
    public bool HeavyAttack;
    public bool Dodge;

    // Held action.
    public bool Block;
}