# Endless Journey Game Plan

更新时间：2026-06-28  
权威性：本文件负责长期开发路线、系统边界、依赖关系和完成标准。日常状态不在这里维护，当前状态以 `FEATURE_TRACKER.md` 为准。

## 文档工作流

旧设计大纲 / 讨论素材  
-> 评审和优化  
-> `GAME_PLAN.md` 确认系统方向  
-> `FEATURE_TRACKER.md` 记录功能状态  
-> `CURRENT_MILESTONE.md` 拆成当前可执行任务  
-> GitHub Issues / 实际开发  
-> 代码、Prefab、Scene、ScriptableObject、测试  
-> 更新 Feature 状态  
-> 里程碑完成后更新 `FUNCTION.md` 和 `DEVELOPMENT_LOG.md`

## 当前项目定位

`EndlessJourney` 当前是 Unity 2D 横版动作原型。当前阶段优先稳定以下核心：

- 玩家移动、冲刺、二段跳、受击反馈。
- 玩家近战、武器、法术、盔甲、法力、铭文。
- 敌人索敌、追击、接触伤害、主动攻击、受击、死亡。
- Storage / Forge / Pause / Keybind 的基础 UI 状态。
- JSON 保存装备、能力、法术书、武器/护甲解锁、铭文状态。

正式剧情、完整地图、最终 UI 包装、正式美术音频、复杂多周目和完整 NPC 系统不属于当前基线目标。

## 状态原则

一个系统不能只因为“代码存在”就被视为完成。必须区分：

- 设计是否已确认。
- 主体代码是否存在。
- Unity Inspector / Prefab / Scene / Layer / Library 是否接线。
- 是否进入实际游戏流程。
- 是否经过指定测试场景验证。
- 是否仍需要重构或优化。

状态枚举的唯一维护位置是 `FEATURE_TRACKER.md`。

## 通用 Definition of Done

默认情况下，Feature 只有满足以下要求后才能标记为 `Development Status = Verified`：

1. 优化后的设计规则已经确认。
2. 主体代码完成。
3. 已接入正确 Scene 或 Prefab。
4. Inspector 引用完整。
5. Layer、Tag 和 Collision Matrix 正确。
6. 必要的 Library 或 ScriptableObject 资产已经注册。
7. 在指定测试场景中完成验证。
8. 没有阻断性的 Console 错误。
9. 与依赖系统交互正常。
10. 存档与读取行为已验证，如果该功能涉及持久化。
11. 关键边界情况至少验证一次。
12. `FEATURE_TRACKER.md` 状态已经更新。
13. `TESTING_GUIDE.md` 包含对应验证方法。

正式美术、最终动画、最终音效和数值平衡不一定是 `Verified` 的必要条件。它们通过 `Maturity` 区分，例如 `Prototype`、`Stable`、`Polished`。

## 系统划分

### Player Core

目标：玩家基础可控、可扩展、可被战斗系统安全打断。

包含：

- 输入读取与键位保存。
- 横向移动、跳跃、冲刺、二段跳。
- 能力解锁。
- 生命、法力、盔甲。
- 受击无敌、受击击退、碰撞忽略。

核心依赖：

- `PlayerInput2D`
- `PlayerCore2D`
- `PlayerAbilityCore2D`
- `PlayerHealth2D`
- `PlayerMana2D`
- `PlayerArmor2D`
- `PlayerArmorEquipmentSystem2D`

### Combat Core

目标：玩家攻击、敌人受击、敌人攻击玩家都通过清晰的数据链路表达。

包含：

- `HitContext` / `HitResult`
- `IHittable` / `IPlayerHarmful`
- 玩家近战方向、active window、recoil。
- 玩家最终近战快照。
- 敌人受击、硬直、击退、死亡。
- 未来 Buff / Status Effect 入口。

### Weapon

目标：武器通过数据资产驱动玩家战斗数值，UI 只修改装备状态。

包含：

- `WeaponData`
- `WeaponLibrary2D`
- `WeaponEquipped2D`
- `PlayerWeaponSystem`
- Dual Wielding effective type。
- Storage Weapon Page。

