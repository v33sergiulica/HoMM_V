using UnityEngine;
using HommClone.Creatures;
using HommClone.Turns;

namespace HommClone.Spells
{
    [CreateAssetMenu(fileName = "HasteSpell", menuName = "HOMM/Spells/Haste")]
    public class HasteSpell : Spell
    {
        public override void ApplyEffect(ITimelineParticipant caster, CreatureStack target, SpellMastery mastery)
        {
            if (caster == null || target == null || target.IsDead) return;

            // Mastery speed boost percent: Basic = 20%, Intermediate = 30%, Advanced = 45%, Expert = 60%
            float boostPercent = 0.20f;
            switch (mastery)
            {
                case SpellMastery.Basic: boostPercent = 0.20f; break;
                case SpellMastery.Intermediate: boostPercent = 0.30f; break;
                case SpellMastery.Advanced: boostPercent = 0.45f; break;
                case SpellMastery.Expert: boostPercent = 0.60f; break;
            }

            // Calculate actual initiative increase amount (minimum 1)
            float initBoostRaw = target.Initiative * boostPercent;
            float initiativeIncrease = Mathf.Max(1f, Mathf.Round(initBoostRaw * 10f) / 10f); // Round to 1 decimal place
            int duration = caster.SpellPower; // Duration is equal to SpellPower

            Debug.Log($"[Spell Cast] {caster.Name} casts Haste on {target.gameObject.name}. Initiative: {target.Initiative} -> {target.Initiative + initiativeIncrease}. Duration: {duration} rounds.");

            // Apply initiative modifier
            target.AddInitiativeModifier(initiativeIncrease);

            // Add status effect to target to restore initiative when the spell expires
            target.AddStatusEffect(new ActiveStatusEffect
            {
                effectName = "Haste",
                duration = duration,
                onRemove = (s) =>
                {
                    s.AddInitiativeModifier(-initiativeIncrease);
                    Debug.Log($"[Spell Expired] Haste expired on {s.gameObject.name}. Removed initiative boost (-{initiativeIncrease}).");
                }
            });

            // Spawn floating text in a nice bright gold/yellow color
            var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
            if (uiManager != null)
            {
                uiManager.SpawnDamageText(target.transform.position + Vector3.up * 1.8f, $"Hasted!\n<color=#ffe044><size=75%>+{initiativeIncrease:F1} Initiative</size></color>", new Color(1f, 0.85f, 0.2f));
            }
        }
    }
}
