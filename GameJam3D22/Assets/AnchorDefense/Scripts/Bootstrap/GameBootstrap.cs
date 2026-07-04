using UnityEngine;

namespace AnchorDefense
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Config Assets")]
        [SerializeField] private CoreConfig coreConfig;
        [SerializeField] private EnemyConfig enemyConfig;
        [SerializeField] private TurretConfig turretConfig;
        [SerializeField] private EndlessModeConfig endlessModeConfig;
        [SerializeField] private UpgradeTreeConfig upgradeTreeConfig;

        [Header("Scene References")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private CoreHealth core;
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private EndlessEnemySpawner spawner;
        [SerializeField] private RingInputController ringInput;
        [SerializeField] private HudController hud;
        [SerializeField] private UpgradeTreeController upgradeTree;
        [SerializeField] private GameInputController inputController;
        [SerializeField] private Transform poolRoot;

        private bool initialized;

        public KillResourceWallet KillWallet { get; private set; }
        public TurretRuntimeStats TurretStats { get; private set; }
        public TurretRegistry TurretRegistry { get; private set; }
        public ProjectileService ProjectileService { get; private set; }
        public UpgradeSystem UpgradeSystem { get; private set; }

        public void Configure(
            CoreConfig newCoreConfig,
            EnemyConfig newEnemyConfig,
            TurretConfig newTurretConfig,
            EndlessModeConfig newEndlessModeConfig,
            Camera newGameplayCamera,
            CoreHealth newCore,
            GameFlowController newGameFlow,
            EndlessEnemySpawner newSpawner,
            RingInputController newRingInput,
            HudController newHud,
            Transform newPoolRoot)
        {
            coreConfig = newCoreConfig;
            enemyConfig = newEnemyConfig;
            turretConfig = newTurretConfig;
            endlessModeConfig = newEndlessModeConfig;
            gameplayCamera = newGameplayCamera;
            core = newCore;
            gameFlow = newGameFlow;
            spawner = newSpawner;
            ringInput = newRingInput;
            hud = newHud;
            poolRoot = newPoolRoot;
        }

        public void ConfigureUpgradeSystem(UpgradeTreeConfig treeConfig, UpgradeTreeController treeView)
        {
            upgradeTreeConfig = treeConfig;
            upgradeTree = treeView;
        }

        public void ConfigureInput(GameInputController gameplayInput)
        {
            inputController = gameplayInput;
        }

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            Time.timeScale = 1f;
            Application.targetFrameRate = 120;

            core.Initialize(coreConfig);
            EnemyRegistry registry = new EnemyRegistry();
            KillWallet = new KillResourceWallet();
            TurretStats = new TurretRuntimeStats(turretConfig);
            OrbitRingController[] rings = FindObjectsOfType<OrbitRingController>(true);
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i].InitializeTurretSlots();
            }
            UpgradeSystem = new UpgradeSystem(
                upgradeTreeConfig,
                KillWallet,
                new UpgradeContext(TurretStats, rings));
            ProjectileService = new ProjectileService(
                turretConfig,
                poolRoot,
                endlessModeConfig.ProjectilePrewarmCount);
            TurretHitVfxService turretHitVfx = new TurretHitVfxService(turretConfig, poolRoot);
            TurretRegistry = new TurretRegistry();

            TurretController[] turrets = FindObjectsOfType<TurretController>(true);
            for (int i = 0; i < turrets.Length; i++)
            {
                turrets[i].Initialize(TurretStats, registry, ProjectileService, turretHitVfx);
                TurretRegistry.Register(turrets[i].Health);
            }

            ringInput.Initialize(gameplayCamera, core.transform, inputController);
            spawner.Initialize(
                endlessModeConfig,
                enemyConfig,
                registry,
                TurretRegistry,
                core,
                gameFlow,
                KillWallet,
                poolRoot);
            hud.Initialize(core, spawner, registry, gameFlow);
            upgradeTree.Initialize(UpgradeSystem, gameFlow, inputController);
            CubeZoneGridController zoneGrid = FindObjectOfType<CubeZoneGridController>(true);
            zoneGrid?.Initialize(registry, TurretRegistry, core.transform);
            CubeZoneAssignmentController zoneAssignment = FindObjectOfType<CubeZoneAssignmentController>(true);
            zoneAssignment?.Initialize(zoneGrid, UpgradeSystem);

            core.Died += gameFlow.EndGame;
            gameFlow.BeginGame();
            initialized = true;
        }

        private void OnDestroy()
        {
            if (initialized && core != null && gameFlow != null)
            {
                core.Died -= gameFlow.EndGame;
            }
            UpgradeSystem?.Dispose();
        }

        private bool ValidateReferences()
        {
            bool sceneReferencesValid = gameplayCamera != null && core != null && gameFlow != null &&
                                        spawner != null && ringInput != null && hud != null && upgradeTree != null &&
                                        inputController != null &&
                                        poolRoot != null;
            bool configReferencesValid = coreConfig != null && enemyConfig != null &&
                                         turretConfig != null && endlessModeConfig != null && upgradeTreeConfig != null;
            bool prefabReferencesValid = configReferencesValid && enemyConfig.Prefab != null &&
                                         enemyConfig.HitEffectPrefab != null && enemyConfig.DeathEffectPrefab != null &&
                                         turretConfig.ProjectilePrefab != null;

            if (!sceneReferencesValid || !configReferencesValid || !prefabReferencesValid)
            {
                Debug.LogError("GameBootstrap is missing scene, config, or prefab references.", this);
                return false;
            }

            return true;
        }
    }
}
