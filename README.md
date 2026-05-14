# Better Load

SPT-AKI 4.0.13 性能优化 Mod | v1.1.3

**功能**: 内存清理、LOD调整、粒子效果控制、EventBus 事件驱动

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

### 3. 粒子控制 (Particle)
- 调整粒子系统模拟速度
- 限制最大粒子数量
- 通过 EventBus 事件驱动：战局开始时调整粒子参数，战局结束时暂停非必要粒子（保留枪口火焰、血液等战斗关键效果）

---

## 配置 (F12)

### 内存清理
| 选项 | 默认 | 说明 |
|------|------|------|
| 启用内存清理 | ✓ | 总开关 |
| 启用战局结束清理 | ✓ | 撤离/死亡后自动清理 |
| 执行完整GC | ✗ | 可能短暂卡顿 |
| 清理延迟（秒） | 5 | 范围: 0-120 |

### LOD调整
| 选项 | 默认 | 说明 |
|------|------|------|
| LOD Bias | 0.5 | 0.1-2.0，低于1更早切换低精度 |
| 最大LOD等级 | 0 | 0=使用所有等级 |

### 粒子控制
| 选项 | 默认 | 说明 |
|------|------|------|
| 速度倍率 | 0.8 | 0.1-2.0 |
| 最大粒子数 | -1 | -1=无限制 |
| 战局结束时暂停 | ✓ | |

---

## 项目结构

```
Better Load/                          # 项目根目录
├── Core/                              # 核心组件
│   ├── BetterLoadFramework.cs        # 插件框架（扫描/加载/卸载插件）
│   ├── EventBus.cs                   # 事件总线（模块/插件间解耦通信）
│   ├── IBetterLoadPlugin.cs          # 插件接口
│   └── ModuleManager.cs              # 模块管理器（含 IUpdatableModule 支持）
├── Modules/                           # 功能模块
│   ├── Memory/                       # 内存清理模块
│   │   ├── MemoryModule.cs
│   │   └── MemoryPatcher.cs          # Harmony Patch（GameWorld.OnGameStarted/Dispose）
│   ├── LOD/                          # LOD调整模块
│   │   └── LODModule.cs
│   └── Particle/                     # 粒子控制模块
│       └── ParticleModule.cs
├── Shared/                            # 共享接口
│   └── IModule.cs                    # IModule + IUpdatableModule 接口
├── ref/                               # 游戏DLL引用（需从游戏目录复制）
├── BetterLoad/                        # 构建输出目录（构建时自动生成）
├── BetterLoad.csproj                  # 项目文件
├── Plugin.cs                          # BepInEx 入口
├── deploy.bat                         # 构建+部署脚本
└── README.md
```

---

## 技术栈

- C# (.NET 4.8) + BepInEx 5.4.x + Lib.Harmony 2.3.3
- Unity 2019.4.x (SPT-AKI)

---

## 开发

### 构建
```bash
dotnet build "Better Load\BetterLoad.csproj" -c Release
```

或直接运行 `deploy.bat` 即可完成构建 + 部署到游戏目录。

---

## 代码规范

### 命名约定

| 类型 | 规范 | 示例 |
|------|------|------|
| 类/接口/枚举/方法 | PascalCase | `MemoryCleanup`, `IPlugin` |
| 私有字段 | `_camelCase` + `_` 前缀 | `_logger`, `_config` |
| 静态字段 | `s_` 前缀 | `s_instance` |
| 局部变量/参数 | camelCase | `delaySeconds`, `forceGC` |
| 常量 | PascalCase | `MaxRetryCount` |
| 接口 | `I` 前缀 | `IMemoryManager` |

**禁止**:
- 使用缩写（如 `obj`, `cnt`）
- 使用下划线分隔（如 `product_category`）
- 枚举添加 `Enum` 后缀

### 格式标准
- 命名空间独立行
- 类之间空一行
- 方法之间空一行
- 使用花括号风格（K&R）

### 注释规范
- 公共 API 添加 XML 文档注释
- 复杂逻辑添加行内说明
- 禁止无意义注释（如 `// comment`）
- 更新日志在文件顶部

---

## 未来开发计划

### 愿景

> 构建 SPT-AKI 生态最专业的内存优化框架

**目标指标**:

| 指标 | 当前 | 目标 |
|------|------|------|
| Hook 成功率 | 100% | 95%+ |
| 代码覆盖率 | 40% | 80%+ |
| 性能开销 | <5% | <2% |
| 内存释放效率 | ~30% | >50% |

### 架构演进

