# Endless Journey Current Function Reference

更新时间：2026-06-28  
用途：说明当前构建版本已经能够实际测试或基本观察到的功能。功能状态的唯一权威来源仍是 `../Planning/FEATURE_TRACKER.md`。

## 当前可测试主循环

当前主要测试场景：`Assets/Scenes/TestingGround.unity`

在 Unity Editor Play Mode 中，当前原型应围绕以下循环测试：

1. 玩家移动、跳跃、冲刺、二段跳。
2. 玩家进行横向、向上、向下近战攻击。
3. 下劈命中敌人后恢复 dash / extra jump。
4. 玩家通过 Storage 查看、选择、装备武器。
5. 玩家在解锁 Dual Wielding 后切换可双持单手剑的 effective type。
6. 玩家通过 Storage 管理法术书页，写入或擦除法术。
7. 玩家通过法术槽释放已写入法术。
8. 玩家通过 Forge 给每把武器独立篆刻铭文。
9. 铭文影响武器重量、锋利度、连击伤害、失血增伤或命中回蓝。
10. Armor 后端支持 ArmorData、ArmorLibrary、当前装备 id、record 保存和应用到 `PlayerArmor2D`。
11. Forge 入口可在手动绑定 `PlayerArmorEquipmentSystem2D` 后打开时修复当前护甲。
12. 敌人可被命中、扣血、击退、硬直和死亡。
13. 敌人可索敌并追击玩家。
14. 玩家受敌人 harm 后触发盔甲减伤、受击无敌、击退和临时忽略敌人碰撞。
15. ESC/R 根据当前 UI 状态关闭 Storage / Forge / Pause。

以上内容仍需要按 `TESTING_GUIDE.md` 执行 Playtest 后，才能在 `FEATURE_TRACKER.md` 标记为 `Verified`。

## Player

### 移动与输入

相关 Feature：

- PLAYER-MOVE-001
- PLAYER-ABILITY-001

当前功能：

- `PlayerInput2D` 统一读取移动、跳跃、冲刺、攻击、施法和法术槽位输入。
- `PlayerMovement2D` 支持横向移动、跳跃、coyote time、jump buffer、low jump、fall/apex 调整。
- `PlayerDash2D` 支持冲刺速度、持续时间、冷却、空中冲刺限制和受击打断。
- `PlayerDoubleJump2D` 支持二段跳，并可被下劈奖励重置。
- `PlayerAbilityCore2D` 从 `PlayerData.json` 读取能力解锁。

当前限制：

- 没有正式动作状态机。
- 动作取消规则仍分散在 dash、spell、melee、harm 各模块。
- Player Prefab 与 TestingGround 实例需要 M0 确认。

### 生命、法力、盔甲

相关 Feature：

- PLAYER-RESOURCE-001
- PLAYER-INVINC-001

当前功能：

- `PlayerHealth2D` 管理生命、harm、direct health loss、回血、死亡、受击无敌。
- `ReceiveHarm` 用于敌人/物理世界伤害，会经过盔甲和受击逻辑。
- `ReceiveDirectHealthLoss` 用于 ManaOut / DoT 等无来源或效果生命损失，不触发盔甲和受击无敌。
- `PlayerMana2D` 管理 mana、potential mana、自然恢复、ManaOut、主动恢复。
- `AllowNaturalRegen` 可关闭自然回蓝，但不影响主动恢复和铭文回蓝。
- `PlayerArmor2D` 管理盔甲耐久和减伤。
- `ArmorData` 定义护甲 id、显示信息、最大耐久和减伤比例。
- `ArmorLibrary2D` 管理护甲资产索引和解锁状态。
- `ArmorEquipped2D` 保存当前装备护甲 id 到 record.json。
- `PlayerArmorEquipmentSystem2D` 将当前装备护甲数值应用到 `PlayerArmor2D`。
- 最后一击盔甲耐久不足时仍完整减伤，之后进入 broken 状态。
- `PlayerInvincibilityCollision2D` 在受击无敌期间临时忽略 player/enemy layer collision。
- `OpenForge2D` 可选择在打开 Forge 时修复当前装备护甲，但需要手动绑定 `PlayerArmorEquipmentSystem2D`。

