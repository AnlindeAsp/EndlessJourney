# Current Milestone: M1 Player Core and Combat Core Stabilization

更新时间：2026-06-28  
当前里程碑：M1 - In Progress  
推荐原因：M0 已经通过用户 Playtest；当前应继续稳定 Player Core、战斗链路、资源/装备后端和后续 TestingGround Player Systems Sandbox 的接入边界。

## 当前 M1 目标

让玩家移动、攻击、施法、受击、资源变化和基础敌人交互链路保持可扩展，并先把 M2 所需的装备/资源后端准备好。

当前已完成的 M1/M2 前置事项：

- `PlayerCore2D` action lock 已从单 bool 改为 owner-based lock，避免 UI/Spell 等系统互相误解锁。
- Armor 后端已完成：`ArmorData / ArmorLibrary2D / ArmorEquipped2D / PlayerArmorEquipmentSystem2D`。
- `PlayerRecordStore2D` 已新增 `unlockedArmorIds` 和 `equippedArmorId`。
- `OpenForge2D` 已有可选打开时修理护甲入口，但需要手动绑定 `PlayerArmorEquipmentSystem2D`。

当前下一步候选：

- 在 Unity 中创建 ArmorData assets，并手动绑定 ArmorLibrary / ArmorEquipped / PlayerArmorEquipmentSystem。
- 为 Forge 设计 Armor 页面或临时调试入口。
- Playtest Armor 装备、record 读写、Forge 修理、普通存档点不修理的边界。
- 继续梳理玩家动作优先级、取消规则和 Buff / Status Effect 最小入口。

## M0 基线记录

确保当前原型可以可靠运行、复制到新场景、继续开发，并且核心功能有最小可重复验证方法。

本里程碑不是为了扩展新玩法，而是为了给后续 `TestingGround Player Systems Sandbox` 打地基。

理由：

- Player 相关系统会集中在 TestingGround 中继续扩展。
- 如果 Prefab、Scene instance、Library、Layer、JSON 和测试流程不稳定，后续护符、护甲、存档点等系统会不断返工。
- 当前阶段应优先把“已经存在的功能”确认可复现，而不是继续堆新系统。

## 范围

- TestingGround 与 Player Prefab 一致性。
- Build Settings 与主测试场景。
- Weapon / Spell / Inscription Library 注册完整性。
- Enemy target layer、contact damage、active melee 规则。
- Enemy active melee 接入 FSM。
- 当前 JSON 字段与持久化范围。
- 基础 smoke test。
- 建立稳定基线 tag 建议。

## 不在本里程碑范围

- 招架、拼刀、远程敌人、Boss 完整战斗。
- 背包、地图、护符、NPC、任务。
- 完整存档点与复活闭环。
- 正式美术、正式音效、正式 UI 风格。
- 复杂 Buff / Status Effect 框架。

## 近期执行顺序

### Step 1：确认 TestingGround 基线

目标：

- 明确 `TestingGround.unity` 是当前唯一主要测试场。
- 确认 Build Settings 是否加入 TestingGround，或明确暂不加入。
- 确认 Scene Player 与 `Assets/prefab/player 1.prefab` 的差异。

理由：后续所有系统都会先接入 TestingGround；如果测试场和 Player 基线不稳定，任何 Playtest 结果都不可靠。

对应任务：

- M0-01
- M0-02

### Step 2：确认 Library 和数据基线

目标：

- 确认当前 Weapon / Inscription Library 中不接入 `Only.asset` / `Will.asset`。
- 验证 `record.json` / `PlayerData.json` 的当前字段能读写。

理由：武器、法术、铭文、能力和后续护符都会依赖数据记录；现在要先确认当前数据边界，而不是引入拓展特殊组合。

对应任务：

- M0-03
- M0-04
- M0-09

### Step 3：确认最小敌人与伤害测试条件

目标：

