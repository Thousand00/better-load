using System;
using BepInEx;
using UnityEngine;

namespace BetterLoad
{
    [BepInPlugin("com.betterload.plugin", "Better Load", Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Version = "1.1.2";
        private BetterLoadFramework _framework;

        private void Awake()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.logMessageReceived += OnLogMessageReceived;

            _framework = new BetterLoadFramework(Logger, Config);

            ModuleManager.Initialize(Logger, Config, _framework.EventBus);

            ModuleManager.Register(new Modules.Memory.MemoryModule());
            ModuleManager.Register(new Modules.LOD.LODModule());
            ModuleManager.Register(new Modules.Particle.ParticleModule());

            ModuleManager.LoadAll();

            _framework.ScanAndLoadPlugins(typeof(Plugin).Assembly);

            Logger.LogInfo($"Better Load v{Version} loaded - {ModuleManager.GetAllModules().Count} modules, {_framework.GetPlugins().Count} plugins");
        }

        private void OnDestroy()
        {
            _framework?.UnloadPlugins();
            ModuleManager.UnloadAll();
        }

        private void Update()
        {
            ModuleManager.UpdateAll();
            _framework?.Update();
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ModuleManager.Logger?.LogError($"[BetterLoad] Unhandled exception: {e.ExceptionObject}");
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
                ModuleManager.Logger?.LogError($"[BetterLoad] Unity exception: {condition}\n{stackTrace}");
        }
    }
}
