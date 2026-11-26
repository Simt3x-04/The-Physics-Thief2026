using System;
using System.Globalization;

namespace PhysicsThief.Items
{
    /// <summary>Outcome of an attempt to eat an apple (the HUD turns it into a message).</summary>
    public enum AppleEatResult
    {
        Eaten,
        HealthFull,
        PlayerDead,
        AppleUnavailable,
        GameplayInactive,
        NoHealthComponent
    }

    /// <summary>
    /// Apple rules (GDD 3.6 and 4.2) in pure C#, so they can be tested without a scene.
    /// An apple restores 25 HP, capped at the maximum. At full health it is not eaten and stays on the tree.
    /// </summary>
    public static class AppleEatRules
    {
        public const float DefaultHealAmount = 25f;

        /// <summary>Decides whether the apple can be eaten now. The order matters: the first failing check is reported.</summary>
        public static AppleEatResult Evaluate(bool appleAvailable, bool gameplayActive, bool playerDead, float currentHP, float maxHP)
        {
            if (!gameplayActive) return AppleEatResult.GameplayInactive;
            if (playerDead) return AppleEatResult.PlayerDead;
            if (!appleAvailable) return AppleEatResult.AppleUnavailable;
            if (currentHP >= maxHP) return AppleEatResult.HealthFull;
            return AppleEatResult.Eaten;
        }

        /// <summary>HP actually restored: e.g. 25 at 60/100, 10 at 90/100, 0 at full health.</summary>
        public static float HealedAmount(float currentHP, float maxHP, float healAmount)
        {
            if (healAmount <= 0f) return 0f;
            return Math.Max(0f, Math.Min(maxHP, currentHP + healAmount) - currentHP);
        }

        /// <summary>Interaction prompt, e.g. "E — Eat apple (+25 HP)".</summary>
        public static string Prompt(float healAmount)
        {
            return "E — Eat apple (+" + FormatHP(healAmount) + " HP)";
        }

        /// <summary>Short message for the player (null when nothing should be shown).</summary>
        public static string ToMessage(AppleEatResult result, float healedAmount)
        {
            switch (result)
            {
                case AppleEatResult.Eaten: return "Ate an apple: +" + FormatHP(healedAmount) + " HP";
                case AppleEatResult.HealthFull: return "Health is already full";
                default: return null;
            }
        }

        private static string FormatHP(float value)
        {
            return Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
        }
    }
}
