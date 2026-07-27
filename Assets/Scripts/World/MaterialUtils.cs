using UnityEngine;

namespace HommClone.World
{
    public static class MaterialUtils
    {
        public static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;

            // 1. Check if the renderer has a valid non-error material. If missing or magenta ErrorShader, assign Sprites/Default fallback
            if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null || renderer.sharedMaterial.shader.name.Contains("Error"))
            {
                Shader safeShader = Shader.Find("Universal Render Pipeline/Lit") 
                                 ?? Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("Sprites/Default") 
                                 ?? Shader.Find("Unlit/Color");
                if (safeShader != null)
                {
                    renderer.sharedMaterial = new Material(safeShader);
                }
            }

            // 2. Use MaterialPropertyBlock - the official Unity way to apply colors at runtime without shader stripping or pink textures
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);

            // 3. Apply directly to material instance properties if supported
            try
            {
                if (renderer.material != null)
                {
                    if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", color);
                    if (renderer.material.HasProperty("_Color")) renderer.material.SetColor("_Color", color);
                }
            }
            catch { }
        }
    }
}
