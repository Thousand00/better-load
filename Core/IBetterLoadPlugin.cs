using System;

namespace BetterLoad
{
    public interface IBetterLoadPlugin
    {
        string Name { get; }
        string Version { get; }
        void OnLoad(BetterLoadFramework framework);
        void OnUnload();
    }

    public interface IUpdatablePlugin : IBetterLoadPlugin
    {
        void OnUpdate();
    }
}