当前限制：

- 死亡后的复活/读档闭环未完成。
- 受击动画和死亡表现仍是后续工作。
- Armor Forge UI 页面尚未完成；当前只完成后端链路和可选修复入口。
- Armor 相关 Library、Equipped、EquipmentSystem、ArmorData 资产仍需 Unity Inspector 手动接线和 Playtest。

## Combat

### 命中上下文

相关 Feature：

- COMBAT-DAMAGE-001
- COMBAT-RUNTIME-001

当前功能：

- `HitContext` 携带伤害、来源、方向、命中点、damage type、hit type、weapon type、weapon weight、hit index、hit count。
- `IHittable` 返回 `HitResult`。
- `HittableBase` 提供通用受击门禁。
- `PlayerCombatRuntime2D` 聚合玩家当前武器、铭文、生命、法力和 CombatCore，生成最终近战快照。

当前限制：

- `IDamageable2D` 旧兼容接口仍存在。
- 全局 Buff / Status Effect 框架还没有统一。

### 玩家近战

相关 Feature：

- COMBAT-MELEE-001
- WEAPON-EQUIP-001

当前功能：

- `PlayerMeleeAttack2D` 支持 Forward / Up / Down 攻击。
- 近战 active window 使用指定 hitbox collider。
- 支持 target layer、自身过滤、重复命中过滤。
- 下劈命中奖励恢复 dash 和 extra jump。
- `PlayerAttackRecoil2D` 处理攻击 recoil。
- `PlayerMeleeAttackAnimator2D` 和 `PlayerMeleeAttackAnimationController2D` 处理攻击动画表现和双持双动画。

当前限制：

- 前摇、后摇、取消规则、招架、拼刀、hit stop、正式特效音效未完成。

## Weapon

相关 Feature：

- WEAPON-EQUIP-001
- WEAPON-DUAL-001
- WEAPON-INSCRIPTION-001

当前功能：

- `WeaponData` 保存 weapon id、显示名、类型、长度、锋利度、重量、图标、详情图、描述、dual wieldable。
- `WeaponLibrary2D` 管武器资产和解锁状态。
- `WeaponEquipped2D` 保存当前武器和 dual wielding mode 到 record.json。
- `PlayerWeaponSystem` 计算 attack range、attack speed、damage per hit、hit count、effective type、effective weight、effective sharpness。
- 单手剑、双刀、重武器有不同公式。
- Dual Wielding 通过 effective weapon type 实现，不直接改原始 WeaponData。
- Storage Weapon UI 支持列表、详情、装备、空格装备、dual wielding 按钮。

当前限制：

- `Only.asset` 属于拓展特殊组合武器，暂不放入当前游戏流程；当前 TestingGround Library 不注册是预期方向，仍需 Unity Inspector 验证。
- 武器获得流程和武技系统未完成。

## Spell

相关 Feature：

- SPELL-BOOK-001
- SPELL-CAST-001
- SPELL-PROJECTILE-001

当前功能：

- `SpellData2D` 保存 spell id、显示名、类型、消耗、吟唱、冷却、释放 offset 和 effects。
- `SpellLibrary2D` 管法术资产和解锁状态。
- `SpellBook2D` 管 1-5 页法术书槽位，槽位数量从 `PlayerData.json -> SpellSlotNum` 读取。
- Storage Spell UI 支持法术列表、页码、预览、写入、擦除。
- 同一个法术写入新页时会从旧页移除。
- `SpellCastSystem` 处理 ability gate、消耗、吟唱、打断、冷却、cast lock 和效果执行。
- 伤害法术主要通过 projectile 链路执行。
- 治疗法术入口已存在。
- Buff 法术入口已存在。

当前限制：

- Buff receiver 和具体 buff 规则未完成。
- 法术冷却 HUD、吟唱进度 HUD、持续效果未完成。

## Inscription / Forge

相关 Feature：

- WEAPON-INSCRIPTION-001
- UI-FORGE-001

