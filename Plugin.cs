using System;
using BepInEx;
using UnityEngine;

namespace BetterLoad
{
    [BepInPlugin("com.betterload.plugin", "Better Load", Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Version = "1.1.5";

        private void Awake()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.logMessageReceived += OnLogMessageReceived;

            ModuleManager.Initialize(Logger, Config);

            ModuleManager.Register(new Modules.Memory.MemoryModule());
            ModuleManager.Register(new Modules.LOD.LODModule());

            ModuleManager.LoadAll();

            Logger.LogInfo($"Better Load v{Version} loaded - {ModuleManager.GetAllModules().Count} modules");
        }

        private void OnDestroy()
        {
            ModuleManager.UnloadAll();
        }

        private void Update()
        {
            ModuleManager.UpdateAll();
        }

        private void OnGUI()
        {
            ModuleManager.OnGUIAll();
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