using UnityEngine;
using HommClone.Creatures;
using HommClone.Turns;

namespace HommClone.Spells
{
    [CreateAssetMenu(fileName = "IceBoltSpell", menuName = "HOMM/Spells/Ice Bolt")]
    public class IceBoltSpell : Spell
    {
        public override void ApplyEffect(ITimelineParticipant caster, CreatureStack target, SpellMastery mastery)
        {
            if (caster == null || target == null || target.IsDead) return;

            // Mastery multiplier cst: Basic = 1, Intermediate = 1.2, Advanced = 1.5, Expert = 2.0
            float cst = 1.0f;
            switch (mastery)
            {
                case SpellMastery.Basic: cst = 1.0f; break;
                case SpellMastery.Intermediate: cst = 1.2f; break;
                case SpellMastery.Advanced: cst = 1.5f; break;
                case SpellMastery.Expert: cst = 2.0f; break;
            }

            // Formula: sp * (cst^2 * 20)
            int spellDamage = Mathf.RoundToInt(caster.SpellPower * (cst * cst * 20f));

            Debug.Log($"[Spell Cast] {caster.Name} casts Ice Bolt on {target.gameObject.name}. SpellPower: {caster.SpellPower}, Mastery: {mastery}, Damage: {spellDamage}");
            
            // Apply damage directly
            target.TakeDamage(spellDamage);

            // Spawn floating text in a nice ice blue/cyan color
            var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
            if (uiManager != null)
            {
                // We show the damage text in cyan with spell name tag
                uiManager.SpawnDamageText(target.transform.position + Vector3.up * 1.8f, $"-{spellDamage}\n<color=#44ccff><size=75%>Ice Bolt</size></color>", Color.cyan);
            }
        }
    }
}
