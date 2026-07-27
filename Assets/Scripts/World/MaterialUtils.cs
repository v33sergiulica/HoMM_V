using UnityEngine;

namespace HommClone.World
{
    public static class MaterialUtils
    {
        private static Shader _cachedShader;

        public static Shader GetSafeShader()
        {
            if (_cachedShader != null) return _cachedShader;

            _cachedShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_cachedShader == null) _cachedShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (_cachedShader == null) _cachedShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (_cachedShader == null) _cachedShader = Shader.Find("Standard");
            if (_cachedShader == null) _cachedShader = Shader.Find("Unlit/Color");
            if (_cachedShader == null) _cachedShader = Shader.Find("Sprites/Default");

            return _cachedShader;
        }

        public static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;

            Material mat = renderer.material;
            if (mat == null)
            {
                Shader shader = GetSafeShader();
                if (shader != null) mat = new Material(shader);
                else return;
                renderer.material = mat;
            }

            // Set both URP BaseColor and Standard Color properties safely
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
        }

        public static Material CreateColorMaterial(Color color)
        {
            Shader shader = GetSafeShader();
            Material mat = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            return mat;
        }
    }
}
