using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HommClone.Creatures;

namespace HommClone.World
{
    [System.Serializable]
    public class ArmySlot
    {
        public CreatureData creatureData;
        public int count;

        public ArmySlot(CreatureData data, int count)
        {
            this.creatureData = data;
            this.count = count;
        }
    }

    [System.Serializable]
    public class PlayerResources
    {
        public int gold = 1000;
        public int wood = 20;
        public int ore = 20;
        public int gems = 5;
    }

    [System.Serializable]
    public struct LevelUpInfo
    {
        public int levelReached;
        public string statGained;

        public LevelUpInfo(int lvl, string stat)
        {
            levelReached = lvl;
            statGained = stat;
        }
    }

    [System.Serializable]
    public class HeroData
    {
        public string heroName = "Knight";
        public Sprite heroPortrait;
        public GameObject heroPrefab;
        public float maxMovementPoints = 15f;
        public float currentMovementPoints = 15f;
        public int level = 1;
        public int currentXP = 0;
        public int xpToNextLevel = 1000;
        public int attack = 2;
        public int defense = 2;
        public int spellPower = 1;
        public int knowledge = 1;
        public Vector2Int worldPosition = new Vector2Int(2, 2);
        public List<ArmySlot> army = new List<ArmySlot>();
        public List<HommClone.Heroes.SecondarySkillSlot> secondarySkills = new List<HommClone.Heroes.SecondarySkillSlot>();

        public int adventureSkillTokens = 1; // Starts with 1 token for initial customization!
        public List<string> unlockedAdventureSkills = new List<string>();

        // Artifact Inventory & Equipment State
        public List<HommClone.Artifacts.ArtifactData> equippedArtifacts = new List<HommClone.Artifacts.ArtifactData>();
        public List<HommClone.Artifacts.ArtifactData> backpack = new List<HommClone.Artifacts.ArtifactData>();

        public HommClone.Artifacts.ArtifactData GetEquippedInSlot(HommClone.Artifacts.ArtifactSlotType slotType)
        {
            return equippedArtifacts.Find(a => a != null && a.slotType == slotType);
        }

        public void EquipArtifact(HommClone.Artifacts.ArtifactData artifact)
        {
            if (artifact == null) return;
            var existing = GetEquippedInSlot(artifact.slotType);
            if (existing != null)
            {
                equippedArtifacts.Remove(existing);
                if (!backpack.Contains(existing)) backpack.Add(existing);
            }
            if (backpack.Contains(artifact)) backpack.Remove(artifact);
            equippedArtifacts.Add(artifact);
        }

        public void UnequipArtifact(HommClone.Artifacts.ArtifactData artifact)
        {
            if (artifact == null) return;
            if (equippedArtifacts.Contains(artifact))
            {
                equippedArtifacts.Remove(artifact);
                if (!backpack.Contains(artifact)) backpack.Add(artifact);
            }
        }

        public int GetTotalAttack()
        {
            int bonus = 0;
            foreach (var a in equippedArtifacts) if (a != null) bonus += a.attackBonus;
            foreach (var setBonus in GetActiveSetBonuses()) bonus += setBonus.attackBonus;
            return attack + bonus;
        }

        public int GetTotalDefense()
        {
            int bonus = 0;
            foreach (var a in equippedArtifacts) if (a != null) bonus += a.defenseBonus;
            foreach (var setBonus in GetActiveSetBonuses()) bonus += setBonus.defenseBonus;
            return defense + bonus;
        }

        public int GetTotalSpellPower()
        {
            int bonus = 0;
            foreach (var a in equippedArtifacts) if (a != null) bonus += a.spellPowerBonus;
            foreach (var setBonus in GetActiveSetBonuses()) bonus += setBonus.spellPowerBonus;
            return spellPower + bonus;
        }

        public int GetTotalKnowledge()
        {
            int bonus = 0;
            foreach (var a in equippedArtifacts) if (a != null) bonus += a.knowledgeBonus;
            foreach (var setBonus in GetActiveSetBonuses()) bonus += setBonus.knowledgeBonus;
            return knowledge + bonus;
        }

        public int GetTotalMorale()
        {
            int bonus = 0;
            foreach (var a in equippedArtifacts) if (a != null) bonus += a.moraleBonus;
            foreach (var setBonus in GetActiveSetBonuses()) bonus += setBonus.moraleBonus;
            return bonus;
        }

        public int GetTotalLuck()
        {
            int bonus = 0;
            foreach (var a in equippedArtifacts) if (a != null) bonus += a.luckBonus;
            foreach (var setBonus in GetActiveSetBonuses()) bonus += setBonus.luckBonus;
            return bonus;
        }

