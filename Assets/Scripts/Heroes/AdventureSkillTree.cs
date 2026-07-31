using System.Collections.Generic;
using UnityEngine;

namespace HommClone.Heroes
{
    [System.Serializable]
    public class AdventureSkillNode
    {
        public string id;
        public string name;
        public string description;
        public string iconSymbol;
        public int tokenCost;
        public string prerequisiteId;
        public Vector2Int uiGridPos; // Grid placement coordinates on skill tree map (column X, row Y)

        public AdventureSkillNode(string id, string name, string description, string iconSymbol, int tokenCost, string prerequisiteId, Vector2Int uiGridPos)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.iconSymbol = iconSymbol;
            this.tokenCost = tokenCost;
            this.prerequisiteId = prerequisiteId;
            this.uiGridPos = uiGridPos;
        }
    }

    public static class AdventureSkillTree
    {
        private static List<AdventureSkillNode> _nodes;

        public static List<AdventureSkillNode> GetAllNodes()
        {
            if (_nodes == null)
            {
                _nodes = new List<AdventureSkillNode>
                {
                    // Row 0: Logistics Branch
                    new AdventureSkillNode("logistics_1", "Logistics 1", "+10% movement on map grid", "👟", 1, null, new Vector2Int(0, 0)),
                    new AdventureSkillNode("logistics_2", "Logistics 2", "+20% movement on map grid", "👟", 2, "logistics_1", new Vector2Int(1, 0)),
                    new AdventureSkillNode("logistics_3", "Logistics 3", "+30% movement on map grid", "👑", 3, "logistics_2", new Vector2Int(2, 0)),

                    // Row 1: PathFinding Branch
                    new AdventureSkillNode("pathfinding_1", "PathFinding 1", "-50% reduction for going through rough terrain", "🌾", 1, null, new Vector2Int(0, 1)),
                    new AdventureSkillNode("pathfinding_2", "PathFinding 2", "-100% reduction for going through rough terrain", "🌾", 2, "pathfinding_1", new Vector2Int(1, 1)),

                    // Row 2: Scouting & Stealth Branch
                    new AdventureSkillNode("scouting", "Scouting", "See exact troop counts in monster stacks & enemy hero armies.", "👁️", 1, null, new Vector2Int(0, 2)),
                    new AdventureSkillNode("stealth", "Stealth", "(Not Working Currently) Opposing heroes cannot see your real army.", "🥷", 2, "scouting", new Vector2Int(1, 2))
                };
            }
            return _nodes;
        }

        public static AdventureSkillNode GetNodeById(string id)
        {
            return GetAllNodes().Find(n => n.id == id);
        }
    }
}
