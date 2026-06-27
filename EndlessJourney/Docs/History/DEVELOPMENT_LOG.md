# Endless Journey Development Log

更新时间：2026-06-28  
用途：保存历史开发记录。本文不作为当前功能状态来源；当前状态请看 `../Planning/FEATURE_TRACKER.md`。

## before 2026-4-23

阶段说明：

- 这一段根据当前项目结构、早期 `basic v0.1` 内容和后续日志反推；git history 中没有早于 2026-04-23 的精确 commit 可逐日还原。

前置基础：

- 创建 Unity 2D 原型项目，完成基础目录、URP/TextMesh Pro/Input System 等 Unity 项目配置。
- 建立 `SampleScene` 与早期 `TestingGround` 测试场景。
- 准备 `Arts / Audio / Settings / TextMesh Pro` 等资源目录和基础占位素材。
- 建立 Inspector-driven 的开发方式：脚本暴露调参字段，Unity 场景/prefab 连接由开发者明确绑定。

玩家基础：

- 建立 `PlayerInput2D`、`PlayerCore2D`、`GroundCheck2D` 等基础脚本雏形。
- 完成基础横向移动与跳跃原型。
- 建立 dash 与 double jump 的模块化结构。

资源与 HUD：

- 建立生命系统初版。
- 建立法力系统初版。
- 建立 `HealthDisplayer` 与 `ManaDisplay` 的早期 HUD 链路。

战斗基础：

- 建立 `WeaponData`、`PlayerWeaponSystem`、`PlayerCombatCore` 的早期武器到战斗数值链路。
- 建立玩家近战横向攻击原型。
- 建立 `IHittable`、`IDamageable2D`、`HitContext`、`HitResult`、`HittableBase`。

敌人与场景循环：

- 建立敌人 core/base、简单巡逻、敌人可受击、接触伤害。
- 建立简单相机跟随脚本。

这一阶段奠定的规则：

- Player 与 Enemy 是当前优先级最高的核心系统。
- 移动、战斗、资源和受击都要拆成可单独理解的模块。
- 战斗数值逐步向 `CombatCore` / runtime snapshot 靠拢。
- UI、背包、地图、剧情和正式关卡先让位于核心 gameplay loop。

## 2026-04-23

- 完成并稳定玩家基础动作链：移动、跳跃、dash、double jump。
- 完成 `PlayerCore` 化整合思路。
- 完成生命/法力系统草稿并扩展：
  - Health：伤害、治疗、死亡、脱战自然回复。
  - Mana：Mana + PotentialMana、过载、枯竭、副作用与恢复优先级。
- 完成 `SpellCastSystem`：learned gate、cast time、cooldown、法力消耗。
- 完成 `WeaponData` 和 `PlayerWeaponSystem` 到 `PlayerCombatCore` 的基础链路。
- 完成 `PlayerMeleeAttack2D` 原型：
  - 前向等腰三角命中感。
  - 近战判定窗口。
  - 命中日志。
  - Debug 线框与 Game View LineRenderer。
- 完成 Hittable 架构落地：
  - `IHittable`
  - `HitContext`
  - `HitResult`
  - `HitType`
  - `HittableBase`
  - `EnemyHittable`
- 完成敌人 AI 原型骨架：
  - `EnemyPerception2D`
  - `EnemyBlackboard2D`
  - `EnemyBrainFSM2D`
  - 与 `EnemyPatrolWalker2D / EnemyContactAttack2D` 可组合。

主要记忆：

- 输入统一从 `PlayerInput2D` 读取。
- 战斗数值以 `PlayerCombatCore` 为读入口。
- 近战支持在没有可用武器但 CombatCore 有有效伤害时进行测试。
- 敌人侧建议以 `EnemyCore2D` 为模块汇聚点。
- 外部 `dotnet build` 仅作为参考，以 Unity Editor 编译为准。
- Player 扣血拆分为 `ReceiveHarm` 和 `ReceiveDirectHealthLoss`。

## 2026-04-25

根据 git 提交还原：

- 开始完善近战 recoil 与攻击方向系统。
- 新增/调整 `PlayerAttackRecoil2D`。
- 新增 `AttackDirection2D` 与 `PlayerAttackDirectionResolver2D`。
- 横向攻击 recoil 进入较稳定状态。
- 修复向上攻击引入时的 recoil 与 offset 问题。
- `PlayerMeleeAttack2D` 从单纯横向攻击扩展到更明确的方向攻击结构。
- `SimpleCameraFollow2D` 与 TestingGround 场景配置进入基础可用状态。

