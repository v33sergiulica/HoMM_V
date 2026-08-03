using UnityEngine;
using HommClone.Creatures;
using HommClone.Turns;

namespace HommClone.Spells
{
    public enum SpellType { Damage, Buff, Debuff }
    public enum MagicSchool { Light, Dark, Destructive }
    public enum SpellMastery { Basic, Intermediate, Advanced, Expert }

    /// <summary>
    /// Base class for castable spells. Blueprints are created as ScriptableObjects.
    /// </summary>
    public abstract class Spell : ScriptableObject
    {
        [SerializeField] private string spellName = "New Spell";
        [SerializeField] private int manaCost = 5;
        [SerializeField] private SpellType spellType = SpellType.Damage;
        [SerializeField] private MagicSchool magicSchool = MagicSchool.Destructive;
        [SerializeField] private Sprite icon;
        [SerializeField] [TextArea(2, 4)] private string description = "";

        public string SpellName => spellName;
        public int ManaCost => manaCost;
        public SpellType Type => spellType;
        public MagicSchool School => magicSchool;
        public Sprite Icon => icon;
        public string Description => description;

        /// <summary>
        /// Executes the spell effect on the target stack.
        /// </summary>
        public abstract void ApplyEffect(ITimelineParticipant caster, CreatureStack target, SpellMastery mastery);
    }
}