- 接上 `EnemyMeleeAttack2D -> EnemyBrainFSM2D`。
- 确认 enemy active melee target layer 只打玩家身体。
- Playtest active melee 与 contact damage 共存时，玩家受伤无敌能避免失控连伤。
- 确认 `EnemySpawner2D` 当前 prefab 选择符合测试目标。

理由：M1/M2 主要仍是 Player 系统，但需要稳定 enemy / dummy 作为受击、受伤、法术和资源测试对象。

对应任务：

- M0-05
- M0-06
- M0-07
- M0-08

### Step 4：执行一次完整 M0 smoke test

目标：

- 按 `TESTING_GUIDE.md` 验证移动、攻击、法术、Storage、Forge、敌人受击、敌人追击、玩家受击、JSON 读写。
- 记录 Console 是否有阻断性错误。
- 根据结果更新 `FEATURE_TRACKER.md` 和 `RISK_REGISTER.md`。

理由：M0 完成标准不是“看起来没问题”，而是有一次可重复的手动验证记录。

对应任务：

- M0-10

## M0 完成后的下一步

M0 通过后，近期开发进入：

1. M1：Player Core 与战斗核心稳定。
2. M2：TestingGround Player Systems Sandbox。

优先候选：

- 动作锁定 owner / token 或 counter，解决 `PlayerCore` bool lock 风险。（已完成代码，待 Playtest）
- 玩家动作优先级和取消规则。
- Buff / Status Effect 最小入口。
- 护甲装备化的轻量结构：`ArmorData / ArmorLibrary / ArmorEquipped / Armor UI`。（后端已完成；UI、资产和 Unity 绑定待做）
- 护符最小系统和 Storage 护符页面。
- Storage / Forge 与未来存档快照模型的接口边界。

这些不是 M0 任务；只有在 M0 基线稳定后再进入。

## M0 Playtest 结果

状态：User Playtest Passed

2026-06-28 用户确认：

- 当前 M0 相关操作逻辑符合预期。
- 当前 M0 相关操作都能够使用。
- 暂无阻断性问题反馈。

说明：

- 该结论表示当前 `TestingGround` 基线可以作为后续 TestingGround Player Systems Sandbox 的开发起点。
- 这不代表后续 M1/M2 的架构整理已经完成。
- Player Prefab 复现、新场景复用、自动 PlayMode 测试等长期工程风险仍按后续里程碑处理。

## 当前任务

任务数量：10

| Task ID | Feature ID | 任务 | 当前状态 | 验证方式 | 完成标准 |
| --- | --- | --- | --- | --- | --- |
| M0-01 | PLAYER-MOVE-001, COMBAT-RUNTIME-001, PLAYER-INVINC-001 | 决定并处理 `TestingGround` Player 与 `Assets/prefab/player 1.prefab` 的组件差异 | Passed by user playtest | Prefab 对比 + Playtest | 当前 TestingGround Player 操作可用；Prefab 复现风险后续继续跟踪 |
| M0-02 | TEST-BASELINE-001 | 将当前主测试场景纳入 Build Settings 或记录暂不纳入原因 | Passed by user playtest | Unity Build Settings 检查 | 当前 TestingGround 可作为 editor 测试基线；Build Settings 仍可后续单独整理 |
| M0-03 | WEAPON-EQUIP-001 | 确认 `Only.asset` 当前未进入 `WeaponLibrary2D` | Passed by user playtest | Library Inspector 检查 | `Only.asset` 作为拓展特殊组合暂不出现在当前游戏流程 |
| M0-04 | WEAPON-INSCRIPTION-001 | 确认 `Will.asset` 当前未进入 `WeaponInscriptionLibrary2D` | Passed by user playtest | Library Inspector 检查 | `Will.asset` 作为 Only/Will 特殊绑定组合暂不出现在当前游戏流程 |
| M0-05 | ENEMY-ATTACK-001 | 把 `EnemyMeleeAttack2D` 接入 `EnemyBrainFSM2D -> Melee Attack Module` | Passed by user playtest | TestingGround Inspector + Playtest | 当前 M0 enemy attack 操作可用 |
| M0-06 | ENEMY-ATTACK-001, PLAYER-INVINC-001 | 把 `EnemyMeleeAttack2D.Target Layers` 限定为 `PlayerSide` | Passed by user playtest | Inspector + Scene Playtest | 当前 active hitbox 行为可用，未反馈误伤阻断问题 |
| M0-07 | ENEMY-ATTACK-001 | 验证主动近战与 contact damage 共存时受伤无敌能避免重复伤害 | Passed by user playtest | Playtest 对比 | Contact damage 可常驻；当前无失控连伤反馈 |
| M0-08 | ENEMY-SPAWN-001, ENEMY-AI-001 | 确认 `EnemySpawner2D` 当前选择的 prefab 符合测试目标 | Passed by user playtest | Prefab 检查 + Playtest | 当前 spawner 选择符合 M0 测试目标 |
| M0-09 | SAVE-DATA-001, SPELL-BOOK-001, WEAPON-INSCRIPTION-001 | 验证 record.json / PlayerData.json 的读写字段 | Passed by user playtest | 删除/生成/修改/重进 Play | 当前武器、法术页、能力、铭文状态读写可用 |
| M0-10 | TEST-BASELINE-001 | 执行并记录基础 smoke test | Passed by user playtest | `TESTING_GUIDE.md` | 移动、攻击、法术、Storage、Forge、敌人受击、敌人追击、玩家受击、JSON 读写均可用 |