## 2026-04-26

根据 git 提交还原：

- 完成并测试向下攻击逻辑。
- 下劈命中后可恢复 dash / extra jump。
- 下劈 recoil 与跳跃/重力手感做过平衡。
- 修正向上攻击 offset。
- `PlayerDash2D / PlayerDoubleJump2D / PlayerMovement2D` 与近战命中奖励开始联动。

## 2026-04-27

根据 git 提交还原：

- 敌人受击反馈进入第一版。
- 新增/整理 `EnemyHitReaction2D`。
- 受击后可产生 stun 与 knockback。
- 敌人 AI / Blackboard / FSM / Patrol 与受击反馈开始组合。
- 玩家攻击 recoil 与敌人 hit reaction 做过一次整体平衡。

## 2026-04-29

根据 git 提交还原：

- 基础 gameplay 场景与 prefab 进入可玩状态。
- 新增 player / enemy / map / camera / canvas 等 prefab。
- TestingGround 与 trial 场景有基础布置。
- 新增或整理敌人基础模块：
  - `EnemyCore2D`
  - `EnemyDeathBehaviour2D`
  - `EnemyStabler2D`
  - floating enemy 原型
- 完成暂停菜单与按键设置初版：
  - `PauseMenuController2D`
  - `KeybindSettingsController2D`
  - `PlayerInput2D` 与 keybind 逻辑联动。

## 2026-04-30

根据 git 提交还原：

- 新增交互与拾取体系基础：
  - `IInteractable`
  - `IPickable`
  - `TriggerInteractable2D`
  - `ReadInteractable2D`
  - `PlayerInteractor2D`
- 新增能力拾取物：
  - `AllowDashPickup2D`
  - `AllowDoubleJumpPickup2D`
  - `AllowSpellCastPickup2D`
  - `PlayerAbilityCore2D`
- 这一天奠定后续统一 R 交互和能力解锁的基础。

## 2026-05-02

根据 git 提交还原：

- 法术系统进入基础版本：
  - `SpellData2D`
  - `SpellEffectData2D`
  - `DamageSpellEffectData2D`
  - `HealSpellEffectData2D`
  - `BuffSpellEffectData2D`
  - `SpellCastSystem`
  - `SpellBook2D`
  - `SpellSlotCaster2D`
  - `SpellLibrary2D`
- 早期 spell 独立 record store 后续统一到 `PlayerRecordStore2D`。
- Projectile 系统进入基础版本：
  - `PlayerProjectile2D`
  - `PlayerProjectileLauncher2D`
  - `ProjectileMovement2D`
- 新增 `record.json` 与初始 spell / weapon asset。
- melee、projectile、spell 目录开始拆分。

## 2026-05-03

根据 git 提交还原：

- 完成存档点逻辑并测试：
  - `OpenSavingLibrary2D`
  - interactable 区域与打开 storage UI 的链路建立。
- Player 战斗代码目录整理：
  - `Combat-Core`
  - `Combat-Melee`
  - `Combat-Spell`
  - `Libraries`
- 新增 `WeaponEquipped2D` 与 `WeaponLibrary2D`。
- 新增 `PlayerRecordStore2D`。
- 新增 `GameCanvasManager2D` 与 `StorageCanvasController2D`。
- Storage UI 初步支持 Weapon / Spell 页面。

## 2026-05-04

根据 git 提交还原：

- Weapon Page UI 进入可用版本：
  - `WeaponPageController2D`
  - `WeaponPageDisplayer2D`
  - `WeaponPageRow2D`
  - `WeaponListPrefab`
- 武器列表、选中态、装备态、锁定态与 weapon detail 显示开始成型。
- `WeaponData` 与 weapon asset 补充 UI 展示需要的数据。

## 2026-05-05

根据 git 提交还原：

- Spell UI 完成基础版本并多次迭代：
  - `SpellPageController2D`
  - `SpellPageDisplayer2D`
  - `SpellPageSlotButton2D`
  - `SpellPageSpellNameRow2D`
- Spell 页面形成法术书页 + 法术名称列表 + 详情预览 + 写入/擦除结构。
- 新增更多法术与 projectile：
  - geowave
  - pyroblast
  - shadow lance
  - recovery / heal 类 spell
- 新增 `PlayerDataStore2D` 与 `PlayerData.json`。
- Armor 进入第一版：
  - `PlayerArmor2D`
  - `ArmorDisplayer`
  - `PlayerHealth2D` 与 armor 结算联动。
