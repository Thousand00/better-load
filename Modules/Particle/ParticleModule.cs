using System;
using UnityEngine;
using BepInEx.Configuration;

namespace BetterLoad.Modules.Particle
{
    public class ParticleModule : IModule
    {
        public string Name => "Particle";
        public string Version => "1.0.0";
        public bool IsEnabled { get; private set; }

        private ConfigEntry<float> _speedMultiplier;
        private ConfigEntry<int> _maxParticles;
        private ConfigEntry<bool> _pauseOnRaidEnd;

        public void OnLoad()
        {
            var section = "3. Particle";

            _speedMultiplier = ModuleManager.Config.Bind(section, "ParticleSpeedMultiplier", 0.8f,
                new ConfigDescription("Particle speed multiplier (0.1-2.0)",
                    new AcceptableValueRange<float>(0.1f, 2.0f)));

            _maxParticles = ModuleManager.Config.Bind(section, "MaxParticlesLimit", -1,
                new ConfigDescription("Max particles limit (-1 = no limit)",
                    new AcceptableValueRange<int>(-1, 10000)));

            _pauseOnRaidEnd = ModuleManager.Config.Bind(section, "PauseParticlesOnRaidEnd", true,
                new ConfigDescription("Pause non-essential particles when raid ends"));

            ModuleManager.EventBus.Subscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            ModuleManager.EventBus.Subscribe<GameEvents.RaidEndEvent>(OnRaidEnd);

            IsEnabled = true;
            ModuleManager.Logger?.LogInfo($"[{Name}] Module loaded");
        }

        public void OnUnload()
        {
            ModuleManager.EventBus.Unsubscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            ModuleManager.EventBus.Unsubscribe<GameEvents.RaidEndEvent>(OnRaidEnd);
            ResumeAll();
            IsEnabled = false;
            ModuleManager.Logger?.LogInfo($"[{Name}] Module unloaded");
        }

        public void ApplySettings()
        {
            try
            {
                var systems = Resources.FindObjectsOfTypeAll<ParticleSystem>();
                int count = 0;

                foreach (var ps in systems)
                {
                    if (ps == null) continue;
                    var main = ps.main;

                    if (Mathf.Abs(_speedMultiplier.Value - 1f) > 0.01f)
                        main.simulationSpeed = _speedMultiplier.Value;

                    if (_maxParticles.Value > 0 && main.maxParticles > _maxParticles.Value)
                        main.maxParticles = _maxParticles.Value;

                    count++;
                }

                ModuleManager.Logger?.LogInfo($"[{Name}] Adjusted {count} particle systems");
            }
            catch (Exception ex)
            {
                ModuleManager.Logger?.LogError($"[{Name}] Failed to apply settings: {ex.Message}");
            }
        }

        public void PauseNonEssential()
        {
            if (!_pauseOnRaidEnd.Value) return;

            try
            {
                var systems = Resources.FindObjectsOfTypeAll<ParticleSystem>();
                int paused = 0;

                foreach (var ps in systems)
                {
                    if (ps == null) continue;
                    var objName = ps.gameObject.name.ToLower();
                    if (objName.Contains("muzzle") || objName.Contains("flash") ||
                        objName.Contains("blood") || objName.Contains("impact"))
                        continue;

                    ps.Pause(true);
                    paused++;
                }

                ModuleManager.Logger?.LogInfo($"[{Name}] Paused {paused} non-essential particle systems");
            }
            catch (Exception ex)
            {
                ModuleManager.Logger?.LogError($"[{Name}] Failed to pause particles: {ex.Message}");
            }
        }

        public void ResumeAll()
        {
            try
            {
                var systems = Resources.FindObjectsOfTypeAll<ParticleSystem>();
                int resumed = 0;

                foreach (var ps in systems)
                {
                    if (ps != null && ps.isPaused)
                    {
                        ps.Play(true);
                        resumed++;
                    }
                }

                ModuleManager.Logger?.LogInfo($"[{Name}] Resumed {resumed} particle systems");
            }
            catch (Exception ex)
            {
                ModuleManager.Logger?.LogError($"[{Name}] Failed to resume particles: {ex.Message}");
            }
        }

        private void OnRaidStart(GameEvents.RaidStartEvent e)
        {
            ApplySettings();
        }

        private void OnRaidEnd(GameEvents.RaidEndEvent e)
        {
            PauseNonEssential();
        }
    }
}
