using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BetterLoad
{
    public sealed class BetterLoadFramework
    {
        private static BetterLoadFramework _instance;
        public static BetterLoadFramework Instance => _instance;

        public ManualLogSource Logger { get; }
        public ConfigFile Config { get; }
        public EventBus EventBus { get; }

        private readonly List<IBetterLoadPlugin> _plugins = new();
        private readonly List<IUpdatablePlugin> _updatablePlugins = new();
        private readonly object _lock = new();

        internal BetterLoadFramework(ManualLogSource logger, ConfigFile config)
        {
            _instance = this;
            Logger = logger;
            Config = config;
            EventBus = new EventBus();
            Logger.LogInfo("[Framework] BetterLoadFramework initialized");
        }

        public void ScanAndLoadPlugins(Assembly mainAssembly)
        {
            var baseDir = Path.GetDirectoryName(mainAssembly.Location);
            var pluginsDir = Path.Combine(baseDir, "Plugins");
            var mainDllName = Path.GetFileName(mainAssembly.Location);

            if (!Directory.Exists(pluginsDir))
            {
                Logger.LogInfo($"[Framework] Plugins directory not found: {pluginsDir}");
                return;
            }

            var dllFiles = Directory.GetFiles(pluginsDir, "*.dll")
                .Where(f => Path.GetFileName(f) != mainDllName).ToArray();
            Logger.LogInfo($"[Framework] Found {dllFiles.Length} candidate plugin DLLs");

            foreach (var dllPath in dllFiles)
            {
                LoadPlugin(dllPath);
            }

            Logger.LogInfo($"[Framework] Loaded {_plugins.Count} plugins: {string.Join(", ", _plugins.Select(p => p.Name))}");
        }

        private void LoadPlugin(string dllPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IBetterLoadPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in pluginTypes)
                {
                    var plugin = (IBetterLoadPlugin)Activator.CreateInstance(type);
                    plugin.OnLoad(this);
                    _plugins.Add(plugin);

                    if (plugin is IUpdatablePlugin updatable)
                        _updatablePlugins.Add(updatable);

                    Logger.LogInfo($"[Framework] Loaded plugin: {plugin.Name} v{plugin.Version}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Framework] Failed to load plugin from {dllPath}: {ex.Message}");
            }
        }

        public void Update()
        {
            lock (_lock)
            {
                foreach (var plugin in _updatablePlugins)
                {
                    try { plugin.OnUpdate(); }
                    catch (Exception ex) { Logger.LogError($"[Framework] {plugin.Name} OnUpdate error: {ex.Message}"); }
                }
            }
        }

        public void UnloadPlugins()
        {
            lock (_lock)
            {
                foreach (var plugin in _plugins.AsEnumerable().Reverse())
                {
                    try
                    {
                        plugin.OnUnload();
                        Logger.LogInfo($"[Framework] Unloaded plugin: {plugin.Name}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[Framework] {plugin.Name} unload error: {ex.Message}");
                    }
                }
                _plugins.Clear();
                _updatablePlugins.Clear();
            }
        }

        public IReadOnlyList<IBetterLoadPlugin> GetPlugins() => _plugins.AsReadOnly();
    }
}
