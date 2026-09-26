using UnityEngine;

namespace AnchorDefense
{
    // Presentation only: corner flash, soft additive mist and a floating skill badge.
    // Gameplay and targeting stay in AICommandField; this object has no collider.
    public sealed class AICommandFieldMarker : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] cornerSegments;
        [SerializeField] private Transform badgeRoot;
        [SerializeField] private SpriteRenderer badgeIcon;
        [SerializeField] private Sprite fallbackIcon;
        [SerializeField] private Material cornerMaterial;

        [Header("Command Mist — additive, no collider")]
        [SerializeField] private Material mistMaterial;
        [SerializeField, Range(8, 48)] private int mistPuffCount = 28;
        [SerializeField, Range(0f, 0.7f)] private float mistOpacity = 0.32f;
        [SerializeField] private Color mistColor = new Color(0.18f, 0.8f, 1f, 1f);
        [SerializeField, Min(0.1f)] private float mistFlashDuration = 0.8f;

        [Header("Cube Corners")]
        [SerializeField, Range(0.05f, 0.4f)] private float cornerLengthRatio = 0.18f;
        [SerializeField, Min(0.005f)] private float lineWidth = 0.055f;
        [SerializeField] private Color flashColor = new Color(0.55f, 0.95f, 1f, 0.78f);
        [SerializeField] private Color restingColor = new Color(0.35f, 0.78f, 1f, 0.055f);
        [SerializeField, Min(0.05f)] private float flashDuration = 0.6f;

        [Header("Floating Skill Icon")]
        [SerializeField, Min(0.05f)] private float badgeVisibleDuration = 1.35f;
        [SerializeField, Min(0.01f)] private float badgeWorldSize = 1.5f;
        [SerializeField, Min(0f)] private float badgeOffset = 0.45f;

        private ActiveSkillDefinition[] operations;
        private Camera gameplayCamera;
        private float elapsed;
        private float operationStagger;
        private float cubeSize;
        private int shownOperation = -1;
        private bool initialized;
        private Mesh mistMesh;
        private MeshRenderer mistRenderer;
        private Material runtimeMistMaterial;
        private Vector3[] mistVertices;
        private Vector3[] mistCenters;
        private Vector2[] mistUv;
        private int[] mistTriangles;
        // Unity may deserialize MonoBehaviours without retaining field initializers.
        private MaterialPropertyBlock mistProperties;

        public void Initialize(ActiveSkillDefinition[] commandOperations, float targetCubeSize,
            float staggerSeconds, Camera camera)
        {
            operations = commandOperations;
            operationStagger = Mathf.Max(0f, staggerSeconds);
            gameplayCamera = camera;
            cubeSize = Mathf.Max(0.1f, targetCubeSize);
            elapsed = 0f;
            shownOperation = -1;
            initialized = true;
            if (cornerSegments == null || cornerSegments.Length != 24)
                cornerSegments = GetComponentsInChildren<LineRenderer>(true);
            if (cornerSegments.Length != 24) CreateRuntimeCornerSegments();
            if (badgeRoot == null || badgeIcon == null) CreateRuntimeBadge();
            SetCornerGeometry();
            CreateMist();
            ShowOperation(0);
            RefreshPresentation();
        }

        private void Update()
        {
            if (!initialized) return;
            elapsed += Time.deltaTime;
            if (operations != null && operations.Length > 0)
            {
                int index = operationStagger <= 0f ? operations.Length - 1 :
                    Mathf.Clamp(Mathf.FloorToInt(elapsed / operationStagger), 0, operations.Length - 1);
                ShowOperation(index);
            }
            RefreshPresentation();
        }

        private void OnDestroy()
        {
            if (mistMesh != null) Destroy(mistMesh);
            if (runtimeMistMaterial != null) Destroy(runtimeMistMaterial);
        }

        private void CreateMist()
        {
            if (mistRenderer != null) return;
            Material source = mistMaterial;
            if (source == null)
            {
                Shader shader = Shader.Find("AnchorDefense/AICommandMist");
                if (shader == null) return;
                runtimeMistMaterial = new Material(shader);
                source = runtimeMistMaterial;
            }
            GameObject mist = new GameObject("Command Mist", typeof(MeshFilter), typeof(MeshRenderer));
            mist.transform.SetParent(transform, false);
            mistRenderer = mist.GetComponent<MeshRenderer>();
            mistRenderer.sharedMaterial = source;
            mistRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mistRenderer.receiveShadows = false;
            mistRenderer.allowOcclusionWhenDynamic = false;
            int count = Mathf.Clamp(mistPuffCount, 8, 48);
            mistVertices = new Vector3[count * 4];
            mistCenters = new Vector3[count];
            mistUv = new Vector2[count * 4];
            mistTriangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                // Deterministic positions keep the visual stable while the field is active.
                mistCenters[i] = new Vector3(Hash(i * 3 + 1), Hash(i * 3 + 2),
                    Hash(i * 3 + 3)) * (cubeSize * 0.68f);
                int v = i * 4;
                mistUv[v] = new Vector2(0f, 0f);
                mistUv[v + 1] = new Vector2(1f, 0f);
                mistUv[v + 2] = new Vector2(0f, 1f);
                mistUv[v + 3] = new Vector2(1f, 1f);
                int t = i * 6;
                mistTriangles[t] = v;
                mistTriangles[t + 1] = v + 2;
                mistTriangles[t + 2] = v + 1;
                mistTriangles[t + 3] = v + 2;
                mistTriangles[t + 4] = v + 3;
                mistTriangles[t + 5] = v + 1;
            }
            mistMesh = new Mesh { name = "AI Command Mist Quads" };
            mistMesh.MarkDynamic();
            mistMesh.vertices = mistVertices;
            mistMesh.uv = mistUv;
            mistMesh.triangles = mistTriangles;
            mist.GetComponent<MeshFilter>().sharedMesh = mistMesh;
            mistMesh.bounds = new Bounds(Vector3.zero, Vector3.one * cubeSize * 2f);
        }

        private static float Hash(int seed)
        {
            float value = Mathf.Sin(seed * 78.233f) * 43758.5453f;
            return (value - Mathf.Floor(value)) * 2f - 1f;
        }

        private void UpdateMist(Camera camera)
        {
            if (mistMesh == null || mistRenderer == null || camera == null) return;
            Vector3 right = transform.InverseTransformDirection(camera.transform.right);
            Vector3 up = transform.InverseTransformDirection(camera.transform.up);
            for (int i = 0; i < mistCenters.Length; i++)
            {
                float phase = elapsed * (0.7f + (i % 5) * 0.12f) + i * 1.7f;
                Vector3 center = mistCenters[i] + Vector3.up * (Mathf.Sin(phase) * cubeSize * 0.045f);
                float radius = cubeSize * (0.12f + (i % 4) * 0.025f);
                int v = i * 4;
                mistVertices[v] = center - right * radius - up * radius;
                mistVertices[v + 1] = center + right * radius - up * radius;
                mistVertices[v + 2] = center - right * radius + up * radius;
                mistVertices[v + 3] = center + right * radius + up * radius;
            }
            mistMesh.vertices = mistVertices;
            float flash = 1f - Mathf.Clamp01(elapsed / mistFlashDuration);
            float pulse = 0.88f + Mathf.Sin(elapsed * 2.7f) * 0.12f;
            if (mistProperties == null) mistProperties = new MaterialPropertyBlock();
            mistProperties.Clear();
            mistProperties.SetColor("_TintColor", mistColor);
            mistProperties.SetFloat("_Opacity", mistOpacity * (pulse + flash * 1.1f));
            mistRenderer.SetPropertyBlock(mistProperties);
        }

        private void SetCornerGeometry()
        {
            if (cornerSegments == null) return;
            float half = cubeSize * 0.5f;
            float length = cubeSize * cornerLengthRatio;
            int segmentIndex = 0;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = new Vector3(x * half, y * half, z * half);
                for (int axis = 0; axis < 3; axis++)
                {
                    if (segmentIndex >= cornerSegments.Length) return;
                    LineRenderer line = cornerSegments[segmentIndex++];
                    if (line == null) continue;
                    Vector3 inward = axis == 0 ? new Vector3(-x * length, 0f, 0f) :
                        axis == 1 ? new Vector3(0f, -y * length, 0f) :
                        new Vector3(0f, 0f, -z * length);
                    line.useWorldSpace = false;
                    line.positionCount = 2;
                    line.SetPosition(0, corner);
                    line.SetPosition(1, corner + inward);
                    line.widthMultiplier = lineWidth;
                }
            }
        }

        private void CreateRuntimeCornerSegments()
        {
            cornerSegments = new LineRenderer[24];
            for (int i = 0; i < cornerSegments.Length; i++)
            {
                GameObject segment = new GameObject($"Corner Segment {i + 1:00}",
                    typeof(LineRenderer));
                segment.transform.SetParent(transform, false);
                LineRenderer line = segment.GetComponent<LineRenderer>();
                line.sharedMaterial = cornerMaterial;
                line.alignment = LineAlignment.View;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sortingOrder = 80;
                cornerSegments[i] = line;
            }
        }

        private void CreateRuntimeBadge()
        {
            GameObject badge = new GameObject("Skill Icon Badge");
            badge.transform.SetParent(transform, false);
            badgeRoot = badge.transform;
            GameObject icon = new GameObject("Skill Icon", typeof(SpriteRenderer));
            icon.transform.SetParent(badgeRoot, false);
            badgeIcon = icon.GetComponent<SpriteRenderer>();
            badgeIcon.sortingOrder = 90;
        }

        private void ShowOperation(int index)
        {
            if (operations == null || operations.Length == 0 || index == shownOperation) return;
            shownOperation = index;
            if (badgeIcon != null)
            {
                Sprite icon = operations[index] != null ? operations[index].Icon : null;
                badgeIcon.sprite = icon != null ? icon : fallbackIcon;
                badgeIcon.gameObject.SetActive(badgeIcon.sprite != null);
            }
        }

        private void RefreshPresentation()
        {
            float flash = 1f - Mathf.Clamp01(elapsed / flashDuration);
            flash = flash * flash * (0.83f + 0.17f * Mathf.Sin(elapsed * 32f));
            Color current = Color.Lerp(restingColor, flashColor, flash);
            if (cornerSegments != null)
            {
                for (int i = 0; i < cornerSegments.Length; i++)
                {
                    LineRenderer line = cornerSegments[i];
                    if (line == null) continue;
                    line.startColor = current;
                    line.endColor = current;
                }
            }

            Camera currentCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            UpdateMist(currentCamera);
            if (badgeRoot == null || badgeIcon == null || badgeIcon.sprite == null) return;
            if (currentCamera != null)
            {
                Transform cameraTransform = currentCamera.transform;
                badgeRoot.position = transform.position +
                    cameraTransform.right * (cubeSize * 0.36f + badgeOffset) +
                    cameraTransform.up * (cubeSize * 0.34f + badgeOffset);
                badgeRoot.rotation = cameraTransform.rotation;
            }
            float spriteSize = Mathf.Max(badgeIcon.sprite.bounds.size.x,
                badgeIcon.sprite.bounds.size.y);
            badgeIcon.transform.localScale = Vector3.one * (badgeWorldSize / Mathf.Max(0.01f, spriteSize));
            float lastActivation = operations != null
                ? Mathf.Max(0, operations.Length - 1) * operationStagger : 0f;
            float remaining = Mathf.Clamp01((lastActivation + badgeVisibleDuration - elapsed) / 0.35f);
            badgeIcon.color = new Color(1f, 1f, 1f, remaining);
        }

#if UNITY_EDITOR
        public void ConfigureEditor(LineRenderer[] segments, Transform badge,
            SpriteRenderer icon, Sprite fallback, Material material, float previewCubeSize)
        {
            cornerSegments = segments;
            badgeRoot = badge;
            badgeIcon = icon;
            fallbackIcon = fallback;
            cornerMaterial = material;
            cubeSize = Mathf.Max(0.1f, previewCubeSize);
            SetCornerGeometry();
            for (int i = 0; i < cornerSegments.Length; i++)
            {
                cornerSegments[i].startColor = restingColor;
                cornerSegments[i].endColor = restingColor;
            }
        }
#endif
    }
}
