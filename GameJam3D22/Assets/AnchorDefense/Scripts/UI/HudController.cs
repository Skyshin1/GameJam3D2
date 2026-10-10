using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class HudController : MonoBehaviour
    {
        [Header("Scene UI References")]
        [SerializeField] private Text healthText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text enemyText;
        [SerializeField] private Text finalTimeText;
        [SerializeField] private Image healthFill;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Sprite waveCatIdleSprite;
        [SerializeField] private Sprite waveCatAlertSprite;

        private CoreHealth core;
        private EndlessEnemySpawner spawner;
        private EnemyRegistry registry;
        private const string ChineseUiFontPath = "Assets/AnchorDefense/Art/UI/PF频凡胡涂体 PFANHUTUTI.ttf";

        private Font chineseUiFont;
        private GameFlowController gameFlow;
        private string coreLabel;
        private string timerLabel;
        private string enemyLabel;
        private string finalTimeLabel;
        private Image waveProgress;
        private Image waveBorder;
        private Image waveMarker;
        private Image waveCatPortrait;
        private RectTransform waveBeacon;
        private RectTransform waveFlag;
        private Image waveFlagIcon;
        private Text waveTitle;
        private Text waveTime;
        private CubeZoneEditModeController zoneEditor;
        private static Sprite roundedWaveSprite;

        public void ConfigureView(
            Text coreHealthText,
            Text elapsedTimeText,
            Text aliveEnemyText,
            Text finalTime,
            Image coreHealthFill,
            GameObject gameOver,
            Button restart)
        {
            healthText = coreHealthText;
            timerText = elapsedTimeText;
            enemyText = aliveEnemyText;
            finalTimeText = finalTime;
            healthFill = coreHealthFill;
            gameOverPanel = gameOver;
            restartButton = restart;
        }

        public void ConfigureWaveArt(Sprite idle, Sprite alert)
        {
            waveCatIdleSprite = idle;
            waveCatAlertSprite = alert;
        }

        public void Initialize(
            CoreHealth coreHealth,
            EndlessEnemySpawner enemySpawner,
            EnemyRegistry enemyRegistry,
            GameFlowController flow)
        {
            core = coreHealth;
            spawner = enemySpawner;
            registry = enemyRegistry;
            ApplyRuntimeFonts();
            gameFlow = flow;

            coreLabel = ExtractLeadingLabel(healthText != null ? healthText.text : null, "CORE");
            timerLabel = ExtractLeadingLabel(timerText != null ? timerText.text : null, "TIME");
            enemyLabel = ExtractLeadingLabel(enemyText != null ? enemyText.text : null, "ENEMY");
            finalTimeLabel = ExtractLeadingLabel(finalTimeText != null ? finalTimeText.text : null, "TIME");

            core.HealthChanged += HandleHealthChanged;
            GameLocalization.LanguageChanged += RefreshLanguage;
            gameFlow.StateChanged += HandleStateChanged;
            restartButton.onClick.AddListener(RestartGame);
            gameOverPanel.SetActive(false);
            zoneEditor = FindObjectOfType<CubeZoneEditModeController>(true);
            BuildWaveBar();
            HandleHealthChanged(core.CurrentHealth, core.MaxHealth);
        }

        private void Update()
        {
            if (spawner == null)
            {
                return;
            }

            timerText.text = $"{GameLocalization.T(timerLabel, "SURVIVAL")}  {FormatTime(spawner.ElapsedTime)}";
            enemyText.text = $"{GameLocalization.T(enemyLabel, "ENEMIES")}  {registry.Count}";
            UpdateWaveBar();
        }

        private void OnDestroy()
        {
            GameLocalization.LanguageChanged -= RefreshLanguage;
            if (core != null)
            {
                core.HealthChanged -= HandleHealthChanged;
            }

            if (gameFlow != null)
            {
                gameFlow.StateChanged -= HandleStateChanged;
            }
        }

        private void HandleHealthChanged(float current, float maximum)
        {
            healthText.text = $"{GameLocalization.T(coreLabel, "CORE")}  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
            healthFill.fillAmount = maximum > 0f ? current / maximum : 0f;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.GameOver)
            {
                gameOverPanel.SetActive(false);
                return;
            }

            finalTimeText.text = $"{GameLocalization.T(finalTimeLabel, "SURVIVED")}  {FormatTime(spawner.ElapsedTime)}";
            gameOverPanel.SetActive(true);
        }

        private void RefreshLanguage()
        {
            if (core != null) HandleHealthChanged(core.CurrentHealth, core.MaxHealth);
            if (spawner != null && timerText != null)
                timerText.text = $"{GameLocalization.T(timerLabel, "SURVIVAL")}  {FormatTime(spawner.ElapsedTime)}";
            if (registry != null && enemyText != null)
                enemyText.text = $"{GameLocalization.T(enemyLabel, "ENEMIES")}  {registry.Count}";
            if (gameOverPanel != null && gameOverPanel.activeSelf && finalTimeText != null && spawner != null)
                finalTimeText.text = $"{GameLocalization.T(finalTimeLabel, "SURVIVED")}  {FormatTime(spawner.ElapsedTime)}";
            UpdateWaveBar();
        }

        private void BuildWaveBar()
        {
            Transform controls = transform.Find("Controls");
            if (controls != null) controls.gameObject.SetActive(false);
            Font font = chineseUiFont != null ? chineseUiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            waveBorder = WaveImage("Cat Transit Beacon", transform, new Color(0.46f, 0.72f, 0.86f, 0.4f));
            waveBeacon = waveBorder.rectTransform;
            waveBeacon.anchorMin = waveBeacon.anchorMax = Vector2.zero;
            waveBeacon.pivot = Vector2.zero;
            waveBeacon.anchoredPosition = new Vector2(18f, 18f);
            waveBeacon.sizeDelta = new Vector2(320f, 82f);

            Image panel = WaveImage("Night Signal", waveBorder.transform, new Color(0.045f, 0.055f, 0.125f, 0.8f));
            Stretch(panel.rectTransform, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            Image catWindow = WaveImage("Cat Porthole", panel.transform, new Color(0.35f, 0.27f, 0.42f, 0.95f));
            Place(catWindow.rectTransform, 9f, 11f, 58f, 58f);
            catWindow.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            waveCatPortrait = WaveImage("Cat Observer", catWindow.transform, Color.white);
            waveCatPortrait.sprite = waveCatIdleSprite;
            waveCatPortrait.type = Image.Type.Simple;
            waveCatPortrait.preserveAspect = true;
            Stretch(waveCatPortrait.rectTransform, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            waveTitle = WaveText("Transit Whisper", panel.transform, font, 16, TextAnchor.MiddleLeft);
            Place(waveTitle.rectTransform, 76f, 39f, 202f, 29f);
            waveTitle.color = new Color(0.94f, 0.93f, 0.97f);
            waveTime = WaveText("Transit Signal", panel.transform, font, 13, TextAnchor.MiddleLeft);
            Place(waveTime.rectTransform, 77f, 18f, 211f, 23f);
            waveTime.color = new Color(0.63f, 0.79f, 0.88f);

            Image track = WaveImage("Quiet Orbit", panel.transform, new Color(0.18f, 0.25f, 0.36f, 0.75f));
            Place(track.rectTransform, 77f, 10f, 214f, 4f);
            waveProgress = WaveImage("Incoming Starlight", track.transform, new Color(0.45f, 0.78f, 0.9f, 0.85f));
            Stretch(waveProgress.rectTransform, Vector2.zero, Vector2.zero);
            waveMarker = WaveImage("Little Comet", track.transform, new Color(0.95f, 0.83f, 0.59f, 0.9f));
            waveFlag = waveMarker.rectTransform;
            waveFlag.anchorMin = waveFlag.anchorMax = new Vector2(0f, 0.5f);
            waveFlag.pivot = new Vector2(0.5f, 0.5f);
            waveFlag.sizeDelta = new Vector2(9f, 9f);

            waveFlagIcon = WaveImage("Visitor Silhouette", panel.transform, Color.white);
            Place(waveFlagIcon.rectTransform, 282f, 46f, 27f, 27f);
            waveFlagIcon.type = Image.Type.Simple;
            waveFlagIcon.preserveAspect = true;
            waveBorder.raycastTarget = panel.raycastTarget = catWindow.raycastTarget = waveCatPortrait.raycastTarget =
                track.raycastTarget = waveProgress.raycastTarget = waveMarker.raycastTarget = waveFlagIcon.raycastTarget = false;
            UpdateWaveBar();
        }

        private void UpdateWaveBar()
        {
            if (spawner == null || waveProgress == null) return;
            bool surge = spawner.IsSurgeActive;
            int wave = surge ? spawner.CurrentWave : spawner.CurrentWave + 1;
            float remaining = surge ? spawner.SurgeSecondsRemaining : spawner.SecondsUntilNextWave;
            float duration = surge ? 10f : 30f;
            float progress = Mathf.Clamp01(1f - remaining / duration);
            waveProgress.rectTransform.anchorMax = new Vector2(progress, 1f);
            bool alert = surge || remaining <= 5f;
            waveProgress.color = surge ? new Color(1f, 0.59f, 0.48f, 0.95f) :
                alert ? new Color(0.98f, 0.79f, 0.5f, 0.95f) : new Color(0.45f, 0.78f, 0.9f, 0.85f);
            waveBorder.color = surge ? new Color(0.94f, 0.61f, 0.55f, 0.6f) :
                new Color(0.46f, 0.72f, 0.86f, 0.4f);
            Color glimmer = waveProgress.color;
            glimmer.a *= Mathf.Lerp(0.55f, 1f, 0.5f + 0.5f * Mathf.Sin(spawner.ElapsedTime * 4f));
            waveMarker.color = glimmer;
            waveCatPortrait.sprite = alert && waveCatAlertSprite != null ? waveCatAlertSprite : waveCatIdleSprite;
            waveTitle.text = surge ? GameLocalization.T("异星群落正集体跃迁", "Alien swarm in transit") :
                alert ? GameLocalization.T("猫咪竖起了耳朵", "The cat is all ears") :
                GameLocalization.T("猫咪听见星海回声", "The cat hears the stars");
            waveTime.text = surge ? GameLocalization.T($"裂隙 {wave:00} · 余辉 {Mathf.CeilToInt(remaining)} 秒", $"RIFT {wave:00} · {Mathf.CeilToInt(remaining)}s remain") :
                GameLocalization.T($"跃迁讯号 {wave:00} · {Mathf.CeilToInt(remaining)} 秒", $"TRANSIT {wave:00} · {Mathf.CeilToInt(remaining)}s");
            waveFlag.anchorMin = waveFlag.anchorMax = new Vector2(progress, 0.5f);
            Sprite icon = wave == 2 || wave == 3 ? spawner.GetWaveIcon(wave) : null;
            waveFlagIcon.sprite = icon;
            waveFlagIcon.enabled = icon != null;
            if (waveBeacon != null && zoneEditor != null)
                waveBeacon.gameObject.SetActive(!zoneEditor.IsEditing);
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Image WaveImage(string name, Transform parent, Color color)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>(); image.color = color;
            image.sprite = RoundedWaveSprite(); image.type = Image.Type.Sliced;
            return image;
        }

        private static Sprite RoundedWaveSprite()
        {
            if (roundedWaveSprite != null) return roundedWaveSprite;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - 15.5f) - 7.5f, 0f);
                float dy = Mathf.Max(Mathf.Abs(y - 15.5f) - 7.5f, 0f);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(8.5f - Mathf.Sqrt(dx * dx + dy * dy)) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels); texture.Apply();
            roundedWaveSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(15f, 15f, 15f, 15f));
            roundedWaveSprite.hideFlags = HideFlags.DontSave;
            return roundedWaveSprite;
        }

        private static Text WaveText(string name, Transform parent, Font font, int size, TextAnchor alignment)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            Text value = item.GetComponent<Text>(); value.font = font; value.fontSize = size;
            value.alignment = alignment; value.color = Color.white; value.raycastTarget = false;
            return value;
        }

        private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = minimum; rect.offsetMax = maximum;
        }

        private static string ExtractLeadingLabel(string initialText, string fallback)
        {
            if (string.IsNullOrWhiteSpace(initialText)) return fallback;
            string value = initialText.Trim();
            int end = 0;
            while (end < value.Length && !char.IsDigit(value[end])) end++;
            string label = value.Substring(0, end).Trim().TrimEnd(':', '：', '/', '-');
            return string.IsNullOrWhiteSpace(label) ? fallback : label.Trim();
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    

private void ApplyRuntimeFonts()
        {
            if (chineseUiFont == null)
            {
                if (healthText != null && healthText.font != null)
                {
                    chineseUiFont = healthText.font;
                }
                else if (timerText != null && timerText.font != null)
                {
                    chineseUiFont = timerText.font;
                }
                else if (enemyText != null && enemyText.font != null)
                {
                    chineseUiFont = enemyText.font;
                }

#if UNITY_EDITOR
                Font editorFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(ChineseUiFontPath);
                if (editorFont != null)
                {
                    chineseUiFont = editorFont;
                }
#endif
            }

            if (chineseUiFont == null)
            {
                return;
            }

            if (healthText != null)
            {
                healthText.font = chineseUiFont;
            }

            if (timerText != null)
            {
                timerText.font = chineseUiFont;
            }

            if (enemyText != null)
            {
                enemyText.font = chineseUiFont;
            }
        }
}
}
