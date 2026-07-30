namespace AdventureGame;

// The character archetypes the player can pick. `Custom` lets the user
// define their own class name and signature move on top of a balanced stat block.
public enum ClassType
{
    Warrior,
    Mage,
    Archer,
    Custom
}

// The player character. Holds identity, stats, and the class-specific
// special ability. Kept as a simple class with public getters/setters
// per the "keep it simple, no complex patterns" requirement.
public class Player
{
    public string Name { get; set; } = "Hero";
    public ClassType Class { get; set; }

    // Display name for the class — shown in prompts and passed to the LLM.
    // For built-in classes this mirrors the enum name; for Custom it's the user-typed name.
    public string ClassName { get; set; } = "";

    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }

    // Name of the class's signature move (shown in the combat menu).
    public string SpecialName { get; set; } = "Special";

    // Limited-use resource so specials feel meaningful without a cooldown system.
    public int SpecialUsesRemaining { get; set; }

    // One-time consumable that restores a random 10-25% of MaxHP.
    // Can be used at any point during the player's turn without consuming the action.
    public bool HasHealthPotion { get; set; } = true;

    // True while the player is defending; halves the next incoming hit.
    public bool IsDefending { get; set; }

    public bool IsDead => HP <= 0;

    // Consume the health potion, restoring 10-25% of MaxHP.
    // Returns the amount actually healed, or 0 if no potion is available or already at full HP.
    public int UseHealthPotion(Random rng)
    {
        if (!HasHealthPotion) return 0;
        HasHealthPotion = false;

        // Random percentage in [30, 60], rounded up so low-MaxHP classes still get at least 1 HP.
        int percent = rng.Next(30, 61);
        int healAmount = (MaxHP * percent + 99) / 100;

        int before = HP;
        HP += healAmount;
        if (HP > MaxHP) HP = MaxHP;
        return HP - before;
    }

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
    // For ClassType.Custom, pass customClassName plus an optional customStats block
    // (typically produced by LlmService.GenerateCustomClassAsync). If customStats
    // is null a balanced default block is used.
    public static Player CreateForClass(
        string name,
        ClassType classType,
        string? customClassName = null,
        CustomClassStats? customStats = null)
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

            case ClassType.Custom:
                // Prefer LLM-designed stats; fall back to a balanced middle-of-the-road block.
                p.MaxHP = customStats?.HP ?? 32;
                p.HP = p.MaxHP;
                p.Attack = customStats?.Attack ?? 9;
                p.Defense = customStats?.Defense ?? 2;
                p.SpecialName = string.IsNullOrWhiteSpace(customStats?.SpecialName)
                    ? "Signature Move"
                    : customStats!.SpecialName;
                p.SpecialUsesRemaining = customStats?.SpecialUses ?? 2;
                break;
        }

        // ClassName display: prefer user input for Custom, otherwise the enum name.
        p.ClassName = classType == ClassType.Custom && !string.IsNullOrWhiteSpace(customClassName)
            ? customClassName!.Trim()
            : classType.ToString();

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

            case ClassType.Custom:
                // Balanced signature move: solid multiplier on Attack with small variance.
                damage = (int)(Attack * 1.75) + rng.Next(1, 5);
                break;

            default:
                damage = Attack;
                break;
        }

        target.TakeDamage(damage);
        return damage;
    }
}
