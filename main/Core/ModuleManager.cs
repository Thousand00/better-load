using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BetterLoad
{
    /// <summary>
    /// 模块管理器 - 负责模块的注册、加载、卸载
    /// </summary>
    public static class ModuleManager
    {
        internal static ManualLogSource Logger { get; private set; }
        internal static ConfigFile Config { get; private set; }
        internal static EventBus EventBus { get; private set; }

        private static readonly List<IModule> _modules = new();
        private static readonly Dictionary<string, IModule> _moduleMap = new();
        private static readonly object _lock = new();

        /// <summary>
        /// 初始化模块管理器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="config">配置管理器</param>
        public static void Initialize(ManualLogSource logger, ConfigFile config, EventBus eventBus)
        {
            Logger = logger;
            Config = config;
            EventBus = eventBus;
            Logger?.LogInfo("ModuleManager initialized");
        }

        /// <summary>
        /// 注册模块
        /// </summary>
        /// <param name="module">模块实例</param>
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
                Logger?.LogDebug($"Registered module: {module.Name}");
            }
        }

        /// <summary>
        /// 加载所有已注册模块
        /// </summary>
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

        /// <summary>
        /// 卸载所有模块
        /// </summary>
        public static void UnloadAll()
        {
            lock (_lock)
            {
                Logger?.LogInfo($"Unloading {_modules.Count} modules...");
                foreach (var module in _modules.Reverse<IModule>())
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

        /// <summary>
        /// 获取模块
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        /// <returns>模块实例</returns>
        public static T GetModule<T>() where T : IModule
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
            return default;
        }

        /// <summary>
        /// 驱动可更新模块的 OnUpdate（每帧由主线程调用）
        /// </summary>
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

        /// <summary>
        /// 获取所有已加载模块
        /// </summary>
        /// <returns>模块列表</returns>
        public static IReadOnlyList<IModule> GetAllModules()
        {
            lock (_lock)
            {
                return _modules.ToList();
            }
        }
    }
}