当前功能：

- 每把武器独立拥有一个铭文槽。
- `WeaponInscriptionData` 定义铭文 id、显示名、文案、effect type、value、timeout。
- `WeaponInscriptionEquipped2D` 保存 `weaponId -> inscriptionId` 到 record.json。
- Forge UI 支持选择武器、选择铭文、Space 篆刻、Backspace/Delete 擦除。
- `WeightMultiplier`、`SharpnessMultiplier` 已影响武器 effective stats。
- `ComboDamageRamp`、`MissingHealthDamageBonus`、`ManaOnHit` 已通过 `PlayerCombatRuntime2D` 生效。

当前限制：

- `Will.asset` 属于 Only/Will 特殊绑定组合铭文，暂不放入当前游戏流程；当前 TestingGround Library 不注册是预期方向，仍需 Unity Inspector 验证。
- 一般铭文可在铁匠铺自由擦除与印刻；Only/Will 这类特殊组合需要后续支持绑定、不可卸下、不可被别的武器使用。
- 铭文材料、类型限制、冲突规则未完成。

## Enemy

相关 Feature：

- ENEMY-HIT-001
- ENEMY-AI-001
- ENEMY-ATTACK-001
- ENEMY-SPAWN-001

当前功能：

- `EnemyHittable` 接收玩家近战/projectile 命中，扣血并触发死亡。
- `EnemyHitReaction2D` 处理硬直和击退。
- 击退主要受 weapon weight / weapon type 影响。
- `EnemyDeathBehaviour2D` 可在死亡后关闭行为脚本、碰撞、渲染或 spawner。
- `EnemyContactAttack2D` 提供接触伤害。
- `EnemyPerception2D`、`EnemyBlackboard2D`、`EnemyBrainFSM2D` 支持索敌、追击、攻击、受击硬直、返回。
- `EnemyMeleeAttack2D` 支持 detection range、windup、active、recovery、cooldown。
- `EnemySpawner2D` 支持启用、间隔、手动和受击生成。

当前限制：

- TestingGround 中 `EnemyBrainFSM2D.meleeAttackModule` 静态检查为空。Needs Unity Verification。
- active melee target layer 需要确认设为 `PlayerSide`。
- active melee 与 contact damage 可以共存；contact damage 理论上一直存在，依靠玩家受伤无敌避免非预期重复伤害。
- Spawner 允许设计者手动选择 prefab；当前若生成旧 `slimeSlider.prefab`，需要确认它符合当前测试目标。

## UI / Data / World Interaction

相关 Feature：

- UI-CANVAS-001
- UI-STORAGE-001
- UI-FORGE-001
- WORLD-INTERACT-001
- SAVE-DATA-001

当前功能：

- `GameCanvasManager2D` 统一 Gameplay / Pause / Storage / Forge 状态。
- Gameplay 下 ESC 打开 Pause；其它 canvas 下 ESC 关闭当前界面。
- Storage/Forge 下 R 也可关闭当前界面。
- `TriggerInteractable2D` 提供世界 prompt 和 trigger 交互。
- `OpenSavingLibrary2D` 打开 Storage。
- `OpenForge2D` 打开 Forge。
- `PlayerRecordStore2D` 读写 record.json。
- `PlayerDataStore2D` 读写 PlayerData.json。

当前保存范围：

- spell unlock state。
- equipped spell ids。
- weapon unlock state。
- equipped weapon id。
- armor unlock state。
- equipped armor id。
- dual wielding mode。
- per-weapon inscription ids。
- unlocked ability ids。
- SpellSlotNum。
- CharmSlotNum。

尚未保存：

- 玩家位置。
- 当前生命、法力、盔甲耐久。
- 背包。
- 房间状态。
- 已击败敌人。
- 已拾取物。
- 最近存档点复活。
- 存档版本号。

## 当前一句话总结

当前项目已经拥有可扩展的 2D 动作原型骨架；最紧急的不是继续堆新系统，而是先完成 M0：把 TestingGround、Prefab、Library、Layer、Build Settings 和基础 smoke test 稳定下来。
