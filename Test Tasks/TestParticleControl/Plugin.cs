using System;
using BetterLoad;
using UnityEngine;

namespace ParticleControl
{
    public class Plugin : IBetterLoadPlugin
    {
        public string Name => "TestParticleControl";
        public string Version => "1.0.0";

        private bool _enableControl = true;
        private float _speedMultiplier = 0.8f;
        private int _maxParticlesLimit = -1;
        private bool _pauseOnRaidEnd = true;

        private long _lastRaidStartTime;
        private long _lastRaidEndTime;
        private const long DebounceMs = 5000;

        public void OnLoad(BetterLoadFramework framework)
        {
            framework.EventBus.Subscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            framework.EventBus.Subscribe<GameEvents.RaidEndEvent>(OnRaidEnd);
            framework.Logger.LogInfo("[TestParticleControl] Subscribed to raid events");
        }

        public void OnUnload()
        {
            BetterLoadFramework.Instance?.EventBus.Unsubscribe<GameEvents.RaidStartEvent>(OnRaidStart);
            BetterLoadFramework.Instance?.EventBus.Unsubscribe<GameEvents.RaidEndEvent>(OnRaidEnd);
            ResumeAllParticles(BetterLoadFramework.Instance);
        }

        private void OnRaidStart(GameEvents.RaidStartEvent e)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - _lastRaidStartTime < DebounceMs) return;
            _lastRaidStartTime = now;

            if (!_enableControl) return;

            ApplyParticleSettings(BetterLoadFramework.Instance);
        }

        private void OnRaidEnd(GameEvents.RaidEndEvent e)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - _lastRaidEndTime < DebounceMs) return;
            _lastRaidEndTime = now;

            if (!_enableControl || !_pauseOnRaidEnd) return;

            PauseNonEssentialParticles(BetterLoadFramework.Instance);
        }

        private void ApplyParticleSettings(BetterLoadFramework framework)
        {
            try
            {
                var systems = Resources.FindObjectsOfTypeAll<ParticleSystem>();
                int count = 0;

                foreach (var ps in systems)
                {
                    if (ps == null) continue;
                    var main = ps.main;

                    if (Mathf.Abs(_speedMultiplier - 1f) > 0.01f)
                        main.simulationSpeed = _speedMultiplier;

                    if (_maxParticlesLimit > 0 && main.maxParticles > _maxParticlesLimit)
                        main.maxParticles = _maxParticlesLimit;

                    count++;
                }

                framework.Logger.LogInfo($"[TestParticleControl] Adjusted {count} particle systems");
            }
            catch (Exception ex)
            {
                framework.Logger.LogError($"[TestParticleControl] Failed: {ex.Message}");
            }
        }

        private void PauseNonEssentialParticles(BetterLoadFramework framework)
        {
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

                framework.Logger.LogInfo($"[TestParticleControl] Paused {paused} non-essential particles");
            }
            catch (Exception ex)
            {
                framework.Logger.LogError($"[TestParticleControl] Pause failed: {ex.Message}");
            }
        }

        private void ResumeAllParticles(BetterLoadFramework framework)
        {
            if (framework == null) return;
            try
            {
                var systems = Resources.FindObjectsOfTypeAll<ParticleSystem>();
                int resumed = 0;

                foreach (var ps in systems)
                {
                    if (ps == null) continue;
                    if (ps.isPaused)
                    {
                        ps.Play(true);
                        resumed++;
                    }
                }

                framework.Logger.LogInfo($"[TestParticleControl] Resumed {resumed} particles");
            }
            catch (Exception ex)
            {
                framework.Logger.LogError($"[TestParticleControl] Resume failed: {ex.Message}");
            }
        }
    }
}
