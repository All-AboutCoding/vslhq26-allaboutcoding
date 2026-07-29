namespace AdventureGame;

// Represents a single enemy the player will fight in one encounter.
// Kept intentionally small: name, flavor description, HP, and attack power.
public class Enemy
{
    public string Name { get; set; } = "Unknown Foe";
    public string Description { get; set; } = "";
    public int HP { get; set; } = 20;
    public int Attack { get; set; } = 5;

    // True once the enemy has been defeated (HP <= 0).
    public bool IsDead => HP <= 0;

    // Applies damage to the enemy, clamping HP at 0 so it never goes negative.
    public void TakeDamage(int amount)
    {
        if (amount < 0) amount = 0;
        HP -= amount;
        if (HP < 0) HP = 0;
    }
}
