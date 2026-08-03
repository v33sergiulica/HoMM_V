using System;
using System.Collections.Generic;
using UnityEngine;

namespace HommClone.Heroes
{
    public enum SecondarySkillType
    {
        Logistics,   // Increases Hero Movement Points on World Map (+10%, +20%, +30%)
        Leadership,  // Increases Army Morale (+1, +2, +3)
        Luck,        // Increases Army Luck (+1, +2, +3)
        Offense,     // Increases Army Attack (+2, +3, +4)
        Armorer,     // Increases Army Defense (+2, +3, +4)
        LightMagic,  // Master Light Magic Spells (Basic/Advanced/Expert Mastery)
        DarkMagic,   // Master Dark Magic Spells (Basic/Advanced/Expert Mastery)
        DestructiveMagic // Master Destructive Magic Spells (Basic/Advanced/Expert Mastery)
    }

    public enum SkillRank
    {
        Basic = 1,
        Advanced = 2,
        Expert = 3
    }

    [Serializable]
    public class SecondarySkillSlot
    {
        public SecondarySkillType type;
        public SkillRank rank;

        public SecondarySkillSlot(SecondarySkillType type, SkillRank rank = SkillRank.Basic)
        {
            this.type = type;
            this.rank = rank;
        }

        public string GetDisplayName()
        {
            return $"{rank} {type}";
        }

        public string GetDescription()
        {
            switch (type)
            {
                case SecondarySkillType.Logistics:
                    float mpBonus = (int)rank * 10f;
                    return $"+{mpBonus:F0}% Hero Movement Points on World Map";
                case SecondarySkillType.Leadership:
                    int moraleBonus = (int)rank;
                    return $"+{moraleBonus} Morale for all army stacks in battle";
                case SecondarySkillType.Luck:
                    int luckBonus = (int)rank;
                    return $"+{luckBonus} Luck for all army stacks in battle";
                case SecondarySkillType.Offense:
                    int offAttBonus = (int)rank + 1;
                    return $"+{offAttBonus} Attack for all army stacks in battle";
                case SecondarySkillType.Armorer:
                    int armDefBonus = (int)rank + 1;
                    return $"+{armDefBonus} Defense for all army stacks in battle";
                case SecondarySkillType.LightMagic:
                    return $"Increases potency and unlocks Advanced/Expert effects for Light Magic Spells ({rank} Mastery)";
                case SecondarySkillType.DarkMagic:
                    return $"Increases potency and unlocks Advanced/Expert effects for Dark Magic Spells ({rank} Mastery)";
                case SecondarySkillType.DestructiveMagic:
                    return $"Increases damage output and unlocks Advanced/Expert effects for Destructive Spells ({rank} Mastery)";
                default:
                    return "";
            }
        }
    }
}
