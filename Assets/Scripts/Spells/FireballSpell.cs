using UnityEngine;
using System.Collections.Generic;
using HommClone.Creatures;
using HommClone.Turns;

namespace HommClone.Spells
{
    [CreateAssetMenu(fileName = "FireballSpell", menuName = "HOMM/Spells/Fireball")]
    public class FireballSpell : Spell
    {
        public override void ApplyEffect(ITimelineParticipant caster, CreatureStack target, SpellMastery mastery)
        {
            if (caster == null || target == null || target.IsDead) return;

            var gridManager = FindFirstObjectByType<Grid.GridManager>();
            if (gridManager == null) return;

            // Mastery multiplier: Basic = 15, Intermediate = 20, Advanced = 25, Expert = 35
            float multiplier = 15f;
            switch (mastery)
            {
                case SpellMastery.Basic: multiplier = 15f; break;
                case SpellMastery.Intermediate: multiplier = 20f; break;
                case SpellMastery.Advanced: multiplier = 25f; break;
                case SpellMastery.Expert: multiplier = 35f; break;
            }

            // Calculate spell damage
            int spellDamage = Mathf.RoundToInt(caster.SpellPower * multiplier);

            Vector2Int center = target.GridPosition;
            List<Vector2Int> targetPositions = new List<Vector2Int> { center };
            
            // Gather 3x3 Chebyshev grid neighbours
            List<Grid.Tile> neighbors = gridManager.GetNeighbours(center, allowDiagonals: true);
            foreach (var neighbor in neighbors)
            {
                if (neighbor != null)
                {
                    targetPositions.Add(neighbor.GridPosition);
                }
            }

            Debug.Log($"[Spell Cast] {caster.Name} casts Fireball centered on {target.gameObject.name}. Targets hit at {targetPositions.Count} grid positions. Base Damage: {spellDamage}");

            var uiManager = FindFirstObjectByType<UI.BattleUIManager>();

            // Apply damage to all units in the 3x3 area
            foreach (var pos in targetPositions)
            {
                CreatureStack victim = gridManager.GetCreatureAt(pos);
                if (victim != null && !victim.IsDead && !victim.IsHero)
                {
                    victim.TakeDamage(spellDamage);

                    if (uiManager != null)
                    {
                        uiManager.SpawnDamageText(
                            victim.transform.position + Vector3.up * 1.8f, 
                            $"-{spellDamage}\n<color=#ff6600><size=75%>Fireball</size></color>", 
                            new Color(1f, 0.4f, 0f) // Bright orange
                        );
                    }
                }
            }
        }
    }
}
