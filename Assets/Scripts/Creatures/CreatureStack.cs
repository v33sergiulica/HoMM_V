using UnityEngine;
using System.Collections.Generic;
using HommClone.Grid;
using HommClone.World;
using TMPro;
using HommClone.Spells;

namespace HommClone.Creatures
{
    /// <summary>
    /// Represents a dynamic stack of similar creatures standing on a grid coordinate.
    /// Manages combat states, health pools, movement paths, and action animations.
    /// </summary>
    public class CreatureStack : MonoBehaviour, Turns.ITimelineParticipant
    {
        [Header("Creature Configuration")]
        [SerializeField] private CreatureData creatureData;
        
        [Header("Stack Dynamic Stats")]
        [SerializeField] private int count = 1;
        [SerializeField] private int currentHealth; // Health of the top-most (injured) creature in the stack
        [SerializeField] private int currentAmmo;
        [SerializeField] private int currentMana;
        [SerializeField] private float atb = 0f;
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private int playerIndex = 1; // 1 for Player 1, 2 for Player 2
        [SerializeField] private int baseMorale = 0;
        [SerializeField] private int baseLuck = 0;

        private bool _useCinematicCameraForCurrentAttack = false;
        private bool _hasRolledCameraThisAction = false;
        private int _moraleModifier = 0;
        private int _luckModifier = 0;

        private float _initiativeModifier = 0f;

        [Header("Visual Components")]
        [SerializeField] private TextMeshPro stackSizeText;

        // ITimelineParticipant implementations
        public string Name => creatureData != null ? creatureData.CreatureName : gameObject.name;
        public Sprite Icon => creatureData != null ? creatureData.Icon : null;
        public float ATB { get => atb; set => atb = value; }
        public float Initiative => Mathf.Max(1f, (creatureData != null ? creatureData.Initiative : 10f) + _initiativeModifier);
        public void AddInitiativeModifier(float amount) => _initiativeModifier += amount;
        public bool IsHero => false;

        // Getters & Setters
        public CreatureData Data => creatureData;
        public int Count => count;
        public int CurrentHealth => currentHealth;
        public int CurrentAmmo => currentAmmo;
        public int CurrentMana => currentMana;
        public Vector2Int GridPosition => gridPosition;
        public int PlayerIndex => playerIndex;
        public bool IsDead => count <= 0;

        public int MaxMana => creatureData != null ? creatureData.MaxMana : 0;

        public List<Spell> Spells => HasAbility<CasterAbility>() ? GetAbility<CasterAbility>().Spells : new List<Spell>();

        /// <summary>
        /// Initializer for instantiating or assigning stack parameters.
        /// </summary>
        public void Initialize(CreatureData data, int initialCount, int pIndex, Vector2Int position)
        {
            creatureData = data;
            count = initialCount;
            playerIndex = pIndex;
            gridPosition = position;

            if (creatureData != null)
            {
                currentHealth = creatureData.MaxHealth;
                currentAmmo = creatureData.MaxAmmo;
                currentMana = creatureData.MaxMana;
            }

            _attackBonus = 0;
            _defenseBonus = 0;
            _speedModifier = 0;
            _moraleModifier = 0;
            _luckModifier = 0;
            _hasDefendedBonus = false;
            activeEffects.Clear();

            ApplyHeroModifiers();

            UpdateVisualLabels();
            CreateDynamicModel();
        }

        public void ApplyHeroModifiers()
        {
            var manager = GameDataManager.Instance;
            if (manager == null) return;

            HeroData hero = (playerIndex == 1) ? manager.player1Hero : manager.player2Hero;
            if (hero != null)
            {
                var mods = Heroes.HeroBattleModifiers.FromHero(hero);
                _attackBonus += mods.attackBonus;
                _defenseBonus += mods.defenseBonus;
                _moraleModifier += mods.moraleBonus;
                _luckModifier += mods.luckBonus;

                Debug.Log($"[HeroBattleModifiers] Applied Hero Mods to {gameObject.name} (P{playerIndex}): +{mods.attackBonus} Att, +{mods.defenseBonus} Def, +{mods.moraleBonus} Morale, +{mods.luckBonus} Luck");
            }
        }

        public void CreateDynamicModel()
        {
            if (creatureData == null) return;

            // Remove old player base if it exists
            Transform oldBase = transform.Find("PlayerBase");
            if (oldBase != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(oldBase.gameObject);
                }
                else
                {
                    DestroyImmediate(oldBase.gameObject);
                }
            }

            // Spawn a thin cylinder base under the unit's feet to represent Player index
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "PlayerBase";
            baseObj.transform.SetParent(transform, false);
            
            var baseCollider = baseObj.GetComponent<Collider>();
            if (baseCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(baseCollider);
                }
                else
                {
                    DestroyImmediate(baseCollider);
                }
            }

            // Position base disk at the feet of the unit
            float baseHeight = -0.3f;
            if (creatureData.Prefab == null)
            {
                switch (creatureData.Tier)
                {
                    case 1: baseHeight = -0.3f; break;
                    case 2: baseHeight = -0.4f; break;
                    case 3: baseHeight = -0.5f; break;
                    case 4: baseHeight = -0.35f; break;
                    case 5: baseHeight = -0.4f; break;
                    case 6: baseHeight = -0.7f; break;
                    case 7: 
                    default: baseHeight = -0.8f; break;
                }
            }
            else
            {
                baseHeight = -0.4f;
            }

