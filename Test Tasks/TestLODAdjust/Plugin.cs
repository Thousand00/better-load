using System;
using BetterLoad;
using UnityEngine;

namespace LODAdjust
{
    public class Plugin : IBetterLoadPlugin
    {
        public string Name => "TestLODAdjust";
        public string Version => "1.0.0";

        private float _lodBias = 0.5f;
        private int _maximumLodLevel = 0;
        private bool _enableOnRaidStart = true;
        private bool _enableOnRaidEnd = true;

        private float _originalLodBias;
        private int _originalMaxLod;
        private long _lastRaidStartTime;
        private long _lastRaidEndTime;
        private const long DebounceMs = 5000;

        public void OnLoad(BetterLoadFramework framework)
        {
            _originalLodBias = QualitySettings.lodBias;
            _originalMaxLod = QualitySettings.maximumLODLevel;

            framework.EventBus.Subscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            framework.EventBus.Subscribe<GameEvents.RaidEndEvent>(OnRaidEnd);
            framework.Logger.LogInfo("[TestLODAdjust] Subscribed to raid events");
            framework.Logger.LogInfo($"[TestLODAdjust] Current: Bias={_originalLodBias}, MaxLOD={_originalMaxLod}");
        }

        public void OnUnload()
        {
            BetterLoadFramework.Instance?.EventBus.Unsubscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            BetterLoadFramework.Instance?.EventBus.Unsubscribe<GameEvents.RaidEndEvent>(OnRaidEnd);
            if (BetterLoadFramework.Instance != null)
                RestoreOriginalSettings(BetterLoadFramework.Instance);
        }

        private void OnRaidStart(GameEvents.RaidStartEvent e)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - _lastRaidStartTime < DebounceMs) return;
            _lastRaidStartTime = now;

            if (!_enableOnRaidStart) return;

            var logger = BetterLoadFramework.Instance.Logger;
            try
            {
                logger.LogInfo($"[TestLODAdjust] Applying LOD: Bias={_lodBias}, MaxLOD={_maximumLodLevel}");
                QualitySettings.lodBias = _lodBias;
                QualitySettings.maximumLODLevel = _maximumLodLevel;
                logger.LogInfo("[TestLODAdjust] LOD settings applied");
            }
            catch (Exception ex)
            {
                logger.LogError($"[TestLODAdjust] Failed to apply LOD: {ex.Message}");
            }
        }

        private void OnRaidEnd(GameEvents.RaidEndEvent e)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - _lastRaidEndTime < DebounceMs) return;
            _lastRaidEndTime = now;

            if (!_enableOnRaidEnd) return;

            RestoreOriginalSettings(BetterLoadFramework.Instance);
        }

        private void RestoreOriginalSettings(BetterLoadFramework framework)
        {
            try
            {
                framework.Logger.LogInfo($"[TestLODAdjust] Restoring: Bias={_originalLodBias}, MaxLOD={_originalMaxLod}");
                QualitySettings.lodBias = _originalLodBias;
                QualitySettings.maximumLODLevel = _originalMaxLod;
                framework.Logger.LogInfo("[TestLODAdjust] Original LOD restored");
            }
            catch (Exception ex)
            {
                framework.Logger.LogError($"[TestLODAdjust] Failed to restore LOD: {ex.Message}");
            }
        }
    }
}
