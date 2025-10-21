using System.Collections.Generic;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using UnityEngine;

namespace GalacticExpansion.Core
{
    /// <summary>
    /// Main entry point responsible for orchestrating services and the tick loop.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameManager : MonoBehaviour
    {
        private const string SaveFileName = "galactic_expansion_save.json";
        private const string SaveVersion = "2";

        [Header("Data")]
        [SerializeField] private ResourceDef[] resources = null!;
        [SerializeField] private GeneratorDef[] generators = null!;
        [SerializeField] private UpgradeDef[] upgrades = null!;
        [SerializeField] private PrestigeDef[] prestiges = null!;
        [SerializeField] private EventDef[] events = null!;
        [SerializeField] private MapNodeDef[] mapNodes = null!;
        [SerializeField] private GameBalance balance = null!;

        private readonly List<IGameService> _services = new();
        private GameServiceProvider _serviceProvider = null!;
        private TimeService _timeService = null!;
        private EconomyService _economyService = null!;
        private UpgradeService _upgradeService = null!;
        private PrestigeService _prestigeService = null!;
        private EventService _eventService = null!;
        private MapService _mapService = null!;
        private SaveService _saveService = null!;
        private MetaCurrencyService _metaCurrencyService = null!;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            BootstrapServices();
        }

        private void Update()
        {
            double deltaTime = Time.deltaTime;
            foreach (IGameService service in _services)
            {
                service.Tick(deltaTime);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _saveService?.Save();
            }
        }

        private void OnApplicationQuit()
        {
            _saveService?.Save();
        }

        private void BootstrapServices()
        {
            _timeService = new TimeService(balance.MaxOfflineHours);
            _mapService = new MapService(mapNodes);
            _eventService = new EventService(events);
            _metaCurrencyService = new MetaCurrencyService();
            _economyService = new EconomyService(_eventService, _mapService, balance.OfflineEfficiency);
            _upgradeService = new UpgradeService(upgrades);
            _upgradeService.Bind(_economyService, _mapService, _metaCurrencyService);
            _economyService.BindUpgradeService(_upgradeService);
            _prestigeService = new PrestigeService(prestiges, _economyService, _mapService, _upgradeService, _metaCurrencyService, balance);
            _saveService = new SaveService(SaveFileName, SaveVersion, _timeService, _economyService);

            _economyService.Configure(resources, generators);

            _serviceProvider = new GameServiceProvider();
            _serviceProvider.Register(_timeService);
            _serviceProvider.Register(_mapService);
            _serviceProvider.Register(_eventService);
            _serviceProvider.Register(_economyService);
            _serviceProvider.Register(_upgradeService);
            _serviceProvider.Register(_prestigeService);
            _serviceProvider.Register(_metaCurrencyService);
            _serviceProvider.Register(_saveService);
            ServiceLocator.Provide(_serviceProvider);

            RegisterServices();
            InitializeSaveables();
            _saveService.Load();
            InitializeServices();
            _saveService.ApplyOfflineProgress();
        }

        private void RegisterServices()
        {
            _services.Clear();
            _services.Add(_timeService);
            _services.Add(_mapService);
            _services.Add(_eventService);
            _services.Add(_economyService);
            _services.Add(_upgradeService);
            _services.Add(_prestigeService);
            _services.Add(_metaCurrencyService);
            _services.Add(_saveService);
        }

        private void InitializeSaveables()
        {
            _saveService.RegisterSaveable(_timeService);
            _saveService.RegisterSaveable(_mapService);
            _saveService.RegisterSaveable(_eventService);
            _saveService.RegisterSaveable(_economyService);
            _saveService.RegisterSaveable(_upgradeService);
            _saveService.RegisterSaveable(_prestigeService);
            _saveService.RegisterSaveable(_metaCurrencyService);
        }

        private void InitializeServices()
        {
            foreach (IGameService service in _services)
            {
                service.Initialize();
            }
        }
    }
}
