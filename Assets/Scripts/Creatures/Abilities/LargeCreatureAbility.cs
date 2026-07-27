using UnityEngine;

namespace HommClone.Creatures
{
    /// <summary>
    /// Large creature ability that makes the unit occupy a 2x2 footprint on the battlefield.
    /// </summary>
    [CreateAssetMenu(fileName = "LargeCreatureAbility", menuName = "HOMM/Abilities/Large Creature")]
    public class LargeCreatureAbility : CreatureAbility
    {
        public override float GetAIPowerMultiplier()
        {
            return 1.1f; // Large units get a +10% power rating bonus
        }
    }
}
