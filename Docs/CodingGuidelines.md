# Coding Guidelines & Best Practices

## 1. C# Style Conventions
* **PascalCase**: Classes, Structs, Enums, Properties, Public methods.
* **camelCase**: Local variables, method parameters.
* **_camelCase**: Private/protected fields.
* **UPPER_CASE**: Constants and readonly static variables.
* **Explicit Access Modifiers**: Always write `private`, `public`, `protected`, `internal`.
* **Namespaces**: Group scripts logically (e.g., `namespace HommClone.Combat`, `namespace HommClone.Grid`).

## 2. Unity Best Practices
* **Cached References**: Avoid `GetComponent()` in `Update()` loops. Cache references in `Awake()` or `Start()`.
* **No Magic Strings**: Use constants or `nameof()` for tags, layers, and scene names.
* **ScriptableObjects**: Use ScriptableObjects for configuration and static data (e.g., creature templates, hero templates).
* **Separate Logic from Presentation**: Stacks should compute damage and movement in pure C# logic; separate visual controllers (animating models, moving gameobjects, UI updates) should listen to state events.
* **Unity UI (Toolkit or UGUI)**: Ensure scalable design with responsive anchors.

## 3. General Principles
* **Single Responsibility (SRP)**: Keep classes small and focused.
* **Self-Documenting Code**: Choose descriptive names over excessive comments.
* **Performant Pathfinding**: Keep distance computations clean and pre-allocated where possible to avoid garbage collection spikes.
