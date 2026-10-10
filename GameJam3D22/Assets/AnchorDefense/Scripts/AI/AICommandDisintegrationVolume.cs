using UnityEngine;

namespace AnchorDefense
{
    // A visual envelope only. The outside brackets continue to mark the real damage bounds
    // while this inner volume assembles and contracts. Two renderers, no colliders or lights.
    public sealed class AICommandDisintegrationVolume : MonoBehaviour
    {
        private Mesh shellMesh;
        private Mesh coreMesh;
        private MeshRenderer shell;
        private MeshRenderer core;
        private MaterialPropertyBlock shellProperties;
        private MaterialPropertyBlock coreProperties;
        private float size;

        public void Initialize(Material material, float cubeSize)
        {
            if (shell != null) return;
            size = cubeSize;
            shellMesh = CreateShell();
            coreMesh = AICommandActorFeedback.CreateQuad("Command Singularity Core");
            shell = CreateVisual("Disintegration Shell", shellMesh, material);
            core = CreateVisual("Singularity Core", coreMesh, material);
            shellProperties = new MaterialPropertyBlock();
            coreProperties = new MaterialPropertyBlock();
        }

        public void Present(Camera camera, float age, float remaining, float formationDuration,
            float collapseDuration, float opacity)
        {
            if (shell == null) return;
            bool visible = age >= 0f && !float.IsInfinity(age) && remaining > 0f;
            shell.enabled = core.enabled = visible;
            if (!visible) return;

            float formation = Mathf.Max(0.1f, formationDuration);
            float charge = formation * 0.3f;
            float unfold = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(charge, formation, age));
            float collapse = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.1f, collapseDuration));
            float shrink = Mathf.SmoothStep(0f, 1f, collapse);
            float scale = Mathf.Lerp(0.1f, 1f, unfold) * Mathf.Lerp(1f, 0.04f, shrink);
            shell.transform.localScale = Vector3.one * size * scale;
            // Rotation settles before the cube reaches its true size, so the visual stays in bounds.
            shell.transform.localRotation = age < charge
                ? Quaternion.Euler(18f, 30f + (1f - age / charge) * 100f, 12f)
                : Quaternion.Slerp(Quaternion.Euler(18f, 30f, 12f), Quaternion.identity, Mathf.Clamp01(unfold * 3f));

            float fade = Mathf.Clamp01(remaining / 0.16f);
            float entrance = Mathf.Clamp01(age / 0.12f);
            shellProperties.SetFloat("_Style", 9f);
            shellProperties.SetFloat("_Progress", age);
            shellProperties.SetFloat("_Dissolve", shrink);
            shellProperties.SetFloat("_SealPulse", Mathf.Exp(-Mathf.Pow((age - formation) * 7f, 2f)));
            shellProperties.SetFloat("_Opacity", opacity * fade * entrance);
            shellProperties.SetColor("_TintColor", new Color(0.42f, 0.56f, 0.78f, 1f));
            shell.SetPropertyBlock(shellProperties);

            float openingPulse = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(charge, formation + 0.25f, age));
            float closingPulse = Mathf.Sin(collapse * Mathf.PI);
            float pulse = Mathf.Max(openingPulse, closingPulse);
            core.enabled = pulse > 0.005f;
            if (camera != null) core.transform.rotation = camera.transform.rotation;
            core.transform.localScale = Vector3.one * size * Mathf.Lerp(0.1f, 0.23f, pulse);
            coreProperties.SetFloat("_Style", 10f);
            coreProperties.SetFloat("_Progress", age);
            coreProperties.SetFloat("_Opacity", pulse * fade * entrance * 0.32f);
            coreProperties.SetColor("_TintColor", new Color(0.7f, 0.8f, 1.05f, 1f));
            core.SetPropertyBlock(coreProperties);
        }

        private MeshRenderer CreateVisual(string label, Mesh mesh, Material material)
        {
            var child = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            child.transform.SetParent(transform, false);
            child.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Mesh CreateShell()
        {
            var vertices = new Vector3[24];
            var uv = new Vector2[24];
            var colors = new Color[24];
            var triangles = new int[36];
            for (int face = 0; face < 6; face++)
            {
                int axis = face / 2;
                Vector3 normal = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                normal *= face % 2 == 0 ? 0.5f : -0.5f;
                Vector3 horizontal = axis == 0 ? Vector3.forward : Vector3.right;
                Vector3 vertical = axis == 1 ? Vector3.forward : Vector3.up;
                for (int corner = 0; corner < 4; corner++)
                {
                    int v = face * 4 + corner;
                    uv[v] = new Vector2(corner % 2, corner / 2);
                    vertices[v] = normal + horizontal * (uv[v].x - 0.5f) + vertical * (uv[v].y - 0.5f);
                    colors[v] = face % 2 == 0 ? Color.white : new Color(0.85f, 0.72f, 1f);
                }
                int t = face * 6, start = face * 4;
                triangles[t] = start; triangles[t + 1] = start + 2; triangles[t + 2] = start + 1;
                triangles[t + 3] = start + 2; triangles[t + 4] = start + 3; triangles[t + 5] = start + 1;
            }
            var mesh = new Mesh { name = "Command Disintegration Envelope" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (shellMesh != null) Destroy(shellMesh);
            if (coreMesh != null) Destroy(coreMesh);
        }
    }
}
