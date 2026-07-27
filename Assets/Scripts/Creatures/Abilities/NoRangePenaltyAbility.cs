using UnityEngine;

namespace HommClone.Creatures
{
    /// <summary>
    /// Ability that negates the 50% damage reduction for ranged attacks beyond half the map size (distance > 6).
    /// </summary>
    [CreateAssetMenu(fileName = "NoRangePenaltyAbility", menuName = "HOMM/Abilities/No Range Penalty")]
    public class NoRangePenaltyAbility : CreatureAbility
    {
        // Tag ability for ranged units to deal full damage at any distance
    }
}
