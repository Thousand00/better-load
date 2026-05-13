using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace BetterLoad.Modules.Memory
{
    internal class MemoryPatcher
    {
        private readonly Harmony _harmony;
        private readonly MemoryModule _module;

        public MemoryPatcher(Harmony harmony, MemoryModule module)
        {
            _harmony = harmony;
            _module = module;
        }

        public void Apply()
        {
            try
            {
                var gameWorldType = FindType("EFT.GameWorld");
                if (gameWorldType == null)
                {
                    ModuleManager.Logger?.LogWarning("[Memory] Could not find GameWorld type");
                    return;
                }

                var onGameStarted = gameWorldType.GetMethod("OnGameStarted",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (onGameStarted != null)
                {
                    _harmony.Patch(onGameStarted,
                        postfix: new HarmonyMethod(typeof(MemoryPatcher), nameof(OnRaidStarted)));
                    ModuleManager.Logger?.LogInfo($"[Memory] Patched GameWorld.OnGameStarted");
                }
                else
                {
                    ModuleManager.Logger?.LogWarning("[Memory] Could not find OnGameStarted");
                }

                var dispose = gameWorldType.GetMethod("Dispose",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (dispose != null)
                {
                    _harmony.Patch(dispose,
                        prefix: new HarmonyMethod(typeof(MemoryPatcher), nameof(OnRaidEnding)));
                    ModuleManager.Logger?.LogInfo($"[Memory] Patched GameWorld.Dispose");
                }
                else
                {
                    ModuleManager.Logger?.LogWarning("[Memory] Could not find Dispose");
                }
            }
            catch (Exception ex)
            {
                ModuleManager.Logger?.LogError($"[Memory] Failed to apply patch: {ex.Message}");
            }
        }

        private static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.FullName == fullName);
        }

        internal static void OnRaidStarted()
        {
            ModuleManager.Logger?.LogInfo("[Memory] Raid started");
            ModuleManager.EventBus.Publish(new GameEvents.RaidStartEvent
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        internal static void OnRaidEnding()
        {
            ModuleManager.Logger?.LogInfo("[Memory] Raid ending detected");
            ModuleManager.EventBus.Publish(new GameEvents.RaidEndEvent());
            var module = ModuleManager.GetModule<MemoryModule>();
            module?.ScheduleCleanup();
        }
    }
}
