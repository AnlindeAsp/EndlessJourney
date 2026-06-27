# Endless Journey Decision Log

更新时间：2026-06-28  
用途：记录已经确认的架构、设计、文档和旧逻辑处理决策。新的优化设计确认后应追加在这里。

## 记录格式

| ID | 日期 | 决策 | 原因 | 影响 | 状态 |
| --- | --- | --- | --- | --- | --- |

## Decisions

| ID | 日期 | 决策 | 原因 | 影响 | 状态 |
| --- | --- | --- | --- | --- | --- |
| DEC-001 | 2026-06-27 | 建立 `Docs/Planning`, `Docs/Reference`, `Docs/History` 三层文档结构 | 旧 README/CHECKLIST/FUNCTION/problem/class map 职责重叠，长期维护容易冲突 | 当前状态以 `FEATURE_TRACKER.md` 为准；README 只做入口 | Approved |
| DEC-002 | 2026-06-27 | `FEATURE_TRACKER.md` 是唯一功能状态来源 | 简单完成/未完成无法表达代码、接线、集成、验证之间的差异 | 其它文档不再维护另一套功能完成状态 | Approved |
| DEC-003 | 2026-06-27 | 当前里程碑选 M0：项目基线稳定 | 静态检查发现 Prefab/Scene 漂移、active melee 接线缺失、Library 漏注册、Build Settings 指向 SampleScene | 短期任务集中在接线、配置、验证，不扩展新大系统 | Approved |
| DEC-004 | 2026-06-27 | 旧版设计大纲和远期设想不是当前实现规范 | 旧设计可能和新的优化逻辑冲突，不能直接变成任务 | 旧内容先分类为 Adopt/Optimize/Later 等，再进入计划 | Approved |
| DEC-005 | 2026-06-27 | 不在本任务中修改实际玩法代码和 Unity 场景文件 | 本次目标是规划和进度系统，不是实现游戏逻辑 | 只创建/整理 Markdown；`.unity`、Prefab、asset、C# 不写入 | Approved |
| DEC-006 | 2026-05-13 | 玩家最终近战状态由 `PlayerCombatRuntime2D` 聚合 | 武器、铭文、生命、法力等状态分散，命中时需要最终快照 | `PlayerMeleeAttack2D` 从 runtime 获取最终 `HitContext` 数据 | Approved |
| DEC-007 | 2026-05-21 | 武器铭文改为每把武器独立保存 | 全局一个铭文方便但不符合武器独立成长的设计 | record.json 使用 `weaponId -> inscriptionId` | Approved |
| DEC-008 | 2026-05-21 | 敌人死亡不默认直接 Destroy，而是由 `EnemyDeathBehaviour2D` 关闭生前组件 | 需要支持死亡动画、Boss 死亡、掉落、生成器关闭等后续扩展 | 死亡后必须确认行为脚本、碰撞和生成器是否关闭 | Approved |
| DEC-009 | 2026-05-21 | 玩家受击无敌期间临时忽略 player/enemy collision | 防止玩家被敌人夹住，受击状态更可控 | 依赖 Layer 配置和 Playtest 验证恢复 | Approved |
| DEC-010 | 2026-06-28 | Active melee enemy 与 contact damage 可以共存 | Contact damage 理论上一直需要存在；玩家有足够受伤无敌时间，能避免非预期重复伤害 | M0 只需 Playtest 验证 active/contact 共存时不会失控连伤 | Approved |
| DEC-011 | 2026-06-28 | M0 通过后进入 M1，同时为 M2 TestingGround Player Systems Sandbox 做后端准备 | 用户确认 M0 操作符合逻辑且可用；下一阶段需要稳定 Player Core、战斗链路和装备/资源后端 | `CURRENT_MILESTONE.md` 当前里程碑改为 M1；M2 相关后端可以先行准备但必须标记集成状态 | Approved |
| DEC-012 | 2026-06-28 | Armor 装备化先完成后端，再接 Forge UI | 当前时间有限，且 UI 需要 Unity 布局；后端可先仿 Weapon/Library/Inscription 模式完成 | 新增 `ArmorData / ArmorLibrary2D / ArmorEquipped2D / PlayerArmorEquipmentSystem2D`；Forge Armor 页面和 Unity 绑定后续 M2 处理 | Approved |

## 旧设计分类

仓库中未发现独立的旧版 Endless Journey 大纲文件。当前可见的远期/旧设想主要来自 `future.md` 和历史讨论沉淀。初始分类如下：

| 旧设想 / 系统 | 分类 | 当前处理 |
| --- | --- | --- |
| 横版移动、跳跃、冲刺、二段跳 | Existing Prototype | 已有原型，M0/M1 稳定动作优先级 |
| 武器装备、武器数据驱动 | Existing Prototype | 已实现，M0 验证 Library 和 Prefab |
| 法术书 1-5 页 | Existing Prototype | 已实现，M0 验证 JSON 与 UI |
| 每把武器独立铭文 | Existing Prototype | 已实现，M0 验证 Library 和即时生效 |
| 护符系统 | Later | 仅 `CharmSlotNum` 占位，M2 做 TestingGround Player Systems Sandbox 时再评审最小系统 |
| 背包系统 | Later | M4 探索区域切片前确认最小范围 |
| 地图 UI | Later | M4 做基础地图 |
| 直觉系统 | Later | M5 Vertical Slice 阶段再设计 |
| 时间分流 / Branching Save / 三时代后果 | Later | 不做复杂时间分流或选择管理；M3 只做存档与死亡回滚 |
| 敌人主动攻击、招架、拼刀 | Optimize | active melee 原型已写，parry/clash 仍需 M1/M2 设计 |
| 完整 Boss、阶段、奖励 | Later | M2 做测试 Boss，不代表正式 Boss 系统 |
| 正式 NPC、任务、剧情 | Later | M5 或更后 |

## 需要设计确认

- Player Prefab 是否应立即同步 TestingGround 的最新组件，还是暂时以场景实例为准。
- Buff / Status Effect 是独立运行时系统，还是先挂在 Spell/Inscription 的接收者接口上。
