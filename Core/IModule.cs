namespace BetterLoad
{
    public interface IModule
    {
        string Name { get; }
        string Version { get; }
        bool IsEnabled { get; }
        void OnLoad();
        void OnUnload();
        void OnGUI();
    }

    public interface IUpdatableModule : IModule
    {
        void OnUpdate();
    }
}