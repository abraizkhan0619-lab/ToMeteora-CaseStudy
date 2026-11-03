using UnityEngine;

namespace ToMeteora
{
    [RequireComponent(typeof(Renderer))]
    public class FogOfWar : MonoBehaviour
    {
        public CharacterSelectDemo demo;
        public Vector2 areaCenter = Vector2.zero;
        public float areaSize = 40f;
        [Tooltip("Fog texture cells per side. Higher = sharper edges.")]
        [Range(8, 512)] public int resolution = 64;
        public float revealRadius = 7f;
        [Tooltip("Width of the soft edge around the clear circle.")]
        public float edgeSoftness = 2f;
        [Range(0f, 1f)] public float unexploredDarkness = 0.88f;
        [Range(0f, 1f)] public float exploredDarkness = 0.5f;

        Texture2D fogTexture;
        bool[] explored;
        Color32[] pixels;
        int builtResolution;
        Vector2 builtCenter;
        float builtSize;

        void Start() => Setup();

        void LateUpdate()
        {
            if (demo != null && demo.Current != null)
                Reveal(demo.Current.transform.position);
        }

        bool SettingsChanged =>
            fogTexture == null || builtResolution != resolution || builtCenter != areaCenter || !Mathf.Approximately(builtSize, areaSize);

        public void Setup()
        {
            resolution = Mathf.Clamp(resolution, 8, 512);
            areaSize = Mathf.Max(areaSize, 1f);
            var newExplored = new bool[resolution * resolution];
            CarryOverExplored(newExplored);

            if (fogTexture != null)
                DestroyTexture();
            fogTexture = new Texture2D(resolution, resolution, TextureFormat.Alpha8, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            explored = newExplored;
            pixels = new Color32[resolution * resolution];
            builtResolution = resolution;
            builtCenter = areaCenter;
            builtSize = areaSize;

            var block = new MaterialPropertyBlock();
            block.SetTexture("_FogTex", fogTexture);
            block.SetVector("_FogArea", new Vector4(areaCenter.x - areaSize / 2f, areaCenter.y - areaSize / 2f, areaSize, areaSize));
            block.SetFloat("_FogActive", 1f);
            GetComponent<Renderer>().SetPropertyBlock(block);

            Reveal(null);
        }

        void CarryOverExplored(bool[] target)
        {
            if (explored == null || builtResolution <= 0)
                return;

            float oldCell = builtSize / builtResolution;
            float oldMinX = builtCenter.x - builtSize / 2f;
            float oldMinZ = builtCenter.y - builtSize / 2f;
            float cell = areaSize / resolution;
            float minX = areaCenter.x - areaSize / 2f;
            float minZ = areaCenter.y - areaSize / 2f;

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int ox = Mathf.FloorToInt((minX + (x + 0.5f) * cell - oldMinX) / oldCell);
                    int oz = Mathf.FloorToInt((minZ + (z + 0.5f) * cell - oldMinZ) / oldCell);
                    if (ox >= 0 && oz >= 0 && ox < builtResolution && oz < builtResolution)
                        target[z * resolution + x] = explored[oz * builtResolution + ox];
                }
            }
        }

        public void Reveal(Vector3? center)
        {
            if (SettingsChanged)
                Setup();

            float cell = areaSize / resolution;
            float minX = areaCenter.x - areaSize / 2f;
            float minZ = areaCenter.y - areaSize / 2f;
            float softness = Mathf.Max(edgeSoftness, 0.01f);

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * resolution + x;
                    float distance = float.MaxValue;
                    if (center.HasValue)
                    {
                        float dx = minX + (x + 0.5f) * cell - center.Value.x;
                        float dz = minZ + (z + 0.5f) * cell - center.Value.z;
                        distance = Mathf.Sqrt(dx * dx + dz * dz);
                    }

                    if (distance < revealRadius - softness / 2f)
                        explored[i] = true;

                    float clear = 1f - Mathf.Clamp01((distance - (revealRadius - softness)) / softness);
                    float darkness = (explored[i] ? exploredDarkness : unexploredDarkness) * (1f - clear);
                    pixels[i] = new Color32(0, 0, 0, (byte)(darkness * 255f));
                }
            }

            fogTexture.SetPixels32(pixels);
            fogTexture.Apply(false);
        }

        void DestroyTexture()
        {
            if (Application.isPlaying)
                Destroy(fogTexture);
            else
                DestroyImmediate(fogTexture);
        }

        void OnDestroy()
        {
            if (fogTexture != null)
                DestroyTexture();
        }
    }
}
