using System;
using UnityEngine;
using BepInEx.Configuration;

namespace BetterLoad.Modules.LOD
{
    public class LODModule : IModule
    {
        public string Name => "LOD";
        public string Version => "1.0.0";
        public bool IsEnabled { get; private set; }

        private ConfigEntry<float> _lodBias;
        private ConfigEntry<int> _maxLodLevel;

        private float _originalBias;
        private int _originalMaxLod;

        public void OnLoad()
        {
            _lodBias = ModuleManager.Config.Bind("2. LOD", "LodBias", 0.5f,
                new ConfigDescription("LOD Bias (0.1-2.0, lower = switch to low detail sooner)",
                    new AcceptableValueRange<float>(0.1f, 2.0f)));

            _maxLodLevel = ModuleManager.Config.Bind("2. LOD", "MaximumLODLevel", 0,
                new ConfigDescription("Maximum LOD Level (0 = use all, higher = lower quality)",
                    new AcceptableValueRange<int>(0, 6)));

            _originalBias = QualitySettings.lodBias;
            _originalMaxLod = QualitySettings.maximumLODLevel;

            ModuleManager.EventBus.Subscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            ModuleManager.EventBus.Subscribe<GameEvents.RaidEndEvent>(OnRaidEnd);

            IsEnabled = true;
            ModuleManager.Logger?.LogInfo($"[{Name}] Module loaded");
        }

        public void OnUnload()
        {
            ModuleManager.EventBus.Unsubscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            ModuleManager.EventBus.Unsubscribe<GameEvents.RaidEndEvent>(OnRaidEnd);
            Restore();
            IsEnabled = false;
        }

        public void Apply()
        {
            try
            {
                QualitySettings.lodBias = _lodBias.Value;
                QualitySettings.maximumLODLevel = _maxLodLevel.Value;
                ModuleManager.Logger?.LogInfo($"[{Name}] Applied LOD settings: bias={_lodBias.Value}, max={_maxLodLevel.Value}");
            }
            catch (Exception ex)
            {
                ModuleManager.Logger?.LogError($"[{Name}] Failed to apply settings: {ex.Message}");
            }
        }

        public void Restore()
        {
            try
            {
                QualitySettings.lodBias = _originalBias;
                QualitySettings.maximumLODLevel = _originalMaxLod;
                ModuleManager.Logger?.LogInfo($"[{Name}] Restored original LOD settings");
            }
            catch (Exception ex)
            {
                ModuleManager.Logger?.LogError($"[{Name}] Failed to restore settings: {ex.Message}");
            }
        }

        private void OnRaidStart(GameEvents.RaidStartEvent e)
        {
            Apply();
        }

        private void OnRaidEnd(GameEvents.RaidEndEvent e)
        {
            Restore();
        }
    }
}