        public int GetMaxMana()
        {
            return GetTotalKnowledge() * 10;
        }

        public List<HommClone.Artifacts.ArtifactSetRequirement> GetActiveSetBonuses()
        {
            List<HommClone.Artifacts.ArtifactSetRequirement> activeBonuses = new List<HommClone.Artifacts.ArtifactSetRequirement>();
            var allSets = HommClone.Artifacts.ArtifactCatalog.GetAllSets();

            foreach (var setData in allSets)
            {
                int equippedCount = 0;
                foreach (var a in equippedArtifacts)
                {
                    if (a != null && a.setId == setData.setId) equippedCount++;
                }

                // Pick ONLY the highest matching set bonus tier (e.g. 3/3 overrides 2/3)
                HommClone.Artifacts.ArtifactSetRequirement highestBonus = null;
                foreach (var req in setData.setBonuses)
                {
                    if (equippedCount >= req.requiredPieces)
                    {
                        if (highestBonus == null || req.requiredPieces > highestBonus.requiredPieces)
                        {
                            highestBonus = req;
                        }
                    }
                }
                if (highestBonus != null)
                {
                    activeBonuses.Add(highestBonus);
                }
            }
            return activeBonuses;
        }

        public float GetLogisticsBonus()
        {
            if (unlockedAdventureSkills.Contains("logistics_3")) return 0.30f;
            if (unlockedAdventureSkills.Contains("logistics_2")) return 0.20f;
            if (unlockedAdventureSkills.Contains("logistics_1")) return 0.10f;
            return 0f;
        }

        public float GetPathfindingDiscount()
        {
            if (unlockedAdventureSkills.Contains("pathfinding_2")) return 1.0f; // -100% terrain penalty
            if (unlockedAdventureSkills.Contains("pathfinding_1")) return 0.5f; // -50% terrain penalty
            return 0f;
        }

        public bool HasScouting()
        {
            return unlockedAdventureSkills.Contains("scouting");
        }

        public bool HasStealth()
        {
            return unlockedAdventureSkills.Contains("stealth");
        }

        public float GetEffectiveMaxMovementPoints()
        {
            return maxMovementPoints * (1f + GetLogisticsBonus());
        }

        public int GetXPForNextLevel(int lvl)
        {
            return Mathf.RoundToInt(1000f * Mathf.Pow(1.25f, lvl - 1));
        }

        public List<LevelUpInfo> pendingLevelUpInfos = new List<LevelUpInfo>();

        public bool GainXP(int amount, out LevelUpInfo info)
        {
            info = new LevelUpInfo(level, "");
            currentXP += amount;
            bool leveledUp = false;

            while (currentXP >= xpToNextLevel)
            {
                currentXP -= xpToNextLevel;
                level++;
                xpToNextLevel = GetXPForNextLevel(level);
                leveledUp = true;
                adventureSkillTokens++; // Grant 1 Adventure Skill Token per level up!

                string currentLevelStat = "";
                // Increase a primary stat randomly biased towards knight / warrior (Attack / Defense)
                int statRoll = UnityEngine.Random.Range(0, 100);
                if (statRoll < 35)
                {
                    attack++;
                    currentLevelStat = "+1 Attack";
                }
                else if (statRoll < 70)
                {
                    defense++;
                    currentLevelStat = "+1 Defense";
                }
                else if (statRoll < 85)
                {
                    spellPower++;
                    currentLevelStat = "+1 Spell Power";
                }
                else
                {
                    knowledge++;
                    currentLevelStat = "+1 Knowledge";
                }

                pendingLevelUpInfos.Add(new LevelUpInfo(level, currentLevelStat));
            }

            if (pendingLevelUpInfos.Count > 0)
            {
                info = pendingLevelUpInfos[0];
                pendingLevelUpInfos.RemoveAt(0);
            }

            return leveledUp;
        }
    }

    /// <summary>
    /// Persistent Singleton manager maintaining global game state (armies, resources, hero stats)
    /// across World Map exploration and Combat scenes.
    /// </summary>
    public class GameDataManager : MonoBehaviour
    {
        public static GameDataManager Instance { get; private set; }

        [Header("Global Game State")]
        public HeroData player1Hero = new HeroData();
        public HeroData player2Hero = new HeroData();
        public PlayerResources player1Resources = new PlayerResources();
        public PlayerResources player2Resources = new PlayerResources();

        [Header("Pending Battle Encounter Data")]
        public List<ArmySlot> pendingBattleEnemyArmy = new List<ArmySlot>();
        public Vector2Int pendingBattleMonsterPosition = new Vector2Int(-1, -1);
        public bool isReturningFromBattle = false;
        public bool battleWon = false;

