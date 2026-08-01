using System.Collections.Generic;

namespace HommClone.Artifacts
{
    public static class ArtifactCatalog
    {
        private static List<ArtifactData> _allArtifacts;
        private static List<ArtifactSetData> _allSets;

        public static List<ArtifactData> GetAllArtifacts()
        {
            if (_allArtifacts == null)
            {
                _allArtifacts = new List<ArtifactData>();

                // Weapons (RightHand)
                var swordOfMight = new ArtifactData("sword_might", "Sword of Might", "Forged in ancient fires. Grants +4 Attack.", "[Weapon]", ArtifactSlotType.RightHand, ArtifactRarity.Major)
                { attackBonus = 4 };
                _allArtifacts.Add(swordOfMight);

                var staffOfMagius = new ArtifactData("staff_magius", "Staff of Magius", "Radiates arcane energy. Grants +4 Spell Power.", "[Staff]", ArtifactSlotType.RightHand, ArtifactRarity.Major)
                { spellPowerBonus = 4 };
                _allArtifacts.Add(staffOfMagius);

                // Shields (LeftHand)
                var shieldDead = new ArtifactData("shield_dead", "Shield of Yawning Dead", "Sturdy bone barrier. Grants +4 Defense.", "[Shield]", ArtifactSlotType.LeftHand, ArtifactRarity.Major)
                { defenseBonus = 4 };
                _allArtifacts.Add(shieldDead);

                // Helmets (Head)
                var crownKnowledge = new ArtifactData("crown_knowledge", "Crown of Supreme Knowledge", "Enhances mental clarity. Grants +4 Knowledge.", "[Helm]", ArtifactSlotType.Head, ArtifactRarity.Major)
                { knowledgeBonus = 4 };
                _allArtifacts.Add(crownKnowledge);

                // Armor (Body)
                var dragonScaleArmor = new ArtifactData("dragon_armor", "Dragon Scale Cuirass", "Impenetrable dragon scales. Grants +3 Defense & +2 Attack.", "[Armor]", ArtifactSlotType.Body, ArtifactRarity.Relic)
                { defenseBonus = 3, attackBonus = 2 };
                _allArtifacts.Add(dragonScaleArmor);

                // Boots (Feet)
                var bootsSpeed = new ArtifactData("boots_speed", "Boots of Speed", "Enchanted footwear. Grants +4 Movement Points per day.", "[Boots]", ArtifactSlotType.Feet, ArtifactRarity.Common)
                { movementPointsBonus = 4f };
                _allArtifacts.Add(bootsSpeed);

                // Necklaces (Neck)
                var amuletLife = new ArtifactData("amulet_life", "Amulet of Life", "Pulses with vitality. Grants +1 Morale & +1 Luck.", "[Amulet]", ArtifactSlotType.Neck, ArtifactRarity.Common)
                { moraleBonus = 1, luckBonus = 1 };
                _allArtifacts.Add(amuletLife);

                // Rings (RingLeft / RingRight)
                var ringLuck = new ArtifactData("ring_luck", "Ring of the Leprechaun", "Brings good fortune. Grants +2 Luck.", "[Ring]", ArtifactSlotType.RingLeft, ArtifactRarity.Common)
                { luckBonus = 2 };
                _allArtifacts.Add(ringLuck);

                var ringLeadership = new ArtifactData("ring_leadership", "Ring of Leadership", "Inspires confidence in troops. Grants +2 Morale.", "[Ring]", ArtifactSlotType.RingRight, ArtifactRarity.Common)
                { moraleBonus = 2 };
                _allArtifacts.Add(ringLeadership);

                // Capes (Cape)
                var capeVampire = new ArtifactData("cape_vampire", "Vampire Mantle", "Woven from shadows. Grants +2 Spell Power & +2 Knowledge.", "[Cape]", ArtifactSlotType.Cape, ArtifactRarity.Major)
                { spellPowerBonus = 2, knowledgeBonus = 2 };
                _allArtifacts.Add(capeVampire);

                // Pocket / Relics (Pocket)
                var tomeFire = new ArtifactData("tome_fire", "Tome of Fire Magic", "Ancient spellbook filled with flame incantations. Grants +3 Spell Power.", "[Tome]", ArtifactSlotType.Pocket, ArtifactRarity.Major)
                { spellPowerBonus = 3 };
                _allArtifacts.Add(tomeFire);

                // --- LIONHEART ARTIFACT SET (Weapon + Armor + Helm) ---
                var lionBlade = new ArtifactData("lion_blade", "Lionheart Blade", "Part of the Lionheart Set. Grants +3 Attack.", "[Set Weapon]", ArtifactSlotType.RightHand, ArtifactRarity.Relic, "lionheart_set")
                { attackBonus = 3 };
                _allArtifacts.Add(lionBlade);

                var lionArmor = new ArtifactData("lion_armor", "Lionheart Plate", "Part of the Lionheart Set. Grants +3 Defense.", "[Set Armor]", ArtifactSlotType.Body, ArtifactRarity.Relic, "lionheart_set")
                { defenseBonus = 3 };
                _allArtifacts.Add(lionArmor);

                var lionHelm = new ArtifactData("lion_helm", "Lionheart Helm", "Part of the Lionheart Set. Grants +2 Morale.", "[Set Helm]", ArtifactSlotType.Head, ArtifactRarity.Relic, "lionheart_set")
                { moraleBonus = 2 };
                _allArtifacts.Add(lionHelm);
            }
            return _allArtifacts;
        }

        public static List<ArtifactSetData> GetAllSets()
        {
            if (_allSets == null)
            {
                _allSets = new List<ArtifactSetData>();

                // Lionheart Set definition
                var lionSet = new ArtifactSetData("lionheart_set", "Lionheart Pride Set", new List<string> { "lion_blade", "lion_armor", "lion_helm" });
                lionSet.setBonuses.Add(new ArtifactSetRequirement(2, "Set Bonus (2/3 Pieces): +3 Attack & +3 Defense")
                { attackBonus = 3, defenseBonus = 3 });
                lionSet.setBonuses.Add(new ArtifactSetRequirement(3, "Set Bonus (3/3 Full Set): +5 Attack, +5 Defense & +2 Morale")
                { attackBonus = 5, defenseBonus = 5, moraleBonus = 2 });

                _allSets.Add(lionSet);
            }
            return _allSets;
        }

        public static ArtifactData GetArtifactById(string id)
        {
            return GetAllArtifacts().Find(a => a.id == id);
        }

        public static ArtifactSetData GetSetById(string setId)
        {
            return GetAllSets().Find(s => s.setId == setId);
        }
    }
}
