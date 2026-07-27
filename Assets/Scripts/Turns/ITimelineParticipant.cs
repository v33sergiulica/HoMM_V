using UnityEngine;

namespace HommClone.Turns
{
    /// <summary>
    /// Represents any participant in the combat timeline queue (e.g. CreatureStack or Hero).
    /// </summary>
    public interface ITimelineParticipant
    {
        string Name { get; }
        int PlayerIndex { get; }
        float ATB { get; set; }
        float Initiative { get; }
        Sprite Icon { get; }
        bool IsDead { get; }
        int SpellPower { get; }
        void OnTurnStart();
    }
}