        [Header("Hotseat Multiplayer State")]
        public int activePlayerIndex = 1;
        public bool isPvPBattle = false;

        [Header("World Turn Counter")]
        public int currentDay = 1;
        public int currentWeek = 1;
        public int currentMonth = 1;

        public HeroData GetActiveHero()
        {
            return activePlayerIndex == 1 ? player1Hero : player2Hero;
        }

        public PlayerResources GetActiveResources()
        {
            return activePlayerIndex == 1 ? player1Resources : player2Resources;
        }

        public void ProcessDailyIncome()
        {
            WorldMine[] mines = FindObjectsByType<WorldMine>(FindObjectsSortMode.None);
            foreach (var mine in mines)
            {
                if (mine == null || mine.OwnerPlayerIndex == 0) continue;
                PlayerResources targetRes = (mine.OwnerPlayerIndex == 1) ? player1Resources : player2Resources;

                if (targetRes != null)
                {
                    int finalIncome = mine.Income;
                    switch (mine.Type)
                    {
                        case ResourceType.Gold: targetRes.gold += finalIncome; break;
                        case ResourceType.Wood: targetRes.wood += finalIncome; break;
                        case ResourceType.Ore: targetRes.ore += finalIncome; break;
                        case ResourceType.Gems: targetRes.gems += finalIncome; break;
                    }
                    Debug.Log($"[GameDataManager] Daily Income: Player {mine.OwnerPlayerIndex} received +{finalIncome} {mine.Type} from mine at {mine.GridPosition}!");
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeStarterArmies();
        }

        public static GameDataManager GetOrCreateInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<GameDataManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject obj = new GameObject("GameDataManager");
                    Instance = obj.AddComponent<GameDataManager>();
                }
            }
            if (Instance != null) Instance.InitializeStarterArmies();
            return Instance;
        }

        [ContextMenu("Reset Starter Army (300 Peasants & 100 Archers)")]
        public void ForceResetStarterArmy()
        {
            if (player1Hero.army == null) player1Hero.army = new List<ArmySlot>();
            player1Hero.army.Clear();
            InitializeStarterArmies();
            Debug.Log("[GameDataManager] Player 1 Army forced reset to 300 Peasants & 100 Archers!");
        }

        public void InitializeStarterArmies(List<CreatureData> availableCreatures = null)
        {
            if (player1Hero == null) player1Hero = new HeroData();
            if (player2Hero == null) player2Hero = new HeroData();

            player1Hero.heroName = "Player 1 Hero";
            player2Hero.heroName = "Player 2 Hero";

            if (player1Hero.army == null) player1Hero.army = new List<ArmySlot>();
            if (player2Hero.army == null) player2Hero.army = new List<ArmySlot>();

            if (player1Hero.unlockedAdventureSkills == null) player1Hero.unlockedAdventureSkills = new List<string>();
            if (player2Hero.unlockedAdventureSkills == null) player2Hero.unlockedAdventureSkills = new List<string>();

            if (player1Hero.equippedArtifacts == null) player1Hero.equippedArtifacts = new List<HommClone.Artifacts.ArtifactData>();
            if (player1Hero.backpack == null) player1Hero.backpack = new List<HommClone.Artifacts.ArtifactData>();
            if (player2Hero.equippedArtifacts == null) player2Hero.equippedArtifacts = new List<HommClone.Artifacts.ArtifactData>();
            if (player2Hero.backpack == null) player2Hero.backpack = new List<HommClone.Artifacts.ArtifactData>();

            // Give starter artifacts for testing equipment and set bonuses
            if (player1Hero.backpack.Count == 0 && player1Hero.equippedArtifacts.Count == 0)
            {
                player1Hero.EquipArtifact(HommClone.Artifacts.ArtifactCatalog.GetArtifactById("sword_might"));
                player1Hero.EquipArtifact(HommClone.Artifacts.ArtifactCatalog.GetArtifactById("shield_dead"));
                player1Hero.backpack.Add(HommClone.Artifacts.ArtifactCatalog.GetArtifactById("boots_speed"));
                player1Hero.backpack.Add(HommClone.Artifacts.ArtifactCatalog.GetArtifactById("lion_blade"));
                player1Hero.backpack.Add(HommClone.Artifacts.ArtifactCatalog.GetArtifactById("lion_armor"));
                player1Hero.backpack.Add(HommClone.Artifacts.ArtifactCatalog.GetArtifactById("lion_helm"));
            }

            if (player2Hero.backpack.Count == 0 && player2Hero.equippedArtifacts.Count == 0)
            {
                player2Hero.EquipArtifact(HommClone.Artifacts.ArtifactCatalog.GetArtifactById("staff_magius"));
                player2Hero.EquipArtifact(HommClone.Artifacts.ArtifactCatalog.GetArtifactById("crown_knowledge"));
                player2Hero.backpack.Add(HommClone.Artifacts.ArtifactCatalog.GetArtifactById("amulet_life"));
            }

            // Only initialize default army if army list is completely empty
            if (player1Hero.army.Count == 0)
            {
                if (availableCreatures == null || availableCreatures.Count == 0)
                {
                    availableCreatures = new List<CreatureData>(Resources.FindObjectsOfTypeAll<CreatureData>());
                }

                if (availableCreatures != null && availableCreatures.Count > 0)
                {
                    CreatureData tier1Data = availableCreatures.FirstOrDefault(c => c != null && (
                        c.CreatureName.ToLower().Contains("peasant") || 
                        c.CreatureName.ToLower().Contains("footman") || 
                        c.name.ToLower().Contains("tier_1") || 
                        c.name.ToLower().Contains("tier 1") || 
                        c.name.ToLower().Contains("peasant")
                    ));
                    if (tier1Data == null) tier1Data = availableCreatures.FirstOrDefault(c => c != null && c.AIValue < 100);
                    if (tier1Data == null) tier1Data = availableCreatures[0];

                    CreatureData tier2Data = availableCreatures.FirstOrDefault(c => c != null && c != tier1Data && (
                        c.CreatureName.ToLower().Contains("archer") || 
                        c.CreatureName.ToLower().Contains("marksman") || 
                        c.name.ToLower().Contains("tier_2") || 
                        c.name.ToLower().Contains("tier 2") || 
                        c.name.ToLower().Contains("archer")
                    ));
                    if (tier2Data == null) tier2Data = availableCreatures.FirstOrDefault(c => c != null && c != tier1Data && c.AIValue < 250);
                    if (tier2Data == null && availableCreatures.Count > 1) tier2Data = availableCreatures[1];

                    player1Hero.army.Add(new ArmySlot(tier1Data, 300));
                    if (tier2Data != null)
                    {
                        player1Hero.army.Add(new ArmySlot(tier2Data, 100));
                    }
                }
            }

            if (player2Hero.army.Count == 0 && availableCreatures != null && availableCreatures.Count > 0)
            {
                CreatureData t1 = availableCreatures[0];
                player2Hero.army.Add(new ArmySlot(t1, 150));
            }
        }

