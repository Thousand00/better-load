using System;
using BetterLoad;
using UnityEngine;

namespace SceneUnload
{
    public class Plugin : IBetterLoadPlugin
    {
        public string Name => "TestSceneUnload";
        public string Version => "1.0.0";

        private bool _enableCleanup = true;

        public void OnLoad(BetterLoadFramework framework)
        {
            framework.EventBus.Subscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            framework.EventBus.Subscribe<GameEvents.RaidEndEvent>(OnRaidEnd);
            framework.Logger.LogInfo("[TestSceneUnload] Subscribed to raid events");
        }

        public void OnUnload()
        {
            BetterLoadFramework.Instance?.EventBus.Unsubscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            BetterLoadFramework.Instance?.EventBus.Unsubscribe<GameEvents.RaidEndEvent>(OnRaidEnd);
        }

        private void OnRaidStart(GameEvents.RaidStartEvent e)
        {
            BetterLoadFramework.Instance.Logger.LogInfo("[TestSceneUnload] Raid started");
        }

        private void OnRaidEnd(GameEvents.RaidEndEvent e)
        {
            try
            {
                if (!_enableCleanup) return;

                long memBefore = GC.GetTotalMemory(false);

                Resources.UnloadUnusedAssets();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long memAfter = GC.GetTotalMemory(true);
                long freed = memBefore - memAfter;

                BetterLoadFramework.Instance.Logger.LogInfo(
                    $"[TestSceneUnload] Memory freed: {freed / 1024 / 1024} MB " +
                    $"({memBefore / 1024 / 1024} MB -> {memAfter / 1024 / 1024} MB)");
            }
            catch (Exception ex)
            {
                BetterLoadFramework.Instance.Logger.LogError($"[TestSceneUnload] Cleanup failed: {ex.Message}");
            }
        }
    }
}
