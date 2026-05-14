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
        public string Version => "1.1.4";
        public bool IsEnabled { get; private set; }

        private HarmonyLib.Harmony _harmony;
        private ConfigEntry<bool> _enableConfig;
        private ConfigEntry<bool> _enableRaidEndCleanup;
        private ConfigEntry<bool> _forceFullGC;
        private ConfigEntry<int> _cleanupDelaySeconds;

        private Timer _cleanupTimer;
        private volatile bool _pendingUnityCleanup;
        private long _lastMemoryMB;
        private long _freedMemoryMB;
        private bool _showFreedThisFrame;

        private ConfigEntry<bool> _showHUD;
        private const int HUD_WIDTH = 160;
        private const int HUD_HEIGHT = 50;
        private const int HUD_POS_X = 10;
        private const int HUD_POS_Y = 10;

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

            if (_showHUD?.Value == true)
            {
                _lastMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
            }
        }

        public void OnGUI()
        {
            if (_showHUD?.Value != true) return;

            var style = new UnityEngine.GUIStyle
            {
                alignment = UnityEngine.TextAnchor.MiddleLeft,
                fontSize = 14,
                normal = { textColor = UnityEngine.Color.white }
            };

            var bgTexture = new UnityEngine.Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new UnityEngine.Color(0, 0, 0, 0.7f));
            bgTexture.Apply();
            var bgStyle = new UnityEngine.GUIStyle { normal = { background = bgTexture } };

            float boxHeight = _showFreedThisFrame && _freedMemoryMB > 0 ? HUD_HEIGHT + 25 : HUD_HEIGHT;

            UnityEngine.GUI.Box(new UnityEngine.Rect(HUD_POS_X - 5, HUD_POS_Y - 5, HUD_WIDTH + 10, boxHeight + 5), UnityEngine.GUIContent.none, bgStyle);
            UnityEngine.GUI.Label(new UnityEngine.Rect(HUD_POS_X, HUD_POS_Y, HUD_WIDTH, 20), $"Memory: {_lastMemoryMB} MB", style);

            if (_showFreedThisFrame && _freedMemoryMB > 0)
            {
                style.normal.textColor = UnityEngine.Color.green;
                UnityEngine.GUI.Label(new UnityEngine.Rect(HUD_POS_X, HUD_POS_Y + 22, HUD_WIDTH, 20), $"Freed: {_freedMemoryMB} MB", style);
                _showFreedThisFrame = false;
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

            _showHUD = ModuleManager.Config.Bind(section, "ShowMemoryHUD", false,
                new ConfigDescription("Show memory HUD on screen"));
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
                _freedMemoryMB = freed / 1024 / 1024;
                _showFreedThisFrame = true;
                ModuleManager.Logger?.LogInfo($"[Memory] Managed memory after: {memoryAfter / 1024 / 1024} MB");
                ModuleManager.Logger?.LogInfo($"[Memory] Freed: {_freedMemoryMB} MB");

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