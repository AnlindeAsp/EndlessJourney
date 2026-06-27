# EndlessJourney

Unity 2D 横版动作原型项目。当前阶段优先稳定 player / enemy / combat / data flow，而不是正式剧情、完整地图或最终 UI 包装。

## Unity Version

- Unity `6000.3.12f1`
- Unity 2D
- URP
- TextMesh Pro
- Input System with legacy fallback in `PlayerInput2D`

## Main Test Scene

当前主要测试场景：

- `Assets/Scenes/TestingGround.unity`

其他场景：

- `Assets/Scenes/SampleScene.unity`
- `Assets/Scenes/trial.unity`

注意：静态检查显示 `ProjectSettings/EditorBuildSettings.asset` 当前仍只启用 `SampleScene.unity`。准备 gameplay build 前需要在 Unity Build Settings 中确认是否加入/启用 `TestingGround.unity`。

## How To Run

1. 用 Unity `6000.3.12f1` 打开项目。
2. 打开 `Assets/Scenes/TestingGround.unity`。
3. 等待 Unity 编译完成。
4. 清空 Console。
5. 进入 Play Mode。
6. 按 `Docs/Reference/TESTING_GUIDE.md` 执行 smoke test。

## Current Focus

当前推荐里程碑：

- `M1: Player Core and Combat Core Stabilization`

当前状态：

- M0 已通过用户 Playtest，可作为当前 TestingGround 基线。
- 当前进入 M1，重点稳定 Player Core、战斗链路、资源/装备后端和后续 UI/Inspector 集成边界。
- Armor 后端已加入，Forge Armor UI 和 Unity 绑定仍待做。

详情见：

- [Current Milestone](Docs/Planning/CURRENT_MILESTONE.md)
- [Feature Tracker](Docs/Planning/FEATURE_TRACKER.md)
- [Risk Register](Docs/Planning/RISK_REGISTER.md)

## Input Defaults

- Move: `A / D`
- Jump: `Space / W / Up`
- Dash: `LeftShift / RightShift`
- Melee: `F`
- Cast: `C`
- Interact / close Storage or Forge: `R`
- Close current UI / Pause from gameplay: `ESC`
- Spell slots: `1-5`

Keybinds are managed by `PlayerInput2D` and `KeybindSettingsController2D`.

## Main Directories

- `Assets/Scripts/Player`：玩家移动、能力、生命/法力/盔甲、近战、武器、法术、投射物、存档读取和交互入口。
- `Assets/Scripts/Enemy`：敌人 core、受击、死亡、索敌、FSM、接触伤害、主动近战和生成器。
- `Assets/Scripts/Combat`：通用 hit context、hit result、damage type 和受击基类。
- `Assets/Scripts/Scriptables`：武器、护甲、铭文、法术和法术效果的数据资产。
- `Assets/Scripts/UI`：HUD、Storage、Forge 和相关页面 controller/displayer。
- `Assets/Scripts/System`：Canvas 状态、Pause、Keybind。
- `Assets/Scripts/Interaction`：世界交互区域、存档点、铁匠铺和阅读交互。
- `Assets/prefab`：Player、Enemy、UI、Projectile、Weapon/Spell/Inscription assets。
- `Docs`：长期计划、状态追踪、参考和历史记录。

## Documentation

### Planning

- [Game Plan](Docs/Planning/GAME_PLAN.md)：长期开发路线、系统划分、依赖关系和 Definition of Done。
- [Feature Tracker](Docs/Planning/FEATURE_TRACKER.md)：所有功能当前状态的唯一权威来源。
- [Current Milestone](Docs/Planning/CURRENT_MILESTONE.md)：当前短期任务。
- [Decision Log](Docs/Planning/DECISION_LOG.md)：架构、设计和旧逻辑处理决策。
- [Risk Register](Docs/Planning/RISK_REGISTER.md)：技术风险、配置风险和阻塞项。

### Reference

- [Function Reference](Docs/Reference/FUNCTION.md)：当前构建能够测试的功能。
- [Class Map](Docs/Reference/CLASS_MAP.md)：模块、类职责和主要调用关系。
- [Testing Guide](Docs/Reference/TESTING_GUIDE.md)：核心功能手动验证步骤。

### History

- [Development Log](Docs/History/DEVELOPMENT_LOG.md)：历史开发记录，不作为当前状态来源。

## Current Stable State

当前项目不是正式稳定版，而是 TestingGround 原型扩展阶段。

静态检查确认：

- Unity 版本为 `6000.3.12f1`。
- 主要测试内容集中在 `TestingGround.unity`。
- 当前没有发现 C# 测试文件。
- Armor equipment 后端代码存在，但 ArmorData asset、Inspector 绑定和 Forge Armor UI 需要 Unity 验证。
- `Only.asset` 和 `Will.asset` 存在但未被 TestingGround 对应 Library 引用，需要设计/Unity 确认。
- Player Prefab 可能落后于 TestingGround 场景 Player，后续仍需作为工程复现风险处理。

进入正式地图和怪物内容前，先在 TestingGround 完成 Player/Storage/Forge/Save 相关系统闭环。
