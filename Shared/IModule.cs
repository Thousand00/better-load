namespace BetterLoad
{
    /// <summary>
    /// 模块接口
    /// 所有功能模块必须实现此接口
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// 模块唯一名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 模块版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 模块是否已启用
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 模块加载时调用
        /// </summary>
        void OnLoad();

        /// <summary>
        /// 模块卸载时调用
        /// </summary>
        void OnUnload();
    }

    /// <summary>
    /// 可更新模块接口
    /// 实现此接口的模块每帧会被调用 OnUpdate
    /// </summary>
    public interface IUpdatableModule : IModule
    {
        /// <summary>
        /// 每帧调用（仅在主线程执行）
        /// </summary>
        void OnUpdate();
    }
}