using System.Collections.Generic;
using UnityEngine;
using HommClone.World;

namespace HommClone.Heroes
{
    /// <summary>
    /// Computes and holds all dynamic combat bonuses granted by the Hero's
    /// Primary Attributes (Attack, Defense, Spell Power, Knowledge),
    /// Secondary Skills (Offense, Armorer, Leadership, Luck), and Special Perks.
    /// </summary>
    public class HeroBattleModifiers
    {
        public int attackBonus;
        public int defenseBonus;
        public int moraleBonus;
        public int luckBonus;
        public float meleeDamageMultiplier = 1.0f;
        public float damageTakenMultiplier = 1.0f;
        public List<string> activeSpecialPerks = new List<string>();

        public static HeroBattleModifiers FromHero(HeroData hero)
        {
            HeroBattleModifiers mods = new HeroBattleModifiers();
            if (hero == null) return mods;

            // 1. Primary Attributes & Artifact/Set Bonuses (Hero Attack, Defense, Morale & Luck add directly to army!)
            mods.attackBonus += hero.GetTotalAttack();
            mods.defenseBonus += hero.GetTotalDefense();
            mods.moraleBonus += hero.GetTotalMorale();
            mods.luckBonus += hero.GetTotalLuck();

            // 2. Secondary Skills
            if (hero.secondarySkills != null)
            {
                foreach (var skill in hero.secondarySkills)
                {
                    switch (skill.type)
                    {
                        case SecondarySkillType.Offense:
                            // Basic: +2 Att, Advanced: +3 Att, Expert: +4 Att
                            mods.attackBonus += (int)skill.rank + 1;
                            break;
                        case SecondarySkillType.Armorer:
                            // Basic: +2 Def, Advanced: +3 Def, Expert: +4 Def
                            mods.defenseBonus += (int)skill.rank + 1;
                            break;
                        // Leadership & Luck are already included in GetTotalMorale() and GetTotalLuck() above!
                    }
                }
            }

            return mods;
        }

        public bool HasSpecialPerk(string perkName)
        {
            return activeSpecialPerks.Contains(perkName);
        }
    }
}
