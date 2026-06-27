# Endless Journey Feature Tracker

更新时间：2026-06-28  
权威性：这是所有功能当前状态的唯一权威来源。README、FUNCTION、CHECKLIST、future 或历史日志都不应维护另一套当前状态。

## 状态枚举

### Design Status

- `Draft`：设计仍在讨论。
- `Review Needed`：旧设计需要重新评审。
- `Approved`：优化后的设计已经确认。
- `Deprecated`：旧设计已经废弃。
- `Future`：已知需求，但暂不进入当前开发。

### Development Status

- `Not Started`：尚未开始。
- `In Progress`：正在实现。
- `Code Complete`：主体代码已经存在。
- `Integration Pending`：等待 Prefab、Scene、Inspector、Layer、Library 等接线。
- `Integrated`：已经接入实际游戏流程。
- `Verified`：已经按完成标准测试通过。
- `Blocked`：被其他依赖或问题阻塞。

### Maturity

- `Prototype`：原型可用。
- `Refactor Needed`：可运行，但架构或规则需要优化。
- `Stable`：结构稳定，可以继续扩展。
- `Polished`：表现、反馈和边界情况基本完善。

## Feature Table

| Feature ID | 功能名称 | 玩家或开发目标 | 当前相关代码 / Prefab / Scene / 数据 | Design | Development | Maturity | 依赖 | 当前问题 | 下一步任务 | 目标里程碑 | Definition of Done |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PLAYER-MOVE-001 | 玩家基础移动 | 玩家可稳定移动、跳跃、冲刺、二段跳 | `PlayerInput2D`, `PlayerCore2D`, `PlayerMovement2D`, `PlayerDash2D`, `PlayerDoubleJump2D`, `GroundCheck2D`, `Assets/prefab/player 1.prefab`, `TestingGround.unity` | Approved | Integrated | Prototype | PLAYER-ABILITY-001 | 还没有统一动作状态机；Prefab 与 Scene 玩家实例存在漂移 | M0 先同步 Player Prefab；M1 再梳理动作优先级 | M0/M1 | Generic DoD + TestingGround 移动 smoke test + Prefab 一致 |
| PLAYER-ABILITY-001 | 玩家能力解锁 | Dash/DoubleJump/SpellCast/DualWielding 由进度控制 | `PlayerAbilityCore2D`, ability pickup scripts, `PlayerData.json` | Approved | Integrated | Prototype | SAVE-DATA-001 | 能力获得 UI/演出未做；pickup 场景使用需确认 | M0 确认当前 PlayerData 字段；M2 做能力拾取切片 | M0/M2 | Generic DoD + 能力 JSON 保存读取验证 |
| PLAYER-RESOURCE-001 | 生命、法力、盔甲 | 玩家有生命、法力、盔甲、受击和资源反馈 | `PlayerHealth2D`, `PlayerMana2D`, `PlayerArmor2D`, `ArmorData`, `ArmorLibrary2D`, `ArmorEquipped2D`, `PlayerArmorEquipmentSystem2D`, `HealthDisplayer`, `ManaDisplay`, `ArmorDisplayer` | Approved | Integrated | Prototype | COMBAT-DAMAGE-001, ARMOR-EQUIP-001 | 死亡后复活未完成；HUD 仍是原型；Armor 装备后端已存在但 Unity 绑定和 UI 未验证 | M2 接 Armor asset / Library / Equipped / Forge UI；M3 接复活和存档快照 | M0/M2/M3 | Generic DoD + harm/direct loss/armor/mana smoke test |
| ARMOR-EQUIP-001 | 护甲装备和铁匠铺修理 | 玩家可获得、解锁、装备少量护甲，并在铁匠铺修理 | `ArmorData`, `ArmorLibrary2D`, `ArmorEquipped2D`, `PlayerArmorEquipmentSystem2D`, `PlayerArmor2D`, `OpenForge2D`, `record.json` | Approved | Integration Pending | Prototype | PLAYER-RESOURCE-001, UI-FORGE-001, SAVE-DATA-001 | 后端代码已完成；ArmorData asset、ArmorLibrary 注册、Player 手动绑定、Forge Armor UI 尚未完成；`OpenForge2D` 修理需要手动绑定 `PlayerArmorEquipmentSystem2D` | 在 TestingGround 创建 ArmorData，接入 Library/Equipped/EquipmentSystem，验证 record 和 Forge 修理；之后设计 Forge Armor 页面 | M2/M3 | 专属 DoD：ArmorData 可注册/解锁/装备/保存读取；装备后 `PlayerArmor2D` 数值立即更新；Forge 修理生效；普通存档点不修理护甲 |
| PLAYER-INVINC-001 | 受击无敌和碰撞忽略 | 受伤后短暂无敌并避免被敌人卡住 | `PlayerInvincibilityCollision2D`, `PlayerHealth2D`, `PlayerHitKnockback2D` | Approved | Integrated | Prototype | PLAYER-RESOURCE-001 | Physics2D 碰撞矩阵全开；layer 忽略需 Playtest | M0 验证 PlayerSide/Enemy layer 忽略恢复 | M0 | Generic DoD + 受击后忽略碰撞并恢复 |
| COMBAT-DAMAGE-001 | 命中上下文 | 玩家攻击与敌人受击通过统一上下文传递 | `HitContext`, `HitResult`, `IHittable`, `IDamageable2D`, `HittableBase`, `DamageType`, `HitType` | Approved | Integrated | Stable | PLAYER-RESOURCE-001 | 旧接口仍存在兼容路径；全局 status effect 未统一 | M1 评估旧接口保留边界 | M1 | Generic DoD + melee/projectile/context 验证 |
| COMBAT-MELEE-001 | 玩家近战攻击 | 玩家可横向/向上/向下攻击并触发 recoil/下劈奖励 | `PlayerMeleeAttack2D`, `PlayerAttackDirectionResolver2D`, `PlayerAttackRecoil2D`, `PlayerMeleeAttackAnimator2D` | Approved | Integrated | Prototype | WEAPON-EQUIP-001, COMBAT-RUNTIME-001 | 前摇/后摇/取消规则未完整；hit stop/特效未做 | M1 做动作阶段和取消规则 | M1 | Generic DoD + 三方向攻击 + 下劈奖励验证 |
| COMBAT-RUNTIME-001 | 玩家战斗运行时快照 | 从当前武器、铭文、血量、法力生成最终近战状态 | `PlayerCombatRuntime2D`, `PlayerCombatCore`, `PlayerWeaponSystem` | Approved | Integrated | Prototype | WEAPON-EQUIP-001, WEAPON-INSCRIPTION-001 | Prefab 中缺该组件；Scene 中已有 | M0 同步 Player Prefab 或明确只用 Scene 实例 | M0 | Generic DoD + Prefab/Scene 都能提供 runtime |
| WEAPON-EQUIP-001 | 武器装备和图鉴 | 玩家可解锁、装备、保存武器 | `WeaponData`, `WeaponLibrary2D`, `WeaponEquipped2D`, `PlayerWeaponSystem`, `record.json` | Approved | Integrated | Prototype | SAVE-DATA-001 | `Only.asset` 属于拓展特殊组合，暂不放入当前游戏流程 | M0 确认当前 Library 不含 Only | M0 | Generic DoD + 所有应出现武器可查询/装备/保存 |
| WEAPON-DUAL-001 | Dual Wielding | 解锁后单手剑可临时作为双持有效类型 | `PlayerAbilityCore2D`, `WeaponData.DualWieldable`, `WeaponEquipped2D`, `PlayerWeaponSystem`, Weapon UI | Approved | Integrated | Prototype | PLAYER-ABILITY-001, WEAPON-EQUIP-001 | 本来就是 Dual 的武器和单手剑双持表现都需要 Playtest | M0 smoke test，M1 平衡公式 | M0/M1 | Generic DoD + ability gate + UI 切换 + 伤害/动画验证 |
| WEAPON-INSCRIPTION-001 | 武器铭文和铁匠铺 | 每把武器独立篆刻一个铭文并即时影响战斗 | `WeaponInscriptionData`, `WeaponInscriptionLibrary2D`, `WeaponInscriptionEquipped2D`, Forge UI, `record.json` | Approved | Integrated | Prototype | WEAPON-EQUIP-001, UI-FORGE-001, COMBAT-RUNTIME-001 | `Will.asset` 属于 Only/Will 特殊绑定组合，暂不放入当前游戏流程；普通铭文可自由擦除/印刻 | M0 确认当前 Library 不含 Will；M1 设计特殊绑定支持 | M0/M1 | Generic DoD + 篆刻/擦除/保存/即时生效验证 |
| SPELL-CAST-001 | 法术释放 | 玩家可按法术槽释放已写入法术 | `SpellCastSystem`, `SpellLibrary2D`, `SpellData2D`, `SpellEffectData2D`, `SpellSlotCaster2D` | Approved | Integrated | Prototype | SPELL-BOOK-001, PLAYER-RESOURCE-001 | Buff 类法术仅入口；冷却/吟唱 HUD 未做 | M1 建 buff receiver 入口 | M1 | Generic DoD + 消耗/吟唱/冷却/打断/释放验证 |
| SPELL-BOOK-001 | 法术书和法术页 UI | 玩家可在 Storage 中写入/擦除法术页 | `SpellBook2D`, `SpellPageController2D`, `SpellPageDisplayer2D`, `record.json`, `PlayerData.json` | Approved | Integrated | Prototype | UI-STORAGE-001, SAVE-DATA-001 | UI 视觉仍需打磨；页数解锁流程未做 | M0 验证 JSON 读写；M2 做能力拾取联动 | M0/M2 | Generic DoD + 写入/擦除/重复移动/保存读取 |
| SPELL-PROJECTILE-001 | 玩家投射物 | 伤害法术可生成 projectile 并命中敌人 | `DamageSpellEffectData2D`, `PlayerProjectileLauncher2D`, `PlayerProjectile2D`, `ProjectileMovement2D`, projectile prefabs | Approved | Integrated | Prototype | SPELL-CAST-001, COMBAT-DAMAGE-001 | projectile 与 enemy layer/碰撞矩阵需 Playtest | M0 projectile smoke test | M0 | Generic DoD + projectile 发射/命中/销毁验证 |
| UI-CANVAS-001 | 顶层 UI 状态管理 | ESC/R 根据当前状态打开或关闭正确 Canvas | `GameCanvasManager2D`, `PauseMenuController2D`, `KeybindSettingsController2D`, `PlayerCore2D` owner-based action lock | Approved | Integrated | Prototype | PLAYER-MOVE-001 | `PlayerCore` action lock 已改为 owner-based；仍需 Playtest Storage/Forge 与施法 lock 不互相误解锁 | M1 验证 Pause/Storage/Forge/Spell lock 组合 | M1 | Generic DoD + Pause/Storage/Forge 状态切换验证 |
| UI-STORAGE-001 | Storage 基础装备页面 | 存档点打开 Storage，管理武器、法术，未来包括护符等基础配置 | `OpenSavingLibrary2D`, `StorageCanvasController2D`, Weapon/Spell UI scripts | Approved | Integrated | Prototype | UI-CANVAS-001, WEAPON-EQUIP-001, SPELL-BOOK-001 | M0 当前可用；不是完整存档点；当前只实现武器/法术页面，护符 UI 未做 | M3 接真正存档；M2/M3 扩展护符与快照边界 | M0/M3 | Generic DoD + R/ESC 关闭 + 操作保存 |
| UI-FORGE-001 | Forge 铭文页面 | 铁匠铺打开铭文界面并修改武器铭文，长期承担高级装备管理 | `OpenForge2D`, Forge Inscription UI scripts, optional armor repair hook | Approved | Integrated | Prototype | UI-CANVAS-001, WEAPON-INSCRIPTION-001, ARMOR-EQUIP-001 | 铭文 UI 当前可用；`OpenForge2D` 已有可选护甲修理入口但需要手动绑定；Armor 管理页面、材料/花费/限制未做 | M2 增加 Armor 管理 UI；M3 接高级存档点逻辑 | M0/M2/M3 | Generic DoD + 篆刻、擦除、关闭、保存验证；Armor 修理需单独按 ARMOR-EQUIP-001 验证 |
| WORLD-INTERACT-001 | 世界交互入口 | 玩家在 trigger 区域按 R 与对象交互 | `IInteractable`, `TriggerInteractable2D`, `PlayerInteractor2D`, `OpenSavingLibrary2D`, `OpenForge2D` | Approved | Integrated | Prototype | UI-CANVAS-001 | prompt 世界文字需要不同物体 Playtest | M0 smoke test | M0 | Generic DoD + priority/prompt/R 交互验证 |
| SAVE-DATA-001 | 轻量 JSON 记录 | 装备、图鉴、能力、槽位可保存读取 | `PlayerRecordStore2D`, `PlayerDataStore2D`, `record.json`, `PlayerData.json` | Approved | Integrated | Prototype | WEAPON-EQUIP-001, ARMOR-EQUIP-001, SPELL-BOOK-001, PLAYER-ABILITY-001 | M0 当前读写可用；record 已新增 `unlockedArmorIds` / `equippedArmorId` 但 Unity Playtest 未验证；无版本号/备份/迁移策略；当前部分数据即时写入，与长期快照/回滚模型不完全一致 | M2 验证 Armor record；M3 做版本、快照与复活存档 | M0/M2/M3 | Generic DoD + 删除/生成/读取/修改保存验证 |
| SAVE-RESPAWN-001 | 存档点与复活闭环 | 死亡后回到最近存档点并丢弃未保存变化 | 当前只有 Storage 入口和数据 store | Draft | Not Started | Prototype | SAVE-DATA-001, PLAYER-RESOURCE-001 | 设计方向已确认：普通存档恢复血蓝并提交快照；死亡 discard 未保存 changes；尚未实现快照/回滚/弱存档点 | M3 设计并实现普通复活闭环 | M3 | 专属 DoD：普通存档、Boss弱存档点、死亡回滚、长期进度验证 |
| ENEMY-HIT-001 | 敌人受击、硬直、死亡 | 敌人可被玩家攻击、扣血、击退、死亡 | `EnemyHittable`, `EnemyHitReaction2D`, `EnemyDeathBehaviour2D`, enemy prefabs | Approved | Integrated | Prototype | COMBAT-DAMAGE-001 | 死亡不是直接 Destroy；普通怪主要掉钱，关键道具应走场景奖励/世界状态 | M0 smoke test；M4 做地图与怪物切片 | M0/M4 | Generic DoD + melee/projectile 命中 + 死亡关停验证 |
| ENEMY-AI-001 | 敌人索敌与追击 | 敌人发现玩家后持续追击 | `EnemyPerception2D`, `EnemyBlackboard2D`, `EnemyBrainFSM2D`, `EnemyPatrolWalker2D`, TestingGround scene enemy | Approved | Integrated | Prototype | ENEMY-HIT-001 | prefab 未同步新 AI；失去目标/返回需 Playtest | M0 同步/确认 prefab 和 TestingGround | M0 | Generic DoD + 发现/追击/返回验证 |
| ENEMY-ATTACK-001 | 敌人主动近战 | 敌人距离足够时起手并在 active 窗口伤害玩家 | `EnemyMeleeAttack2D`, `EnemyBrainFSM2D`, `EnemyContactAttack2D` | Approved | Integrated | Prototype | ENEMY-AI-001, PLAYER-RESOURCE-001 | 2026-06-28 用户确认当前 M0 操作逻辑可用；正式敌人类型和表现后续 M4 继续打磨 | M4 接地图与怪物切片时继续扩展 | M0/M4 | Generic DoD + FSM 调用 + layer 正确 + 受伤无敌避免重复伤害 |
| ENEMY-SPAWN-001 | 敌人生成器 | 按启用、间隔、受击等条件生成敌人 | `EnemySpawner2D`, TestingGround, enemy prefabs | Approved | Integrated | Prototype | ENEMY-HIT-001 | Spawner 应允许设计者手动选择 prefab；当前生成旧 `slimeSlider.prefab` 只需按测试目标确认 | M0 确认 spawner prefab 选择符合当前测试目标；M4 接地图与怪物切片 | M0/M4 | Generic DoD + 生成敌人与目标行为一致 |
| CHARM-CORE-001 | 护符系统 | 玩家装备护符获得额外增益 | 仅 `PlayerData2D.CharmSlotNum` 占位 | Draft | Not Started | Prototype | SAVE-DATA-001, UI-CANVAS-001 | 已有阳刻/阴刻容量和护符图鉴草稿；未实现、无 UI | M2 做 TestingGround 玩家系统闭环时设计并实现最小护符系统 | M2 | 设计确认后再写专属 DoD |
| UI-INVENTORY-001 | 背包 UI | 管理物品、材料、消耗品 | 尚未实现 | Future | Not Started | Prototype | SAVE-DATA-001, UI-CANVAS-001 | 未设计 | M4 前确认最小背包范围 | M4 | 设计确认后再写专属 DoD |
| UI-MAP-001 | 地图 UI | 显示探索区域和房间状态 | 尚未实现 | Future | Not Started | Prototype | WORLD-ROOM-001, UI-CANVAS-001 | 未设计 | M4 做基础地图 | M4 | 设计确认后再写专属 DoD |
| WORLD-ROOM-001 | 房间与探索区域 | 小型探索区域、门、隐藏房、Boss 房 | `TestingGround.unity`, `trial.unity`, map prefabs | Draft | Not Started | Prototype | ENEMY-AI-001, SAVE-RESPAWN-001 | 当前只是测试场景；未来至少记录 Boss 死亡、门/阻拦开放、宝箱开启，并处理普通敌人存档刷新 | M4 建探索区域切片 | M4 | 专属 DoD：房间状态/边界/重置/地图验证 |
| TEST-BASELINE-001 | 基线验证体系 | 每次大改后有可重复 smoke test | `TESTING_GUIDE.md`, manual TestingGround checks | Approved | Verified | Prototype | 所有 M0 功能 | 2026-06-28 用户确认 M0 操作符合逻辑并可用；仍无自动 PlayMode 测试 | M1/M2 补最小自动化或继续维护手动 smoke test | M0/M1 | 所有 M0 smoke test 通过并记录 |

## 更新规则

1. 新功能必须先获得 Feature ID。
2. 状态变更必须同时更新当前问题、下一步任务和目标里程碑。
3. `Verified` 必须满足 `GAME_PLAN.md` 的通用 Definition of Done。
4. 需要 Unity 手动确认的内容必须写 `Needs Unity Verification`，不能伪装成已验证。
5. 需要玩家实际操作验证的内容必须写 `Needs Playtest`。
6. 需要重新讨论规则的内容必须写 `Needs Design Confirmation`。
