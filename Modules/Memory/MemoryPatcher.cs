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
        private static Type _cachedGameWorldType;

        public MemoryPatcher(Harmony harmony, MemoryModule module)
        {
            _harmony = harmony;
            _module = module;
        }

        public void Apply()
        {
            try
            {
                var gameWorldType = FindTypeCached("EFT.GameWorld");
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

                var gameWorldField = gameWorldType.GetField("Instance",
                    BindingFlags.Public | BindingFlags.Static);
                if (gameWorldField != null)
                {
                    ModuleManager.Logger?.LogInfo($"[Memory] Found GameWorld.Instance field");
                }
            }
            catch (Exception ex)
            {
                ModuleManager.Logger?.LogError($"[Memory] Failed to apply patch: {ex.Message}");
            }
        }

        private static Type FindTypeCached(string fullName)
        {
            if (_cachedGameWorldType != null && _cachedGameWorldType.FullName == fullName)
                return _cachedGameWorldType;

            _cachedGameWorldType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.FullName == fullName);

            return _cachedGameWorldType;
        }

        internal static void OnRaidStarted()
        {
            ModuleManager.Logger?.LogInfo("[Memory] Raid started");
            ModuleManager.EventBus.Publish(new GameEvents.RaidStartEvent
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        internal static void OnRaidEnding(object __instance)
        {
            ModuleManager.Logger?.LogInfo("[Memory] Raid ending detected");

            bool isDeath = false;
            string exitLocation = string.Empty;

            try
            {
                if (__instance != null)
                {
                    var locationSetter = __instance.GetType().GetProperty("LastLocation");
                    if (locationSetter != null)
                    {
                        var value = locationSetter.GetValue(__instance);
                        if (value != null)
                        {
                            exitLocation = value.ToString();
                        }
                    }
                }
            }
            catch
            {
            }

            ModuleManager.EventBus.Publish(new GameEvents.RaidEndEvent
            {
                IsDeath = isDeath,
                ExitLocation = exitLocation
            });

            var module = ModuleManager.GetModule<MemoryModule>();
            module?.ScheduleCleanup();
        }
    }
}
