using UnityEngine;
using System.Collections.Generic;
using HommClone.Spells;

namespace HommClone.Creatures
{
    public enum Faction
    {
        Haven,
        Inferno,
        Necropolis,
        Academy,
        Sylvan,
        Dungeon,
        Fortress,
        Stronghold,
        Neutrals
    }

    /// <summary>
    /// Static data configuration template for a creature type.
    /// Created as a ScriptableObject asset inside the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCreatureData", menuName = "HOMM/Creature Data")]
    public class CreatureData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string creatureName = "New Creature";
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite icon;

        [Header("Faction & Tier")]
        [SerializeField] private Faction faction = Faction.Haven;
        [SerializeField] [Range(1, 7)] private int tier = 1;
        [SerializeField] private int weeklyGrowth = 1;

        [Header("Combat Attributes")]
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int attack = 5;
        [SerializeField] private int defense = 5;
        [SerializeField] private int initiative = 10;
        [SerializeField] private int speed = 5;

        [Header("Damage")]
        [SerializeField] private int minDamage = 1;
        [SerializeField] private int maxDamage = 2;

        [Header("Special Attributes")]
        [SerializeField] private int maxAmmo = 0;
        [SerializeField] private int maxMana = 0;
        [SerializeField] private bool isRanged = false;

        [Header("Abilities")]
        [SerializeField] private List<CreatureAbility> abilities = new List<CreatureAbility>();

        [Header("AI & Balancing Override")]
        [SerializeField] private float customPowerMultiplier = 1f;
        [SerializeField] private int customPowerOffset = 0;

        // Getters
        public string CreatureName => creatureName;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;
        public Faction FactionType => faction;
        public int Tier => tier;
        public int WeeklyGrowth => weeklyGrowth;
        public int MaxHealth => maxHealth;
        public int Attack => attack;
        public int Defense => defense;
        public int Initiative => initiative;
        public int Speed => speed;
        public int MinDamage => minDamage;
        public int MaxDamage => maxDamage;
        public int MaxAmmo => maxAmmo;
        public int MaxMana => maxMana;
        public bool IsRanged => isRanged;
        public List<CreatureAbility> Abilities => abilities;

        /// <summary>
        /// Returns true if the creature possesses the Flying ability in its list.
        /// </summary>
        public bool IsFlying
        {
            get
            {
                if (abilities == null) return false;
                foreach (var ability in abilities)
                {
                    if (ability is FlyingAbility) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if the creature possesses the Large Creature ability.
        /// </summary>
        public bool IsLarge
        {
            get
            {
                if (abilities == null) return false;
                foreach (var ability in abilities)
                {
                    if (ability is LargeCreatureAbility) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Calculates the single creature's AI power value based on standard attributes.
        /// New balanced formula factoring in Attack/Defense ratios, speed scaling, and initiative modifiers.
        /// </summary>
        public int AIValue
        {
            get
            {
                float avgDmg = (minDamage + maxDamage) / 2f;
                float A = avgDmg * (1f + 0.05f * attack);
                float rawA = A;
                float D = maxHealth * (1f + 0.05f * defense);

                // For shooters without NoRangePenalty: A is reduced by 25% (A = 0.75 * A)
                if (isRanged)
                {
                    bool hasNoRangePenalty = false;
                    if (abilities != null)
                    {
                        foreach (var ab in abilities)
                        {
                            if (ab != null && ab.GetType().Name == "NoRangePenaltyAbility")
                            {
                                hasNoRangePenalty = true;
                                break;
                            }
                        }
                    }

                    if (!hasNoRangePenalty)
                    {
                        A *= 0.75f;
                    }

                A *= 2.5f;
                }

                float S = speed;
                float I = initiative;

                // Let abilities modify attributes (e.g. CasterAbility adjusts A component)
                if (abilities != null)
                {
                    foreach (var ab in abilities)
                    {
                        if (ab != null)
                        {
                            ab.ModifyAIAttributes(this, ref A, ref D, ref S, ref I);
                        }
                    }
                }

                float x = 1f + 1.5f * Mathf.Min(Mathf.Max(0f, A / D - 0.25f), 3);
                float y = 1f + 1f * Mathf.Min(Mathf.Max(0f, D / rawA - 3.5f), 2);
                // Base physical power (using the user's balanced additive formula)
                float rawPower = (2f * x * A) + (y * D) + S + I - 2f;

                // Apply dynamic ability multipliers and offsets (e.g. FlyingAbility adding +15% power)
                float abMultiplier = 1.0f;
                int abOffset = 0;
                if (abilities != null)
                {
                    foreach (var ab in abilities)
                    {
                        if (ab != null)
                        {
                            abMultiplier *= ab.GetAIPowerMultiplier();
                            abOffset += ab.GetAIPowerOffset();
                        }
                    }
                }

                float finalPower = (rawPower * abMultiplier + abOffset) * customPowerMultiplier + customPowerOffset;
                return Mathf.Max(1, Mathf.RoundToInt(finalPower));
            }
        }
    }
}
