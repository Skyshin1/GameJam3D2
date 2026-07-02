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
        private GameFlowController gameFlow;

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
            gameFlow = flow;

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

            timerText.text = $"生存时间  {FormatTime(spawner.ElapsedTime)}";
            enemyText.text = $"敌人  {registry.Count}";
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
            healthText.text = $"核心  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
            healthFill.fillAmount = maximum > 0f ? current / maximum : 0f;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.GameOver)
            {
                gameOverPanel.SetActive(false);
                return;
            }

            finalTimeText.text = $"坚持了 {FormatTime(spawner.ElapsedTime)}";
            gameOverPanel.SetActive(true);
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
    }
}
