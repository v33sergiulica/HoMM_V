using System;
using System.Collections.Generic;

namespace HommClone.Artifacts
{
    [Serializable]
    public class ArtifactSetRequirement
    {
        public int requiredPieces; // e.g. 2 pieces required for partial bonus, 3 for full set
        public string bonusDescription;

        // Stat Bonuses Granted by Set Completion
        public int attackBonus;
        public int defenseBonus;
        public int spellPowerBonus;
        public int knowledgeBonus;
        public int moraleBonus;
        public int luckBonus;
        public float movementPointsBonus;

        public ArtifactSetRequirement(int requiredPieces, string bonusDescription)
        {
            this.requiredPieces = requiredPieces;
            this.bonusDescription = bonusDescription;
        }
    }

    [Serializable]
    public class ArtifactSetData
    {
        public string setId;
        public string setName;
        public List<string> artifactIds; // IDs of artifacts belonging to this set
        public List<ArtifactSetRequirement> setBonuses;

        public ArtifactSetData(string setId, string setName, List<string> artifactIds)
        {
            this.setId = setId;
            this.setName = setName;
            this.artifactIds = artifactIds ?? new List<string>();
            this.setBonuses = new List<ArtifactSetRequirement>();
        }
    }
}
