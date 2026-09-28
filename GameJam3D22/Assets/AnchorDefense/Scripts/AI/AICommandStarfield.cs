using UnityEngine;

namespace AnchorDefense
{
    // One small mesh for all the field's stars; no per-frame objects or particle textures.
    public sealed class AICommandStarfield : MonoBehaviour
    {
        private Mesh mesh;
        private MeshRenderer visual;
        private MaterialPropertyBlock properties;
        private Vector3[] homes;
        private Vector3[] vertices;
        private Color[] colors;
        private float[] sizes;
        private float[] phases;
        private float[] rates;
        private float cubeSize;
        private float brightness;
        private float driftSpeed;
        private LineRenderer[] lightArcs;
        private MaterialPropertyBlock arcProperties;
        private static readonly Color[] spectrum = {
            new Color(0.28f, 0.62f, 1f), new Color(0.66f, 0.36f, 1f),
            new Color(1f, 0.36f, 0.68f), new Color(1f, 0.75f, 0.3f),
            new Color(0.25f, 1f, 0.72f), new Color(0.25f, 0.88f, 1f)
        };

        public static Color Palette(float phase)
        {
            float index = Mathf.Repeat(phase, 1f) * spectrum.Length;
            int start = Mathf.FloorToInt(index);
            return Color.Lerp(spectrum[start], spectrum[(start + 1) % spectrum.Length], index - start);
        }

        public void Initialize(Material material, int count, float fieldSize, float starSize, float opacity, float speed)
        {
            count = Mathf.Clamp(count, 0, 128);
            cubeSize = fieldSize;
            brightness = opacity;
            driftSpeed = speed;
            homes = new Vector3[count];
            sizes = new float[count];
            phases = new float[count];
            rates = new float[count];
            vertices = new Vector3[count * 4];
            colors = new Color[count * 4];
            var uv = new Vector2[count * 4];
            var triangles = new int[count * 6];
            var random = new System.Random(27183);
            float Next() => (float)random.NextDouble();
            for (int i = 0; i < count; i++)
            {
                homes[i] = i < 8 ? new Vector3((i & 1) == 0 ? -0.48f : 0.48f,
                    (i & 2) == 0 ? -0.48f : 0.48f, (i & 4) == 0 ? -0.48f : 0.48f) * cubeSize :
                    new Vector3(Next() - 0.5f, Next() - 0.5f, Next() - 0.5f) * cubeSize * 0.9f;
                sizes[i] = starSize * (i < 8 ? 1.45f : Mathf.Lerp(0.55f, 1.2f, Next()));
                phases[i] = Next() * Mathf.PI * 2f;
                rates[i] = Mathf.Lerp(1.3f, 3f, Next());
                uv[i * 4] = Vector2.zero;
                uv[i * 4 + 1] = Vector2.right;
                uv[i * 4 + 2] = Vector2.up;
                uv[i * 4 + 3] = Vector2.one;
                int t = i * 6, v = i * 4;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 2; triangles[t + 4] = v + 3; triangles[t + 5] = v + 1;
            }
            mesh = new Mesh { name = "Command Twinkling Stars" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (cubeSize * 1.3f + starSize * 2f));
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            visual = gameObject.AddComponent<MeshRenderer>();
            visual.sharedMaterial = material;
            visual.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            visual.receiveShadows = false;
            properties = new MaterialPropertyBlock();
            CreateLightArcs(material);
        }

        private void CreateLightArcs(Material material)
        {
            lightArcs = new LineRenderer[2];
            arcProperties = new MaterialPropertyBlock();
            for (int i = 0; i < lightArcs.Length; i++)
            {
                var arcObject = new GameObject("Stardust Light Arc");
                arcObject.transform.SetParent(transform, false);
                LineRenderer arc = arcObject.AddComponent<LineRenderer>();
                arc.useWorldSpace = false;
                arc.positionCount = 40;
                arc.widthMultiplier = cubeSize * 0.0025f;
                arc.sharedMaterial = material;
                arc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                arc.receiveShadows = false;
                for (int j = 0; j < arc.positionCount; j++)
                {
                    float t = j / (arc.positionCount - 1f), angle = -1.1f + t * 2.2f + i * Mathf.PI;
                    arc.SetPosition(j, new Vector3(Mathf.Cos(angle) * 0.34f,
                        -0.12f + i * 0.24f + Mathf.Sin(t * Mathf.PI) * 0.09f,
                        Mathf.Sin(angle) * 0.34f) * cubeSize);
                }
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Palette(i * 0.45f), 0f),
                    new GradientColorKey(Palette(i * 0.45f + 0.18f), 0.5f),
                    new GradientColorKey(Palette(i * 0.45f + 0.36f), 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 0.35f),
                        new GradientAlphaKey(0.55f, 0.65f), new GradientAlphaKey(0f, 1f) });
                arc.colorGradient = gradient;
                lightArcs[i] = arc;
            }
        }

        public void Present(Camera camera, float time, float activationAge, float remaining, float fadeDuration)
        {
            if (mesh == null) return;
            visual.enabled = remaining > 0f && brightness > 0f && homes.Length > 0;
            foreach (LineRenderer arc in lightArcs) arc.enabled = visual.enabled;
            if (!visual.enabled) return;
            Vector3 right = camera != null ? transform.InverseTransformVector(camera.transform.right) : Vector3.right;
            Vector3 up = camera != null ? transform.InverseTransformVector(camera.transform.up) : Vector3.up;
            float fade = Mathf.Clamp01(remaining / Mathf.Max(0.1f, fadeDuration));
            float entrance = Mathf.Clamp01(time / 0.25f);
            float burst = activationAge >= 0f && activationAge < 0.8f ? Mathf.Sin(activationAge / 0.8f * Mathf.PI) * 0.3f : 0f;
            for (int i = 0; i < homes.Length; i++)
            {
                float phase = phases[i];
                float blink = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(time * rates[i] + phase), 5f);
                Vector3 position = homes[i];
                if (i >= 8) position += new Vector3(Mathf.Sin(time * driftSpeed + phase),
                    Mathf.Sin(time * driftSpeed * 0.7f + phase), Mathf.Cos(time * driftSpeed + phase)) * cubeSize * 0.018f;
                float size = sizes[i] * (0.85f + blink * 0.35f);
                Vector3 x = right * size * 0.5f, y = up * size * 0.5f;
                int v = i * 4;
                vertices[v] = position - x - y; vertices[v + 1] = position + x - y;
                vertices[v + 2] = position - x + y; vertices[v + 3] = position + x + y;
                Color tint = Color.Lerp(Palette(phase / (Mathf.PI * 2f) + time * 0.008f), Color.white, blink * 0.12f);
                tint.a = Mathf.Clamp01(0.12f + blink * 0.88f + burst) * fade * entrance;
                for (int j = 0; j < 4; j++) colors[v + j] = tint;
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            properties.SetFloat("_Style", 5f);
            properties.SetFloat("_Opacity", brightness);
            properties.SetColor("_TintColor", Color.white);
            visual.SetPropertyBlock(properties);
            arcProperties.SetFloat("_Style", 7f);
            arcProperties.SetFloat("_Progress", time);
            arcProperties.SetFloat("_Opacity", brightness * fade * entrance * 0.65f);
            arcProperties.SetColor("_TintColor", Color.white);
            foreach (LineRenderer arc in lightArcs) arc.SetPropertyBlock(arcProperties);
        }

        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