## 阻塞项

| ID | 阻塞内容 | 影响 |
| --- | --- | --- |
| BLOCK-M0-01 | 当前 TestingGround 已通过用户 Playtest；Prefab 复现和新场景复制仍是后续工程风险 | M1/M2 |
| BLOCK-M0-02 | 主动近战和接触伤害共存已通过当前用户 Playtest；后续正式敌人仍需按敌人类型继续验证 | M4 |
| BLOCK-M0-03 | Only/Will 当前不进入游戏流程，当前 M0 行为可用；后续扩展特殊组合时再接入 | M1/M2 |

## 里程碑完成标准

M0 完成时应满足：

- Player Prefab 与当前测试 Player 的差异已处理或明确记录。
- 主测试场景配置明确。
- Weapon / Spell / Inscription Library 注册情况明确。
- Enemy active melee 能通过 FSM 进入攻击流程。
- Enemy target layer 不误伤非玩家身体触发区。
- contact damage 与 active melee 的规则明确。
- 当前 JSON 字段和持久化范围记录清楚。
- `TESTING_GUIDE.md` 的 M0 smoke test 已执行，并没有阻断性 Console 错误。
- `FEATURE_TRACKER.md` 和 `RISK_REGISTER.md` 已根据结果更新。
- 可以创建一个稳定基线 git tag，例如 `baseline-m0-ready`。

当前结果：

- 2026-06-28 用户 Playtest 已确认 M0 操作符合逻辑并可用。
- M0 可视为当前 TestingGround 基线已通过。

## 验证步骤摘要

详细步骤见 `../Reference/TESTING_GUIDE.md`。

1. 打开 Unity 6000.3.12f1。
2. 打开 `Assets/Scenes/TestingGround.unity`。
3. 清 Console。
4. 进入 Play。
5. 执行移动、攻击、法术、Storage、Forge、敌人索敌、敌人攻击、受击、JSON 读写测试。
6. 停止 Play 后检查 Console 和 JSON 状态。
7. 更新 Feature Tracker 与 Risk Register。

## 里程碑交付结果

- 可复现的 TestingGround 基线。
- 同步或明确例外的 Player Prefab。
- 清晰的 enemy active melee 接线和伤害规则。
- 已知 Library 注册缺口被处理或记录。
- 一份可重复执行的手动 smoke test。
- 一个可用于继续 M1 的稳定基础。
