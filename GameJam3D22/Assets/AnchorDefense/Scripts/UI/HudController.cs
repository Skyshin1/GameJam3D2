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
            gameFlow.StateChanged += HandleStateChanged;
            restartButton.onClick.AddListener(RestartGame);
            gameOverPanel.SetActive(false);
            HandleHealthChanged(core.CurrentHealth, core.MaxHealth);
        }

        private void Update()
        {
            if (spawner == null)
            {
                return;
            }

            timerText.text = $"{timerLabel}  {FormatTime(spawner.ElapsedTime)}";
            enemyText.text = $"{enemyLabel}  {registry.Count}";
        }

        private void OnDestroy()
        {
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
            healthText.text = $"{coreLabel}  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
            healthFill.fillAmount = maximum > 0f ? current / maximum : 0f;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.GameOver)
            {
                gameOverPanel.SetActive(false);
                return;
            }

            finalTimeText.text = $"{finalTimeLabel}  {FormatTime(spawner.ElapsedTime)}";
            gameOverPanel.SetActive(true);
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
