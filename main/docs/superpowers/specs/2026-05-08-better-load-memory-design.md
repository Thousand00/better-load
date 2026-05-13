# Better Load - 内存稳定性优化 Mod 设计文档

## 版本: 1.0.0
## 日期: 2026-05-08
## 目标: SPT 4.0.13

---

## 1. 功能概述

在战局结束时执行内存清理，优化长时间运行的内存稳定性。

---

## 2. 技术方案

### 2.1 实现方式
- **框架**: BepInEx 5.x + HarmonyX
- **语言**: C# (.NET Standard 2.0 / Unity Mono)
- **平台**: 客户端 Mod (Client-side)

### 2.2 核心功能
1. **战局结束检测** - 拦截 `GameWorld.OnGameSessionEnd` 或等效事件
2. **托管内存清理** - 调用 `GC.Collect()` + `GC.SuppressFinalize()`
3. **Unity资源卸载** - 调用 `Resources.UnloadUnusedAssets()`

### 2.3 Hook 目标 (待探索)
需要通过反编译确定 SPT 4.0.13 的战局结束回调方法。

---

## 3. 配置项

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| 启用内存清理 | bool | true | 总开关 |
| 启用战局结束清理 | bool | true | 战局结束时触发 |
| 执行完整GC | bool | false | 是否执行完整GC（可能导致卡顿） |
| 清理延迟(秒) | int | 5 | 战局结束后的延迟清理时间 |

---

## 4. 架构设计

```
BetterLoad/
├── BetterLoad.csproj
├── Plugin.cs              # BepInEx 插件入口
├── BetterLoad.cs          # 主类
├── MemoryCleanup.cs       # 内存清理核心逻辑
├── Patches/
│   └── RaidEndPatch.cs   # 战局结束Patch
├── Config/
│   └── Config.cs          # 配置管理
└── README.md              # 说明文档
```

---

## 5. 验收标准

- [ ] Mod 成功加载，无报错
- [ ] 战局结束时触发内存清理
- [ ] 内存占用在长时间游戏后保持稳定
- [ ] 配置文件可正常读写