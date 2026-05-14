# Better Load

SPT-AKI 4.0.13 性能优化 Mod | v1.1.5

**功能**: 内存清理、LOD调整、内存监控HUD

---

## 安装

1. 安装 [BepInEx 5.x](https://docs.bepinex.dev/articles/guides/getting_started.html)
2. 复制 `BetterLoad.dll` → `BepInEx/plugins/`
3. 复制 `com.betterload.plugin.jsonc` → `BepInEx/plugins/zh-cn/`
4. 启动游戏，按 F12 配置

---

## 功能模块

### 1. 内存清理 (Memory)
- 战局结束时自动清理未使用资源
- 执行 GC.Collect 释放内存（Timer 线程异步执行，避免卡顿）
- Unity 资源卸载在主线程执行（通过 IUpdatableModule 机制）
- 可配置延迟和完整GC模式

### 2. LOD调整 (LOD)
- 调整 `lodBias` 控制远处物体细节切换时机
- 设置 `maximumLODLevel` 限制最高细节等级
- 通过 EventBus 事件驱动：战局开始时应用配置，战局结束时恢复原始设置

### 3. 内存监控HUD
- 游戏内实时显示内存使用量
- 战局结束时显示释放内存统计

---

## 配置 (F12)

### 内存清理
| 选项 | 默认 | 说明 |
|------|------|------|
| 启用内存清理 | ✓ | 总开关 |
| 启用战局结束清理 | ✓ | 撤离/死亡后自动清理 |
| 执行完整GC | ✗ | 可能短暂卡顿 |
| 清理延迟（秒） | 5 | 范围: 0-120 |
| 显示内存HUD | ✗ | 左上角显示内存使用 |

### LOD调整
| 选项 | 默认 | 说明 |
|------|------|------|
| LOD Bias | 2.5 | 0.1-2.0，2.5=游戏原生值 |
| 最大LOD等级 | 0 | 0=使用所有等级 |

---

## 项目结构

```
Better Load/
├── Core/
│   ├── EventBus.cs          # 事件总线
│   ├── IModule.cs           # 模块接口
│   └── ModuleManager.cs     # 模块管理器
├── Modules/
│   ├── Memory/
│   │   ├── MemoryModule.cs
│   │   └── MemoryPatcher.cs # Harmony Patch
│   └── LOD/
│       └── LODModule.cs
├── ref/                     # 游戏DLL引用
├── BetterLoad/              # 构建输出
├── BetterLoad.csproj
├── Plugin.cs
├── deploy.bat
├── README.md
└── com.betterload.plugin.jsonc
```

---

## 技术栈

- C# (.NET 4.8) + BepInEx 5.4.x + Lib.Harmony 2.3.3
- Unity 2019.4.x (SPT-AKI)

---

## 开发

### 构建
```bash
dotnet build BetterLoad.csproj -c Release
```

或直接运行 `deploy.bat` 即可完成构建 + 部署到游戏目录。

---

## 版本历史

### v1.1.5
- 添加内存监控HUD功能
- 优化内存清理日志输出

### v1.1.4
- 架构简化：移除插件框架，合并为单一Mod
- 移除粒子控制模块

### v1.1.3
- 优化字符串比较性能
- 添加内存清理延迟配置