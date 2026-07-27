using UnityEngine;

namespace HommClone.Creatures
{
    /// <summary>
    /// Base class for all creature abilities.
    /// Derived classes can implement specific hooks to modify combat behavior.
    /// </summary>
    public abstract class CreatureAbility : ScriptableObject
    {
        [Header("Ability Info")]
        [SerializeField] private string abilityName;
        [SerializeField] [TextArea] private string description;

        public string AbilityName => abilityName;
        public string Description => description;

        // Hook definitions for derived abilities to override
        public virtual void OnAttack(CreatureStack attacker, CreatureStack defender, ref int damage) { }
        public virtual void OnTakeDamage(CreatureStack defender, CreatureStack attacker, ref int damage) { }
        public virtual void OnTurnStart(CreatureStack stack) { }
        public virtual void OnTurnEnd(CreatureStack stack) { }

        // Hooks for extensible AIValue power calculation
        public virtual float GetAIPowerMultiplier() => 1.0f;
        public virtual int GetAIPowerOffset() => 0;
        public virtual void ModifyAIAttributes(CreatureData data, ref float attackComp, ref float defenseComp, ref float speed, ref float initiative) { }
    }
}
