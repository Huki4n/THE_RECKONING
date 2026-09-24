using TheReckoning.Morale;

namespace TheReckoning
{
    public static class TooltipContent
    {
        public static void Order(OrderAbility ability, out string title, out string description)
        {
            switch (ability)
            {
                case OrderAbility.HeavenlyStrike:
                    title = "Heavenly Wrath";
                    description = "Choose a point between the bases. After 0.5 s, Cult units in the area take 150 damage. Cooldown: 30 s.";
                    break;
                case OrderAbility.HolyShield:
                    title = "Holy Shield";
                    description = "Current Order units take 40% less damage for 5 s. Cooldown: 35 s.";
                    break;
                case OrderAbility.Encouragement:
                    title = "Inspiration";
                    description = "Order units gain 25% damage and 20% attack speed for 6 s, including new recruits. Cooldown: 35 s.";
                    break;
                case OrderAbility.Purification:
                    title = "Cleansing";
                    description = "Remove Panic and block Fear for 3 s. If neither is active, gain 20% production speed for 3 s. Cooldown: 30 s.";
                    break;
                default:
                    title = "";
                    description = "";
                    break;
            }
        }

        public static string Cult(CultAbilityEffect effect)
        {
            switch (effect)
            {
                case CultAbilityEffect.FallenCurse:
                    return "When enough Order units are alive, they lose health but stay above 0 HP.";
                case CultAbilityEffect.DarkBlessing:
                    return "When the Cult base is damaged and Order units approach it, current Cult units take less damage.";
                case CultAbilityEffect.Bloodlust:
                    return "After several Order units die quickly, Cult units gain damage and movement speed.";
                case CultAbilityEffect.WillOfTheFallen:
                    return "When the Cult base becomes heavily damaged, current Cult units gain damage and attack speed. Triggers once.";
                default: return "";
            }
        }

        public static string Morale(MoraleEffect effect)
        {
            switch (effect)
            {
                case MoraleEffect.Panic: return "Heavy recent losses slow this side's production.";
                case MoraleEffect.Inspiration: return "A burst of kills increases this side's attack speed.";
                case MoraleEffect.Demoralization: return "The death of a veteran lowers this side's damage.";
                case MoraleEffect.BattleCry: return "Nearby allied veterans increase their allies' damage while in range.";
                case MoraleEffect.Rage: return "Heavy damage to the base increases this side's damage and movement speed.";
                case MoraleEffect.Resilience: return "After a threat to the base ends, units take less damage and production speeds up.";
                case MoraleEffect.Confidence: return "A successful kill streak opens a window in which new recruits receive a damage bonus.";
                case MoraleEffect.Fear: return "An experienced enemy veteran nearby reduces this unit's attack speed.";
                default: return "";
            }
        }
    }
}
