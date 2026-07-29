namespace AdventureGame;

// Represents a single enemy the player will fight in one encounter.
// Kept intentionally small: name, flavor description, HP, and attack power.
public class Enemy
{
    public string Name { get; set; } = "Unknown Foe";
    public string Description { get; set; } = "";
    public int HP { get; set; } = 20;
    public int MaxHP { get; set; } = 20;
    public int Attack { get; set; } = 5;

    // Signature move flavor name shown in combat when the enemy unleashes it.
    public string SpecialName { get; set; } = "Savage Blow";

    // One-time-use special ability. Enemies may unleash it at any point during
    // their turn (currently: alongside their normal attack, once per fight).
    public bool HasSpecial { get; set; } = true;

    // True once the enemy has been defeated (HP <= 0).
    public bool IsDead => HP <= 0;

    // Applies damage to the enemy, clamping HP at 0 so it never goes negative.
    public void TakeDamage(int amount)
    {
        if (amount < 0) amount = 0;
        HP -= amount;
        if (HP < 0) HP = 0;
    }

    // Consume the one-shot special and deal a heavy blow to the player.
    // Ignores the player's Defense stat (but still respects "defending" halving)
    // by pre-adding the defense value to the raw damage. Returns actual damage dealt,
    // or 0 if the special was already used.
    public int UseSpecial(Player target, Random rng)
    {
        if (!HasSpecial) return 0;
        HasSpecial = false;

        // Roughly double a normal hit plus variance; add the target's defense
        // back in so Player.TakeDamage's defense subtraction is effectively bypassed.
        int raw = Math.Min(target.HP/2, (Attack * 2) + rng.Next(2, 6));
        return target.TakeDamage(raw);
    }
}
