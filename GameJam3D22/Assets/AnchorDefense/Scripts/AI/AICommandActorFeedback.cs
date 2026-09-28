using UnityEngine;

namespace AnchorDefense
{
    // Owned by the field, never the pooled actor. Refreshing reuses one small cue per actor/style.
    public sealed class AICommandActorFeedback : MonoBehaviour
    {
        private Transform target;
        private Renderer actorRenderer;
        private EnemyController enemy;
        private int spawnVersion;
        private Mesh mesh;
        private Material material;
        private MeshRenderer cueRenderer;
        private MaterialPropertyBlock properties;
        private Camera viewCamera;
        private Color tint;
        private int style;
        private float size;
        private float age;
        private float expiresAt;
        private float opacity;
        public int Style => style;

        public static Color ColorFor(ActiveSkillEffect effect)
        {
            if (effect is AreaDamageSkillEffect) return new Color(1f, 0.7f, 0.32f);
            if (effect is TurretRepairSkillEffect) return new Color(0.3f, 1f, 0.72f);
            if (effect is EnemySlowSkillEffect) return new Color(0.4f, 0.8f, 1f);
            if (effect is TurretBoostSkillEffect) return new Color(0.95f, 0.65f, 1f);
            return new Color(0.35f, 0.88f, 1f);
        }

        public static bool Supports(ActiveSkillEffect effect) => effect is AreaDamageSkillEffect ||
            effect is TurretRepairSkillEffect || effect is EnemySlowSkillEffect ||
            effect is TurretBoostSkillEffect || effect is EnemyRepulsionSkillEffect;

        public void Initialize(Transform actor, EnemyController enemyActor, Camera gameplayCamera,
            int cueStyle, Color color, Vector3 position, float worldSize, float alpha, Material source)
        {
            style = cueStyle;
            tint = color;
            target = style == 1 ? null : actor; // Hit flashes stay at the impact even if the enemy dies.
            actorRenderer = VisualFor(actor);
            enemy = enemyActor;
            spawnVersion = enemy != null ? enemy.SpawnVersion : 0;
            viewCamera = gameplayCamera;
            size = worldSize;
            opacity = alpha;
            transform.position = position;
            transform.localScale = Vector3.one; // The field may be scaled; compensate in LateUpdate.
            mesh = CreateQuad("Command Actor Cue");
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            cueRenderer = gameObject.AddComponent<MeshRenderer>();
            if (source == null)
            {
                Shader shader = Shader.Find("AnchorDefense/AICommandSignal");
                if (shader == null) { enabled = false; return; }
                material = new Material(shader);
                source = material;
            }
            cueRenderer.sharedMaterial = source;
            cueRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cueRenderer.receiveShadows = false;
            cueRenderer.sortingOrder = 85;
            properties = new MaterialPropertyBlock();
            Refresh();
            LateUpdate();
        }

        public void Refresh()
        {
            if (style == 1) age = 0f;
            expiresAt = age + (style == 1 ? 0.32f : 0.24f);
        }

        public static Vector3 FrontPosition(Transform actor, Renderer visual, Camera view)
        {
            Vector3 center = visual != null ? visual.bounds.center : actor.position;
            if (view == null) return center;
            Vector3 towardCamera = view.orthographic ? -view.transform.forward : (view.transform.position - center).normalized;
            Vector3 extents = visual != null ? visual.bounds.extents : Vector3.one * 0.15f;
            float depth = Mathf.Abs(towardCamera.x) * extents.x + Mathf.Abs(towardCamera.y) * extents.y + Mathf.Abs(towardCamera.z) * extents.z;
            return center + towardCamera * (depth + 0.04f);
        }

        public static Renderer VisualFor(Transform actor)
        {
            if (actor == null) return null;
            Renderer largest = null;
            float largestSize = 0f;
            foreach (Renderer visual in actor.GetComponentsInChildren<Renderer>())
            {
                if (!visual.enabled || !(visual is SpriteRenderer || visual is MeshRenderer || visual is SkinnedMeshRenderer)) continue;
                float size = visual.bounds.size.sqrMagnitude;
                if (size <= largestSize) continue;
                largest = visual;
                largestSize = size;
            }
            return largest;
        }

        public static Mesh CreateQuad(string name)
        {
            var result = new Mesh { name = name };
            result.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            result.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            result.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            result.RecalculateBounds();
            return result;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= expiresAt || (style != 1 && (target == null || !target.gameObject.activeInHierarchy ||
                (enemy != null && (!enemy.IsAlive || enemy.SpawnVersion != spawnVersion)))))
                Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (cueRenderer == null || properties == null) return;
            Camera view = viewCamera != null ? viewCamera : Camera.main;
            if (target != null) transform.position = FrontPosition(target, actorRenderer, view);
            if (view != null) transform.rotation = view.transform.rotation;
            Vector3 scale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(size / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                size / Mathf.Max(0.001f, Mathf.Abs(scale.y)), size / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            float fade = style == 1 ? Mathf.Pow(Mathf.Clamp01((expiresAt - age) / 0.32f), 0.65f) :
                Mathf.Clamp01((expiresAt - age) / 0.08f);
            properties.SetColor("_TintColor", tint);
            properties.SetFloat("_Style", style);
            properties.SetFloat("_Progress", style == 1 ? age / 0.32f : age);
            properties.SetFloat("_Opacity", opacity * fade);
            cueRenderer.SetPropertyBlock(properties);
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
