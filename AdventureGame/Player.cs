namespace AdventureGame;

// The three character archetypes the player can pick.
public enum ClassType
{
    Warrior,
    Mage,
    Archer
}

// The player character. Holds identity, stats, and the class-specific
// special ability. Kept as a simple class with public getters/setters
// per the "keep it simple, no complex patterns" requirement.
public class Player
{
    public string Name { get; set; } = "Hero";
    public ClassType Class { get; set; }

    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }

    // Name of the class's signature move (shown in the combat menu).
    public string SpecialName { get; set; } = "Special";

    // Limited-use resource so specials feel meaningful without a cooldown system.
    public int SpecialUsesRemaining { get; set; }

    // True while the player is defending; halves the next incoming hit.
    public bool IsDefending { get; set; }

    public bool IsDead => HP <= 0;

    // Reduce HP, honoring the Defense stat and the "defending" state.
    // Returns the actual damage dealt so the UI can report it.
    public int TakeDamage(int incoming)
    {
        // Defense subtracts from raw damage; defending halves what's left.
        int dmg = incoming - Defense;
        if (dmg < 1) dmg = 1; // Guarantee a small chip so fights don't stall.
        if (IsDefending) dmg = dmg / 2;
        if (dmg < 0) dmg = 0;

        HP -= dmg;
        if (HP < 0) HP = 0;

        // "Defend" only protects for the one incoming attack after it was chosen.
        IsDefending = false;
        return dmg;
    }

    // Factory: builds a fresh Player with the stat block for the chosen class.
    // Tweak these numbers to rebalance the game.
    public static Player CreateForClass(string name, ClassType classType)
    {
        Player p = new Player { Name = name, Class = classType };

        switch (classType)
        {
            case ClassType.Warrior:
                p.MaxHP = 40; p.HP = 40;
                p.Attack = 8; p.Defense = 3;
                p.SpecialName = "Power Strike";
                p.SpecialUsesRemaining = 2;
                break;

            case ClassType.Mage:
                p.MaxHP = 25; p.HP = 25;
                p.Attack = 10; p.Defense = 1;
                p.SpecialName = "Fireball";
                p.SpecialUsesRemaining = 2;
                break;

            case ClassType.Archer:
                p.MaxHP = 30; p.HP = 30;
                p.Attack = 9; p.Defense = 2;
                p.SpecialName = "Piercing Shot";
                p.SpecialUsesRemaining = 2;
                break;
        }

        return p;
    }

    // Execute the class-specific special move against an enemy.
    // Returns the damage dealt, or 0 if no uses remain.
    public int UseSpecial(Enemy target, Random rng)
    {
        if (SpecialUsesRemaining <= 0) return 0;
        SpecialUsesRemaining--;

        int damage;
        switch (Class)
        {
            case ClassType.Warrior:
                // Power Strike: double normal attack with small variance.
                damage = (Attack * 2) + rng.Next(0, 4);
                break;

            case ClassType.Mage:
                // Fireball: big flat magical hit.
                damage = 15 + rng.Next(0, 5);
                break;

            case ClassType.Archer:
                // Piercing Shot: ignores enemy defense (we don't model enemy
                // defense so just add a solid bonus on top of attack).
                damage = Attack + 5 + rng.Next(0, 3);
                break;

            default:
                damage = Attack;
                break;
        }

        target.TakeDamage(damage);
        return damage;
    }
}