        public void ProcessDaySkip()
        {
            currentDay++;
            currentWeek = ((currentDay - 1) / 7) + 1;
            currentMonth = ((currentDay - 1) / 28) + 1;

            // Award daily production from all owned mines
            var mines = FindObjectsByType<WorldMine>(FindObjectsSortMode.None);
            int goldGained = 0, woodGained = 0, oreGained = 0, gemsGained = 0;

            foreach (var mine in mines)
            {
                if (mine != null && mine.OwnerPlayerIndex == 1)
                {
                    switch (mine.MineType)
                    {
                        case ResourceType.Gold: player1Resources.gold += mine.DailyIncome; goldGained += mine.DailyIncome; break;
                        case ResourceType.Wood: player1Resources.wood += mine.DailyIncome; woodGained += mine.DailyIncome; break;
                        case ResourceType.Ore: player1Resources.ore += mine.DailyIncome; oreGained += mine.DailyIncome; break;
                        case ResourceType.Gems: player1Resources.gems += mine.DailyIncome; gemsGained += mine.DailyIncome; break;
                    }
                }
            }

            Debug.Log($"[Day Skip] Day {currentDay} (Month {currentMonth}, Week {currentWeek}). Daily Income Gained -> Gold: +{goldGained}, Wood: +{woodGained}, Ore: +{oreGained}, Gems: +{gemsGained}");

            // Reset Hero Movement Points on Day Skip!
            ResetHeroMovement(player1Hero);

            // Update Resource Bar UI
            var resUI = FindFirstObjectByType<UI.ResourceBarUI>();
            if (resUI != null) resUI.UpdateUI();
        }

        public void ResetHeroMovement(HeroData hero)
        {
            if (hero != null)
            {
                hero.currentMovementPoints = hero.maxMovementPoints;
            }
        }

        public void AdvanceDay()
        {
            currentDay++;
            if (currentDay > 7)
            {
                currentDay = 1;
                currentWeek++;
                if (currentWeek > 4)
                {
                    currentWeek = 1;
                    currentMonth++;
                }

                // Weekly resource income
                player1Resources.gold += 1000;
                player2Resources.gold += 1000;
            }

            ResetHeroMovement(player1Hero);
            ResetHeroMovement(player2Hero);
        }
    }
}
