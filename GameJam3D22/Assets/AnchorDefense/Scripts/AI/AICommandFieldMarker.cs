using UnityEngine;

namespace AnchorDefense
{
    // Presentation only: prismatic scan, breathing corners and twinkling stars.
    // Gameplay and targeting stay in AICommandField; this object has no collider.
    public sealed class AICommandFieldMarker : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] cornerSegments;
        [SerializeField, HideInInspector] private Transform badgeRoot;
        [SerializeField, HideInInspector] private SpriteRenderer badgeIcon;
        [SerializeField] private Material cornerMaterial;

        [Header("Starlight Burst")]
        [SerializeField] private Material signalMaterial;
        [SerializeField, Min(0.1f)] private float starBurstDuration = 0.75f;
        [SerializeField, Range(0f, 1f)] private float starBurstOpacity = 0.5f;
        [SerializeField, Range(0f, 0.5f)] private float cornerBreathAmount = 0.18f;
        [SerializeField, Min(0.1f)] private float endingFadeDuration = 0.45f;

        [Header("Prismatic Scan")]
        [UnityEngine.Serialization.FormerlySerializedAs("scanDuration")]
        [SerializeField, Min(0.1f)] private float scanSweepDuration = 0.9f;
        [UnityEngine.Serialization.FormerlySerializedAs("scanOpacity")]
        [SerializeField, Range(0f, 1f)] private float scanSweepOpacity = 0.38f;
        [SerializeField, Range(0f, 1f)] private float cornerColorVariation = 0.6f;

        [Header("Twinkling Stars")]
        [SerializeField, Range(0, 128)] private int starCount = 42;
        [SerializeField, Min(0.01f)] private float starWorldSize = 0.3f;
        [SerializeField, Range(0f, 1f)] private float starOpacity = 0.85f;
        [SerializeField, Range(0f, 2f)] private float starDriftSpeed = 0.35f;

        [Header("Actor Feedback")]
        [SerializeField, Min(0.1f)] private float actorCueWorldSize = 0.9f;
        [SerializeField, Range(0f, 1f)] private float actorCueOpacity = 0.9f;
        public Material SignalMaterial => signalMaterial != null ? signalMaterial : runtimeSignalMaterial;
        public float ActorCueWorldSize => actorCueWorldSize;
        public float ActorCueOpacity => actorCueOpacity;

        [Header("Cube Corners")]
        [SerializeField, Range(0.05f, 0.4f)] private float cornerLengthRatio = 0.18f;
        [SerializeField, Min(0.005f)] private float lineWidth = 0.055f;
        [SerializeField] private Color flashColor = new Color(0.55f, 0.95f, 1f, 0.78f);
        [SerializeField] private Color restingColor = new Color(0.35f, 0.78f, 1f, 0.13f);
        [SerializeField, Min(0.05f)] private float flashDuration = 0.6f;

        private ActiveSkillDefinition[] operations;
        private Camera gameplayCamera;
        private float elapsed;
        private float operationStagger;
        private float cubeSize;
        private int shownOperation = -1;
        private bool initialized;
        private Mesh burstMesh;
        private MeshRenderer burstRenderer;
        private Material runtimeSignalMaterial;
        private MaterialPropertyBlock burstProperties;
        private float burstStarted;
        private float endsAt;
        private float fieldDuration;
        private bool drivenByField;
        private Color burstColor = new Color(0.35f, 0.88f, 1f);
        private AICommandStarfield stars;
        private MeshRenderer scanRenderer;
        private MaterialPropertyBlock scanProperties;

        public void Initialize(ActiveSkillDefinition[] commandOperations, float targetCubeSize,
            float staggerSeconds, Camera camera, float lifetime = 6f, bool externallyDriven = false)
        {
            operations = commandOperations;
            operationStagger = Mathf.Max(0f, staggerSeconds);
            gameplayCamera = camera;
            cubeSize = Mathf.Max(0.1f, targetCubeSize);
            fieldDuration = Mathf.Max(0.1f, lifetime);
            drivenByField = externallyDriven;
            elapsed = 0f;
            shownOperation = -1;
            endsAt = fieldDuration;
            burstStarted = float.NegativeInfinity;
            initialized = true;
            if (cornerSegments == null || cornerSegments.Length != 24)
                cornerSegments = GetComponentsInChildren<LineRenderer>(true);
            if (cornerSegments.Length != 24) CreateRuntimeCornerSegments();
            HideLegacyBadge();
            SetCornerGeometry();
            CreateBurst();
            CreateScan();
            if (stars == null && SignalMaterial != null)
            {
                var starObject = new GameObject("Command Starlight");
                starObject.transform.SetParent(transform, false);
                stars = starObject.AddComponent<AICommandStarfield>();
                stars.Initialize(SignalMaterial, starCount, cubeSize, starWorldSize, starOpacity, starDriftSpeed);
            }
            if (!drivenByField && operations != null && operations.Length > 0)
                NotifyActivation(operations[0]);
            RefreshPresentation();
        }

        // Called only after the real operation activates, including operations delayed by other groups.
        public void NotifyActivation(ActiveSkillDefinition operation)
        {
            if (!initialized || operation == null) return;
            shownOperation = operations != null ? System.Array.IndexOf(operations, operation) : -1;
            burstColor = AICommandActorFeedback.ColorFor(operation.Effect);
            burstStarted = elapsed;
            endsAt = elapsed + fieldDuration;
            RefreshPresentation();
        }

        private void Update()
        {
            if (!initialized) return;
            elapsed += Time.deltaTime;
            if (!drivenByField && operations != null && operations.Length > 0)
            {
                int index = operationStagger <= 0f ? operations.Length - 1 :
                    Mathf.Clamp(Mathf.FloorToInt(elapsed / operationStagger), 0, operations.Length - 1);
                if (index != shownOperation) NotifyActivation(operations[index]);
            }
            RefreshPresentation();
        }

        private void OnDestroy()
        {
            if (burstMesh != null) Destroy(burstMesh);
            if (runtimeSignalMaterial != null) Destroy(runtimeSignalMaterial);
        }

        private void CreateBurst()
        {
            if (burstRenderer != null) return;
            Material source = signalMaterial;
            if (source == null)
            {
                Shader shader = Shader.Find("AnchorDefense/AICommandSignal");
                if (shader == null) return;
                runtimeSignalMaterial = new Material(shader);
                source = runtimeSignalMaterial;
            }
            GameObject plane = new GameObject("Command Starburst", typeof(MeshFilter), typeof(MeshRenderer));
            plane.transform.SetParent(transform, false);
            plane.transform.localScale = Vector3.one * cubeSize * 1.15f;
            burstMesh = AICommandActorFeedback.CreateQuad("AI Command Starlight Burst");
            plane.GetComponent<MeshFilter>().sharedMesh = burstMesh;
            burstRenderer = plane.GetComponent<MeshRenderer>();
            burstRenderer.sharedMaterial = source;
            burstRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            burstRenderer.receiveShadows = false;
            burstProperties = new MaterialPropertyBlock();
        }

        private void UpdateBurst()
        {
            if (burstRenderer == null) return;
            float t = (elapsed - burstStarted) / Mathf.Max(0.1f, starBurstDuration);
            burstRenderer.enabled = t >= 0f && t < 1f;
            if (!burstRenderer.enabled) return;
            Camera view = gameplayCamera != null ? gameplayCamera : Camera.main;
            if (view != null) burstRenderer.transform.rotation = view.transform.rotation;
            burstRenderer.transform.localPosition = Vector3.up * cubeSize * t * 0.025f;
            burstProperties.SetColor("_TintColor", Color.Lerp(burstColor, new Color(0.6f, 0.83f, 1f), 0.75f));
            burstProperties.SetFloat("_Style", 0f);
            burstProperties.SetFloat("_Progress", t);
            burstProperties.SetFloat("_Opacity", starBurstOpacity * Mathf.Sin(t * Mathf.PI));
            burstRenderer.SetPropertyBlock(burstProperties);
        }

        private void CreateScan()
        {
            if (scanRenderer != null || SignalMaterial == null) return;
            var plane = new GameObject("Command Scan", typeof(MeshFilter), typeof(MeshRenderer));
            plane.transform.SetParent(transform, false);
            plane.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            plane.transform.localScale = Vector3.one * cubeSize * 0.98f;
            plane.GetComponent<MeshFilter>().sharedMesh = burstMesh;
            scanRenderer = plane.GetComponent<MeshRenderer>();
            scanRenderer.sharedMaterial = SignalMaterial;
            scanRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            scanRenderer.receiveShadows = false;
            scanProperties = new MaterialPropertyBlock();
        }

        private void UpdateScan(float activationAge)
        {
            if (scanRenderer == null) return;
            float progress = activationAge / Mathf.Max(0.1f, scanSweepDuration);
            scanRenderer.enabled = progress >= 0f && progress < 1f;
            if (!scanRenderer.enabled) return;
            scanRenderer.transform.localPosition = Vector3.up * Mathf.Lerp(cubeSize * 0.48f, -cubeSize * 0.48f, progress);
            scanProperties.SetFloat("_Style", 6f);
            scanProperties.SetFloat("_Progress", progress);
            scanProperties.SetFloat("_Opacity", scanSweepOpacity * Mathf.Sin(progress * Mathf.PI));
            scanProperties.SetColor("_TintColor", Color.white);
            scanRenderer.SetPropertyBlock(scanProperties);
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

        private void HideLegacyBadge()
        {
            if (badgeRoot != null) badgeRoot.gameObject.SetActive(false);
            if (badgeIcon != null) badgeIcon.enabled = false;
        }

        private void RefreshPresentation()
        {
            float activationAge = elapsed - burstStarted;
            float flash = 0f;
            if (!float.IsInfinity(activationAge) && activationAge >= 0f)
            {
                flash = 1f - Mathf.Clamp01(activationAge / flashDuration);
                flash = flash * flash * (0.83f + 0.17f * Mathf.Sin(activationAge * 32f));
            }
            float remaining = Mathf.Max(0f, endsAt - elapsed);
            float ending = 1f - Mathf.Clamp01(remaining / endingFadeDuration);
            float endingFlash = Mathf.Sin(ending * Mathf.PI) * 0.65f;
            Color current = Color.Lerp(restingColor, flashColor, Mathf.Max(flash, endingFlash));
            if (flash <= 0f && ending <= 0f)
                current.a *= 1f + Mathf.Sin(elapsed * 2f) * cornerBreathAmount;
            current.a *= Mathf.Clamp01(remaining / Mathf.Min(0.22f, endingFadeDuration));
            if (cornerSegments != null)
                for (int i = 0; i < cornerSegments.Length; i++)
                {
                    LineRenderer line = cornerSegments[i];
                    if (line == null) continue;
                    Color color = Color.Lerp(current, AICommandStarfield.Palette(i / 24f + elapsed * 0.012f), cornerColorVariation);
                    color.a = current.a;
                    line.startColor = color;
                    Color tip = Color.Lerp(color, AICommandStarfield.Palette(i / 24f + 0.08f), 0.25f);
                    tip.a = color.a;
                    line.endColor = tip;
                }

            UpdateBurst();
            UpdateScan(activationAge);
            Camera currentCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            if (stars != null) stars.Present(currentCamera, elapsed, activationAge, remaining, endingFadeDuration);
        }

#if UNITY_EDITOR
        public void ConfigureEditor(LineRenderer[] segments, Transform badge,
            SpriteRenderer icon, Sprite fallback, Material material, float previewCubeSize)
        {
            cornerSegments = segments;
            badgeRoot = badge;
            badgeIcon = icon;
            HideLegacyBadge();
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
