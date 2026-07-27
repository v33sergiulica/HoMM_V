using UnityEngine;

namespace HommClone.Heroes
{
    /// <summary>
    /// Visual 3D component attached to sideline Hero models.
    /// Controls facing direction, pedestal styling, idle animation, and spellcasting/strike animations.
    /// </summary>
    public class Hero3DView : MonoBehaviour
    {
        [Header("Hero 3D Model Prefab")]
        [SerializeField] private GameObject customHeroPrefab;

        [Header("Facing Direction")]
        [SerializeField] private bool faceRight = true;

        private Vector3 _targetFacingDir;

        private void Start()
        {
            SetupHeroVisual(customHeroPrefab, faceRight);
        }

        public void SetupHeroVisual(GameObject customPrefab = null, bool lookRight = true)
        {
            faceRight = lookRight;
            _targetFacingDir = faceRight ? Vector3.right : Vector3.left;
            transform.rotation = Quaternion.LookRotation(_targetFacingDir);

            if (customPrefab != null)
            {
                customHeroPrefab = customPrefab;
            }

            if (customHeroPrefab != null)
            {
                // Remove placeholder children if any exist
                foreach (Transform child in transform)
                {
                    Destroy(child.gameObject);
                }
                GameObject instantiated = Instantiate(customHeroPrefab, transform, false);
                instantiated.transform.localPosition = Vector3.zero;
                instantiated.transform.localRotation = Quaternion.identity;

                // Remove duplicate Hero component attached on the prefab child to prevent double turns & stat conflicts!
                var childHeroes = instantiated.GetComponentsInChildren<Heroes.Hero>(true);
                foreach (var childHero in childHeroes)
                {
                    if (Application.isPlaying) Destroy(childHero);
                    else DestroyImmediate(childHero);
                }
            }
            else
            {
                var renderers = GetComponentsInChildren<Renderer>();
                if (renderers == null || renderers.Length == 0)
                {
                    CreateFallbackHeroVisual();
                }
            }
        }

        public void PlayAttackAnimation()
        {
            // Simple visual punch/shake scale animation for Hero Strike
            StartCoroutine(AnimateStrikeSequence());
        }

        public void PlayCastAnimation()
        {
            // Simple magic levitation scale animation for Spell Cast
            StartCoroutine(AnimateCastSequence());
        }

        private System.Collections.IEnumerator AnimateStrikeSequence()
        {
            Vector3 startPos = transform.position;
            Vector3 forward = faceRight ? Vector3.right : Vector3.left;
            Vector3 stepForward = startPos + forward * 0.8f;

            float timer = 0f;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, stepForward, timer / 0.2f);
                yield return null;
            }

            timer = 0f;
            while (timer < 0.3f)
            {
                timer += Time.deltaTime;
                transform.position = Vector3.Lerp(stepForward, startPos, timer / 0.3f);
                yield return null;
            }

            transform.position = startPos;
        }

        private System.Collections.IEnumerator AnimateCastSequence()
        {
            Vector3 startPos = transform.position;
            Vector3 liftPos = startPos + Vector3.up * 0.5f;

            float timer = 0f;
            while (timer < 0.3f)
            {
                timer += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, liftPos, timer / 0.3f);
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            timer = 0f;
            while (timer < 0.3f)
            {
                timer += Time.deltaTime;
                transform.position = Vector3.Lerp(liftPos, startPos, timer / 0.3f);
                yield return null;
            }

            transform.position = startPos;
        }

        private void CreateFallbackHeroVisual()
        {
            // Pedestal Base
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "HeroPedestal";
            pedestal.transform.SetParent(transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            pedestal.transform.localScale = new Vector3(1.4f, 0.2f, 1.4f);

            var pedRen = pedestal.GetComponent<Renderer>();
            if (pedRen != null)
            {
                pedRen.material = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                pedRen.material.color = faceRight ? new Color(0.85f, 0.7f, 0.2f) : new Color(0.75f, 0.2f, 0.2f); // Gold for P1, Red for P2
            }

            // Hero Figure Capsule Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "HeroBody";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            body.transform.localScale = new Vector3(0.8f, 1.1f, 0.8f);

            var bodyRen = body.GetComponent<Renderer>();
            if (bodyRen != null)
            {
                bodyRen.material = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                bodyRen.material.color = faceRight ? new Color(0.2f, 0.5f, 0.9f) : new Color(0.8f, 0.3f, 0.3f);
            }

            // Hero Crown / Helm
            GameObject helm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            helm.name = "HeroHelm";
            helm.transform.SetParent(body.transform, false);
            helm.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            helm.transform.localScale = new Vector3(0.7f, 0.4f, 0.7f);

            var helmRen = helm.GetComponent<Renderer>();
            if (helmRen != null)
            {
                helmRen.material = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                helmRen.material.color = Color.gold;
            }
        }
    }
}
