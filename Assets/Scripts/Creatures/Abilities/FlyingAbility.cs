using UnityEngine;

namespace HommClone.Creatures
{
    /// <summary>
    /// Flying ability allowing a unit to bypass obstacles during movement.
    /// Range checks are simplified to direct distance calculations without path obstruction checks.
    /// </summary>
    [CreateAssetMenu(fileName = "FlyingAbility", menuName = "HOMM/Abilities/Flying")]
    public class FlyingAbility : CreatureAbility
    {
        public override float GetAIPowerMultiplier()
        {
            return 1.15f; // Flying units get a +15% power rating bonus
        }
    }
}
