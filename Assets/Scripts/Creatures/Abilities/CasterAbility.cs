using UnityEngine;
using System.Collections.Generic;
using HommClone.Spells;

namespace HommClone.Creatures
{
    /// <summary>
    /// Caster ability allowing a unit to access a separate spell menu during combat.
    /// </summary>
    [CreateAssetMenu(fileName = "CasterAbility", menuName = "HOMM/Abilities/Caster")]
    public class CasterAbility : CreatureAbility
    {
        [Header("Spell Book Configuration")]
        [SerializeField] private List<Spells.Spell> spells = new List<Spells.Spell>();
        [SerializeField] private SpellMastery mastery = SpellMastery.Basic;

        public List<Spells.Spell> Spells => spells;
        public SpellMastery Mastery => mastery;

        public override void ModifyAIAttributes(CreatureData data, ref float attackComp, ref float defenseComp, ref float speed, ref float initiative)
        {
            // For caster: attackComp = (magical_power + physical_power) / 2
            float magicalA = data.MaxMana / 20f;
            attackComp = (magicalA + attackComp) / 2f;
        }
    }
}