- UI 目录整理为 `Storage/Spell` 与 `Storage/Weapon`。

## 2026-05-07

根据 git 提交还原：

- 玩家攻击动画进入第一版：
  - `PlayerMeleeAttackAnimator2D`
  - `PlayerMeleeAttack2D` 发出攻击方向事件供动画播放。
- 修复攻击动画导致的攻击碰撞判定异常。
- 新增多把武器 asset：
  - Elder Wood
  - Lyka'sWill
  - Only
  - RoyalMark

## 2026-05-10

根据 git 提交还原：

- 修复 player attack hitbox 过度影响其它触发区域的问题：
  - `TriggerInteractable2D`
  - `AbilityPickupItem2D`
  - `EnemyContactAttack2D`
  - `PlayerMeleeAttackAnimator2D`
- 新增向上攻击动画素材与 `upSlash.anim`。
- `PlayerMeleeAttackAnimator2D` 扩展 Up / Down 动画播放和方向处理。

## 2026-05-11

根据 git 提交还原：

- 完成 dual wielding 能力与装备逻辑：
  - `PlayerAbilityCore2D`
  - `WeaponData`
  - `WeaponEquipped2D`
  - `PlayerWeaponSystem`
  - weapon UI。
- 新增 dual attack animation 控制：
  - `PlayerMeleeAttackAnimationController2D`
  - 支持双持时主/副攻击动画播放。
- 新增 `EnemySpawner2D` 与 boss / enemy 生成测试相关内容。
- TestingGround 场景有一次较大整合更新。

## 2026-05-13

阶段性开发整理：

- 整理 Player 战斗相关目录：
  - `Player/Combat-Core`
  - `Player/Combat-Melee`
  - `Player/Combat-Spell`
  - `Player/Libraries`
- 完成存档点与 UI 状态管理基础：
  - `TriggerInteractable2D` 增加世界提示文字。
  - `OpenSavingLibrary2D` 通过 `GameCanvasManager2D` 打开仓库/存档 UI。
  - ESC/R 关闭当前 gameplay canvas。
- 完成 Storage UI 初版：
  - `StorageCanvasController2D`
  - `WeaponPageController2D + WeaponPageDisplayer2D`
  - `SpellPageController2D + SpellPageDisplayer2D`
  - `Assets/Scripts/UI/UI_controller.md`
- 完成玩家进度数据基础：
  - `PlayerData.json`
  - `PlayerAbilityCore2D` 从 PlayerData 读取能力。
- 完成盔甲系统。
- 扩展武器系统：
  - detail image
  - description
  - dual wieldable
  - effective weapon type。
- 攻击动画进入第一版。
- 扩展战斗命中上下文：
  - `PlayerCombatRuntime2D`
  - `HitContext` 增加 damage type、weapon type、weapon weight、hit index、hit count。
  - `EnemyHitReaction2D` 击退改为主要受 weapon weight / weapon type 影响。
- 新增 `EnemySpawner2D` 原型。

## 2026-05-21

今日主要成果：

- 完成武器铭文系统第一版：
  - `WeaponInscriptionData`
  - `WeaponInscriptionLibrary2D`
  - WeightMultiplier / SharpnessMultiplier 参与武器 weight / sharpness 计算。
- 铭文存档从全局一个铭文改为每把武器独立铭文。
- 完成铁匠铺铭文 UI 第一版：
  - `GameCanvasState2D.Forge`
  - `OpenForge2D`
  - `WeaponInscriptionPageController2D`
  - `WeaponInscriptionPageDisplayer2D`
  - weapon row / inscription row prefab 脚本。
- Forge 页面支持选武器、选铭文、Space 篆刻、Backspace/Delete 擦除、R/ESC 关闭。
- 修正铭文即时生效链路：
  - `PlayerWeaponSystem` 监听 `OnWeaponInscriptionChanged`
  - 当前装备武器变化后立即 `RecalculateCombatSnapshot`
- 修正 `WeaponInscriptionLibrary2D` 初始化顺序风险。
- `EnemyHittable` 增加可开关受击伤害 log。
- 完成剩余动态铭文效果：
  - `ComboDamageRamp`
  - `MissingHealthDamageBonus`
  - `ManaOnHit`
- `PlayerMana2D` 增加 `Allow Natural Regen`。
- `EnemyDeathBehaviour2D` 新增 `behavioursToDisable`。
- 玩家受击无敌逻辑增强：
  - `PlayerHealth2D.OnInvincibilityChanged`
  - `PlayerInvincibilityCollision2D`
