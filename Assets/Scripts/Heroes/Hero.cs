using UnityEngine;
using System.Collections.Generic;
using HommClone.Turns;
using HommClone.Spells;
using HommClone.Creatures;

namespace HommClone.Heroes
{
    /// <summary>
    /// Represents a commanding Hero participating on the sideline of the battle timeline.
    /// Does not occupy grid tiles and cannot be directly targeted or killed.
    /// </summary>
    public class Hero : MonoBehaviour, ITimelineParticipant
    {
        [Header("Hero Info")]
        [SerializeField] private string heroName = "Hero";
        [SerializeField] [Range(1, 2)] private int playerIndex = 1;
        [SerializeField] private Sprite icon;

        [Header("Hero Stats")]
        [SerializeField] private int attack = 5;
        [SerializeField] private int defense = 5;
        [SerializeField] private int spellPower = 5;
        [SerializeField] private int knowledge = 5;
        [SerializeField] private int morale = 0;
        [SerializeField] private int luck = 0;

        [Header("Spellbook")]
        [SerializeField] private List<Spell> spells = new List<Spell>();

        private float atb = 0f;
        private int currentMana;

        // ITimelineParticipant implementations
        public string Name => heroName;
        public int PlayerIndex => playerIndex;
        public float ATB { get => atb; set => atb = value; }
        public float Initiative => 10f; // Heroes have initiative 10
        public Sprite Icon => icon;
        public bool IsDead => false; // Heroes are sidelined and never die directly in combat
        public int SpellPower => spellPower;

        // Hero Properties
        public int Attack => attack;
        public int Defense => defense;
        public int Knowledge => knowledge;
        public int MaxMana => knowledge * 10;
        public int CurrentMana => currentMana;
        public int Morale => morale;
        public int Luck => luck;
        public List<Spell> Spells => spells;

        public void SetStats(int atk, int def, int sp, int knw, string hName = null, Sprite portrait = null, int pIndex = 1)
        {
            attack = atk;
            defense = def;
            spellPower = sp;
            knowledge = knw;
            playerIndex = pIndex;
            if (!string.IsNullOrEmpty(hName)) heroName = hName;
            if (portrait != null) icon = portrait;
            currentMana = MaxMana;
        }

        private void Start()
        {
            // Set initial mana pool based on knowledge
            if (currentMana <= 0) currentMana = MaxMana;
        }

        public void OnTurnStart()
        {
            Debug.Log($"[Hero Turn] {heroName}'s turn starts on the sidelines.");
        }

        public void ConsumeMana(int amount)
        {
            currentMana = Mathf.Max(0, currentMana - amount);
        }

        /// <summary>
        /// Renders a sideline direct ranged projectile strike against an enemy stack.
        /// </summary>
        public System.Collections.IEnumerator DirectAttackCoroutine(CreatureStack target, System.Action onComplete)
        {
            if (target == null || target.IsDead)
            {
                onComplete?.Invoke();
                yield break;
            }

            Debug.Log($"[Hero Strike] {heroName} strikes {target.gameObject.name} directly from the sidelines!");

            var view = GetComponent<Hero3DView>();
            if (view != null) view.PlayAttackAnimation();

            // Calculate damage using Hero formula: 10 * Attack
            int rawDamage = 10 * attack;
            int finalDamage = target.CalculateRealDamage(rawDamage, target.Defense);
            
            // Apply damage
            target.TakeDamage(finalDamage);

            // Spawn floating text in gold color
            var uiManager = FindFirstObjectByType<UI.BattleUIManager>();
            if (uiManager != null)
            {
                uiManager.SpawnDamageText(
                    target.transform.position + Vector3.up * 1.8f, 
                    $"-{finalDamage}\n<color=#ffcc44><size=75%>Hero Strike</size></color>", 
                    new Color(1f, 0.8f, 0.2f)
                );
            }

            yield return new WaitForSeconds(1.5f);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Launches a spellcast, consumes mana, and applies effect using Expert mastery.
        /// </summary>
        public System.Collections.IEnumerator CastSpellCoroutine(Spell spell, CreatureStack target, System.Action onComplete)
        {
            if (spell == null || target == null || currentMana < spell.ManaCost)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Consume mana
            ConsumeMana(spell.ManaCost);
            Debug.Log($"[Hero Spell] {heroName} casts {spell.SpellName}. Mana remaining: {currentMana}/{MaxMana}");

            // Apply effect using Expert mastery for Heroes
            spell.ApplyEffect(this, target, SpellMastery.Expert);

            yield return new WaitForSeconds(1.5f);
            onComplete?.Invoke();
        }
    }
}
