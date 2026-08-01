using System;
using UnityEngine;

namespace HommClone.Artifacts
{
    public enum ArtifactSlotType
    {
        Head,       // Helmet / Crown
        Neck,       // Amulet / Pendant
        Body,       // Armor / Robes
        RightHand,  // Primary Weapon / Sword / Staff
        LeftHand,   // Shield / Tome
        RingLeft,   // Left Ring
        RingRight,  // Right Ring
        Feet,       // Boots
        Cape,       // Cloak / Mantle
        Pocket      // Misc / Relic / Pocket
    }

    public enum ArtifactRarity
    {
        Common,
        Major,
        Relic
    }

    [Serializable]
    public class ArtifactData
    {
        public string id;
        public string name;
        public string description;
        public string iconSymbol;
        public Sprite iconSprite;
        public ArtifactSlotType slotType;
        public ArtifactRarity rarity;

        // Base Stat Bonuses
        public int attackBonus;
        public int defenseBonus;
        public int spellPowerBonus;
        public int knowledgeBonus;
        public int moraleBonus;
        public int luckBonus;
        public float movementPointsBonus;

        // Set Link (Optional, e.g. "lionheart_set", "dragon_slayer_set")
        public string setId;

        public ArtifactData(string id, string name, string description, string iconSymbol, ArtifactSlotType slotType, ArtifactRarity rarity = ArtifactRarity.Common, string setId = null)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.iconSymbol = iconSymbol;
            this.slotType = slotType;
            this.rarity = rarity;
            this.setId = setId;
        }

        public ArtifactData Clone()
        {
            return (ArtifactData)this.MemberwiseClone();
        }
    }
}
