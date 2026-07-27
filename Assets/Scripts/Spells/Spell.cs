using UnityEngine;
using HommClone.Creatures;
using HommClone.Turns;

namespace HommClone.Spells
{
    public enum SpellType { Damage, Buff, Debuff }

    /// <summary>
    /// Base class for castable spells. Blueprints are created as ScriptableObjects.
    /// </summary>
    public abstract class Spell : ScriptableObject
    {
        [SerializeField] private string spellName = "New Spell";
        [SerializeField] private int manaCost = 5;
        [SerializeField] private SpellType spellType = SpellType.Damage;

        public string SpellName => spellName;
        public int ManaCost => manaCost;
        public SpellType Type => spellType;

        /// <summary>
        /// Executes the spell effect on the target stack.
        /// </summary>
        public abstract void ApplyEffect(ITimelineParticipant caster, CreatureStack target, SpellMastery mastery);
    }
}