- 当日验证：
  - `dotnet build .\Assembly-CSharp.csproj` 通过。
  - 仅剩 Unity Inspector 序列化字段相关 warning。

## 2026-06-27

阶段性整理：

- 敌人索敌与追击进入可测版本：
  - `EnemyPerception2D`
  - `EnemyBlackboard2D`
  - `EnemyBrainFSM2D`
- 新增敌人主动近战攻击原型：
  - `EnemyMeleeAttack2D`
  - 支持 attack detection range、windup、active、recovery、cooldown。
  - 起手后即使 Player 离开范围，攻击流程也会继续。
  - active 窗口通过 hitbox 扫描并调用 Player harm 入口。
- 完成一次代码/文档/Unity YAML 审查：
  - 新增 `problem.md`
  - 确认 `dotnet build .\Assembly-CSharp.csproj` 通过。
  - 确认没有 scene/prefab missing script。
  - 主要风险集中在 scene/prefab 绑定不一致、enemy melee 未接入 FSM、Build Settings 仍指向 SampleScene。
- 更新文档：
  - `Assets/Scripts/Player/Properties/Specification.md`
  - README
  - `CHECKLIST.md`
  - `future.md`
  - `class map.md`
  - `FUNCTION.MD`
- 建立长期规划体系：
  - `Docs/Planning/GAME_PLAN.md`
  - `Docs/Planning/FEATURE_TRACKER.md`
  - `Docs/Planning/CURRENT_MILESTONE.md`
  - `Docs/Planning/DECISION_LOG.md`
  - `Docs/Planning/RISK_REGISTER.md`
  - `Docs/Reference/FUNCTION.md`
  - `Docs/Reference/CLASS_MAP.md`
  - `Docs/Reference/TESTING_GUIDE.md`
  - `Docs/History/DEVELOPMENT_LOG.md`

## 2026-06-28

规划与验证：

- 将开发路线调整为先在 `TestingGround` 中完成单场景浓缩功能场：
  - M1：Player Core 与战斗核心稳定。
  - M2：TestingGround Player Systems Sandbox。
  - M3：TestingGround Save / Storage / Forge 闭环。
  - M4：地图与怪物切片。
- 用户确认当前 M0 相关操作逻辑符合预期，且都能够使用。
- 将 M0 标记为当前 TestingGround 基线 User Playtest Passed。
- 同步更新：
  - `CURRENT_MILESTONE.md`
  - `FEATURE_TRACKER.md`
  - `RISK_REGISTER.md`
  - `GAME_PLAN.md`
- `PlayerCore2D` action lock 从单 bool 改为 owner-based lock：
  - `SpellCastSystem` 释放法术锁时不会误解开 Storage / Forge 等 UI 锁。
  - `GameCanvasManager2D` 使用自身作为 action lock owner。
- 完成 Armor 装备后端第一版：
  - `ArmorData`
  - `ArmorLibrary2D`
  - `ArmorEquipped2D`
  - `PlayerArmorEquipmentSystem2D`
  - `PlayerRecordStore2D` 新增 `unlockedArmorIds` / `equippedArmorId`
  - `PlayerArmor2D` 新增 `ApplyArmorStats`
  - `OpenForge2D` 新增可选打开时修理护甲入口。
- 当前 Armor 状态：
  - 后端代码和 record 字段已完成。
  - ArmorData asset、Unity Inspector 绑定、Forge Armor UI 和 Playtest 尚未完成。
- 当日验证：
  - `dotnet build .\Assembly-CSharp.csproj` 通过。
  - 最新一次构建为 0 warning / 0 error。

## Git History Snapshot

可用 git 记录最早从 2026-04-25 开始：

```text
2026-04-25 First push / basic v0.1 / camera / recoil / attack offset
2026-04-26 up/down attack and down hit reward
2026-04-27 enemy hit stun and knockback
2026-04-29 gameplay implementation, keybinding/menu, floating enemy
2026-04-30 multiple functions / interaction / pickup foundation
2026-05-02 spell casting logic
2026-05-03 saving point and storage UI stream
2026-05-04 weapon page UI
2026-05-05 spell UI and player data json
2026-05-07 player attack animation
2026-05-10 attack hitbox fixes and up/down animation
2026-05-11 dual wielding, dual animation, boss/spawner tests
2026-05-13 player runtime and hit enemy chain rebuild
2026-05-21 inscription UI/effects, enemy death, player got hit behavior
```
