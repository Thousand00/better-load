using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BetterLoad
{
    public static class ModuleManager
    {
        internal static ManualLogSource Logger { get; private set; }
        internal static ConfigFile Config { get; private set; }
        public static EventBus EventBus { get; private set; }

        private static readonly List<IModule> _modules = new();
        private static readonly Dictionary<string, IModule> _moduleMap = new();
        private static readonly object _lock = new();

        public static void Initialize(ManualLogSource logger, ConfigFile config)
        {
            Logger = logger;
            Config = config;
            EventBus = new EventBus();
            Logger?.LogInfo("ModuleManager initialized");
        }

        public static void Register(IModule module)
        {
            if (module == null)
            {
                Logger?.LogWarning("Attempted to register null module");
                return;
            }

            lock (_lock)
            {
                if (_moduleMap.ContainsKey(module.Name))
                {
                    Logger?.LogWarning($"Module '{module.Name}' is already registered");
                    return;
                }

                _modules.Add(module);
                _moduleMap[module.Name] = module;
            }
        }

        public static void LoadAll()
        {
            lock (_lock)
            {
                Logger?.LogInfo($"Loading {_modules.Count} modules...");
                foreach (var module in _modules)
                {
                    try
                    {
                        module.OnLoad();
                        Logger?.LogInfo($"Loaded module: {module.Name} v{module.Version}");
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError($"Failed to load module '{module.Name}': {ex.Message}");
                    }
                }
            }
        }

        public static void UnloadAll()
        {
            lock (_lock)
            {
                Logger?.LogInfo($"Unloading {_modules.Count} modules...");
                foreach (var module in _modules.AsEnumerable().Reverse())
                {
                    try
                    {
                        module.OnUnload();
                        Logger?.LogInfo($"Unloaded module: {module.Name}");
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError($"Failed to unload module '{module.Name}': {ex.Message}");
                    }
                }

                _modules.Clear();
                _moduleMap.Clear();
                EventBus?.Clear();
            }
        }

        public static T GetModule<T>() where T : class, IModule
        {
            lock (_lock)
            {
                foreach (var module in _modules)
                {
                    if (module is T t)
                    {
                        return t;
                    }
                }
            }
            return null!;
        }

        public static void UpdateAll()
        {
            lock (_lock)
            {
                foreach (var module in _modules)
                {
                    if (module is IUpdatableModule updatable && updatable.IsEnabled)
                    {
                        try
                        {
                            updatable.OnUpdate();
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError($"Update error in module '{module.Name}': {ex.Message}");
                        }
                    }
                }
            }
        }

        public static void OnGUIAll()
        {
            lock (_lock)
            {
                foreach (var module in _modules)
                {
                    if (!module.IsEnabled) continue;
                    try
                    {
                        module.OnGUI();
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError($"OnGUI error in module '{module.Name}': {ex.Message}");
                    }
                }
            }
        }

        public static IReadOnlyList<IModule> GetAllModules()
        {
            lock (_lock)
            {
                return _modules.ToList();
            }
        }
    }
}