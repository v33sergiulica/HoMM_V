using UnityEngine;
using HommClone.Creatures;
using HommClone.Turns;

namespace HommClone.Spells
{
    [CreateAssetMenu(fileName = "SlowSpell", menuName = "HOMM/Spells/Slow")]
    public class SlowSpell : Spell
    {
        public override void ApplyEffect(ITimelineParticipant caster, CreatureStack target, SpellMastery mastery)
        {
            if (caster == null || target == null || target.IsDead) return;

            // Mastery speed reduction percent: Basic = 20%, Intermediate = 30%, Advanced = 45%, Expert = 60%
            float reductionPercent = 0.20f;
            switch (mastery)
            {
                case SpellMastery.Basic: reductionPercent = 0.20f; break;
                case SpellMastery.Intermediate: reductionPercent = 0.30f; break;
                case SpellMastery.Advanced: reductionPercent = 0.45f; break;
                case SpellMastery.Expert: reductionPercent = 0.60f; break;
            }

            // Calculate actual initiative reduction amount (minimum 1)
            float initReductionRaw = target.Initiative * reductionPercent;
            float initiativeReduction = Mathf.Max(1f, Mathf.Round(initReductionRaw * 10f) / 10f); // Round to 1 decimal place
            int duration = caster.SpellPower; // Duration is equal to SpellPower

            Debug.Log($"[Spell Cast] {caster.Name} casts Slow on {target.gameObject.name}. Initiative: {target.Initiative} -> {target.Initiative - initiativeReduction}. Duration: {duration} rounds.");

            // Apply initiative modifier
            target.AddInitiativeModifier(-initiativeReduction);

            // Add status effect to target to restore initiative when the spell expires
            target.AddStatusEffect(new ActiveStatusEffect
            {
                effectName = "Slow",
                duration = duration,
                onRemove = (s) =>
                {
                    s.AddInitiativeModifier(initiativeReduction);
                    Debug.Log($"[Spell Expired] Slow expired on {s.gameObject.name}. Restored initiative (+{initiativeReduction}).");
                }
            });

            // Spawn floating text in a nice purple/violet color
            var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
            if (uiManager != null)
            {
                uiManager.SpawnDamageText(target.transform.position + Vector3.up * 1.8f, $"Slowed!\n<color=#c080ff><size=75%>-{initiativeReduction:F1} Initiative</size></color>", new Color(0.75f, 0.5f, 1f));
            }
        }
    }
}