### Spell

目标：法术通过数据资产和法术书驱动施法，支持写入/擦除槽位。

包含：

- `SpellData2D`
- `SpellEffectData2D`
- `SpellLibrary2D`
- `SpellBook2D`
- `SpellCastSystem`
- `PlayerProjectile2D`
- Storage Spell Page。

### Inscription / Forge

目标：每把武器独立拥有一个铭文槽，铭文在 Forge 中操作，并实际影响近战数值或命中后效果。

包含：

- `WeaponInscriptionData`
- `WeaponInscriptionLibrary2D`
- `WeaponInscriptionEquipped2D`
- Forge Inscription UI。
- 静态效果：重量、锋利度。
- 动态效果：连击增伤、失血增伤、命中回蓝。

### Armor

目标：装甲保持少量、简单、可切换，并只通过耐久和减伤比例影响玩家受 harm 后的结算。

包含：

- `ArmorData`
- `ArmorLibrary2D`
- `ArmorEquipped2D`
- `PlayerArmorEquipmentSystem2D`
- `PlayerArmor2D`
- Forge 修理入口。
- 后续 Forge Armor UI。

### Enemy

目标：敌人能被玩家攻击，也能通过明确规则主动威胁玩家。

包含：

- 敌人受击与死亡。
- 巡逻、索敌、追击、返回。
- 接触伤害。
- 主动近战攻击。
- 生成器。
- 后续远程敌人、Boss、精英变体。

### UI / Data

目标：UI 管理玩家状态选择，Data 保存长期进度和装备状态。

包含：

- `GameCanvasManager2D`
- Pause / Settings / Keybind。
- Storage Weapon / Spell。
- Forge Inscription。
- `record.json`
- `PlayerData.json`

## 里程碑路线

当前路线原则：

先把 `TestingGround` 做成单场景浓缩功能场，在一个 Scene 中验证完整 Player 相关系统、基础 UI、普通存档点和铁匠铺流程。  
理由：玩家系统、装备系统、资源计算、UI 数据流和存档回滚是后续地图与敌人内容的地基；先在单场景跑通，可以减少正式地图阶段的返工。

### M0：项目基线稳定

目标：确保当前原型能够可靠运行、复制和继续开发。

理由：先确保 TestingGround、Prefab、Inspector、Layer、Library、JSON 和 smoke test 可复现，否则后续任何系统集成都不稳定。

范围：

- TestingGround 玩家实例与 Player Prefab 一致性。
- Build Settings 与主测试场景。
- Weapon / Spell / Inscription Library 注册完整性。
- Enemy Layer、Target Layer、Collision Matrix。
- 主动近战敌人接入 FSM。
- 主动攻击和 Contact Damage 规则确认。
- 当前 JSON 字段盘点。
- 基础 Smoke Test。
- 建议建立稳定基线 tag。

### M1：Player Core 与战斗核心稳定

目标：让玩家移动、攻击、施法、受击、资源变化和基础敌人受击链路稳定可扩展。

理由：这一阶段重点是玩家控制和战斗数据链路，不追求完整地图或敌人生态；敌人主要作为受击、伤害和反馈测试对象。

范围：

- 玩家动作优先级。
- 移动、攻击、施法、冲刺、受击之间的锁定关系。
- 动作取消规则。
- 近战前摇、active、后摇。
- 敌人受击、硬直、死亡链路。
- 敌人攻击阶段和打断规则的最小验证。
- 统一 Buff / Status Effect 入口。
- 持续伤害和效果类生命变化。
- 基础战斗 HUD。
- 伤害和状态调试信息。
- 最低限度 PlayMode 测试。

### M2：TestingGround Player Systems Sandbox

目标：在 TestingGround 中完成玩家相关系统的浓缩闭环。

理由：先把武器、法术、铭文、护符、护甲、生命、法力和各种机制计算集中验证清楚，再进入地图和怪物开发。

建议包含：

