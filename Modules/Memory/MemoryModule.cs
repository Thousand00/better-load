using System;
using System.Threading;
using BepInEx.Configuration;

namespace BetterLoad.Modules.Memory
{
    /// <summary>
    /// 内存优化模块
    /// </summary>
    public class MemoryModule : IModule, IUpdatableModule
    {
        public string Name => "Memory";
        public string Version => "1.0.0";
        public bool IsEnabled { get; private set; }

        private HarmonyLib.Harmony _harmony;
        private ConfigEntry<bool> _enableConfig;
        private ConfigEntry<bool> _enableRaidEndCleanup;
        private ConfigEntry<bool> _forceFullGC;
        private ConfigEntry<int> _cleanupDelaySeconds;

        private Timer _cleanupTimer;
        private volatile bool _pendingUnityCleanup;

        public void OnLoad()
        {
            IsEnabled = true;
            BindConfig();
            ApplyPatch();
            ModuleManager.Logger?.LogInfo($"[{Name}] Module loaded");
        }

        public void OnUnload()
        {
            RemovePatch();
            IsEnabled = false;
            _cleanupTimer?.Dispose();
            _cleanupTimer = null;
            ModuleManager.Logger?.LogInfo($"[{Name}] Module unloaded");
        }

        public void OnUpdate()
        {
            if (_pendingUnityCleanup)
            {
                _pendingUnityCleanup = false;
                ExecuteUnityCleanup();
            }
        }

        private void BindConfig()
        {
            var section = "1. Memory";
            _enableConfig = ModuleManager.Config.Bind(section, "EnableMemoryCleanup", true,
                new ConfigDescription("Enable memory module"));

            _enableRaidEndCleanup = ModuleManager.Config.Bind(section, "EnableRaidEndCleanup", true,
                new ConfigDescription("Cleanup on raid end"));

            _forceFullGC = ModuleManager.Config.Bind(section, "ForceFullGC", false,
                new ConfigDescription("Force full GC collection (may cause brief lag)"));

            _cleanupDelaySeconds = ModuleManager.Config.Bind(section, "CleanupDelaySeconds", 5,
                new ConfigDescription("Delay before cleanup (seconds)",
                    new AcceptableValueRange<int>(0, 120)));
        }

        private void ApplyPatch()
        {
            if (!_enableConfig.Value) return;

            _harmony = new HarmonyLib.Harmony("com.betterload.memory");
            var patcher = new MemoryPatcher(_harmony, this);
            patcher.Apply();
        }

        private void RemovePatch()
        {
            _harmony?.UnpatchSelf();
        }

        public void ScheduleCleanup()
        {
            if (!_enableRaidEndCleanup.Value)
            {
                ModuleManager.Logger?.LogInfo("[Memory] Raid end cleanup disabled in config, skipping");
                return;
            }

            var delaySeconds = _cleanupDelaySeconds?.Value ?? 5;
            ModuleManager.Logger?.LogInfo($"[Memory] Scheduling cleanup in {delaySeconds} seconds...");
            _cleanupTimer?.Dispose();

            _cleanupTimer = new Timer(
                _ => ExecuteManagedCleanup(),
                null,
                delaySeconds * 1000,
                Timeout.Infinite);
        }

        private void ExecuteManagedCleanup()
        {
            try
            {
                ModuleManager.Logger?.LogInfo("[Memory] === Starting cleanup ===");

                long memoryBefore = GC.GetTotalMemory(false);
                ModuleManager.Logger?.LogInfo($"[Memory] Managed memory before: {memoryBefore / 1024 / 1024} MB");

                var forceFullGC = _forceFullGC?.Value ?? false;
                if (forceFullGC)
                {
                    ModuleManager.Logger?.LogInfo("[Memory] Performing full GC...");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                else
                {
                    ModuleManager.Logger?.LogInfo("[Memory] Performing incremental GC...");
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);
                }

                long memoryAfter = GC.GetTotalMemory(true);
                long freed = memoryBefore - memoryAfter;
                ModuleManager.Logger?.LogInfo($"[Memory] Managed memory after: {memoryAfter / 1024 / 1024} MB");
                ModuleManager.Logger?.LogInfo($"[Memory] Freed: {freed / 1024 / 1024} MB");

                _pendingUnityCleanup = true;
                ModuleManager.Logger?.LogInfo("[Memory] Unity cleanup scheduled on main thread");
            }
            catch (Exception ex)
            {
                ModuleManager.Logger?.LogError($"[Memory] Cleanup failed: {ex.Message}");
            }
        }

        private void ExecuteUnityCleanup()
        {
            try
            {
                ModuleManager.Logger?.LogInfo("[Memory] Unloading unused Unity resources...");
                UnityEngine.Resources.UnloadUnusedAssets();
                ModuleManager.Logger?.LogInfo("[Memory] === Cleanup completed ===");
            }
            catch (Exception ex)
            {
                ModuleManager.Logger?.LogWarning($"[Memory] Unity resource unload failed: {ex.Message}");
            }
        }
    }
}