            baseObj.transform.localPosition = new Vector3(0f, baseHeight, 0f);
            
            // Adjust radius of the pedestal disc based on tier so it scales with unit width
            float baseRadius = 0.7f + (creatureData.Tier - 1) * 0.05f;
            baseObj.transform.localScale = new Vector3(baseRadius, 0.02f, baseRadius);

            // Set color based on playerIndex
            Color teamColor = (playerIndex == 1) 
                ? new Color(0.1f, 0.45f, 0.9f)   // Soft Royal Blue
                : new Color(0.9f, 0.15f, 0.15f); // Soft Crimson Red

            var baseRenderer = baseObj.GetComponent<MeshRenderer>();
            if (baseRenderer != null)
            {
                MaterialUtils.SetRendererColor(baseRenderer, teamColor);
            }

            // Remove old visual child if it exists
            Transform oldVisual = transform.Find("VisualModel");
            if (oldVisual != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(oldVisual.gameObject);
                }
                else
                {
                    DestroyImmediate(oldVisual.gameObject);
                }
            }

            // Disable base MeshRenderer (e.g. the default placeholder cube on the prefab root)
            var rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            // If a custom visual prefab is specified, spawn it
            if (creatureData.Prefab != null)
            {
                GameObject visualObj = Instantiate(creatureData.Prefab, transform);
                visualObj.name = "VisualModel";
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;
                
                // Adjust scale if needed, e.g. based on tier
                float scaleMod = 1f + (creatureData.Tier - 1) * 0.1f;
                visualObj.transform.localScale = Vector3.one * scaleMod;
            }
            else
            {
                // Otherwise, generate dynamic primitive shape + color based on Faction & Tier
                PrimitiveType primitive = PrimitiveType.Cube;
                Vector3 modelScale = Vector3.one;

                switch (creatureData.Tier)
                {
                    case 1:
                        primitive = PrimitiveType.Cube;
                        modelScale = new Vector3(0.6f, 0.6f, 0.6f);
                        break;
                    case 2:
                        primitive = PrimitiveType.Cylinder;
                        modelScale = new Vector3(0.6f, 0.4f, 0.6f);
                        break;
                    case 3:
                        primitive = PrimitiveType.Capsule;
                        modelScale = new Vector3(0.6f, 0.5f, 0.6f);
                        break;
                    case 4:
                        primitive = PrimitiveType.Sphere;
                        modelScale = new Vector3(0.7f, 0.7f, 0.7f);
                        break;
                    case 5:
                        primitive = PrimitiveType.Cube;
                        modelScale = new Vector3(0.8f, 0.8f, 0.8f);
                        break;
                    case 6:
                        primitive = PrimitiveType.Cylinder;
                        modelScale = new Vector3(0.8f, 0.7f, 0.8f);
                        break;
                    case 7:
                    default:
                        primitive = PrimitiveType.Capsule;
                        modelScale = new Vector3(1.0f, 0.8f, 1.0f);
                        break;
                }

                GameObject visualObj = GameObject.CreatePrimitive(primitive);
                visualObj.name = "VisualModel";
                visualObj.transform.SetParent(transform, false);

                // Destroy the collider of the primitive child so it doesn't block raycasting of the main collider
                var primitiveCollider = visualObj.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(primitiveCollider);
                    }
                    else
                    {
                        DestroyImmediate(primitiveCollider);
                    }
                }

                visualObj.transform.localPosition = new Vector3(0f, 0f, 0f);
                visualObj.transform.localScale = modelScale;

                // Set color based on faction
                Color factionColor = Color.white;
                switch (creatureData.FactionType)
                {
                    case Faction.Haven:
                        factionColor = new Color(0.95f, 0.9f, 0.7f); // Pale Gold / White
                        break;
                    case Faction.Inferno:
                        factionColor = new Color(0.85f, 0.15f, 0.05f); // Fiery Red
                        break;
                    case Faction.Necropolis:
                        factionColor = new Color(0.35f, 0.35f, 0.35f); // Grave Grey / Greenish
                        break;
                    case Faction.Academy:
                        factionColor = new Color(0.6f, 0.2f, 0.8f); // Violet Purple
                        break;
                    case Faction.Sylvan:
                        factionColor = new Color(0.1f, 0.7f, 0.2f); // Forest Green
                        break;
                    case Faction.Dungeon:
                        factionColor = new Color(0.15f, 0.05f, 0.25f); // Indigo Black
                        break;
                    case Faction.Fortress:
                        factionColor = new Color(0.55f, 0.3f, 0.2f); // Brick Red / Bronze
                        break;
                    case Faction.Stronghold:
                        factionColor = new Color(0.8f, 0.55f, 0.25f); // Desert Sandy Gold
                        break;
                    case Faction.Neutrals:
                        factionColor = new Color(0.5f, 0.55f, 0.6f); // Steel Blue Grey
                        break;
                }

                var renderer = visualObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    MaterialUtils.SetRendererColor(renderer, factionColor);
                }
            }

            // Adjust stackSizeText position to sit closely above the model's head and set text style
            if (stackSizeText != null)
            {
                stackSizeText.color = Color.black;
                stackSizeText.fontStyle = FontStyles.Bold;
                
                float textHeight = 0.5f;
                switch (creatureData.Tier)
                {
                    case 1: textHeight = 0.32f; break;
                    case 2: textHeight = 0.45f; break;
                    case 3: textHeight = 0.55f; break;
                    case 4: textHeight = 0.4f; break;
                    case 5: textHeight = 0.45f; break;
                    case 6: textHeight = 0.75f; break;
                    case 7: 
                    default: textHeight = 0.85f; break;
                }
                stackSizeText.transform.localPosition = new Vector3(0f, textHeight, 0f);
            }
        }

        public void SetGridPosition(Vector2Int newPosition)
        {
            gridPosition = newPosition;
        }

        public void SetATB(float value)
        {
            atb = value;
        }

        public void UpdateATB(float amount)
        {
            atb += amount;
        }

        public int CalculateRawDamage()
        {
            if (creatureData == null || count <= 0) return 0;

            // Roll a single random value for the entire stack between minDamage * count and maxDamage * count
            int raw = Random.Range(creatureData.MinDamage * count, (creatureData.MaxDamage * count) + 1);

            int luckScore = Luck;
            if (luckScore > 0)
            {
                float luckChance = Mathf.Clamp(luckScore * 0.1f, 0f, 1f);
                if (Random.value < luckChance)
                {
                    raw *= 2;
                    Debug.Log($"[Good Luck] {gameObject.name} rolls Good Luck! Damage doubled: {raw}");
                    var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
                    if (uiManager != null)
                    {
                        uiManager.SpawnDamageText(transform.position + Vector3.up * 2.2f, "Good Luck!", new Color(0.2f, 0.9f, 1f));
                    }
                }
            }
            else if (luckScore < 0)
            {
                float badLuckChance = Mathf.Clamp(-luckScore * 0.1f, 0f, 1f);
                if (Random.value < badLuckChance)
                {
                    raw = Mathf.Max(1, raw / 2);
                    Debug.Log($"[Bad Luck] {gameObject.name} rolls Bad Luck! Damage halved: {raw}");
                    var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
                    if (uiManager != null)
                    {
                        uiManager.SpawnDamageText(transform.position + Vector3.up * 2.2f, "Bad Luck!", new Color(0.8f, 0.3f, 0.1f));
                    }
                }
            }

            return raw;
        }

        /// <summary>
        /// Calculates final damage using a piecewise Attack/Defense formula.
        /// Prevents scaling issues at extreme stat differences.
        /// </summary>
        // Extra stat modifiers (e.g. from Rage, active buffs, or hero bonuses)
        private int _attackBonus = 0;
        private int _defenseBonus = 0;
        private int _speedModifier = 0;
        private bool _hasDefendedBonus = false;

        private Heroes.Hero _heroOwner;
        public Heroes.Hero HeroOwner
        {
            get
            {
                if (_heroOwner == null)
                {
                    var heroes = FindObjectsByType<Heroes.Hero>(FindObjectsSortMode.None);
                    _heroOwner = System.Array.Find(heroes, h => h != null && h.PlayerIndex == playerIndex);
                }
                return _heroOwner;
            }
            set => _heroOwner = value;
        }

        public int Attack => (creatureData != null ? creatureData.Attack : 0) + _attackBonus + (HeroOwner != null ? HeroOwner.Attack : 0);
        public int Defense => (creatureData != null ? creatureData.Defense : 0) + _defenseBonus + (HeroOwner != null ? HeroOwner.Defense : 0);
        public int Speed => Mathf.Max(1, (creatureData != null ? creatureData.Speed : 0) + _speedModifier);
        public int Morale => baseMorale + _moraleModifier;
        public int Luck => baseLuck + _luckModifier;
        public List<ActiveStatusEffect> ActiveEffects => activeEffects;

        public void AddAttackBonus(int amount) => _attackBonus += amount;
        public void AddDefenseBonus(int amount) => _defenseBonus += amount;
        public void AddSpeedModifier(int amount) => _speedModifier += amount;
        public void AddMoraleModifier(int amount) => _moraleModifier += amount;
        public void AddLuckModifier(int amount) => _luckModifier += amount;
        public void ClearStatModifiers()
        {
            _attackBonus = 0;
            _defenseBonus = 0;
            _speedModifier = 0;
            _moraleModifier = 0;
            _luckModifier = 0;
            _initiativeModifier = 0f;
            _hasDefendedBonus = false;
        }

        /// <summary>
        /// Applies the defense bonus from a Defend action.
        /// </summary>
        public void ApplyDefendBonus(int amount)
        {
            if (!_hasDefendedBonus)
            {
                _hasDefendedBonus = true;
                AddDefenseBonus(amount);
                Debug.Log($"[CreatureStack] {gameObject.name} defends. Added +{amount} defense (Total: {Defense}).");
            }
        }

        /// <summary>
        /// Clears the defense bonus when starting a new turn.
        /// </summary>
        public void ClearDefendBonus(int amount)
        {
            if (_hasDefendedBonus)
            {
                _hasDefendedBonus = false;
                AddDefenseBonus(-amount);
                Debug.Log($"[CreatureStack] {gameObject.name} starts turn. Cleared defend bonus (Total: {Defense}).");
            }
        }

        public bool IsLarge => HasAbility<LargeCreatureAbility>();

        public List<Vector2Int> GetOccupiedTiles()
        {
            List<Vector2Int> occupied = new List<Vector2Int>();
            occupied.Add(gridPosition);
            if (IsLarge)
            {
                occupied.Add(new Vector2Int(gridPosition.x + 1, gridPosition.y));
                occupied.Add(new Vector2Int(gridPosition.x, gridPosition.y + 1));
                occupied.Add(new Vector2Int(gridPosition.x + 1, gridPosition.y + 1));
            }
            return occupied;
        }

        public bool OccupiesTile(Vector2Int position)
        {
            if (gridPosition == position) return true;
            if (IsLarge)
            {
                if (position.x == gridPosition.x + 1 && position.y == gridPosition.y) return true;
                if (position.x == gridPosition.x && position.y == gridPosition.y + 1) return true;
                if (position.x == gridPosition.x + 1 && position.y == gridPosition.y + 1) return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if this stack has a specific type of ability in its data sheet.
        /// </summary>
        public bool HasAbility<T>() where T : CreatureAbility
        {
            if (creatureData == null || creatureData.Abilities == null) return false;
            foreach (var ability in creatureData.Abilities)
            {
                if (ability is T) return true;
            }
            return false;
        }

        private List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();

        /// <summary>
        /// Gets a specific ability instance if this creature stack possesses it.
        /// </summary>
        public T GetAbility<T>() where T : CreatureAbility
        {
            if (creatureData == null || creatureData.Abilities == null) return null;
            foreach (var ability in creatureData.Abilities)
            {
                if (ability is T specificAbility) return specificAbility;
            }
            return null;
        }

        /// <summary>
        /// Calculates the dynamic Spell Power of the stack based on its current size and weekly growth.
        /// Formula: 4 * SQRT[NOWOG] + NOWOG - 0.75 where NOWOG = count / weeklyGrowth
        /// </summary>
        public int SpellPower
        {
            get
            {
                if (creatureData == null || count <= 0) return 1;
                
                int growth = Mathf.Max(1, creatureData.WeeklyGrowth);
                float nowog = (float)count / growth;
                float spVal = 4f * Mathf.Sqrt(nowog) + nowog - 0.75f;
                
                return Mathf.Max(1, Mathf.RoundToInt(spVal));
            }
        }

        /// <summary>
        /// Returns the total power of this stack on the battlefield (used by AI or army evaluation).
        /// </summary>
        public int TroopPower => (creatureData != null) ? (creatureData.AIValue * count) : 0;

        /// <summary>
        /// Registers a new active turn-based buff/debuff. Overwrites duplicate effects by name.
        /// </summary>
        public void AddStatusEffect(ActiveStatusEffect effect)
        {
            if (effect == null) return;
            
            // Re-apply logic: if the same effect already exists, we clean it and remove it
            ActiveStatusEffect existing = activeEffects.Find(e => e.effectName == effect.effectName);
            if (existing != null)
            {
                existing.onRemove?.Invoke(this);
                activeEffects.Remove(existing);
            }
            
            activeEffects.Add(effect);
            Debug.Log($"[Status Effect] Applied {effect.effectName} to {gameObject.name} for {effect.duration} rounds.");
        }

        /// <summary>
        /// Deducts a specified amount of mana from the stack's current mana pool.
        /// </summary>
        public void ConsumeMana(int amount)
        {
            currentMana = Mathf.Max(0, currentMana - amount);
        }

        /// <summary>
        /// Executed at the start of this stack's turn. Ticks down status effects.
        /// </summary>
        public void OnTurnStart()
        {
            // Reset retaliation status
            ResetRetaliation();

            // Clear any active Defend bonuses for the stack that is starting its turn
            SendMessage("ClearDefendBonus", 3, SendMessageOptions.DontRequireReceiver); // default bonus is 3 defense
            
            // Tick down status effects
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.duration--;
                Debug.Log($"[Status Effect] {effect.effectName} on {gameObject.name} ticked down. Remaining: {effect.duration} rounds.");
                
                if (effect.duration <= 0)
                {
                    effect.onRemove?.Invoke(this);
                    activeEffects.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Performs a spellcast action against a target stack.
        /// Consumes mana, triggers spell effect, and returns turn action.
        /// </summary>
        public System.Collections.IEnumerator CastSpellCoroutine(Spells.Spell spell, CreatureStack target, System.Action onComplete)
        {
            if (spell == null || target == null || this.IsDead || currentMana < spell.ManaCost)
            {
                onComplete?.Invoke();
                yield break;
            }

            // 1. Consume mana
            ConsumeMana(spell.ManaCost);
            Debug.Log($"[Spell Cast] {gameObject.name} casts {spell.SpellName}. Mana remaining: {currentMana}/{MaxMana}");

            // 2. Face target
            FaceTarget(target.transform.position);

            // 3. Retrieve caster mastery
            SpellMastery mastery = SpellMastery.Basic;
            if (IsHero)
            {
                mastery = SpellMastery.Expert; // Heroes are expert spellcasters!
            }
            else
            {
                var casterAbility = GetAbility<CasterAbility>();
                if (casterAbility != null)
                {
                    mastery = casterAbility.Mastery;
                }
            }

            // 4. Apply the spell effect
            spell.ApplyEffect(this, target, mastery);

            // 5. Wait briefly for floating numbers to rise
            yield return new WaitForSeconds(1.5f);

            // 6. Restore default facing directions if they still exist
            if (this != null)
            {
                FaceDefaultDirection();
            }
            if (target != null && !target.IsDead)
            {
                target.FaceDefaultDirection();
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Calculates final damage using a piecewise Attack/Defense formula.
        /// Prevents scaling issues at extreme stat differences.
        /// </summary>
        public int CalculateRealDamage(int rawDamage, int opponentDefense)
        {
            if (creatureData == null) return 0;

            int selfAttack = this.Attack; // Use the modified Attack property
            float y = (opponentDefense - selfAttack) / 100f;
            float multiplier = 0f;
            int tempRange = (int)(y * 100);

            // Piecewise logic for Attack/Defense modifier from original implementation
            switch (tempRange)
            {
                case < -120:
                    multiplier = 4.4f - y / 2f;
                    break;
                case < -100:
                    multiplier = 3.8f - y;
                    break;
                case < -60:
                    multiplier = 1.8f - 3f * y;
                    break;
                case < -20:
                    multiplier = 1.2f - 4f * y;
                    break;
                case < 0:
                    multiplier = 1f - 5f * y;
                    break;
                case < 20:
                    multiplier = 1f - 2f * y;
                    break;
                case < 40:
                    multiplier = 0.9f - 1.5f * y;
                    break;
                case < 60:
                    multiplier = 0.4f - y / 4f;
                    break;
                case < 85:
                    multiplier = 0.37f - y / 5f;
                    break;
                default:
                    multiplier = 0.17f / y;
                    break;
            }

            return Mathf.Max(1, (int)(rawDamage * multiplier));
        }

        /// <summary>
        /// Calculates the estimated damage range and dynamic casualties that would occur if attacking a target stack.
        /// </summary>
        public void GetDamageEstimation(CreatureStack target, bool isMelee, out int minDamage, out int maxDamage, out int minKills, out int maxKills)
        {
            minDamage = 0;
            maxDamage = 0;
            minKills = 0;
            maxKills = 0;

            if (target == null || target.Data == null || creatureData == null) return;

            // 1. Min and Max raw damage rolls based on stack count
            int minRaw = creatureData.MinDamage * count;
            int maxRaw = creatureData.MaxDamage * count;

            // 2. Apply Melee Penalty (50% reduction) if active stack is ranged but forced to melee
            if (isMelee && creatureData.IsRanged)
            {
                minRaw = Mathf.Max(1, minRaw / 2);
                maxRaw = Mathf.Max(1, maxRaw / 2);
            }
            // Apply Ranged Penalty (50% reduction) if distance > 6 and unit lacks NoRangePenaltyAbility
            else if (!isMelee && creatureData.IsRanged)
            {
                int distance = Mathf.Max(Mathf.Abs(gridPosition.x - target.GridPosition.x), Mathf.Abs(gridPosition.y - target.GridPosition.y));
                bool hasNoRangePenalty = HasAbility<NoRangePenaltyAbility>();
                if (distance > 6 && !hasNoRangePenalty)
                {
                    minRaw = Mathf.Max(1, minRaw / 2);
                    maxRaw = Mathf.Max(1, maxRaw / 2);
                }
            }

            // 3. Scale with opponent's defense
            minDamage = CalculateRealDamage(minRaw, target.Defense);
            maxDamage = CalculateRealDamage(maxRaw, target.Defense);

            // 4. Calculate casualties
            minKills = CalculateCasualties(target, minDamage);
            maxKills = CalculateCasualties(target, maxDamage);
        }

        public int CalculateCasualties(CreatureStack target, int damageDealt)
        {
            if (target == null || target.Data == null) return 0;

            int totalHealthBefore = (target.Count - 1) * target.Data.MaxHealth + target.CurrentHealth;
            if (damageDealt >= totalHealthBefore)
            {
                return target.Count;
            }

            int kills = damageDealt / target.Data.MaxHealth;
            int remainingDamage = damageDealt % target.Data.MaxHealth;

            // If the remaining damage of the roll exceeds the current health of the top-most unit, it dies
            if (target.CurrentHealth <= remainingDamage)
            {
                kills += 1;
            }

            return Mathf.Min(kills, target.Count);
        }

        /// <summary>
        /// Applies damage to the stack, killing individual creatures sequentially.
        /// </summary>
        public void TakeDamage(int damageDealt)
        {
            if (damageDealt <= 0 || creatureData == null) return;

            int initialCount = count;

            // Total HP remaining in the stack
            int totalHealthBefore = (count - 1) * creatureData.MaxHealth + currentHealth;

            if (damageDealt >= totalHealthBefore)
            {
                count = 0;
                currentHealth = 0;
                UpdateVisualLabels();

                int casualties = initialCount;
                NotifyUIDamage(damageDealt, casualties);

                Die();
                return;
            }

            int troopsTaken = damageDealt / creatureData.MaxHealth;
            int remainingHealthDamage = damageDealt % creatureData.MaxHealth;

            count -= troopsTaken;

            if (currentHealth > remainingHealthDamage)
            {
                currentHealth -= remainingHealthDamage;
            }
            else
            {
                count -= 1;
                currentHealth = currentHealth + creatureData.MaxHealth - remainingHealthDamage;
            }

            if (count <= 0)
            {
                count = 0;
                currentHealth = 0;

                int casualties = initialCount;
                NotifyUIDamage(damageDealt, casualties);

                Die();
            }
            else
            {
                UpdateVisualLabels();

                int casualties = initialCount - count;
                NotifyUIDamage(damageDealt, casualties);
            }
        }

        private void NotifyUIDamage(int damageDealt, int casualties)
        {
            var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
            if (uiManager != null)
            {
                string text = $"-{damageDealt}";
                if (casualties > 0)
                {
                    // Rich text formatting: smaller and gray for casualties
                    text += $"\n<color=#cccccc><size=75%>-{casualties} Dead</size></color>";
                }

                // Spawn well above the unit
                uiManager.SpawnDamageText(transform.position + Vector3.up * 1.8f, text, Color.red);
            }
        }

        /// <summary>
        /// Updates the overlay text display.
        /// </summary>
        public void UpdateVisualLabels()
        {
            if (stackSizeText != null)
            {
                if (count > 0)
                {
                    string cName = (creatureData != null) ? creatureData.CreatureName : "Creature";
                    stackSizeText.text = $"{count}\n<size=65%>{cName}</size>";
                }
                else
                {
                    stackSizeText.text = "Dead";
                }
            }
        }

        private bool hasRetaliatedThisRound = false;
        public bool HasRetaliatedThisRound => hasRetaliatedThisRound;

        /// <summary>
        /// Resets the retaliation flag so this stack can strike back again.
        /// </summary>
        public void ResetRetaliation()
        {
            hasRetaliatedThisRound = false;
            Debug.Log($"[CreatureStack] Retaliation reset for {gameObject.name}");
        }

        /// <summary>
        /// A shooter unit is blocked if there is at least one active enemy stack on any of the 8 adjacent tiles.
        /// </summary>
        public bool IsBlocked()
        {
            var gridManager = FindFirstObjectByType<Grid.GridManager>();
            if (gridManager == null) return false;

            var neighbours = gridManager.GetNeighbours(gridPosition, allowDiagonals: true);
            foreach (var neighbour in neighbours)
            {
                CreatureStack occupant = gridManager.GetCreatureAt(neighbour.GridPosition);
                if (occupant != null && occupant.PlayerIndex != this.playerIndex && !occupant.IsDead)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the stack is a ranged unit, has ammo left, and is not currently blocked by adjacent enemies.
        /// </summary>
        public bool CanShoot()
        {
            if (creatureData == null) return false;
            return creatureData.IsRanged && currentAmmo > 0 && !IsBlocked();
        }

        /// <summary>
        /// Instantly rotates the stack to face its default direction (Player 1 faces Right, Player 2 faces Left).
        /// </summary>
        public void FaceDefaultDirection()
        {
            Vector3 facing = (playerIndex == 1) ? Vector3.right : Vector3.left;
            transform.rotation = Quaternion.LookRotation(facing);
        }

        /// <summary>
        /// Rotates the stack to face a target's world position.
        /// </summary>
        public void FaceTarget(Vector3 targetPos)
        {
            Vector3 dir = (targetPos - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        /// <summary>
        /// Performs a melee attack against a target stack.
        /// Deals damage, waits, and triggers retaliation.
        /// </summary>
        public System.Collections.IEnumerator MeleeAttackCoroutine(CreatureStack target, System.Action onComplete)
        {
            if (target == null || target.IsDead || this.IsDead)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Roll camera chance if we didn't move (already adjacent)
            if (!_hasRolledCameraThisAction)
            {
                _useCinematicCameraForCurrentAttack = (UnityEngine.Random.value < 0.5f);
                _hasRolledCameraThisAction = true;
            }

            // Trigger cinematic camera strike
            var camController = FindFirstObjectByType<HommClone.CameraControl.BattleCameraController>();
            if (camController != null && _useCinematicCameraForCurrentAttack)
            {
                camController.StartCinematicStrike(transform.position, target.transform.position);
            }

            // 1. Face target
            FaceTarget(target.transform.position);

            // Wait a short moment (0.5s) for the camera to zoom in before applying damage
            yield return new WaitForSeconds(0.5f);

            // Re-check target and self in case they were destroyed during the camera wait
            if (target == null || target.IsDead || this == null)
            {
                if (camController != null && _useCinematicCameraForCurrentAttack) camController.StopCinematicStrike();
                _hasRolledCameraThisAction = false;
                onComplete?.Invoke();
                yield break;
            }

            // 2. Apply damage
            if (HommClone.Audio.AudioManager.Instance != null)
            {
                HommClone.Audio.AudioManager.Instance.PlayMeleeSound();
            }

            int rawDamage = CalculateRawDamage();
            
            // Ranged units suffer a 50% melee penalty when attacking in melee
            if (creatureData != null && creatureData.IsRanged)
            {
                rawDamage = Mathf.Max(1, rawDamage / 2);
                Debug.Log($"[Melee Attack] {gameObject.name} attacks in melee with 50% melee penalty. Reduced Raw Damage: {rawDamage}");
            }

            int finalDamage = CalculateRealDamage(rawDamage, target.Defense);
            Debug.Log($"[Melee Attack] {gameObject.name} (Player {playerIndex}) attacks {target.gameObject.name} (Player {target.PlayerIndex}). Raw: {rawDamage}, Final: {finalDamage}");
            target.TakeDamage(finalDamage);

            // 3. Handle Retaliation if target survives
            if (target != null && !target.IsDead)
            {
                if (!target.HasRetaliatedThisRound)
                {
                    // Delay between attack and retaliation (e.g. 1.5 seconds)
                    yield return new WaitForSeconds(1.5f);

                    // Re-check target and self in case they were destroyed during the wait delay
                    if (target == null || target.IsDead || this == null)
                    {
                        if (camController != null && _useCinematicCameraForCurrentAttack) camController.StopCinematicStrike();
                        _hasRolledCameraThisAction = false;
                        onComplete?.Invoke();
                        yield break;
                    }

                    // Target faces attacker
                    target.FaceTarget(transform.position);

                    // Execute retaliation
                    target.Retaliate(this);

                    if (this == null)
                    {
                        if (camController != null && _useCinematicCameraForCurrentAttack) camController.StopCinematicStrike();
                        _hasRolledCameraThisAction = false;
                        onComplete?.Invoke();
                        yield break;
                    }

                    // Wait another short moment to let numbers float up before ending turn
                    yield return new WaitForSeconds(1.3f);

                    if (this == null)
                    {
                        if (camController != null && _useCinematicCameraForCurrentAttack) camController.StopCinematicStrike();
                        _hasRolledCameraThisAction = false;
                        onComplete?.Invoke();
                        yield break;
                    }
                }
                else
                {
                    Debug.Log($"[Melee Attack] {target.gameObject.name} cannot retaliate (already retaliated this round).");
                    yield return new WaitForSeconds(1.2f);
                }
            }
            else
            {
                // Wait briefly for death visuals to float up
                yield return new WaitForSeconds(1.2f);
            }

            // Restore default facing directions for both if they still exist
            if (this != null)
            {
                FaceDefaultDirection();
            }
            if (target != null && !target.IsDead)
            {
                target.FaceDefaultDirection();
            }

            // Exit cinematic camera strike and wait for transition
            if (camController != null && _useCinematicCameraForCurrentAttack)
            {
                camController.StopCinematicStrike();
                yield return new WaitForSeconds(0.8f);
            }

            _hasRolledCameraThisAction = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Performs a ranged shot against a target stack.
        /// Consumes ammo, deals full damage, and triggers no retaliation.
        /// </summary>
        public System.Collections.IEnumerator RangedAttackCoroutine(CreatureStack target, System.Action onComplete)
        {
            if (target == null || target.IsDead || this.IsDead || !CanShoot())
            {
                onComplete?.Invoke();
                yield break;
            }

            // Roll camera chance (ranged attack starts directly)
            _useCinematicCameraForCurrentAttack = (UnityEngine.Random.value < 0.5f);

            // Trigger cinematic camera strike
            var camController = FindFirstObjectByType<HommClone.CameraControl.BattleCameraController>();
            if (camController != null && _useCinematicCameraForCurrentAttack)
            {
                camController.StartCinematicStrike(transform.position, target.transform.position);
            }

            // Wait a short moment (0.5s) for the camera to zoom in before starting the shot
            yield return new WaitForSeconds(0.5f);

            if (target == null || target.IsDead || this == null || !CanShoot())
            {
                if (camController != null && _useCinematicCameraForCurrentAttack) camController.StopCinematicStrike();
                onComplete?.Invoke();
                yield break;
            }

            // 1. Consume ammo
            currentAmmo--;
            if (HommClone.Audio.AudioManager.Instance != null)
            {
                HommClone.Audio.AudioManager.Instance.PlayRangedSound();
            }
            Debug.Log($"[Ranged Attack] {gameObject.name} fired a shot. Ammo remaining: {currentAmmo}/{creatureData.MaxAmmo}");

            // 2. Face target
            FaceTarget(target.transform.position);

            // 3. Apply damage (checking for range penalty)
            int distance = Mathf.Max(Mathf.Abs(gridPosition.x - target.GridPosition.x), Mathf.Abs(gridPosition.y - target.GridPosition.y));
            int rawDamage = CalculateRawDamage();
            
            bool hasNoRangePenalty = HasAbility<NoRangePenaltyAbility>();
            bool appliesRangePenalty = (distance > 6) && !hasNoRangePenalty;

            if (appliesRangePenalty)
            {
                rawDamage = Mathf.Max(1, rawDamage / 2);
                Debug.Log($"[Ranged Attack] {gameObject.name} suffers 50% range penalty! Distance: {distance} (> 6). Reduced Raw: {rawDamage}");
            }
            else
            {
                Debug.Log($"[Ranged Attack] {gameObject.name} shoots at normal range. Distance: {distance}, NoRangePenalty: {hasNoRangePenalty}");
            }

            int finalDamage = CalculateRealDamage(rawDamage, target.Defense);
            Debug.Log($"[Ranged Attack] {gameObject.name} shoots {target.gameObject.name} (No Retaliation). Final Damage: {finalDamage}");
            target.TakeDamage(finalDamage);

            // 4. Wait briefly for numbers to float up
            yield return new WaitForSeconds(1.8f);

            // 5. Restore default facing directions if they still exist
            if (this != null)
            {
                FaceDefaultDirection();
            }
            if (target != null && !target.IsDead)
            {
                target.FaceDefaultDirection();
            }

            // Exit cinematic camera strike and wait for transition
            if (camController != null && _useCinematicCameraForCurrentAttack)
            {
                camController.StopCinematicStrike();
                yield return new WaitForSeconds(0.8f);
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Performs a retaliation strike back against an attacker.
        /// </summary>
        public void Retaliate(CreatureStack attacker)
        {
            if (attacker == null || attacker.IsDead || this.IsDead) return;

            hasRetaliatedThisRound = true;

            if (HommClone.Audio.AudioManager.Instance != null)
            {
                HommClone.Audio.AudioManager.Instance.PlayRetaliationSound();
            }

            // Face attacker
            FaceTarget(attacker.transform.position);

            int rawDamage = CalculateRawDamage();
            int finalDamage = CalculateRealDamage(rawDamage, attacker.Defense);

            Debug.Log($"[Retaliation] {gameObject.name} strikes back at {attacker.gameObject.name} for {finalDamage} damage!");
            attacker.TakeDamage(finalDamage);

            // Restore default facing direction
            FaceDefaultDirection();
        }

        /// <summary>
        /// Moves the stack smoothly along the given path of grid coordinates.
        /// Optional attackTarget triggers the cinematic camera on the final step.
        /// </summary>
        public System.Collections.IEnumerator MoveAlongPathCoroutine(System.Collections.Generic.List<Vector2Int> path, System.Action onComplete, CreatureStack attackTarget = null)
        {
            var gridManager = FindFirstObjectByType<Grid.GridManager>();
            if (gridManager == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Roll cinematic camera chance for this move-and-attack sequence
            if (attackTarget != null)
            {
                _useCinematicCameraForCurrentAttack = (UnityEngine.Random.value < 0.5f);
                _hasRolledCameraThisAction = true;
            }
            else
            {
                _useCinematicCameraForCurrentAttack = false;
                _hasRolledCameraThisAction = false;
            }

            int startIndex = (path.Count > 0 && path[0] == gridPosition) ? 1 : 0;

            if (startIndex < path.Count && HommClone.Audio.AudioManager.Instance != null)
            {
                HommClone.Audio.AudioManager.Instance.PlayMoveSound();
            }

            for (int i = startIndex; i < path.Count; i++)
            {
                Vector2Int node = path[i];
                Tile tile = gridManager.GetTileAt(node);
                if (tile != null)
                {
                    Vector3 targetPos = new Vector3(tile.transform.position.x, tile.transform.position.y + heightOffset, tile.transform.position.z);
                    Vector3 startPos = transform.position;
                    float elapsed = 0f;
                    float duration = 0.2f; // Time to travel between adjacent tiles

                    // Rotate to face travel direction
                    Vector3 dir = (targetPos - startPos).normalized;
                    dir.y = 0;
                    if (dir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(dir);
                    }

                    // Trigger cinematic camera slightly earlier (just as we start the final step towards attack target)
                    if (i == path.Count - 1 && attackTarget != null && _useCinematicCameraForCurrentAttack)
                    {
                        var camController = FindFirstObjectByType<HommClone.CameraControl.BattleCameraController>();
                        if (camController != null)
                        {
                            camController.StartCinematicStrike(targetPos, attackTarget.transform.position);
                        }
                    }

                    while (elapsed < duration)
                    {
                        transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    transform.position = targetPos;
                    gridPosition = node;
                }
            }

            if (HommClone.Audio.AudioManager.Instance != null)
            {
                HommClone.Audio.AudioManager.Instance.StopMoveSound();
            }

            FaceDefaultDirection();
            onComplete?.Invoke();
        }

        private void Die()
        {
            Debug.Log($"[CreatureStack] {gameObject.name} (Player {playerIndex}) has been set to dead state (hidden).");
            
            // Disable collider so it can't be hovered or hit
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Hide the visual model child
            Transform visual = transform.Find("VisualModel");
            if (visual != null) visual.gameObject.SetActive(false);

            // Also hide root renderer just in case
            var rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            // Hide the text label
            if (stackSizeText != null) stackSizeText.gameObject.SetActive(false);
        }

        [Header("Grid Snapping")]
        [SerializeField] private float heightOffset = 1f;

        /// <summary>
        /// Snaps the physical 3D GameObject position to match its 2D grid coordinates on the battlefield.
        /// </summary>
        [ContextMenu("Snap to Grid Position")]
        public void SnapToGridPosition()
        {
            var gridManager = FindFirstObjectByType<Grid.GridManager>();
            if (gridManager != null)
            {
                var tile = gridManager.GetTileAt(gridPosition);
                if (tile != null)
                {
                    Vector3 tilePos = tile.transform.position;
                    if (IsLarge)
                    {
                        var diagonalTile = gridManager.GetTileAt(new Vector2Int(gridPosition.x + 1, gridPosition.y + 1));
                        if (diagonalTile != null)
                        {
                            Vector3 diagPos = diagonalTile.transform.position;
                            transform.position = new Vector3((tilePos.x + diagPos.x) / 2f, (tilePos.y + diagPos.y) / 2f + heightOffset, (tilePos.z + diagPos.z) / 2f);
                        }
                        else
                        {
                            // Fallback assuming standard spacing of 1.0 (X and Z)
                            transform.position = new Vector3(tilePos.x + 0.5f, tilePos.y + heightOffset, tilePos.z + 0.5f);
                        }
                    }
                    else
                    {
                        // Snap world position: Grid Y maps to World Z
                        transform.position = new Vector3(tilePos.x, tilePos.y + heightOffset, tilePos.z);
                    }
                }
                else
                {
                    Debug.LogWarning($"[CreatureStack] Grid Position {gridPosition} is out of bounds for snapping {gameObject.name}!");
                }
            }
        }

        private void Start()
        {
            if (creatureData != null && currentHealth == 0)
            {
                Initialize(creatureData, count, playerIndex, gridPosition);
            }
            else
            {
                UpdateVisualLabels();
                CreateDynamicModel();
            }

            SnapToGridPosition();
            FaceDefaultDirection();
        }
    }

    /// <summary>
    /// Level of mastery/knowledge a caster possesses for spell formulas.
    /// </summary>
    public enum SpellMastery
    {
        Basic,
        Intermediate,
        Advanced,
        Expert
    }

    /// <summary>
    /// Represents a dynamic combat status buff or debuff with a turn-based duration.
    /// </summary>
    [System.Serializable]
    public class ActiveStatusEffect
    {
        public string effectName;
        public int duration; // Remaining duration in rounds
        public System.Action<CreatureStack> onRemove;
    }
}