- 武器装备、双持、武器数据和 UI。
- 法术书、法术释放、伤害法术、治疗法术和基础 buff 入口。
- 铭文篆刻、擦除、即时生效和特殊绑定规则预留。
- 护符数据、装备规则、阳刻/阴刻容量和最小 UI。
- 护甲数据、装备切换、耐久、减伤和铁匠铺修理入口。
  - 当前后端已完成：`ArmorData / ArmorLibrary2D / ArmorEquipped2D / PlayerArmorEquipmentSystem2D`。
  - 下一步是 ArmorData asset、Inspector 绑定、Forge Armor UI 和 Playtest。
- 生命、法力、隐藏法力、ManaOut、自然回血成长等资源计算。
- 最小能力拾取和能力解锁验证。
- 简单敌人或 dummy 只作为玩家系统测试对象。

### M3：TestingGround Save / Storage / Forge 闭环

目标：在 TestingGround 中完成普通存档点、Storage、Forge、死亡回滚和 Boss 后弱存档点的最小完整流程。

理由：存档点和铁匠铺是装备、资源和世界状态提交的核心入口；在进入正式地图前必须先验证数据提交、回滚和 UI 状态切换。

范围：

- 普通存档点。
- Storage 基础装备页面。
- Forge / 铁匠铺页面。
- 铁匠铺作为高级存档点，修复护甲并管理铭文等高级装备功能。
- 玩家位置、生命、法力、盔甲耐久。
- 普通存档恢复生命和法力。
- 死亡回滚到上一次存档快照。
- Boss 后一次性弱存档点。
- 长期进度保留。
- 死亡后复活。
- 存档版本号与基础迁移。

不进入：

- 复杂时间分流。
- Branching Save / Timeline Merge。
- 三周目复杂继承。

### M4：地图与怪物切片

目标：在玩家系统和存档闭环稳定后，再把它们放入一个小型可探索区域，并开始系统化制作敌人和地图。

理由：地图、房间、敌人生态、Boss 和探索节奏应建立在已经稳定的 Player/Storage/Forge/Save 基础上，避免内容制作过程中反复改底层。

建议包含：

- 起点。
- 普通房间。
- 能力门。
- 隐藏区域。
- 存档点。
- 铁匠铺。
- 能力拾取。
- 三种敌人。
- Boss。
- 区域结束点。
- 摄像机边界。
- 房间状态。
- 基础地图。
- 最小物品和背包。

### M5：首个 Vertical Slice

目标：完成一段能够代表最终游戏玩法和表现方向的短流程。

理由：在核心系统、存档闭环、地图和怪物切片都跑通后，再投入正式表现层、文本、音效和可展示流程，产出才不容易被底层返工拖垮。

此阶段集中加入：

- 正式动画。
- 视觉效果。
- 音效和音乐。
- UI 风格。
- NPC 和文本。
- 直觉系统第一版。
- 一段可展示的完整短流程。

## 旧设计处理规则

旧版 Endless Journey 大纲或远期讨论只能作为设计素材，不是当前实现规范。每个旧设想进入开发前需要归类：

| 分类 | 含义 | 处理方式 |
| --- | --- | --- |
| Adopt | 可以直接采用 | 加入 GAME_PLAN 或 FEATURE_TRACKER |
| Optimize | 保留概念，但需要重新设计 | 进入 DECISION_LOG，确认后再开发 |
| Existing Prototype | 已有原型实现 | 在 FEATURE_TRACKER 中标明实际状态 |
| Refactor | 已有实现，但要按新结构重做 | 进入对应里程碑 |
| Unconfirmed | 尚未确认 | 不进入当前任务 |
| Later | 以后考虑 | 保留到远期路线 |
| Cut | 确定不做 | 记录原因 |
| Deprecated | 已被新逻辑替代 | 记录替代方案 |

## 当前最高优先级

当前推荐里程碑是 M1：Player Core 与战斗核心稳定，同时为 M2 的 TestingGround Player Systems Sandbox 做后端准备。  
原因：M0 已经通过用户 Playtest；下一步应继续稳定 action lock、资源/装备后端、战斗状态入口和后续 UI/Inspector 集成边界。