```
当前 (v1.1.3)                 目标 (v2.0)
┌─────────────────────┐       ┌─────────────────────┐
│ Plugin.cs            │       │ Plugin.cs (DI)      │
│   ├─ EventBus        │       │   ├─ Event Bus     │
│   ├─ ModuleManager   │──────▶│   └─ [模块系统]    │
│   └─ [事件驱动模块]   │       │         │          │
└─────────────────────┘       │         ▼          │
                              │     Core           │
                              │  GC/Profiler/API   │
                              └─────────────────────┘
```

### 模块 Roadmap

| 模块 | 优先级 | 状态 | 功能 |
|------|--------|------|------|
| **Core** | P0 | ✅ | 生命周期管理、事件总线 |
| **Memory** | P0 | ✅ | GC调度、内存监控、战局结束清理 |
| **HookEngine** | P0 | ⬚ | Harmony Patch 管理、方法定位缓存 |
| **ConfigManager** | P1 | ⬚ | 配置持久化、校验、迁移 |
| **Profiler** | P1 | ⬚ | 性能数据采集、统计、导出 |
| **APIGateway** | P2 | ⬚ | 对外 API、Mod 集成 |

### 版本路线图

| 版本 | 状态 | 目标 |
|------|------|------|
| **v1.1.0** | ✅ | 激活 EventBus，Memory/LOD/Particle 全模块事件驱动通信，Patch `GameWorld.OnGameStarted` 和 `GameWorld.Dispose` |
| **v1.1.1** | ✅ | 关键 Bug 修复：游戏版本 0.16.9 的正确 Patch 方法确认；EventBus 事件链路正常工作 |
| **v1.1.2** | ✅ | 代码质量改进：RaidEndEvent 反射获取 LastLocation；Timer dispose 修复；GetModule nullable；反射缓存 |
| **v1.1.3** | ✅ | 性能优化：移除 ToLower() 字符串分配；EventBus GC 优化；TryGetValue 替代 ContainsKey |
| **v1.2.0** | ⬚ | Hook 引擎抽象，统一反射查找与 fallback 策略，内存监控加强 |
| **v1.3.0** | ⬚ | 实时内存 HUD、智能 GC 调度、Hook 精确率提升 + 版本兼容 |
| **v2.0.0** | ⬚ | 正式发布 + API 开放 |

### 技术研究方向

- **GC 策略优化**: 分代回收、自适应调度、与 Unity 协作式清理，避免激烈交火时触发 GC 导致卡顿
- **Hook 精确度**: 方法签名验证、版本检测框架、符号数据分析，按游戏版本自动选择 Hook 目标
- **性能分析**: 实时内存监控、后端采样、Jenkins 日志 + HTML 报告
- **扩展 API**: 允许第三方 Mod 注册自定义清理回调、查询内存状态、共享 GC 调度策略

### 风险与缓冲

| 风险 | 影响 | 缓解 |
|------|------|------|
| SPT 版本更新 Hook 失效 | 高 | 版本检测 + 降级 |
| 内存清理触发游戏 bug | 高 | 灰度发布 + 反馈 |
| 其他 Mod 冲突 | 中 | API 隔离 + 冲突检测 |

---

## 版本历史

| 版本 | 日期 | 更新 |
|------|------|------|
| v1.1.3 | 2026-05-14 | **性能优化**：ParticleModule 移除 ToLower() 避免字符串分配，使用 OrdinalIgnoreCase 比较；EventBus 移除 ToList() 避免 GC 分配，使用 TryGetValue 优化查找；缓存配置值减少属性访问 |
| v1.1.2 | 2026-05-14 | **代码质量改进**：RaidEndEvent 尝试通过反射获取 LastLocation；Timer dispose 修复防止资源泄漏；GetModule 返回类型改为 nullable；反射类型查找添加缓存提升性能 |
| v1.1.0 | 2026-05-11 | 激活 EventBus 事件总线，Memory/LOD/Particle 三大模块全部接入事件驱动；MemoryPatcher 同时 Patch StartGame 和 ExitLocation/StopGame；LOD 模块战局开始应用配置、结束恢复原始设置；粒子模块战局开始调整参数、结束暂停非必要粒子；补齐 LOD/Particle 共 5 项配置汉化；提取 FindGameMethod 公共反射方法 |
| v1.0.2 | 2026-05-10 | 集成 LOD/粒子模块到主项目；修复 Harmony Patch 目标定位、BindingFlags、Unity 主线程调用问题；新增防抖机制、单例模式、全局异常处理、IUpdatableModule 接口；统一命名空间和配置映射 |
| v1.0.1 | 2026-05-09 | 稳定性优化、Hook 精确率提升 |
| v1.0.0 | 2026-05-08 | 初始版本 |

---

**MIT License** | [GitHub](https://github.com/Thousand00/better-load)
