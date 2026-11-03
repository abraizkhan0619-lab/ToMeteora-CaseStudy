using UnityEngine;

namespace ToMeteora
{
    [RequireComponent(typeof(Renderer))]
    public class FogOfWar : MonoBehaviour
    {
        public CharacterSelectDemo demo;
        [Tooltip("Radius of the clear circle around the character, in Unity units.")]
        public float revealRadius = 7f;
        [Tooltip("Width of the soft edge, in Unity units. At 0 the edge is sharp but still antialiased.")]
        public float edgeSoftness = 1.5f;
        [Tooltip("How dark everything outside the circle is.")]
        [Range(0f, 1f)] public float darkness = 0.85f;
        public Color fogColor = new Color(0.04f, 0.06f, 0.12f);

        MaterialPropertyBlock block;
        Renderer fogRenderer;

        void LateUpdate()
        {
            if (demo != null && demo.Current != null)
                Reveal(demo.Current.transform.position);
        }

        public void Reveal(Vector3 centre)
        {
            block ??= new MaterialPropertyBlock();
            if (fogRenderer == null)
                fogRenderer = GetComponent<Renderer>();

            block.SetVector("_RevealCenter", centre);
            block.SetFloat("_RevealRadius", Mathf.Max(0f, revealRadius));
            block.SetFloat("_EdgeSoftness", Mathf.Max(0f, edgeSoftness));
            block.SetFloat("_Darkness", darkness);
            block.SetColor("_FogColor", fogColor);
            block.SetFloat("_FogActive", 1f);
            fogRenderer.SetPropertyBlock(block);
        }

        public void Hide()
        {
            if (fogRenderer == null)
                fogRenderer = GetComponent<Renderer>();
            fogRenderer.SetPropertyBlock(null);
        }
    }
}
