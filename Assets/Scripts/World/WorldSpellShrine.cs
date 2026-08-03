using UnityEngine;
using HommClone.Spells;
using HommClone.Grid;

namespace HommClone.World
{
    /// <summary>
    /// Represents a World Map Shrine of Magic structure.
    /// When visited by a Hero, teaches the designated Spell asset assigned in the Unity Inspector.
    /// </summary>
    public class WorldSpellShrine : MonoBehaviour
    {
        [Header("Shrine Configuration")]
        [SerializeField] private string shrineName = "Shrine of Magic";
        [SerializeField] private Spell spellToTeach;
        [SerializeField] private Vector2Int gridPosition;

        [Header("Visual Model")]
        [SerializeField] private Color particleGlowColor = new Color(0.3f, 0.7f, 1f);

        public string ShrineName => shrineName;
        public Spell SpellToTeach => spellToTeach;
        public Vector2Int GridPosition => gridPosition;

        private void Start()
        {
            var grid = FindFirstObjectByType<WorldGridManager>();
            if (grid != null)
            {
                transform.position = grid.GetTileWorldPosition(gridPosition);
            }
            SetupShrineVisuals();
        }

        public void Initialize(Spell spell, Vector2Int pos, string name = "Shrine of Magic")
        {
            spellToTeach = spell;
            gridPosition = pos;
            shrineName = name;

            var grid = FindFirstObjectByType<WorldGridManager>();
            if (grid != null)
            {
                transform.position = grid.GetTileWorldPosition(gridPosition);
            }
            SetupShrineVisuals();
        }

        public bool Interact(HeroData hero)
        {
            if (hero == null)
            {
                var manager = GameDataManager.GetOrCreateInstance();
                if (manager != null) hero = manager.GetActiveHero();
            }

            if (hero == null) return false;

            if (spellToTeach == null)
            {
                Debug.LogWarning($"[WorldSpellShrine] Shrine at {gridPosition} has no Spell assigned in Inspector!");
                return false;
            }

            bool learnedNew = hero.TeachSpell(spellToTeach);
            Color schoolColor = (spellToTeach.School == MagicSchool.Light) ? new Color(1f, 0.85f, 0.3f) :
                                (spellToTeach.School == MagicSchool.Dark) ? new Color(0.75f, 0.45f, 1f) : new Color(1f, 0.35f, 0.35f);

            if (learnedNew)
            {
                Debug.Log($"[WorldSpellShrine] Hero '{hero.heroName}' visited Shrine and learned '{spellToTeach.SpellName}' [{spellToTeach.School}]!");
                UI.WorldNotificationUI.ShowNotification(
                    "SPELL LEARNED!",
                    $"<b>{hero.heroName}</b> learned <b>{spellToTeach.SpellName}</b> [{spellToTeach.School}]!",
                    icon: spellToTeach.Icon,
                    accentColor: schoolColor
                );
            }
            else
            {
                Debug.Log($"[WorldSpellShrine] Hero '{hero.heroName}' already masters '{spellToTeach.SpellName}'.");
                UI.WorldNotificationUI.ShowNotification(
                    "SHRINE VISITED",
                    $"<b>{hero.heroName}</b> already masters <b>{spellToTeach.SpellName}</b>.",
                    icon: spellToTeach.Icon,
                    accentColor: schoolColor * 0.7f
                );
            }

            return learnedNew;
        }

        private void SetupShrineVisuals()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                CreateFallbackShrineModel();
            }
        }

        private void CreateFallbackShrineModel()
        {
            // Base Altar Structure
            GameObject altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            altar.name = "AltarBase";
            altar.transform.SetParent(transform, false);
            altar.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            altar.transform.localScale = new Vector3(1.2f, 0.25f, 1.2f);
            
            Renderer altarRen = altar.GetComponent<Renderer>();
            if (altarRen != null)
            {
                MaterialUtils.SetRendererColor(altarRen, new Color(0.25f, 0.28f, 0.35f)); // Dark slate stone
            }

            // Floating Glowing Magic Crystal / Orb
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "MagicOrb";
            orb.transform.SetParent(transform, false);
            orb.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            orb.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

            Renderer orbRen = orb.GetComponent<Renderer>();
            if (orbRen != null)
            {
                Color schoolGlow = (spellToTeach != null) ?
                    (spellToTeach.School == MagicSchool.Light ? new Color(1f, 0.9f, 0.3f) :
                     spellToTeach.School == MagicSchool.Dark ? new Color(0.7f, 0.3f, 1f) : new Color(1f, 0.3f, 0.3f))
                    : particleGlowColor;

                MaterialUtils.SetRendererColor(orbRen, schoolGlow);
            }

            // Remove primitive colliders from internal visual children
            var childCols = GetComponentsInChildren<Collider>();
            foreach (var c in childCols)
            {
                if (c.gameObject != gameObject) Destroy(c);
            }

            // Add main collider on root object if missing
            if (GetComponent<Collider>() == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.8f, 0f);
                box.size = new Vector3(1.2f, 1.6f, 1.2f);
            }
        }
    }
}
